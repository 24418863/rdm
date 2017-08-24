﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Forms;
using ReusableLibraryCode;
using ReusableLibraryCode.DatabaseHelpers;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.DatabaseHelpers.Discovery.Microsoft;

namespace ReusableUIComponents
{
    public delegate void IntegratedSecurityUseChangedHandler(bool use);


    /// <summary>
    /// Lets you select a server database or table.  Includes auto population of database/table lists.  This is a reusable component.
    /// 
    /// </summary>
    public partial class ServerDatabaseTableSelector : UserControl
    {
        private bool _allowTableValuedFunctionSelection;

        public string Server
        {
            get { return cbxServer.Text; }
            set { cbxServer.Text = value; }
        }

        public string Database
        {
            get { return cbxDatabase.Text; }
            set { cbxDatabase.Text = value; }
        }

        public string Table
        {
            get { return cbxTable.Text; }
            set { cbxTable.Text = value; }
        }

        public string Username
        {
            get { return tbUsername.Text; }
            set { tbUsername.Text = value; }
        }

        public string Password
        {
            get { return tbPassword.Text; }
            set { tbPassword.Text = value; }
        }

        public string TableValuedFunction {get { return cbxTableValueFunctions.Text; }}

        public event Action SelectionChanged;
        private IDiscoveredServerHelper _helper;

        private BackgroundWorker _workerRefreshDatabases = new BackgroundWorker();
        CancellationTokenSource _workerRefreshDatabasesToken;
        private string[] _listDatabasesAsyncResult;

        private BackgroundWorker _workerRefreshTables = new BackgroundWorker();
        CancellationTokenSource _workerRefreshTablesToken;
        private List<DiscoveredTable> _listTablesAsyncResult;

        //constructor
        public ServerDatabaseTableSelector()
        {
            InitializeComponent();
            ddDatabaseType.DataSource = Enum.GetValues(typeof (DatabaseType));

            _workerRefreshDatabases.DoWork += UpdateDatabaseListAsync;
            _workerRefreshDatabases.WorkerSupportsCancellation = true;
            _workerRefreshDatabases.RunWorkerCompleted+=UpdateDatabaseAsyncCompleted;

            _workerRefreshTables.DoWork += UpdateTablesListAsync;
            _workerRefreshTables.WorkerSupportsCancellation = true;
            _workerRefreshTables.RunWorkerCompleted += UpdateTablesAsyncCompleted;

        }
        
        #region Async Stuff

        private void UpdateTablesListAsync(object sender, DoWorkEventArgs e)
        {
            var builder = (DbConnectionStringBuilder)((object[])e.Argument)[0];
            var database = (string)((object[])e.Argument)[1];

            var discoveredDatabase = new DiscoveredServer(builder).ExpectDatabase(database);
            IDiscoveredDatabaseHelper databaseHelper = discoveredDatabase.Helper;

            _workerRefreshTablesToken = new CancellationTokenSource();

            var syntaxHelper = new MicrosoftQuerySyntaxHelper();
            try
            {
                var con = discoveredDatabase.Server.GetConnection();
                var openTask = con.OpenAsync(_workerRefreshTablesToken.Token);
                openTask.Wait(_workerRefreshTablesToken.Token);
                
                List<DiscoveredTable> result = new List<DiscoveredTable>();

                result.AddRange(databaseHelper.ListTables(discoveredDatabase, syntaxHelper, con, database, true));
                result.AddRange(databaseHelper.ListTableValuedFunctions(discoveredDatabase, syntaxHelper, con, database));

                _listTablesAsyncResult = result;
            }
            catch (OperationCanceledException)//user cancels
            {
                _listTablesAsyncResult = new List<DiscoveredTable>();
            }
        }

        private void UpdateTablesAsyncCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            //success?
            ragSmiley1.Reset();

