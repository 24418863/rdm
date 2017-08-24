﻿using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.SimpleDialogs;
using CatalogueManager.TestsAndSetup;
using CatalogueManager.TestsAndSetup.ServicePropogation;
using MapsDirectlyToDatabaseTableUI;
using ReusableLibraryCode.DataAccess;
using ReusableUIComponents;

namespace CatalogueManager.LocationsMenu
{
    /// <summary>
    /// The RDMP Data Catalogue database is the central resource for storing all information about what is where, what datasets there are, what servers they are on etc.  This includes 
    /// keeping track of the locations of other servers such as the Logging server/database, Data Quality Engine reporting database, anonymisation databases, query caching databases
    /// etc. 
    /// 
    /// This dialog lets you do 3 things:
    /// 
    /// 1. Create references to servers (ExternalDatabaseServer) this requires logistical name (what you want to call it) and servername.  Optionally you can specify a database (required
    /// in the case of references to specific databases e.g. Logging Database), if you omit it then the 'master' database will be used.  If you do not specify a username/password then
    /// Integrated Security will be used when connecting (the preferred method).  Usernames and passwords are stored in encrypted form (See PasswordEncryptionKeyLocationUI).
    /// 
    /// 2. Create an encryption key for the usernames/passwords stored in your Data Catalogue Database (See PasswordEncryptionKeyLocationUI)
    /// 
    /// 3. Configure/Create default servers required for certain parts of the RDMP software to work (e.g. Logging servers, a Data Quality Engine Reporting Databases etc).  If you are not
    /// sure what a database is (e.g. Identifier Dump) then don't create one! 
    /// 
    /// </summary>
    public partial class ManageExternalServers : RDMPForm
    {
        private ExternalDatabaseServer _externalDatabaseServer;
        
        ServerDefaults defaults;
        private readonly ICoreIconProvider _coreIconProvider;


        bool bloading;

        private ExternalDatabaseServer ExternalDatabaseServer
        {
            get { return _externalDatabaseServer; }
            set
            {
                _externalDatabaseServer = value;
                
                gbEdit.Enabled = _externalDatabaseServer != null;
                bloading = true;
                if (value != null)
                {
                    tbID.Text = value.ID.ToString();
                    tbName.Text = value.Name;
                    tbServerName.Text = value.Server;
                    tbDatabaseName.Text = value.Database;
                    tbUsername.Text = value.Username;
                    tbPassword.Text = value.GetDecryptedPassword();
                    ddSetKnownType.Text = value.CreatedByAssembly;

                    pbServer.Image = _coreIconProvider.GetImage(value);
                }
                else
                {
                    pbServer.Image = null;
                    tbID.Text = "";
                    tbName.Text = "";
                    tbServerName.Text = "";
                    tbDatabaseName.Text = "";
                    tbName.Text = "";
                    tbPassword.Text = "";
                    ddSetKnownType.Text = null;
                }
                bloading = false;
            }
        }

        public ManageExternalServers(ICoreIconProvider coreIconProvider)
        {
            InitializeComponent();
            _coreIconProvider = coreIconProvider;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (VisualStudioDesignMode)
                return;

            RefreshUIFromDatabase();

            ddSetKnownType.Items.AddRange(
                AppDomain.CurrentDomain.GetAssemblies() //get all current assemblies that are loaded
                .Select(n=>n.GetName().Name)//get the name of the assembly
                .Where(s => s.EndsWith(".Database") && //if it is a .Database assembly advertise it to the user as a known type of database
                    !(s.EndsWith("CatalogueLibrary.Database") || s.EndsWith("DataExportManager.Database"))).ToArray()); //unless it's one of the core ones (catalogue/data export)

        }

