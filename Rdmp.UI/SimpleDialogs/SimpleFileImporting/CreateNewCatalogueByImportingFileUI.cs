// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FAnsi.Discovery;
using Rdmp.Core.Curation;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.DataLoad.Engine.Pipeline;
using Rdmp.Core.DataLoad.Engine.Pipeline.Destinations;
using Rdmp.Core.DataLoad.Modules.DataFlowSources;
using Rdmp.UI.CommandExecution.AtomicCommands;
using Rdmp.UI.Icons.IconProvision;
using Rdmp.UI.ItemActivation;
using Rdmp.UI.Refreshing;
using Rdmp.UI.SimpleDialogs.ForwardEngineering;
using Rdmp.UI.TestsAndSetup.ServicePropogation;
using Rdmp.UI.Tutorials;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using ReusableUIComponents;
using ReusableUIComponents.SingleControlForms;
using ReusableUIComponents.TransparentHelpSystem;

namespace Rdmp.UI.SimpleDialogs.SimpleFileImporting
{
    /// <summary>
    /// Allows you to import a flat file into your database with appropriate column data types based on the values read from the file.  This data table will then be referenced by an RDMP
    /// Catalogue which can be used to interact with it through RDMP.  
    /// </summary>
    public partial class CreateNewCatalogueByImportingFileUI : RDMPForm
    {
        private readonly ExecuteCommandCreateNewCatalogueByImportingFile _command;

        private FileInfo _selectedFile;
        private DataFlowPipelineContext<DataTable> _context;

        public HelpWorkflow HelpWorkflow { get; set; }

        public CreateNewCatalogueByImportingFileUI(IActivateItems activator, ExecuteCommandCreateNewCatalogueByImportingFile command):base(activator)
        {
            _command = command;
            InitializeComponent();

            pbFile.Image = activator.CoreIconProvider.GetImage(RDMPConcept.File);
            serverDatabaseTableSelector1.HideTableComponents();
            serverDatabaseTableSelector1.SelectionChanged += serverDatabaseTableSelector1_SelectionChanged;
            SetupState(State.SelectFile);
            
            if (command.File != null)
                SelectFile(command.File);

            pbHelp.Image = FamFamFamIcons.help;

            BuildHelpFlow();
        }

        private void BuildHelpFlow()
        {
            var tracker = new TutorialTracker(Activator);

            HelpWorkflow = new HelpWorkflow(this, _command, tracker);

            //////Normal work flow
            var root = new HelpStage(gbPickFile, "Choose the file you want to import here.\r\n" +
                                                 "\r\n" +
                                                 "Click on the red icon to disable this help.");
            var stage2 = new HelpStage(gbPickDatabase, "Select the database to use for importing data.\r\n" +
                                                       "Username and Password are optional; if not set, the connection will be attempted using your windows user");
            var stage3 = new HelpStage(gbPickPipeline, "Select the pipeline to execute in order to transfer the data from the files into the DB.\r\n" +
                                                       "If you are not sure, ask the admin which one to use or click 'Advanced' to go into the advanced pipeline UI.");
            var stage4 = new HelpStage(gbExecute, "Click Preview to peek at what data is in the selected file.\r\n" +
                                                  "Click Execute to run the process and import your file.");

            root.SetOption(">>", stage2);
            stage2.SetOption(">>", stage3);
            stage3.SetOption(">>", stage4);
            stage4.SetOption("|<<", root);
            //stage4.SetOption("next...", stage2);
            
            HelpWorkflow.RootStage = root;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Comma Separated Values|*.csv|Excel File|*.xls*|All Files (Advanced UI Only)|*.*";
            DialogResult result = ofd.ShowDialog();

            if (result == DialogResult.OK)
                SelectFile(new FileInfo(ofd.FileName));
        }

        private void SelectFile(FileInfo fileName)
        {
            _selectedFile = fileName;
            SetupState(State.FileSelected);

        }

        private void btnClearFile_Click(object sender, EventArgs e)
        {
            SetupState(State.SelectFile);

            btnConfirmDatabase.Enabled = serverDatabaseTableSelector1.GetDiscoveredDatabase() != null;
        }

