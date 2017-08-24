using System;
using System.Collections.Generic;
using System.Linq;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using DataLoadEngine.DatabaseManagement;
using DataLoadEngine.DatabaseManagement.EntityNaming;
using DataLoadEngine.Migration;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DatabaseHelpers.Discovery;

namespace DataLoadEngine.Checks.Checkers
{
    public class PreExecutionChecker :  ICheckable
    {
        private readonly ILoadMetadata _loadMetadata;
        private readonly IList<ICatalogue> _cataloguesToLoad;
        private readonly HICDatabaseConfiguration _databaseConfiguration;
        
        public PreExecutionChecker(ILoadMetadata loadMetadata, HICDatabaseConfiguration overrideDatabaseConfiguration) 
        {
            _loadMetadata = loadMetadata;
            _databaseConfiguration = overrideDatabaseConfiguration ?? new HICDatabaseConfiguration(loadMetadata);
            _cataloguesToLoad = loadMetadata.GetAllCatalogues().ToList();
        }

        private void PreExecutionStagingDatabaseCheck(bool skipLookups)
        {
            var allTableInfos = _cataloguesToLoad.SelectMany(catalogue => catalogue.GetTableInfoList(!skipLookups)).Distinct().ToList();
            CheckDatabaseExistsForStage(LoadBubble.Staging, "STAGING database found", "STAGING database not found");

            if (_databaseConfiguration.RequiresStagingTableCreation)
            {
                CheckTablesDoNotExistOnStaging(allTableInfos);
            }
            else
            {
                CheckTablesAreEmptyInDatabaseOnServer();
                CheckColumnInfosMatchWithWhatIsInDatabaseAtStage(allTableInfos, LoadBubble.Staging);
                CheckStandardColumnsArePresentInStaging(allTableInfos);
            }
        }

        private void CheckDatabaseExistsForStage(LoadBubble deploymentStage, string successMessage, string failureMessage)
        {
            var dbInfo = _databaseConfiguration.DeployInfo[deploymentStage];

            if (!dbInfo.Exists())
            {
                var createDatabase = _notifier.OnCheckPerformed(new CheckEventArgs(failureMessage + ": " + dbInfo, CheckResult.Fail, null, "Create " + dbInfo.GetRuntimeName() + " on " + dbInfo.Server.Name));

                
                if (createDatabase)
                    dbInfo.Server.CreateDatabase(dbInfo.GetRuntimeName());
            }
            else
                _notifier.OnCheckPerformed(new CheckEventArgs(successMessage + ": " + dbInfo, CheckResult.Success, null));
        }

        private void CheckTablesDoNotExistOnStaging(IEnumerable<TableInfo> allTableInfos)
        {
            var stagingDbInfo = _databaseConfiguration.DeployInfo[LoadBubble.Staging];
            var alreadyExistingTableInfosThatShouldntBeThere = new List<string>();

            var tableNames = allTableInfos.Select(info => info.GetRuntimeNameFor(_databaseConfiguration.DatabaseNamer, LoadBubble.Staging));
            foreach (var tableName in tableNames)
            {
                if (DatabaseOperations.CheckTableExists(tableName, stagingDbInfo))
                    alreadyExistingTableInfosThatShouldntBeThere.Add(tableName);
            }

            if (alreadyExistingTableInfosThatShouldntBeThere.Any())
            {
                bool nukeTables;

                nukeTables = _notifier.OnCheckPerformed(new CheckEventArgs(
                        "The following tables: '" +
                        alreadyExistingTableInfosThatShouldntBeThere.Aggregate("", (s, n) => s + n + ",") +
                        "' exists in the Staging database (" + stagingDbInfo.GetRuntimeName() +
                        ") but the database load configuration requires that tables are created during the load process",
                        CheckResult.Fail, null, "Drop the tables"));

                if (nukeTables)
                    DatabaseOperations.RemoveTablesFromDatabase(alreadyExistingTableInfosThatShouldntBeThere,
                        stagingDbInfo);
            }
            else
                _notifier.OnCheckPerformed(new CheckEventArgs("Staging table is clear", CheckResult.Success, null));
        }

        private void CheckTablesAreEmptyInDatabaseOnServer()
        {
            var stagingDbInfo = _databaseConfiguration.DeployInfo[LoadBubble.Staging];
            if (!DatabaseOperations.CheckTablesAreEmptyInDatabaseOnServer(stagingDbInfo))
                _notifier.OnCheckPerformed(new CheckEventArgs("Staging database '" + stagingDbInfo.GetRuntimeName() + "' is not empty on " + stagingDbInfo.Server.Name, CheckResult.Fail, null));
            else
                _notifier.OnCheckPerformed(new CheckEventArgs("Staging database is empty (" + stagingDbInfo + ")", CheckResult.Success, null));
        }