            if (e.Error != null)
                ragSmiley1.Fatal(e.Error);
            else
                if (!e.Cancelled)
                {
                    cbxTable.Items.AddRange(_listTablesAsyncResult.Where(t => ! (t is DiscoveredTableValuedFunction)).Select(r=>r.GetRuntimeName()).ToArray());
                    cbxTableValueFunctions.Items.AddRange(_listTablesAsyncResult.Where(t => t is DiscoveredTableValuedFunction).Select(r=>r.GetRuntimeName()).ToArray());
                }
                else
                    ragSmiley1.Warning(new Exception("User Cancelled"));
                

            SetLoading(false);

            cbxTable.Focus();
        }

        


        //do work
        private void UpdateDatabaseListAsync(object sender, DoWorkEventArgs e)
        {
            var builder = (DbConnectionStringBuilder)((object[])e.Argument)[0];
            
            _workerRefreshDatabasesToken = new CancellationTokenSource();
            try
            {
                _listDatabasesAsyncResult = _helper.ListDatabasesAsync(builder, _workerRefreshDatabasesToken.Token);
            }
            catch (OperationCanceledException )//user cancels
            {
                _listDatabasesAsyncResult = new string[0];
            }
        }

        //handle complete
        private void UpdateDatabaseAsyncCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //success?
            ragSmiley1.Reset();

            if (e.Error != null)
                ragSmiley1.Fatal(e.Error);
            else if (!e.Cancelled)
                cbxDatabase.Items.AddRange(_listDatabasesAsyncResult);
            else
                ragSmiley1.Warning(new Exception("User Cancelled"));