        private void SetupState(State state)
        {
            switch (state)
            {
                case State.SelectFile:
                    
                    //turn things off
                    pbFile.Visible = false;
                    lblFile.Visible = false;
                    btnClearFile.Visible = false;
                    ragSmileyFile.Visible = false;
                    ddPipeline.DataSource = null;
                    gbPickPipeline.Enabled = false;
                    gbExecute.Enabled = false;
                    gbPickDatabase.Enabled = false;
                    btnConfirmDatabase.Enabled = false;

                    btnAdvanced.Enabled = false;
                    
                    _selectedFile = null;

                    //turn things on
                    btnBrowse.Visible = true;
                    
                    break;
                case State.FileSelected:

                    //turn things off
                    btnBrowse.Visible = false;
                    gbExecute.Enabled = false;

                    //turn things on
                    pbFile.Visible = true;
                    btnAdvanced.Enabled = false;
                    gbPickDatabase.Enabled = true;

                    //text of the file they selected
                    lblFile.Text = _selectedFile.Name;
                    lblFile.Left = pbFile.Right + 2;
                    lblFile.Visible = true;

                    ragSmileyFile.Visible = true;
                    ragSmileyFile.Left = lblFile.Right + 2;

                    btnClearFile.Left = ragSmileyFile.Right + 2;
                    btnClearFile.Visible = true;

                    IdentifyCompatiblePipelines();

                    IdentifyCompatibleServers();

                    break;
                case State.DatabaseSelected:
                    //turn things off

                    //turn things on
                    gbExecute.Enabled = true;
                    btnAdvanced.Enabled = true;
                    gbPickDatabase.Enabled = true; //user still might want to change his mind about targets
                    btnConfirmDatabase.Enabled = false;

                    break;
                default:
                    throw new ArgumentOutOfRangeException("state");
            }
        }

        private void IdentifyCompatibleServers()
        {
            var servers = Activator.CoreChildProvider.AllServers;

            if (servers.Length == 1)
            {
                var s = servers.Single();


                var uniqueDatabaseNames =
                    Activator.CoreChildProvider.AllTableInfos.Select(t => t.GetDatabaseRuntimeName())
                        .Distinct()
                        .ToArray();

                if (uniqueDatabaseNames.Length == 1)
                {
                    serverDatabaseTableSelector1.SetExplicitDatabase(s.ServerName, uniqueDatabaseNames[0]);
                    SetupState(State.DatabaseSelected);
                }
                else
                    serverDatabaseTableSelector1.SetExplicitServer(s.ServerName);
            }
            else if(servers.Length > 1)
            {
                serverDatabaseTableSelector1.SetDefaultServers(
                    servers.Select(s=>s.ServerName).ToArray()
                    );
            }
        }

        void serverDatabaseTableSelector1_SelectionChanged()
        {
            btnConfirmDatabase.Enabled = serverDatabaseTableSelector1.GetDiscoveredDatabase() != null;
            btnAdvanced.Enabled = btnConfirmDatabase.Enabled;
        }

        private void IdentifyCompatiblePipelines()
        {
            gbPickPipeline.Enabled = true;
            ragSmileyFile.Reset();

            _context = new DataFlowPipelineContextFactory<DataTable>().Create(PipelineUsage.LoadsSingleFlatFile);
            _context.MustHaveDestination = typeof(DataTableUploadDestination);

            if (_selectedFile.Extension == ".csv")
                _context.MustHaveSource = typeof (DelimitedFlatFileDataFlowSource);

            if(_selectedFile.Extension.StartsWith(".xls"))
                _context.MustHaveSource = typeof(ExcelDataFlowSource);

            var compatiblePipelines = Activator.RepositoryLocator.CatalogueRepository.GetAllObjects<Pipeline>().Where(_context.IsAllowable).ToArray();

            if (compatiblePipelines.Length == 0)
            {
                ragSmileyFile.OnCheckPerformed(new CheckEventArgs("No Pipelines are compatible with the selected file",CheckResult.Fail));
                return;
            }

            ddPipeline.DataSource = compatiblePipelines;
            ddPipeline.SelectedItem = compatiblePipelines.First();
        }

        private enum State
        {
            SelectFile,
            FileSelected,
            DatabaseSelected
        }

        private void ddPipeline_SelectedIndexChanged(object sender, EventArgs e)
        {

            DataFlowPipelineEngineFactory factory = GetFactory();

            var p = ddPipeline.SelectedItem as Pipeline;

            if(p == null)
                return;
            try
            {
                var source = factory.CreateSourceIfExists(p);
                ((IPipelineRequirement<FlatFileToLoad>)source).PreInitialize(new FlatFileToLoad(_selectedFile),new FromCheckNotifierToDataLoadEventListener(ragSmileyFile));
                ((ICheckable) source).Check(ragSmileyFile);
            }
            catch (Exception exception)
            {
                ragSmileyFile.Fatal(exception);
            }
        }


