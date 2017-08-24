﻿using System;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using CatalogueManager.TestsAndSetup.ServicePropogation;
using HIC.Logging;
using MapsDirectlyToDatabaseTableUI;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DataAccess;
using ReusableUIComponents;
using ReusableUIComponents.ChecksUI;


namespace CatalogueManager.SimpleDialogs
{
    /// <summary>
    /// Every dataset (Catalogue) can have it's own Logging task and Logging server.  If you have multiple logging servers (e.g. a test logging server and a live logging server). You 
    /// can configure each of these independently.  If you only have one logging server then just set the live logging server. 
    /// 
    /// Once you have set the logging server you should create or select an existing task (e.g. 'Loading Biochemistry' might be a good logging task for Biochemistry dataset).  All datasets
    /// in a given load (see LoadMetadataUI) must share the same logging task so it is worth considering the naming for example you might call the task 'Loading Hospital Data' and another
    /// 'Loading Primary Care Data'.
    /// 
    /// Data Extraction always gets logged under a task called 'Data Extraction' but the server you select here will be the one that it is logged against when the dataset is extracted.
    /// 
    /// You can configure defaults for the logging servers of new datasets through ManageExternalServers dialog (See ManageExternalServers)
    /// </summary>
    public partial class ChooseLoggingTaskUI : RDMPUserControl, ICheckNotifier
    {
        private Catalogue _catalogue;
        private string expectedDatabaseTypeString = "HIC.Logging.Database";
        public Catalogue Catalogue
        {
            get { return _catalogue; }
            set
            {
                _catalogue = value;
                RefreshUIFromDatabase();
            }
        }

        private void RefreshUIFromDatabase()
        {
            if(RepositoryLocator == null || _catalogue == null)
                return;

            var servers = RepositoryLocator.CatalogueRepository.GetAllObjects<ExternalDatabaseServer>().Where(s => string.Equals(expectedDatabaseTypeString, s.CreatedByAssembly)).ToArray();

            ddLoggingServer.Items.Clear();
            ddLoggingServer.Items.AddRange(servers);

            ddTestLoggingServer.Items.Clear();
            ddTestLoggingServer.Items.AddRange(servers);

            ExternalDatabaseServer liveserver = null;

            if (_catalogue.LiveLoggingServer_ID != null)
            {
                liveserver = ddLoggingServer.Items.Cast<ExternalDatabaseServer>()
                    .SingleOrDefault(i => i.ID == (int)_catalogue.LiveLoggingServer_ID);

                if(liveserver == null)
                    throw new Exception("Catalogue '" + _catalogue + "' lists it's Live Logging Server as '" + _catalogue.LiveLoggingServer + "' did not appear in combo box, possibly it is not marked as a '" + expectedDatabaseTypeString + "' server? Try editting it in Locations=>Manage External Servers");

                ddLoggingServer.SelectedItem = liveserver;
            }
            
            if (_catalogue.TestLoggingServer_ID != null)
            {
                var testLogging = ddTestLoggingServer.Items.Cast<ExternalDatabaseServer>()
                    .SingleOrDefault(i => i.ID == (int)_catalogue.TestLoggingServer_ID);
                
                if(testLogging == null)
                    throw new Exception("Catalogue '" + _catalogue + "' lists it's Test Logging Server as '" + _catalogue.TestLoggingServer + "' did not appear in combo box, possibly it is not marked as a  '" + expectedDatabaseTypeString + "' server? Try editting it in Locations=>Manage External Servers");

                ddTestLoggingServer.SelectedItem = testLogging;

                
            }
            
            try
            {
                //load data tasks (new architecture)
                //if the catalogue knows its logging server - populate values
                if (liveserver != null)
                {
                    LogManager lm = new LogManager(liveserver);

                    foreach (var t in lm.ListDataTasks())
                        if (!cbxDataLoadTasks.Items.Contains(t))
                            cbxDataLoadTasks.Items.Add(t);
                }

            }
            catch (Exception ex)
            {

                MessageBox.Show("Problem getting the list of DataTasks from the new logging architecture:" + ex.Message);
            }

            if (!string.IsNullOrWhiteSpace(_catalogue.LoggingDataTask))
                cbxDataLoadTasks.Text = _catalogue.LoggingDataTask;

            CheckNameExists();
        }

        public ChooseLoggingTaskUI()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            
            base.OnLoad(e);

            if(RepositoryLocator == null)
                return;
            
            RefreshUIFromDatabase();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshTasks();
        }

        private void RefreshTasks()
        {
            ExternalDatabaseServer liveserver = ddLoggingServer.SelectedItem as ExternalDatabaseServer;
            var server = DataAccessPortal.GetInstance().ExpectServer(liveserver, DataAccessContext.Logging);

            if (liveserver != null)
            {
                cbxDataLoadTasks.Items.Clear();

                try
                {
                    LogManager lm = new LogManager(server);
                    cbxDataLoadTasks.Items.AddRange(lm.ListDataTasks());
                }
                catch (Exception e)
                {
                    ExceptionViewer.Show(e);
                }
            }

        }