            SetLoading(false);
            cbxDatabase.Focus();
        }
        
        //aborting
        private void AbortWorkers()
        {
            if (_workerRefreshDatabases.IsBusy)
            {
                _workerRefreshDatabases.CancelAsync();
                if(_workerRefreshDatabasesToken != null)
                    _workerRefreshDatabasesToken.Cancel();
            }

            if (_workerRefreshTables.IsBusy)
            {
                _workerRefreshTables.CancelAsync();
                if(_workerRefreshTablesToken != null)
                    _workerRefreshTablesToken.Cancel();
            }
        }
        #endregion

        public void SetDefaultServers(string[] defaultServers)
        {
            cbxServer.Items.AddRange(defaultServers);
        }

        public bool AllowTableValuedFunctionSelection
        {
            get { return _allowTableValuedFunctionSelection; }
            set
            {
                _allowTableValuedFunctionSelection = value;
                
                lblOr.Visible = value;
                lblTableValuedFunction.Visible = value;
                cbxTableValueFunctions.Visible = value;
            }
        }

        public DatabaseType DatabaseType {
            get { return (DatabaseType) ddDatabaseType.SelectedValue; }
        }

        public DiscoveredServer Result { get { return new DiscoveredServer(GetBuilder()); } }

        private void cbxServer_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if they have not selected anything selected something
            if (string.IsNullOrWhiteSpace(cbxServer.Text))
                return;

            UpdateDatabaseList();
        }

        private void cbxDatabase_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTableList();
        }

        private bool _clearingTable = false;
        private void cbxTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            //dont clear both!
            if (_clearingTable)
                return;
            _clearingTable = true;

            cbxTableValueFunctions.Text = null;
            if (SelectionChanged != null)
                SelectionChanged();

            _clearingTable = false;
        }

        private void cbxTableValueFunctions_SelectedIndexChanged(object sender, EventArgs e)
        {
            //dont clear both!
            if (_clearingTable)
                return;

            _clearingTable = true;
            cbxTable.Text = null;
            if (SelectionChanged != null)
                SelectionChanged();

            _clearingTable = false;
        }

        public void HideTableComponents()
        {
            AllowTableValuedFunctionSelection = false;
            cbxTable.Visible = false;
            lblTable.Visible = false;
            btnRefreshTables.Visible = false;
        }

        private void cbxServer_Leave(object sender, EventArgs e)
        {
            UpdateDatabaseList();
        }

        public string GetTableNameFullyQualified()
        {
            return SqlSyntaxHelper.EnsureFullyQualifiedMicrosoftSQL(Database, Table);
        }

        private void UpdateTableList()
        {
            if (string.IsNullOrWhiteSpace(cbxServer.Text) || string.IsNullOrWhiteSpace(cbxDatabase.Text))
                return;

            if (SelectionChanged != null)
                SelectionChanged();

            AbortWorkers();

            cbxTable.Items.Clear();
            cbxTableValueFunctions.Items.Clear();

            SetLoading(true);

            if (!_workerRefreshTables.IsBusy)
                _workerRefreshTables.RunWorkerAsync(new object[]
                    {
                        GetBuilder(),
                        cbxDatabase.Text
                    });
        }


        private void UpdateDatabaseList()
        {
            if (string.IsNullOrWhiteSpace(cbxServer.Text) || _helper == null)
                return;

            AbortWorkers();
            cbxDatabase.Items.Clear();

            SetLoading(true);

            if (SelectionChanged != null)
                SelectionChanged();


            if(!_workerRefreshDatabases.IsBusy)
                _workerRefreshDatabases.RunWorkerAsync(new object[]
                    {
                        GetBuilder()
                    });
        }

        private void SetLoading(bool isLoading)
        {

            llLoading.Visible = isLoading;
            pbLoading.Visible = isLoading;

            cbxServer.Enabled = !isLoading;
            cbxDatabase.Enabled = !isLoading;
            ddDatabaseType.Enabled = !isLoading;
            btnRefreshDatabases.Enabled = !isLoading;
            btnRefreshTables.Enabled = !isLoading;

        }

        public event IntegratedSecurityUseChangedHandler IntegratedSecurityUseChanged;

        private string oldUsername = null;

        private void tbUsername_TextChanged(object sender, EventArgs e)
        {
            //if nobody is listening who cares
            if (IntegratedSecurityUseChanged == null)
                return;

            //if the last value they typed was blank and the new value is not blank
            if (string.IsNullOrWhiteSpace(oldUsername) && !string.IsNullOrWhiteSpace(Username))
                IntegratedSecurityUseChanged(false);

            //if the last value they typed was NOT blank and it is now blank
            if (!string.IsNullOrWhiteSpace(oldUsername) && string.IsNullOrWhiteSpace(Username))
                IntegratedSecurityUseChanged(true);

            oldUsername = tbUsername.Text;

        }

        public DiscoveredDatabase GetDiscoveredDatabase()
        {
            if (string.IsNullOrWhiteSpace(cbxServer.Text))
                return null;

            if (string.IsNullOrWhiteSpace(cbxDatabase.Text))
                return null;

            return new DiscoveredServer(GetBuilder()).ExpectDatabase(cbxDatabase.Text);
        }

        public DbConnectionStringBuilder GetBuilder()
        {
            return _helper.GetConnectionStringBuilder(cbxServer.Text, cbxDatabase.Text, tbUsername.Text, tbPassword.Text);
        }

        private void ddDatabaseType_SelectedIndexChanged(object sender, EventArgs e)
        {
            _helper = new DatabaseHelperFactory(DatabaseType).CreateInstance();
            UpdateDatabaseList();
        }
        
        private void llLoading_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            AbortWorkers();
        }

        private void btnRefreshDatabases_Click(object sender, EventArgs e)
        {
            cbxDatabase.Text = "";
            UpdateDatabaseList();
        }

        private void btnRefreshTables_Click(object sender, EventArgs e)
        {
            UpdateTableList();
        }

        public void SetExplicitServer(string serverName)
        {
            cbxServer.Text = serverName;
            UpdateDatabaseList();
        }

        public void SetExplicitDatabase(string serverName, string databaseName)
        {
            cbxServer.Text = serverName;
            UpdateDatabaseList();
            cbxDatabase.Text = databaseName;
        }

        private void cbxDatabase_TextChanged(object sender, EventArgs e)
        {
            if (SelectionChanged != null)
                SelectionChanged();
        }
    }
}