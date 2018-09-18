using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using BrightIdeasSoftware;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Aggregation;
using CatalogueLibrary.Data.PerformanceImprovement;
using CatalogueLibrary.Nodes;
using CatalogueManager.Collections.Providers;
using CatalogueManager.Collections.Providers.Filtering;
using CatalogueManager.CommandExecution;
using CatalogueManager.CommandExecution.AtomicCommands;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using CatalogueManager.Menus;
using CatalogueManager.Refreshing;
using MapsDirectlyToDatabaseTable;
using CatalogueManager.Copying;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.CommandExecution.AtomicCommands;
using ReusableLibraryCode.Settings;
using ReusableUIComponents.CommandExecution.AtomicCommands;

namespace CatalogueManager.Collections
{
    /// <summary>
    /// Main window for Data Management, this Control lets you view all your datasets, curate descriptive metadata, configure extractable columns, generate reports etc
    /// 
    /// <para>The tree shows you all the datasets you have configured for use with the RDMP.  Double clicking on a dataset (called a Catalogue) will show you the descriptive data you 
    /// have recorded. Right clicking a Catalogue will give you access to operations relevant to Catalogues (e.g. viewing dataset extraction logic if any).  Right clicking a
    /// CatalogueItem will give you access to operations relevant to CatalogueItems (e.g. adding an Issue).  And so on.</para>
    /// 
    /// <para>Each Catalogue has 1 or more CatalogueItems (visible through the CatalogueItems tab), these are the columns in the dataset that are maintained by RDMP. If you have very 
    /// wide data tables with hundreds of columns you might only configure a subset of those columns (the ones most useful  to researchers) for extraction.</para>
    /// 
    /// <para>You can also drag Catalogues between folders or into other Controls (e.g. dragging a Catalogue into a CohortIdentificationCollectionUI container to add the dataset to the identification
    /// criteria).</para>
    /// 
    /// <para>Pressing the Del key will prompt you to delete the selected item.</para>
    /// 
    /// <para>By default Deprecated, Internal and ColdStorage Catalogues do not appear, you can turn visibility of these on by selecting the relevant tick boxes.</para>
    /// 
    /// <para>Finally you can launch 'Checking' for every dataset, this will attempt to verify the extraction SQL you
    /// have configured for each dataset and to ensure that it runs and that at least 1 row of data is returned.  Checking all the datasets can take a while so runs asynchronously.</para>
    /// </summary>
    public partial class CatalogueCollectionUI : RDMPCollectionUI
    {
        private IActivateItems _activator;
        
        private Catalogue[] _allCatalogues;

        //constructor

        private bool bLoading = true;
        public CatalogueCollectionUI()
        {
            InitializeComponent();
            
            cbShowInternal.Checked = UserSettings.ShowInternalCatalogues;
            cbShowDeprecated.Checked = UserSettings.ShowDeprecatedCatalogues ;
            cbShowColdStorage.Checked = UserSettings.ShowColdStorageCatalogues;
            cbProjectSpecific.Checked = UserSettings.ShowProjectSpecificCatalogues;
            cbShowNonExtractable.Checked = UserSettings.ShowNonExtractableCatalogues;

            olvCheckResult.ImageGetter += CheckImageGetter;
            olvFilters.AspectGetter += FilterAspectGetter;
            
            bLoading = false;
        }

        //The color to highlight each Catalogue based on its extractability status
        private object ocheckResultsDictionaryLock = new object();
        Dictionary<ICheckable, CheckResult> checkResultsDictionary = new Dictionary<ICheckable, CheckResult>();
        private Thread checkingThread;

