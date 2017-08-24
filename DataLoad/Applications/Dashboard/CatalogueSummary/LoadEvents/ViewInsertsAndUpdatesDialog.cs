﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueLibrary.Triggers;
using HIC.Logging.PastEvents;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableUIComponents;
using ReusableUIComponents.ChecksUI;
using ReusableUIComponents.SqlDialogs;


namespace Dashboard.CatalogueSummary.LoadEvents
{
    /// <summary>
    /// Allows you to view a sample of the data in your dataset before and after a data load.  This includes a sample of the new records added and a side by side comparison of the 
    /// changes (See DiffDataTables).  Depending on your indexes and the volume of your data it might take some time to execute the sample fetching query.
    /// 
    /// Generate the preview by entering an appropriate timeout (e.g. 120 seconds) and selecting 'Try To Fetch Appropriate Data'.  This will show you the SQL that the system is about
    /// to run so that you can (if you want/need to) run the code in Sql Management Studio with the query analyser on which might suggest indexes to help with performance problems.
    /// 
    /// Once the queries have finished executing (you will see progress messages appearing in the 'Fetch Data' tab), INSERTS that were part of the data load will appear in 'View Inserts' 
    /// tab and side by side views of UPDATES (old vs new) will appear in the 'View Updates' tab (see DiffDataTables).
    /// </summary>
    public partial class ViewInsertsAndUpdatesDialog : Form, ICheckNotifier
    {
        
        private int _dataLoadRunID;
        private TableInfo _toInterrogate;
        private int _batchSizeToGet;
        private int _timeout;

        public ViewInsertsAndUpdatesDialog(ArchivalTableLoadInfo toAttemptToDisplay, List<TableInfo> potentialTableInfos)
        {
            
            InitializeComponent();

            if (toAttemptToDisplay == null)
                return;
            
            _dataLoadRunID = toAttemptToDisplay.Parent.ID;
            _toInterrogate = GetTableInfoFromConstructorArguments(toAttemptToDisplay, potentialTableInfos,checksUI1);
            tbBatchSizeToGet.Text = "20";
            _batchSizeToGet = 20;//technically redundant since text changed will fire but nvm
            tbTimeout.Text = "30";
            _timeout = 30;
        }


        private void tbBatchSizeToGet_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _batchSizeToGet = int.Parse(tbBatchSizeToGet.Text);
                tbBatchSizeToGet.ForeColor = Color.Black;
            }
            catch (Exception)
            {
                tbBatchSizeToGet.ForeColor = Color.Red;
            }
        }
        
        private void tbTimeout_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _timeout = int.Parse(tbTimeout.Text);
                tbTimeout.ForeColor = Color.Black;
            }
            catch (Exception)
            {
                tbTimeout.ForeColor = Color.Red;
            }
        }

        private bool fetchDataResultedInNoErrors = true;

        private void btnFetchData_Click(object sender, EventArgs e)
        {
            fetchDataResultedInNoErrors = true;
            var fetcher = new DiffDatabaseDataFetcher(_batchSizeToGet, _toInterrogate, _dataLoadRunID, _timeout);
            
            fetcher.FetchData(this);

            dgInserts.DataSource = fetcher.Inserts;

            if (fetcher.Updates_New == null || fetcher.Updates_Replaced == null)
                diffDataTables1.Clear();
            else
                diffDataTables1.PopulateWith(fetcher.Updates_New,fetcher.Updates_Replaced);

            //if user is already viewing the updates we will need to re-highlight
            if (tabControl1.SelectedTab == tpViewUpdates)
                diffDataTables1.HighlightDiffCells();

            //we didn't see any errors so probably everything was fine
            if(fetchDataResultedInNoErrors)
                WideMessageBox.Show("Data ready for you to view in the Inserts / Updates tabs");

        }


        private TableInfo GetTableInfoFromConstructorArguments(ArchivalTableLoadInfo toAttemptToDisplay, List<TableInfo> potentialTableInfos, ICheckNotifier checkNotifier)
        {
            checkNotifier.OnCheckPerformed(new CheckEventArgs("Table user is attempting to view updates/inserts for is called " + toAttemptToDisplay.TargetTable, CheckResult.Success));

            string runtimeName = SqlSyntaxHelper.GetRuntimeName(toAttemptToDisplay.TargetTable);

            checkNotifier.OnCheckPerformed(new CheckEventArgs("The runtime name of the table is " + runtimeName, CheckResult.Success));

            checkNotifier.OnCheckPerformed(
                new CheckEventArgs(
                    "The following TableInfos were given to us to pick from " +
                    string.Join(",", potentialTableInfos), CheckResult.Success));

            var candidates = potentialTableInfos.Where(t => t.GetRuntimeName().Equals(runtimeName)).ToArray();

            if (!candidates.Any())
            {
                checkNotifier.OnCheckPerformed(new CheckEventArgs("Could not find an appropriate TableInfo from those mentioned above", CheckResult.Fail));
                return null;
            }

            if (candidates.Count() > 1)
            {
                checkNotifier.OnCheckPerformed(new CheckEventArgs("Found multiple TableInfos (mentioned above) with the runtime name " + runtimeName + " I don't know which one you want to view", CheckResult.Fail));
                return null;
            }

            checkNotifier.OnCheckPerformed(new CheckEventArgs("Found the correct TableInfo!", CheckResult.Success));

            return candidates.Single();
        }

        private bool _yesToAll = false;

        public bool OnCheckPerformed(CheckEventArgs args)
        {
            //if it has proposed fix it will be an SQL select query - probably
            if (args.ProposedFix != null)
            {
                if (_yesToAll)
                    return true;

                //we will handle this one instead of the checksui
                SQLPreviewWindow window = new SQLPreviewWindow("Send Query?","The following query is about to be submitted:",args.ProposedFix);
                try
                {
                    return window.ShowDialog() == DialogResult.OK;
                }
                finally
                {
                    _yesToAll = window.YesToAll;
                }
            }

            if (args.Result == CheckResult.Fail)
                fetchDataResultedInNoErrors = false;//don't tell them everything was fine because it wasn't

            return checksUI1.OnCheckPerformed(args);
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(tabControl1.SelectedTab == tpViewUpdates)
                diffDataTables1.HighlightDiffCells();
        }

    }
}