        private void RefreshUIFromDatabase()
        {
            try
            {
                defaults = new ServerDefaults(RepositoryLocator.CatalogueRepository);

                var allServers = RepositoryLocator.CatalogueRepository.GetAllObjects<ExternalDatabaseServer>().ToArray();

                ddKnownServers.Items.Clear();
                ddKnownServers.Items.AddRange(allServers);
                
                InitializeServerDropdown(ddDefaultLoggingServer, ServerDefaults.PermissableDefaults.LiveLoggingServer_ID, allServers);
                InitializeServerDropdown(ddDefaultTestLoggingServer, ServerDefaults.PermissableDefaults.TestLoggingServer_ID, allServers);
                InitializeServerDropdown(ddDQEServer, ServerDefaults.PermissableDefaults.DQE, allServers);
                InitializeServerDropdown(ddWebServiceQueryCacheServer, ServerDefaults.PermissableDefaults.WebServiceQueryCachingServer_ID, allServers);
                InitializeServerDropdown(ddCohortIdentificationQueryCacheServer, ServerDefaults.PermissableDefaults.CohortIdentificationQueryCachingServer_ID, allServers);
                InitializeServerDropdown(ddDefaultIdentifierDump, ServerDefaults.PermissableDefaults.IdentifierDumpServer_ID, allServers);
                InitializeServerDropdown(ddOverrideRawServer, ServerDefaults.PermissableDefaults.RAWDataLoadServer, allServers);
                InitializeServerDropdown(ddDefaultANOStore, ServerDefaults.PermissableDefaults.ANOStore, allServers);

                btnCreateNewDQEServer.Enabled = ddDQEServer.SelectedItem == null;
                btnClearDQEServer.Enabled = ddDQEServer.SelectedItem != null;

                btnCreateNewWebServiceQueryCache.Enabled = ddWebServiceQueryCacheServer.SelectedItem == null;
                btnClearWebServiceQueryCache.Enabled = ddWebServiceQueryCacheServer.SelectedItem != null;

                btnCreateNewCohortIdentificationQueryCache.Enabled = ddCohortIdentificationQueryCacheServer.SelectedItem == null;
                btnClearCohortIdentificationQueryCache.Enabled = ddCohortIdentificationQueryCacheServer.SelectedItem !=null;
                
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
        }

        private void InitializeServerDropdown(ComboBox comboBox, ServerDefaults.PermissableDefaults permissableDefault, ExternalDatabaseServer[] allServers)
        {
            comboBox.Items.Clear();

            var currentDefault = defaults.GetDefaultFor(permissableDefault);
            Tier2DatabaseType? expectedTypeOfServer = ServerDefaults.PermissableDefaultToTier2DatabaseType(permissableDefault);
            
            var toAdd = allServers;
            
            if(expectedTypeOfServer != null) //we expect an explicit type e.g. a HIC.Logging.Database 
            {
                var compatibles = RepositoryLocator.CatalogueRepository.GetAllTier2Databases(expectedTypeOfServer.Value);

                if (currentDefault == null || compatibles.Contains(currentDefault))//if there is not yet a default or the existing default is of the correct type
                    toAdd = compatibles;//then we can go ahead and use the restricted type

                //otherwise what we have is a default of the wrong server type! eep.
            }

            comboBox.Items.AddRange(toAdd);

            //select the server
            if (currentDefault != null)
                comboBox.SelectedItem = comboBox.Items.Cast<ExternalDatabaseServer>().Single(s => s.ID == currentDefault.ID);
        }


        private void ddKnownServers_SelectedIndexChanged(object sender, EventArgs e)
        {
            ExternalDatabaseServer = ddKnownServers.SelectedItem as ExternalDatabaseServer;
        }

        private void btnCheckState_Click(object sender, EventArgs e)
        {
            if (ExternalDatabaseServer != null)
                try
                {
                    ragSmiley1.Reset();
                    ragSmiley1.Visible = true;

                    DataAccessPortal.GetInstance().ExpectServer(ExternalDatabaseServer,DataAccessContext.InternalDataProcessing).TestConnection();
                    lblState.Text = "State:OK";
                    lblState.ForeColor = Color.Green;
                }
                catch (Exception exception)
                {
                    lblState.Text = "State" + exception.Message;
                    lblState.ForeColor = Color.Red;
                    ragSmiley1.Fatal(exception);
                }
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            if(ExternalDatabaseServer != null)
            {
                try
                {
                    pbServer.Image = _coreIconProvider.GetImage(ExternalDatabaseServer);
                    ExternalDatabaseServer.SaveToDatabase();
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message);
                }
                RefreshUIFromDatabase();
            }
        }

        private void tbName_TextChanged(object sender, EventArgs e)
        {
            if (ExternalDatabaseServer != null)
                ExternalDatabaseServer.Name = ((TextBox)(sender)).Text;
        }

        private void tbServerName_TextChanged(object sender, EventArgs e)
        {
            if (ExternalDatabaseServer != null)
                ExternalDatabaseServer.Server = ((TextBox)(sender)).Text;

        }