        // Check that the column infos from the catalogue match up with what is actually in the staging databases
        private void CheckColumnInfosMatchWithWhatIsInDatabaseAtStage(IEnumerable<TableInfo> allTableInfos, LoadBubble deploymentStage)
        {
            var dbInfo = _databaseConfiguration.DeployInfo[deploymentStage];
            foreach (var tableInfo in allTableInfos)
            {
                var columnNames = tableInfo.ColumnInfos.Select(info => info.GetRuntimeName()).ToList();

                if (!columnNames.Any())
                    _notifier.OnCheckPerformed(new CheckEventArgs("Table '" + tableInfo.GetRuntimeName() + "' has no ColumnInfos", CheckResult.Fail, null));


                if(deploymentStage == LoadBubble.Live)
                {
                
                    TableInfoSynchronizer sync = new TableInfoSynchronizer(tableInfo);
                    bool isSynched = sync.Synchronize(_notifier);
                    
                
                    if(isSynched)
                        _notifier.OnCheckPerformed(new CheckEventArgs("Live table  " + tableInfo.GetRuntimeName() + " is synchronized with the Catalogue TableInfos", CheckResult.Success, null));
                    else
                        _notifier.OnCheckPerformed(new CheckEventArgs("Live table  " + tableInfo.GetRuntimeName() + " is not synchronized with the Catalogue TableInfos", CheckResult.Fail, null));
                    
                }
                else
                {
                    string tableName = tableInfo.GetRuntimeNameFor(_databaseConfiguration.DatabaseNamer, deploymentStage);
                    var table = dbInfo.ExpectTable(tableName);
                    
                    if(!table.Exists())
                        throw new Exception("PreExecutionChecker spotted that table does not exist:" + table + " it was about to check whether the TableInfo matched the columns or not");
                }

                
            }
        }

        private void CheckStandardColumnsArePresentInStaging(IEnumerable<TableInfo> allTableInfos)
        {
            // check standard columns are present in staging database
            var standardColumnNames = new List<string>();
            CheckStandardColumnsArePresentForStage(allTableInfos, standardColumnNames, LoadBubble.Staging, LoadBubble.Staging);
        }
        private void CheckStandardColumnsArePresentInLive(IEnumerable<TableInfo> allTableInfos)
        {
            // check standard columns are present in live database
            var standardColumnNames = MigrationColumnSet.GetStandardColumnNames();
            CheckStandardColumnsArePresentForStage(allTableInfos, standardColumnNames, LoadBubble.Live, LoadBubble.Live);
        }

        private void CheckStandardColumnsArePresentForStage(IEnumerable<TableInfo> allTableInfos, List<string> columnNames, LoadBubble deploymentStage, LoadBubble tableNamingConvention)
        {
            var dbInfo = _databaseConfiguration.DeployInfo[LoadBubble.Live];
            foreach (var tableInfo in allTableInfos)
            {
                var tableName = tableInfo.GetRuntimeNameFor(_databaseConfiguration.DatabaseNamer, tableNamingConvention);
                try
                {
                    DatabaseOperations.CheckTableContainsColumns(dbInfo, tableName, columnNames);
                }
                catch (Exception e)
                {
                    _notifier.OnCheckPerformed(new CheckEventArgs("Standard columns (" + string.Join(",", columnNames) + ") not included in database structure for table '" + tableName + "'", CheckResult.Fail, e));
                }
            }

            _notifier.OnCheckPerformed(new CheckEventArgs(deploymentStage + " database '" + dbInfo + "' is correctly configured", CheckResult.Success, null));
        }

        private void PreExecutionDatabaseCheck()
        {
            var allNonLookups = _cataloguesToLoad.SelectMany(catalogue => catalogue.GetTableInfoList(false)).Distinct().ToList();
            CheckDatabaseExistsForStage(LoadBubble.Live, "LIVE database found", "LIVE database not found");
            CheckColumnInfosMatchWithWhatIsInDatabaseAtStage(allNonLookups, LoadBubble.Live);
            
            CheckStandardColumnsArePresentInLive(allNonLookups);
            CheckUpdateTriggers(allNonLookups);
            CheckRAWDatabaseIsNotPresent();
        }

        private void CheckRAWDatabaseIsNotPresent()
        {
            var rawDbInfo = _databaseConfiguration.DeployInfo[LoadBubble.Raw];

            // Check that the raw database is not present
            if (!rawDbInfo.Exists()) return;

            var shouldDrop = _notifier.OnCheckPerformed(new CheckEventArgs("RAW database '" + rawDbInfo + "' exists", CheckResult.Fail, null, "Drop database " + rawDbInfo));
            
            if(!rawDbInfo.GetRuntimeName().EndsWith("_RAW"))
                throw new Exception("rawDbInfo database name did not end with _RAW! It was:" + rawDbInfo.GetRuntimeName()+ " (Why is the system trying to drop this database?)");
            if (shouldDrop)
            {
                foreach (DiscoveredTable t in rawDbInfo.DiscoverTables(true))
                {
                    _notifier.OnCheckPerformed(new CheckEventArgs("Dropping table " + t.GetFullyQualifiedName() + "...", CheckResult.Success));
                    t.Drop();
                }

                _notifier.OnCheckPerformed(new CheckEventArgs("Finally dropping database" + rawDbInfo + "...", CheckResult.Success));
                rawDbInfo.Drop();
            }
        }

        private void CheckUpdateTriggers(IEnumerable<TableInfo> allTableInfos)
        {
            // Check that the update triggers are present/enabled
            foreach (var tableInfo in allTableInfos)
            {
                TriggerChecks checker = new TriggerChecks(_databaseConfiguration.DeployInfo[LoadBubble.Live], tableInfo.GetRuntimeName(), true, null);
                checker.Check(_notifier);
            }
        }

 
        private ICheckNotifier _notifier;

 
        public void Check(ICheckNotifier notifier)
        {
            //extra super not threadsafe eh?
            _notifier = notifier;

            AtLeastOneTaskCheck();

            PreExecutionStagingDatabaseCheck(false);
            PreExecutionDatabaseCheck();
            
        }

        private void AtLeastOneTaskCheck()
        {
            if (!_loadMetadata.ProcessTasks.Any())
                _notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "There are no ProcessTasks defined for '" + _loadMetadata +
                        "'",
                        CheckResult.Fail));
        }
    }
      
}