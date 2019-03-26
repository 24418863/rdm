// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueLibrary.Reports;
using CatalogueManager.Collections;
using CatalogueManager.CommandExecution.AtomicCommands;
using CatalogueManager.CommandExecution.AtomicCommands.UIFactory;
using CatalogueManager.FindAndReplace;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using CatalogueManager.ItemActivation.Emphasis;
using CatalogueManager.LocationsMenu.Ticketing;
using CatalogueManager.MainFormUITabs;
using CatalogueManager.PluginManagement;
using CatalogueManager.PluginManagement.CodeGeneration;
using CatalogueManager.SimpleControls;
using CatalogueManager.SimpleDialogs;
using CatalogueManager.SimpleDialogs.NavigateTo;
using CatalogueManager.SimpleDialogs.Reports;
using CatalogueManager.TestsAndSetup;
using CatalogueManager.TestsAndSetup.ServicePropogation;
using CatalogueManager.Theme;
using CatalogueManager.Tutorials;
using CohortManager.CommandExecution.AtomicCommands;
using DataExportLibrary.Data.DataTables;
using DataExportManager.CommandExecution.AtomicCommands;
using DataExportManager.CommandExecution.AtomicCommands.CohortCreationCommands;
using DataQualityEngine;
using HIC.Logging;
using MapsDirectlyToDatabaseTableUI;
using ResearchDataManagementPlatform.Menus.MenuItems;
using ResearchDataManagementPlatform.WindowManagement;
using ResearchDataManagementPlatform.WindowManagement.ContentWindowTracking;
using ResearchDataManagementPlatform.WindowManagement.ContentWindowTracking.Persistence;
using ResearchDataManagementPlatform.WindowManagement.Licenses;
using ReusableLibraryCode;
using ReusableLibraryCode.CommandExecution.AtomicCommands;
using ReusableLibraryCode.Settings;
using ReusableUIComponents;
using ReusableUIComponents.ChecksUI;
using ReusableUIComponents.Dialogs;
using ReusableUIComponents.Settings;
using WeifenLuo.WinFormsUI.Docking;

namespace ResearchDataManagementPlatform.Menus
{

    /// <summary>
    /// The Top menu of the RDMP lets you do most tasks that do not relate directly to a single object (most single object tasks are accessed by right clicking the object).
    /// 
    /// <para>Locations:
    /// - Change which DataCatalogue database you are pointed at (not usually needed unless you have two different databases e.g. a Test database and a Live database)
    /// - Setup Logging / Anonymisation / Query Caching / Data Quality Engine databases
    /// - Configure a Ticketing system e.g. Jira for tracking time against tickets (you can set a ticket identifier for datasets, project extractions etc)
    /// - Perform bulk renaming operations across your entire catalogue database (useful for when someone remaps your server drives to a new letter! e.g. 'D:\Datasets\Private\' becomes 'E:\')
    /// - Refresh the window by reloading all Catalogues/TableInfos etc </para>
    /// 
    /// <para>View:
    /// - View/Edit dataset loading logic
    /// - View/Edit the governance approvals your datasets have (including attachments, period covered, datasets included in approval etc)
    /// - View the Logging database contents (a relational view of all activities undertaken by all Data Analysts using the RDMP - loading, extractions, dqe runs etc).</para>
    /// 
    /// <para>Reports
    /// - Generate a variety of reports that summarise the state of your datasets / governance etc</para>
    /// 
    /// <para>Help
    /// - View the user manual
    /// - View a technical description of each of the core objects maintained by RDMP (Catalogues, TableInfos etc) and what they mean (intended for programmers)
    /// - Generate user interface document (the document you are currently reading).</para>
    /// </summary>

    public partial class RDMPTopMenuStripUI : RDMPUserControl
    {
        private WindowManager _windowManager;
        
        private SaveMenuItem _saveToolStripMenuItem;
        private AtomicCommandUIFactory _atomicCommandUIFactory;

        public RDMPTopMenuStripUI()
        {
            InitializeComponent();
        }
        
        private void configureExternalServersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ExecuteCommandConfigureDefaultServers(Activator).Execute();
        }