        public void CheckCatalogues()
        {
            if (checkingThread != null && checkingThread.IsAlive)
            {
                MessageBox.Show("Checking is already happening");
                return;
            }
            
            //reset the dictionary
            lock (ocheckResultsDictionaryLock)
            {
                checkResultsDictionary = new Dictionary<ICheckable, CheckResult>();
            }

            var visibleCatalogues = tlvCatalogues.FilteredObjects.OfType<Catalogue>().ToArray();

            //reset the progress bar
            progressBar1.Maximum = visibleCatalogues.Length;
            progressBar1.Value = 0;
            progressBar1.Visible = true;

            checkingThread = new Thread(() =>
            {
                //only check the items that are visible int he listview
                foreach (var catalogue in visibleCatalogues)//make copy to prevent synchronization issues
                {
                    var notifier = new ToMemoryCheckNotifier();
                    catalogue.Check(notifier);

                    lock (ocheckResultsDictionaryLock)
                        checkResultsDictionary.Add(catalogue, notifier.GetWorst());

                    //increase progress bar count by one
                    Invoke(new MethodInvoker(() => { progressBar1.Value++; }));
                }
                
                
                foreach (var configuration in RepositoryLocator.CatalogueRepository.GetAllObjects<AggregateConfiguration>().ToArray())
                {
                    var notifier = new ToMemoryCheckNotifier();
                    configuration.Check(notifier);
                        
                    lock (ocheckResultsDictionaryLock)
                        checkResultsDictionary.Add(configuration, notifier.GetWorst());
                }


                //now load images to UI
                Invoke(new MethodInvoker(() =>
                {
                    progressBar1.Visible = false;
                    tlvCatalogues.RebuildColumns();//should update the images?
                }));

            });

            checkingThread.Start();
        }

        private object CheckImageGetter(object rowobject)
        {
            var checkable = rowobject as ICheckable;
            if (checkable == null)
                return null;
            
            lock (ocheckResultsDictionaryLock)
            {
                if (checkResultsDictionary.ContainsKey(checkable))
                    return CommonFunctionality.CoreIconProvider.GetImage(checkResultsDictionary[checkable]);

            }
            //not been checked yet
            return null;
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (checkingThread != null)
                checkingThread.Abort();

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool isFirstTime = true;

        public void RefreshUIFromDatabase(object oRefreshFrom)
        {   
            //if there are new catalogues we don't already have in our tree
            if (_allCatalogues != null)
            {
                var newCatalogues = CommonFunctionality.CoreChildProvider.AllCatalogues.Except(_allCatalogues);
                if (newCatalogues.Any())
                {
                    oRefreshFrom = CatalogueFolder.Root;//refresh from the root instead
                    tlvCatalogues.RefreshObject(oRefreshFrom);
                }
            }

            _allCatalogues = CommonFunctionality.CoreChildProvider.AllCatalogues;
            
            if(isFirstTime || Equals(oRefreshFrom, CatalogueFolder.Root))
            {
                tlvCatalogues.RefreshObject(CatalogueFolder.Root);
                ExpandAllFolders(CatalogueFolder.Root);
                isFirstTime = false;
            }
        }

        private void ExpandAllFolders(CatalogueFolder model)
        {
            //expand it
            tlvCatalogues.Expand(model);
            
            foreach (var folder in tlvCatalogues.GetChildren(model).OfType<CatalogueFolder>())
                ExpandAllFolders(folder);
        }

        public void ApplyFilters()
        {
            if(bLoading)
                return;

            UserSettings.ShowInternalCatalogues = cbShowInternal.Checked;
            UserSettings.ShowDeprecatedCatalogues = cbShowDeprecated.Checked;
            UserSettings.ShowColdStorageCatalogues = cbShowColdStorage.Checked;
            UserSettings.ShowProjectSpecificCatalogues = cbProjectSpecific.Checked;
            UserSettings.ShowNonExtractableCatalogues = cbShowNonExtractable.Checked;
            
            tlvCatalogues.UseFiltering = true;
            tlvCatalogues.ModelFilter = new CatalogueCollectionFilter(_activator.CoreChildProvider);
        }

        public enum HighlightCatalogueType
        {
            None,
            Extractable,
            ExtractionBroken,
            TOP1Worked
        }
        
        private void btnCheckCatalogues_Click(object sender, EventArgs e)
        {
            CheckCatalogues();
        }