        private void btnAdvanced_Click(object sender, EventArgs e)
        {
            ToggleAdvanced();
        }

        private Project _projectSpecific;

        private void ToggleAdvanced()
        {
            var db = serverDatabaseTableSelector1.GetDiscoveredDatabase();

            if (db == null)
                return;

            //flip it
            var advanced = new CreateNewCatalogueByImportingFileUI_Advanced(Activator, db, _selectedFile, true, _projectSpecific);
            var form = new SingleControlForm(advanced);
            form.Show();
        }

        private void btnConfirmDatabase_Click(object sender, EventArgs e)
        {
            var db = serverDatabaseTableSelector1.GetDiscoveredDatabase();

            if (db == null)
                MessageBox.Show("You must select a Database");
            else
            if(db.Exists())
                SetupState(State.DatabaseSelected);
            else
            {
                if(Activator.YesNo("Create Database '" + db.GetRuntimeName() +"'","Create Database"))
                {
                    db.Server.CreateDatabase(db.GetRuntimeName());
                    SetupState(State.DatabaseSelected);
                }
            }
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            var p = ddPipeline.SelectedItem as Pipeline;

            if (p == null)
            {
                MessageBox.Show("No Pipeline Selected");
                return;
            }

            var source = (IDataFlowSource<DataTable>)GetFactory().CreateSourceIfExists(p);

            ((IPipelineRequirement<FlatFileToLoad>)source).PreInitialize(new FlatFileToLoad(_selectedFile), new FromCheckNotifierToDataLoadEventListener(ragSmileyFile));
            
            Cursor.Current = Cursors.WaitCursor;
            var preview = source.TryGetPreview();
            Cursor.Current = Cursors.Default;

            if(preview != null)
            {
                DataTableViewerUI dtv = new DataTableViewerUI(preview,"Preview");
                SingleControlForm.ShowDialog(dtv);
            }
        }

        private UploadFileUseCase GetUseCase()
        {
            return new UploadFileUseCase(_selectedFile, serverDatabaseTableSelector1.GetDiscoveredDatabase());
        }

        private DataFlowPipelineEngineFactory GetFactory()
        {
            return new DataFlowPipelineEngineFactory(GetUseCase(), Activator.RepositoryLocator.CatalogueRepository.MEF);
        }


        private void btnExecute_Click(object sender, EventArgs e)
        {
            var p = ddPipeline.SelectedItem as Pipeline;

            if (p == null)
            {
                MessageBox.Show("No Pipeline Selected");
                return;
            }

            ragSmileyExecute.Reset();
            try
            {
                var db = serverDatabaseTableSelector1.GetDiscoveredDatabase();
                var engine = GetFactory().Create(p, new FromCheckNotifierToDataLoadEventListener(ragSmileyExecute));
                engine.Initialize(new FlatFileToLoad(_selectedFile), db);

                Cursor.Current = Cursors.WaitCursor;

                engine.ExecutePipeline(new GracefulCancellationToken());

                var dest = (DataTableUploadDestination) engine.DestinationObject;

                Cursor.Current = Cursors.Default;

                ForwardEngineer(db.ExpectTable(dest.TargetTableName));


            }
            catch (Exception exception)
            {
                ragSmileyExecute.Fatal(exception);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void ForwardEngineer(DiscoveredTable targetTableName)
        {
            var extractionPicker = new ConfigureCatalogueExtractabilityUI(Activator, new TableInfoImporter(Activator.RepositoryLocator.CatalogueRepository, targetTableName), "File '" + _selectedFile.FullName + "'", _projectSpecific);
            extractionPicker.ShowDialog();

            var catalogue = extractionPicker.CatalogueCreatedIfAny;
            if (catalogue != null)
            {
                Activator.RefreshBus.Publish(this, new RefreshObjectEventArgs(catalogue));
            
                MessageBox.Show("Successfully imported new Dataset '" + catalogue + "'." +
                                "\r\n" +
                                "The edit functionality will now open.");

                Activator.WindowArranger.SetupEditCatalogue(this, catalogue);
                
            }
            if (cbAutoClose.Checked)
                this.Close();
            else
                MessageBox.Show("Creation completed successfully, close the Form when you are finished reviewing the output");
        }

        private void pbHelp_Click(object sender, EventArgs e)
        {
            HelpWorkflow.Start(force: true);
        }

        public void SetProjectSpecific(Project project)
        {
            _projectSpecific = project;
        }
    }
}