        private void tbDatabaseName_TextChanged(object sender, EventArgs e)
        {
            if (ExternalDatabaseServer != null)
                ExternalDatabaseServer.Database = ((TextBox)(sender)).Text;

        }

        private void tbUsername_TextChanged(object sender, EventArgs e)
        {
            var str = ((TextBox) (sender)).Text;
            if (ExternalDatabaseServer != null)
                ExternalDatabaseServer.Username = str;

            lblUsernameError.Text = !str.Trim().Equals(str) ? "Username has leading/trailing whitespace!" : "";
        }

        private void tbPassword_TextChanged(object sender, EventArgs e)
        {
            var str = ((TextBox) (sender)).Text;
            if (ExternalDatabaseServer != null)
                ExternalDatabaseServer.Password = str;


            lblPasswordError.Text = !str.Trim().Equals(str) ? "Password has leading/trailing whitespace!" : "";
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            int toSelect = new ExternalDatabaseServer(RepositoryLocator.CatalogueRepository,"New ExternalDatabaseServer " + Guid.NewGuid()).ID;
            RefreshUIFromDatabase();
            ddKnownServers.SelectedItem =ddKnownServers.Items.Cast<ExternalDatabaseServer>().Single(eds => eds.ID == toSelect);
            
            tbName.Focus();
            tbName.SelectAll();

        }

        private void btnDeleteServer_Click(object sender, EventArgs e)
        {
            if(ExternalDatabaseServer != null)
            {
                try
                {
                    ExternalDatabaseServer.DeleteInDatabase();
                    ExternalDatabaseServer = null;
                }
                catch (Exception exception)
                {
                    MessageBox.Show("Cannot delete (it is probably referenced use by one or more Entities):" + Environment.NewLine + "\t" + exception.Message);

                }
                RefreshUIFromDatabase();
            }
        }

        private void ddDefault_SelectedIndexChanged(object sender, EventArgs e)
        {
            ServerDefaults.PermissableDefaults toChange;

            if(sender == ddDefaultIdentifierDump)
                toChange = ServerDefaults.PermissableDefaults.IdentifierDumpServer_ID;
            else
            if (sender == ddDefaultLoggingServer)
                toChange = ServerDefaults.PermissableDefaults.LiveLoggingServer_ID;
            else if (sender == ddDefaultTestLoggingServer)
                toChange = ServerDefaults.PermissableDefaults.TestLoggingServer_ID;
            else if(sender == ddOverrideRawServer)
                toChange = ServerDefaults.PermissableDefaults.RAWDataLoadServer;
            else if (sender == ddDefaultANOStore)
                toChange = ServerDefaults.PermissableDefaults.ANOStore;
            else if (sender == ddWebServiceQueryCacheServer)
                toChange = ServerDefaults.PermissableDefaults.WebServiceQueryCachingServer_ID;
            else if (sender == ddCohortIdentificationQueryCacheServer)
                toChange = ServerDefaults.PermissableDefaults.CohortIdentificationQueryCachingServer_ID;
            else
                throw new Exception("Did not recognise sender:" + sender);

            var selectedItem = ((ComboBox) sender).SelectedItem as ExternalDatabaseServer;

            //user selected nothing
            if(selectedItem == null)
                return;

            defaults.SetDefault(toChange, selectedItem);
        }