        private void setTicketingSystemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TicketingSystemConfigurationUI ui = new TicketingSystemConfigurationUI();
            Activator.ShowWindow(ui, true);
        }


        private void governanceReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var generator = new GovernanceReport(new DatasetTimespanCalculator(), Activator.RepositoryLocator.CatalogueRepository);
            generator.GenerateReport();
        }
        private void logViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var cmd = new ExecuteCommandViewLoggedData(Activator, LoggingTables.DataLoadTask);
            cmd.Execute();
        }
        
        private void databaseAccessComplexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WordAccessRightsByUserUI dialog = new WordAccessRightsByUserUI();
            dialog.Show();
        }

        private void metadataReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var cmd = new ExecuteCommandGenerateMetadataReport(Activator, null);
            cmd.Execute();
        }


        private void serverSpecReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new DatabaseSizeReportUI(Activator);
            dialog.Show();
        }

        private void dITAExtractionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form f = new Form();
            f.Text = "DITA Extraction of Catalogue Metadata";
            DitaExtractorUI d = new DitaExtractorUI();
            d.SetItemActivator(Activator);
            f.Width = d.Width + 10;
            f.Height = d.Height + 50;
            f.Controls.Add(d);
            f.Show();
        }

        private void launchDiagnosticsScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DiagnosticsUI dialog = new DiagnosticsUI(Activator, null);
            dialog.ShowDialog();
        }

        private void generateTestDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ExecuteCommandGenerateTestData(Activator).Execute();
        }
        
        private void showPerformanceCounterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new PerformanceCounterUI().Show();
        }

        private void openExeDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                UsefulStuff.GetInstance().ShowFolderInWindowsExplorer(new DirectoryInfo(Environment.CurrentDirectory));
            }
            catch (Exception exception)
            {
                ExceptionViewer.Show(exception);
            }
        }

        private void userManualToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                FileInfo f = UsefulStuff.SprayFile(typeof(CatalogueCollectionUI).Assembly, "CatalogueManager.UserManual.docx", "UserManual.docx");
                Process.Start(f.FullName);
            }
            catch (Exception exception)
            {
                ExceptionViewer.Show(exception);
            }
        }

        private void generateClassTableSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var report = new DocumentationReportMapsDirectlyToDatabaseOfficeBit();
            report.GenerateReport(Activator.RepositoryLocator.CatalogueRepository.CommentStore,
                new PopupChecksUI("Generating class summaries", false),
                Activator.CoreIconProvider,
                typeof(Catalogue).Assembly,
                typeof(ExtractionConfiguration).Assembly);
        }

        private void generateUserInterfaceDocumentationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var generator = new DocumentationReportFormsAndControlsUI(Activator);
            generator.Show();
        }
        
        private void showHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var current = _windowManager.Navigation.CurrentTab;

            var t = current as RDMPSingleControlTab;
            if(t == null)
                return;

            t.ShowHelp(Activator);
        }

        public void SetWindowManager(WindowManager windowManager)
        {
            SetItemActivator(windowManager.ActivateItems);

            _windowManager = windowManager;
            _atomicCommandUIFactory = new AtomicCommandUIFactory(Activator);
            

            //top menu strip setup / adjustment
            LocationsMenu.DropDownItems.Add(new DataExportMenu(Activator));
            _saveToolStripMenuItem = new SaveMenuItem
            {
                Enabled = false,
                Name = "saveToolStripMenuItem",
                Size = new System.Drawing.Size(214, 22)
            };
            fileToolStripMenuItem.DropDownItems.Add(_saveToolStripMenuItem);

            _windowManager.TabChanged += WindowFactory_TabChanged;

            var tracker = new TutorialTracker(Activator);
            foreach (Tutorial t in tracker.TutorialsAvailable)
                tutorialsToolStripMenuItem.DropDownItems.Add(new LaunchTutorialMenuItem(tutorialsToolStripMenuItem, Activator, t, tracker));

            tutorialsToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());

            tutorialsToolStripMenuItem.DropDownItems.Add(new DisableTutorialsMenuItem(tutorialsToolStripMenuItem, tracker));
            tutorialsToolStripMenuItem.DropDownItems.Add(new ResetTutorialsMenuItem(tutorialsToolStripMenuItem, tracker));

            closeToolStripMenuItem.Enabled = false;

            rdmpTaskBar1.SetWindowManager(_windowManager);

            //menu is dynamic but we need to add something to it or it won't show arrow
            newToolStripMenuItem.DropDownOpening += NewToolStripMenuItemOnDropDownOpening;
            newToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());

            // Location menu
            LocationsMenu.DropDownItems.Add(_atomicCommandUIFactory.CreateMenuItem(new ExecuteCommandChoosePlatformDatabase(Activator.RepositoryLocator)));

            Activator.Theme.ApplyTo(menuStrip1);
        }

        private void NewToolStripMenuItemOnDropDownOpening(object sender, EventArgs eventArgs)
        {
            //recreate this menu each time since it can change the IsImpossible status of stuff
            newToolStripMenuItem.DropDownItems.Clear();
            
            //Catalogue commands
            AddToNew(new ExecuteCommandCreateNewCatalogueByImportingFile(Activator));
            AddToNew(new ExecuteCommandCreateNewCatalogueByImportingExistingDataTable(Activator, false));
            AddToNew(new ExecuteCommandCreateNewCohortIdentificationConfiguration(Activator));
            AddToNew(new ExecuteCommandCreateNewLoadMetadata(Activator));
            AddToNew(new ExecuteCommandCreateNewStandardRegex(Activator));

            //Saved cohorts database creation
            newToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            AddToNew(new ExecuteCommandCreateNewCohortDatabaseUsingWizard(Activator));

            //cohort creation
            newToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            AddToNew(new ExecuteCommandCreateNewCohortByExecutingACohortIdentificationConfiguration(Activator));
            AddToNew(new ExecuteCommandCreateNewCohortFromFile(Activator));
            AddToNew(new ExecuteCommandCreateNewCohortFromCatalogue(Activator));

            //Data export commands
            newToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            AddToNew(new ExecuteCommandCreateNewExtractableDataSetPackage(Activator));
            AddToNew(new ExecuteCommandCreateNewDataExtractionProject(Activator));
            AddToNew(new ExecuteCommandRelease(Activator) { OverrideCommandName = "New Release..." });

            newToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            AddToNew(new ExecuteCommandCreateANOVersion(Activator));
        }

        private void AddToNew(IAtomicCommand cmd)
        {
            newToolStripMenuItem.DropDownItems.Add(_atomicCommandUIFactory.CreateMenuItem(cmd));
        }

        void WindowFactory_TabChanged(object sender, IDockContent newTab)
        {
            closeToolStripMenuItem.Enabled = newTab != null && !(newTab is PersistableToolboxDockContent);
            showHelpToolStripMenuItem.Enabled = newTab is RDMPSingleControlTab;

            var singleObjectControlTab = newTab as RDMPSingleControlTab;
            if (singleObjectControlTab == null)
            {
                _saveToolStripMenuItem.Saveable = null;
                return;
            }

            var saveable = singleObjectControlTab.GetControl() as ISaveableUI;
            var singleObject = singleObjectControlTab.GetControl() as IRDMPSingleDatabaseObjectControl;

            //if user wants to emphasise on tab change and theres an object we can emphasise associated with the control
            if (singleObject != null && UserSettings.EmphasiseOnTabChanged && singleObject.DatabaseObject != null)
                Activator.RequestItemEmphasis(this, new EmphasiseRequest(singleObject.DatabaseObject));

            _saveToolStripMenuItem.Saveable = saveable;

            navigateBackwardToolStripMenuItem.Enabled = _windowManager.Navigation.CanBack();
            navigateForwardToolStripMenuItem.Enabled = _windowManager.Navigation.CanForward();
        }
        
        private void managePluginsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PluginManagementFormUI dialogue = new PluginManagementFormUI(Activator);
            dialogue.Show();
        }

        private void codeGenerationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ui = new GenerateClassCodeFromTableUI();
            ui.Show();
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new RunUI(_windowManager.ActivateItems);
            dialog.Show();
        }

        private void userSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var settings = new UserSettingsFileUI();
            settings.Show();

        }

        private void licenseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var l = new LicenseUI();
            l.ShowDialog();
        }

        public void InjectButton(ToolStripButton button)
        {
            rdmpTaskBar1.InjectButton(button);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var navigate = new NavigateToObjectUI(Activator);
            navigate.CompletionAction = (o) => Activator.WindowArranger.SetupEditAnything(this, o);
            navigate.Show();
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var visibleCollection = _windowManager.GetFocusedCollection();

            new NavigateToObjectUI(Activator, null, visibleCollection).Show();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _windowManager.CloseCurrentTab();
        }

        private void findAndReplaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Activator.ShowWindow(new FindAndReplaceUI(Activator),true);
        }

        private void navigateBackwardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _windowManager.Navigation.Back(true);
        }

        private void navigateForwardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _windowManager.Navigation.Forward(true);
        }
    }
}

