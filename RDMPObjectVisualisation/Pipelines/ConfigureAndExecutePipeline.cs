﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using CatalogueLibrary.Data.Pipelines;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataFlowPipeline.Events;
using CatalogueLibrary.DataFlowPipeline.Requirements;
using CatalogueLibrary.Repositories;
using HIC.Logging.Listeners;
using RDMPObjectVisualisation.DataObjects;
using ReusableLibraryCode.Progress;
using ReusableUIComponents;
using ReusableUIComponents.SingleControlForms;

namespace RDMPObjectVisualisation.Pipelines
{
    /// <summary>
    /// Reusable component shown by the RDMP whenever it wants you to select a pipeline to achieve a task (See 'A brief overview of what a pipeline is' is UserManual.docx).  The task will
    /// be clearly described at the top of the form, this might be 'loading a flat file into the database to create a new cohort' (the actual description will be more verbose and clear 
    /// though).
    /// 
    /// You should read the task description and select an appropriate pipeline (which will appear in the pipeline diagram along with the input objects this window was launched with).  If
    /// you don't have a pipeline yet you can create a new one (See ConfigurePipelineUI).
    /// 
    /// Input objects are the objects that are provided to accomplish the task (for example a file you are trying to load).  You can usually double click input objects to learn more about
    /// them (e.g. open a file, view a cohort etc).  
    /// 
    /// This may seem like a complicated approach to user interface design but it allows for maximum plugin architecture and freedom to build your own business practices into routine tasks
    /// like cohort committing, data extraction etc.
    /// 
    /// </summary>
    public partial class ConfigureAndExecutePipeline : UserControl
    {
        DataObjectVisualisationFactory visualisationFactory = new DataObjectVisualisationFactory();
        private PipelineSelectionUI<DataTable> _pipelineSelectionUI;
        private PipelineDiagram<DataTable> pipelineDiagram1;

        public event PipelineEngineEventHandler PipelineExecutionFinishedsuccessfully;

        private ForkDataLoadEventListener fork = null;

        public ConfigureAndExecutePipeline()
        {
            InitializeComponent();
            InitializationObjects = new Dictionary<object,Control>();

            pipelineDiagram1  = new PipelineDiagram<DataTable>();
            
            pipelineDiagram1.Dock = DockStyle.Fill;
            panel_pipelineDiagram1.Controls.Add(pipelineDiagram1);

            fork = new ForkDataLoadEventListener(progressUI1);
        }

        [Description("The task you are trying to achieve with this pipeline execution (should tell the user about the context and allowable components etc)")]
        public string TaskDescription {
            get { return lblTask.Text; }
            set { lblTask.Text = value; }
        }

        private Dictionary<object, Control> InitializationObjects { get; set; }
        


        public void AddInitializationObject(object o)
        {
            var visualization = visualisationFactory.Create(o);
            InitializationObjects.Add(o,visualization);
            flpInputObjects.Controls.Add(visualization);
            
        }

        private bool _pipelineOptionsSet = false;

        private DataFlowPipelineContext<DataTable> _context;
        private IDataFlowDestination<DataTable> _destinationIfExists;
        private IDataFlowSource<DataTable> _sourceIfExists;
        
        public DataFlowPipelineEngineFactory<DataTable> PipelineFactory { get; private set; }
        
        public void SetPipelineOptions(IDataFlowSource<DataTable> sourceIfExists, IDataFlowDestination<DataTable> destinationIfExists, DataFlowPipelineContext<DataTable> context, CatalogueRepository repository)
        {
            if (_pipelineOptionsSet)
                throw new Exception("CreateDatabase SetPipelineOptions has already been called, it should only be called once per instance lifetime");

            _context = context;
            _sourceIfExists = sourceIfExists;
            _destinationIfExists = destinationIfExists;
            _pipelineOptionsSet = true;

            _pipelineSelectionUI = new PipelineSelectionUI<DataTable>(sourceIfExists, destinationIfExists, repository)
            {
                Context = context,
                Dock = DockStyle.Fill
            };
            _pipelineSelectionUI.PipelineChanged += _pipelineSelectionUI_PipelineChanged;
            _pipelineSelectionUI.PipelineDeleted += () => pipelineDiagram1.Clear();

            pPipelineSelection.Controls.Add(_pipelineSelectionUI);
            
            //setup factory
            PipelineFactory = new DataFlowPipelineEngineFactory<DataTable>(repository.MEF, _context)
            {
                ExplicitSource = _sourceIfExists,
                ExplicitDestination = _destinationIfExists
            };

            _pipelineOptionsSet = true;

            RefreshDiagram();
            
            lblContext.Text = _context.GetHumanReadableDescription();
        }

        void _pipelineSelectionUI_PipelineChanged(object sender, EventArgs e)
        {
            RefreshDiagram();

            //repaint entire control so that the arrows are rendered correctly
            Invalidate();
            Refresh();
        }

        
        private void ConfigureAndExecutePipeline_Load(object sender, EventArgs e)
        {
            RefreshDiagram();
        }