        private void btnClearServer_Click(object sender, EventArgs e)
        {
            ServerDefaults.PermissableDefaults toClear;

            if(sender == btnClearTestLoggingServer)
            {
                toClear = ServerDefaults.PermissableDefaults.TestLoggingServer_ID;
                ddDefaultTestLoggingServer.SelectedItem = null;

            }
            else
            if(sender == btnClearLoggingServer)
            {
                toClear = ServerDefaults.PermissableDefaults.LiveLoggingServer_ID;
                ddDefaultLoggingServer.SelectedItem = null;
            }
            else
            if(sender == btnClearIdentifierDump)
            {
                toClear = ServerDefaults.PermissableDefaults.IdentifierDumpServer_ID;
                ddDefaultIdentifierDump.SelectedItem = null;
            }
            else if (sender == btnClearDQEServer)
            {
                toClear = ServerDefaults.PermissableDefaults.DQE;
                ddDQEServer.SelectedItem = null;

            }
            else if (sender == btnClearRAWServer)
            {
                toClear = ServerDefaults.PermissableDefaults.RAWDataLoadServer;
                ddOverrideRawServer.SelectedItem = null;
            }
            else if (sender == btnClearANOStore)
            {
                toClear = ServerDefaults.PermissableDefaults.ANOStore;
                ddDefaultANOStore.SelectedItem = null;
            }
            else if (sender == btnClearWebServiceQueryCache)
            {
                toClear = ServerDefaults.PermissableDefaults.WebServiceQueryCachingServer_ID;
                ddWebServiceQueryCacheServer.SelectedItem = null;
            }
            else if (sender == btnClearCohortIdentificationQueryCache)
            {
                toClear = ServerDefaults.PermissableDefaults.CohortIdentificationQueryCachingServer_ID;
                ddCohortIdentificationQueryCacheServer.SelectedItem = null;
            }
            else
                throw new Exception("Did not recognise sender:" + sender);

            defaults.ClearDefault(toClear);
            RefreshUIFromDatabase();
        }
        
        private void CreateNewExternalServer(ServerDefaults.PermissableDefaults defaultToSet, Assembly databaseAssembly)
        {

            if(CreatePlatformDatabase.CreateNewExternalServer(RepositoryLocator.CatalogueRepository,defaultToSet, databaseAssembly) != null)
                RefreshUIFromDatabase();
        }


        private void ddDQEServer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(ddDQEServer.SelectedItem != null)
                defaults.SetDefault(ServerDefaults.PermissableDefaults.DQE, (ExternalDatabaseServer) ddDQEServer.SelectedItem);
        }

        private void btnCreateNewDQEServer_Click(object sender, EventArgs e)
        {
            CreateNewExternalServer(ServerDefaults.PermissableDefaults.DQE, typeof(DataQualityEngine.Database.Class1).Assembly);
        }
        private void btnCreateNewWebServiceQueryCache_Click(object sender, EventArgs e)
        {
            CreateNewExternalServer(ServerDefaults.PermissableDefaults.WebServiceQueryCachingServer_ID, typeof(QueryCaching.Database.Class1).Assembly);
        }

        private void btnCreateNewLoggingServer_Click(object sender, EventArgs e)
        {
            CreateNewExternalServer(ServerDefaults.PermissableDefaults.LiveLoggingServer_ID, typeof(HIC.Logging.Database.Class1).Assembly);
        }

        private void btnCreateNewTestLoggingServer_Click(object sender, EventArgs e)
        {
            CreateNewExternalServer(ServerDefaults.PermissableDefaults.TestLoggingServer_ID, typeof(HIC.Logging.Database.Class1).Assembly);
        }

        private void btnCreateNewIdentifierDump_Click(object sender, EventArgs e)
        {
            CreateNewExternalServer(ServerDefaults.PermissableDefaults.IdentifierDumpServer_ID, typeof(IdentifierDump.Database.Class1).Assembly);
        }

        private void btnCreateNewANOStore_Click(object sender, EventArgs e)
        {
            CreateNewExternalServer(ServerDefaults.PermissableDefaults.ANOStore, typeof(ANOStore.Database.Class1).Assembly);
        }

        private void btnCreateNewCohortIdentificationQueryCache_Click(object sender, EventArgs e)
        {
            CreateNewExternalServer(ServerDefaults.PermissableDefaults.CohortIdentificationQueryCachingServer_ID, typeof(QueryCaching.Database.Class1).Assembly);
        }

        private void ddSetKnownType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(bloading)
                return;
            
            if(ExternalDatabaseServer != null)
            {
                ExternalDatabaseServer.CreatedByAssembly = ddSetKnownType.SelectedItem as string;
                btnSaveChanges_Click(null,null);

                
            }
        }

        private void btnClearKnownType_Click(object sender, EventArgs e)
        {
            if (ExternalDatabaseServer != null)
            {
                ExternalDatabaseServer.CreatedByAssembly = null;
                ddSetKnownType.SelectedItem = null;
                ddSetKnownType.Text = null;
            }
        }

        private void ddSetKnownType_Leave(object sender, EventArgs e)
        {
            if (ExternalDatabaseServer != null)
            {
                ExternalDatabaseServer.CreatedByAssembly = ddSetKnownType.Text;
            }
        }




    }
}