        private object FilterAspectGetter(object rowObject)
        {
            var cataItem = rowObject as CatalogueItem;
            if (cataItem != null)
                return _activator.CoreChildProvider.GetAllChildrenRecursively(cataItem).OfType<IFilter>().Count();

            return null;
        }

        public override void SetItemActivator(IActivateItems activator)
        {
            _activator = activator;

            _activator.Emphasise += _activator_Emphasise;

            //important to register the setup before the lifetime subscription so it gets priority on events
            CommonFunctionality.SetUp(
                RDMPCollection.Catalogue,
                tlvCatalogues,
                _activator,
                olvColumn1, //the icon column
                //we have our own custom filter logic so no need to pass tbFilter
                olvColumn1 //also the renameable column
                );
            
            //Things that are always visible regardless
            CommonFunctionality.WhitespaceRightClickMenuCommands = new IAtomicCommand[]
            {
                new ExecuteCommandCreateNewCatalogueByImportingFile(_activator),
                new ExecuteCommandCreateNewCatalogueByImportingExistingDataTable(_activator, true),
                new ExecuteCommandCreateNewEmptyCatalogue(_activator)
            };

            _activator.RefreshBus.EstablishLifetimeSubscription(this);

            tlvCatalogues.AddObject(activator.CoreChildProvider.AllGovernanceNode);
            tlvCatalogues.AddObject(CatalogueFolder.Root);
            ApplyFilters();

            RefreshUIFromDatabase(CatalogueFolder.Root);
        }

        void _activator_Emphasise(object sender, ItemActivation.Emphasis.EmphasiseEventArgs args)
        {
            //user wants this object emphasised
            var c = args.Request.ObjectToEmphasise as Catalogue;
            
            if (c == null)
            {
                var descendancy = _activator.CoreChildProvider.GetDescendancyListIfAnyFor(args.Request.ObjectToEmphasise);

                if (descendancy != null)
                    c = descendancy.Parents.OfType<Catalogue>().SingleOrDefault();
            }
            
            if (c != null)
            {
                if ((c.IsColdStorageDataset || c.IsDeprecated || c.IsInternalDataset))
                {
                    //trouble is our flags might be hiding it so make sure it is visible
                    cbShowColdStorage.Checked = cbShowColdStorage.Checked || c.IsColdStorageDataset;
                    cbShowDeprecated.Checked = cbShowDeprecated.Checked || c.IsDeprecated;
                    cbShowInternal.Checked = cbShowInternal.Checked || c.IsInternalDataset;
                }

                var isExtractable = c.GetExtractabilityStatus(null);

                cbShowNonExtractable.Checked = cbShowNonExtractable.Checked || isExtractable == null || isExtractable.IsExtractable == false;
                
                ApplyFilters();
            }
        }

        public void RefreshBus_RefreshObject(object sender, RefreshObjectEventArgs e)
        {
            var o = e.Object;
            var cata = o as Catalogue;

            if (cata != null)
            {
                var oldFolder = tlvCatalogues.GetParent(cata) as CatalogueFolder;

                //if theres a change to the folder of the catalogue or it is a new Catalogue (no parent folder) we have to rebuild the entire tree
                if (oldFolder == null || !oldFolder.Path.Equals(cata.Folder.Path))
                    RefreshUIFromDatabase(CatalogueFolder.Root);
                else
                    RefreshUIFromDatabase(o);
                return;
            }

            if( o is CatalogueItem || o is AggregateConfiguration ||
            o is ColumnInfo || o is TableInfo || o is ExtractionFilter || o is ExtractionFilterParameter ||
            o is ExtractionFilterParameterSet || o is ExtractionInformation ||
            o is AggregateFilterContainer || o is AggregateFilter || o is AggregateFilterParameter|| o is CatalogueItemIssue)
            {
                //then refresh us
                RefreshUIFromDatabase(o);
            }
        }
        
        private void rbFlag_CheckedChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        public static bool IsRootObject(object root)
        {
            return root.Equals(CatalogueFolder.Root);
        }

        public void SelectCatalogue(Catalogue catalogue)
        {
            tlvCatalogues.SelectObject(catalogue, true);
        }
    }
}