        private void RefreshDiagram()
        {
            //not ready to refresh diagram yet
            if (!_pipelineOptionsSet)
                return;

            pipelineDiagram1.SetTo(
                PipelineFactory, //the factory that knows about fixed sources/destinations and context
                _pipelineSelectionUI.Pipeline, //the possibly invalid pipeline configuration the user has chosen
                InitializationObjects.Keys.ToArray());//objects that will satisfy IPipelineRequirement e.g. cohort/file 
            
            _pipelineSelectionUI.InitializationObjectsForPreviewPipeline.Clear();
            _pipelineSelectionUI.InitializationObjectsForPreviewPipeline.AddRange(InitializationObjects.Keys);
        }


        private CancellationTokenSource _cancel;

        private void btnExecute_Click(object sender, EventArgs e)
        {
            var pipeline = CreateAndInitializePipeline();

            //if it is already executing
            if (btnExecute.Text == "Stop")
            {
                _cancel.Cancel();//set the cancellation token
                return;
            }

            btnExecute.Text = "Stop";

            if(pipeline != null)
            {
                _cancel = new CancellationTokenSource();

                //clear any old results
                progressUI1.Clear();
                tabControl2.SelectTab(tpExecute);
                

                //start a new thread
                Thread threadExecute = new Thread(() =>
                {
                    try
                    {
                        //execute the pipeline using the cancellation token
                        pipeline.ExecutePipeline(new GracefulCancellationToken(_cancel.Token, _cancel.Token));

                        //if it successfully got here then Thread has run the engine to completion successfully
                        if (PipelineExecutionFinishedsuccessfully != null)
                            Invoke(new MethodInvoker(() => //switch to UI thread
                                PipelineExecutionFinishedsuccessfully(this, new PipelineEngineEventArgs(pipeline))
                                //Fire completion success event
                                ));
                    }
                    catch (Exception exception) //Thread crashed during pipeline execution
                    {
                        //notify the progress UI
                        fork.OnNotify(this,
                            new NotifyEventArgs(ProgressEventType.Error, "Pipeline execution failed", exception));
                        
                        if (IsHandleCreated && !IsDisposed)
                            //Switch to UI thread 
                            Invoke(new MethodInvoker(() =>
                            {
                                btnExecute.Text = "Execute";//make it so user can execute again
                            }));
                    }
                });

                threadExecute.Start();
            }
           
        }

        
        private void btnPreviewSource_Click(object sender, EventArgs e)
        {
            var pipeline = CreateAndInitializePipeline();
            
            if (pipeline != null)
                try
                {
                    DataTableViewer dtv = new DataTableViewer(((IDataFlowSource<DataTable>) pipeline.SourceObject).TryGetPreview(),"Preview");
                    SingleControlForm.ShowDialog(dtv);
                }
                catch (Exception exception)
                {
                    ExceptionViewer.Show("Preview Generation Failed",exception);
                }
        }

        private IDataFlowPipelineEngine CreateAndInitializePipeline()
        {
            progressUI1.Clear();

            IDataFlowPipelineEngine pipeline = null;
            try
            {
                pipeline = PipelineFactory.Create(_pipelineSelectionUI.Pipeline, fork);
            }
            catch (Exception exception)
            {
                fork.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Could not instantiate pipeline", exception));
                return null;
            }


            try
            {
                pipeline.Initialize(InitializationObjects.Keys.ToArray());
            }
            catch (Exception exception)
            {
                fork.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Failed to Initialize pipeline", exception));
                return null;
            }

            return pipeline;
        }

        private void tabControl2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void tpConfigure_Click(object sender, EventArgs e)
        {

        }

        private void tpConfigure_Paint(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (var kvp in pipelineDiagram1.GetAnchorPointsInScreenSpace())
            {
                Control originVisualization = InitializationObjects[kvp.Key];
                
                Point screenPointForOrigin = flpInputObjects.PointToScreen(new Point(originVisualization.Location.X + (originVisualization.Width/2),originVisualization.Bottom));
                Point localPointForOrigin = tpConfigure.PointToClient(screenPointForOrigin);
                
                foreach (Point screenPointDestination in kvp.Value)
                {
                    Point destination = tpConfigure.PointToClient(screenPointDestination);
                    
                    AdjustableArrowCap bigArrow = new AdjustableArrowCap(5, 5);
                    Pen pen = new Pen(Color.Black, 2);
                    pen.CustomEndCap = bigArrow;

                    g.DrawLine(pen, localPointForOrigin, destination);        
                }
            }
        }

        public void CancelIfRunning()
        {
            if(_cancel != null && !_cancel.IsCancellationRequested)
                _cancel.Cancel();
        }

        public void SetAdditionalProgressListener(IDataLoadEventListener listener)
        {
            fork = new ForkDataLoadEventListener(progressUI1,listener);
        }
    }
}