        private void cbxDataLoadTasks_SelectedIndexChanged(object sender, EventArgs e)
        {
            _catalogue.LoggingDataTask = (string) cbxDataLoadTasks.SelectedItem;
            _catalogue.SaveToDatabase();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void ddLoggingServer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddLoggingServer.SelectedItem == null)
            {
                _catalogue.LiveLoggingServer_ID = null;
                _catalogue.SaveToDatabase();
                return;
            }

            _catalogue.LiveLoggingServer_ID = ((ExternalDatabaseServer)ddLoggingServer.SelectedItem).ID;
            _catalogue.SaveToDatabase();
            RefreshTasks();
        }

        private void ddTestLoggingServer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddTestLoggingServer.SelectedItem == null)
            {
                _catalogue.TestLoggingServer_ID = null;
                _catalogue.SaveToDatabase();
                return;
            }

            _catalogue.TestLoggingServer_ID = ((ExternalDatabaseServer)ddTestLoggingServer.SelectedItem).ID;
            _catalogue.SaveToDatabase();
        }

        private void cbxDataLoadTasks_TextChanged(object sender, EventArgs e)
        {
            if (_catalogue == null)
                return;

            _catalogue.LoggingDataTask = cbxDataLoadTasks.Text;
            _catalogue.SaveToDatabase();
        }

        private void btnCreateNewLoggingTask_Click(object sender, EventArgs e)
        {
            try
            {
                var testServer =  ddTestLoggingServer.SelectedItem as ExternalDatabaseServer;
                var liveServer =  ddLoggingServer.SelectedItem as ExternalDatabaseServer;

                string target = "";

                string toCreate = cbxDataLoadTasks.Text;

                if (liveServer != null)
                    target = liveServer.Server + "." + liveServer.Database;

                if (liveServer != null && testServer != null)
                    target += " and ";

                if(testServer != null)
                    target += testServer.Server + "." + testServer.Database;

                if (string.IsNullOrEmpty(target))
                {

                    MessageBox.Show("You must select a logging server");
                    return;
                }

                var dr = MessageBox.Show("Create a new dataset and new data task called \"" + toCreate + "\" in " + target, "Create new logging task", MessageBoxButtons.YesNo);

                if(dr == DialogResult.Yes)
                {
                    if (liveServer != null)
                    {

                        LoggingDatabaseChecker checker = new LoggingDatabaseChecker(liveServer);
                        checker.Check(this);

                        new LogManager(liveServer)
                            .CreateNewLoggingTaskIfNotExists(toCreate);
                        
                    }

                    if(testServer!= null)
                        new LogManager(testServer)
                            .CreateNewLoggingTaskIfNotExists(toCreate);

                    MessageBox.Show("Done");

                    RefreshTasks();
                }

                RefreshUIFromDatabase();
            }
            catch (Exception exception)
            {
                ExceptionViewer.Show(exception);
            }
        }

        public bool OnCheckPerformed(CheckEventArgs args)
        {

            if (args.ProposedFix != null)
                return MakeChangePopup.ShowYesNoMessageBoxToApplyFix(null, args.Message, args.ProposedFix);
            else
            {
                //if it is sucessful user doesn't need to be spammed with messages
                if(args.Result == CheckResult.Success)
                    return true;

                //its a warning or an error possibly with an exception attached
                if (args.Ex != null)
                    ExceptionViewer.Show(args.Message,args.Ex);
                else
                    MessageBox.Show(args.Message);

                return false;
            }
        }

        private void cbxDataLoadTasks_Leave(object sender, EventArgs e)
        {
            CheckNameExists();
        }

        private void cbxDataLoadTasks_KeyUp(object sender, KeyEventArgs e)
        {
            CheckNameExists();
        }

        private void CheckNameExists()
        {
            ragSmiley1.Reset();

            if(string.IsNullOrWhiteSpace(cbxDataLoadTasks.Text))
                ragSmiley1.Warning(new Exception("You must provide a Data Task name e.g. 'Loading my cool dataset'"));
            else
            if (!cbxDataLoadTasks.Items.Contains(cbxDataLoadTasks.Text))
                ragSmiley1.Fatal(new Exception("Task '" + cbxDataLoadTasks.Text + "' does not exist yet, select 'Create' to create it"));
        }

        private void btnCreateNewLoggingServer_Click(object sender, EventArgs e)
        {
            CreatePlatformDatabase.CreateNewExternalServer(RepositoryLocator.CatalogueRepository,ServerDefaults.PermissableDefaults.LiveLoggingServer_ID, typeof(HIC.Logging.Database.Class1).Assembly);
            RefreshUIFromDatabase();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            if (sender == btnClearLive)
                ddLoggingServer.SelectedItem = null;

            if (sender == btnClearTest)
                ddTestLoggingServer.SelectedItem = null;
        }
    }
}