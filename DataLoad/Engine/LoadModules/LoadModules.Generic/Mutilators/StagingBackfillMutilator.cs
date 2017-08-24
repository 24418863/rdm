using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Linq;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.Data.EntityNaming;
using CatalogueLibrary.Repositories;
using LoadModules.Generic.Mutilators.QueryBuilders;
using DataLoadEngine.Migration;
using DataLoadEngine.Mutilators;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DataAccess;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.Progress;

namespace LoadModules.Generic.Mutilators
{
    public class StagingBackfillMutilator : IPluginMutilateDataTables
    {
        private DiscoveredDatabase _dbInfo;
        private TableInfo _tiWithTimeColumn;
        private BackfillSqlHelper _sqlHelper;
        private MigrationConfiguration _migrationConfiguration;
        
        // Only a test runner can set this
        public bool TestContext { get; set; }

        [DemandsInitialization("Time periodicity field", Mandatory = true)]
        public ColumnInfo TimePeriodicityField { get; set; }

        // Currently hardcode this as the TableNamingScheme when not in a test context: it is hardcoded in the load and ITableNamingScheme is not a supported process argument.
        //[DemandsInitialization("The class name of the ITableNamingScheme used for this load. This should really come from the load itself, as we don't want a different class from the rest of the load process being chosen here but that will require some design changes.")]
        public INameDatabasesAndTablesDuringLoads TableNamingScheme { get; set; }

        public ExitCodeType Mutilate(IDataLoadEventListener listener)
        {
            if (TimePeriodicityField == null)
                throw new InvalidOperationException("TimePeriodicityField has not been set.");

            var liveDatabaseInfo = GetLiveDatabaseInfo();

            if (TestContext)
            {
                // If we are operating inside a test, the client is responsible for providing a TableNamingScheme
                if (TableNamingScheme == null)
                    throw new InvalidOperationException("Executing within test context but no TableNamingScheme has been provided");
            }
            else
                // If we are not operating inside a Test, hardwire the TableNamingScheme
                TableNamingScheme = new FixedStagingDatabaseNamer(liveDatabaseInfo.GetRuntimeName());

            // create invariant helpers
            _sqlHelper = new BackfillSqlHelper(TimePeriodicityField, _dbInfo, liveDatabaseInfo);
            _migrationConfiguration = new MigrationConfiguration(liveDatabaseInfo, LoadBubble.Live, LoadBubble.Staging, TableNamingScheme);
            
            // starting with the TimePeriodicity table, we descend the join relationships to the leaf tables then ascend back up to the TimePeriodicity table
            // at each step we determine the effective date of the record by joining back to the TimePeriodicity table
            // this allows us to remove updates that are older than the corresponding record in live
            // - however we don't remove rows that still have children, hence the recursion from leaves upwards
            // -- a record may be an 'old update', but have a child for insertion (i.e. the child is not in live), in this case the parent must remain in staging despite it being 'old'
            // - 'old updates' that are not deleted (because they have new descendants) must have their own data updated to reflect what is in live if there is a difference between the two, otherwise we may overwrite live with stale data
            //
            _tiWithTimeColumn = TimePeriodicityField.TableInfo;
            ProcessOldUpdatesInTable(_tiWithTimeColumn, new List<JoinInfo>());

            // Having processed all descendants of the TimePeriodicity table, we now recursively ascend back up through its predecessors to the top of the join tree
            // Doing effectively the same thing, removing items that are older than the corresponding live items that do not also have new descendants and updating staging rows with live data where required.
            ProcessPredecessors(_tiWithTimeColumn, new List<JoinInfo>());

            return ExitCodeType.Success;
        }

        /// <summary>
        /// Get the database credentials for the Live server, accessing them via the TimePeriodicityField ColumnInfo
        /// </summary>
        /// <returns></returns>
        private DiscoveredDatabase GetLiveDatabaseInfo()
        {
            var timePeriodicityTable = TimePeriodicityField.TableInfo;
            return DataAccessPortal.GetInstance().ExpectDatabase(timePeriodicityTable, DataAccessContext.DataLoad);
        }

        /// <summary>
        /// Ascends join tree from the TimePeriodicity table, processing tables at each step
        /// </summary>
        /// <param name="tiCurrent"></param>
        /// <param name="joinPathToTimeTable"></param>
        private void ProcessPredecessors(TableInfo tiCurrent, List<JoinInfo> joinPathToTimeTable)
        {
            var repository = (CatalogueRepository) tiCurrent.Repository;

            // Find all parents of this table
            var allJoinInfos = repository.JoinInfoFinder.GetAllJoinInfos();
            var joinsWithThisTableAsChild = allJoinInfos.Where(info => info.ForeignKey.TableInfo_ID == tiCurrent.ID).ToList();
            
            // Infinite recursion check
            var seenBefore = joinPathToTimeTable.Intersect(joinsWithThisTableAsChild).ToList();
            if (seenBefore.Any())
                throw new InvalidOperationException("Join loop: I've seen join(s) " + string.Join(",", seenBefore.Select(j => j.PrimaryKey + " -> " + j.ForeignKey)) + " before so we must have hit a loop (and will never complete the recursion).");

            // Process this table and its children (we need info about the children in order to join and detect childless rows)
            var joinsWithThisTableAsParent = allJoinInfos.Where(info => info.PrimaryKey.TableInfo_ID == tiCurrent.ID).ToList();
            ProcessTable(tiCurrent, joinPathToTimeTable, joinsWithThisTableAsParent);
            
            // Ascend into parent tables once this table has been processed
            foreach (var join in joinsWithThisTableAsChild)
            {
                var tiParent = join.PrimaryKey.TableInfo;

                // We may have a stale ID, some other pass may have deleted the table through a different path of JoinInfos
                if (tiParent == null)
                    continue;

                ProcessPredecessors(tiParent, new List<JoinInfo>(joinPathToTimeTable){join});
            }
            
        }

        /// <summary>
        /// Descends to leaves of join tree, then processes tables on way back up
        /// </summary>
        /// <param name="tiCurrent"></param>
        /// <param name="joinPathToTimeTable"></param>
        private void ProcessOldUpdatesInTable(TableInfo tiCurrent, List<JoinInfo> joinPathToTimeTable)
        {
            var repository = (CatalogueRepository)tiCurrent.Repository;

            // Process old updates in children first
            // Does toCurrent have any children?
            var allJoinInfos = repository.JoinInfoFinder.GetAllJoinInfos();
            var joinsToProcess = allJoinInfos.Where(info => info.PrimaryKey.TableInfo_ID == tiCurrent.ID).ToList();
            foreach (var join in joinsToProcess)
            {
                var tiChild = join.ForeignKey.TableInfo;
                ProcessOldUpdatesInTable(tiChild, new List<JoinInfo>(joinPathToTimeTable){join});
            }

            ProcessTable(tiCurrent, joinPathToTimeTable, joinsToProcess);
        }

        /// <summary>
        /// Deletes any rows in tiCurrent that are out-of-date (with respect to live) and childless, then updates remaining out-of-date rows with the values from staging.
        /// Out-of-date remaining rows will only be present if they have children which are to be inserted. Any other children will have been deleted in an earlier pass through the recursion (since it starts at the leaves and works upwards).
        /// </summary>
        /// <param name="tiCurrent"></param>
        /// <param name="joinPathToTimeTable">Chain of JoinInfos back to the TimePeriodicity table so we can join to it and recover the effective date of a particular row</param>
        /// <param name="childJoins"></param>
        private void ProcessTable(TableInfo tiCurrent, List<JoinInfo> joinPathToTimeTable, List<JoinInfo> childJoins)
        {
            var columnSetsToMigrate = _migrationConfiguration.CreateMigrationColumnSetFromTableInfos(new[] {tiCurrent}.ToList(),null, new BackfillMigrationFieldProcessor());
            var columnSet = columnSetsToMigrate.Single();
            var queryHelper = new ReverseMigrationQueryHelper(columnSet);
            var mcsQueryHelper = new MigrationColumnSetQueryHelper(columnSet);

            // Any DELETEs needed?
            DeleteEntriesHavingNoChildren(tiCurrent, joinPathToTimeTable, childJoins, mcsQueryHelper);

            // Update any out-of-date rows that have survived the delete, so they don't overwrite live with stale data. They will only survive the delete if they have children due for insertion into live.
            UpdateOldParentsThatHaveNewChildren(tiCurrent, joinPathToTimeTable, queryHelper, mcsQueryHelper);
        }

        private void UpdateOldParentsThatHaveNewChildren(TableInfo tiCurrent, List<JoinInfo> joinPathToTimeTable, ReverseMigrationQueryHelper queryHelper, MigrationColumnSetQueryHelper mcsQueryHelper)
        {
            var update = string.Format(@"WITH 
{0}
UPDATE CurrentTable
SET {1}
FROM 
LiveDataForUpdating LEFT JOIN {2} AS CurrentTable {3}",
                GetLiveDataToUpdateStaging(tiCurrent, joinPathToTimeTable),
                queryHelper.BuildUpdateClauseForRow("LiveDataForUpdating", "CurrentTable"),
                "[" + _dbInfo.GetRuntimeName() + "]..[" + tiCurrent.GetRuntimeName() + "]",
                mcsQueryHelper.BuildJoinClause("LiveDataForUpdating", "CurrentTable"));

            using (var connection = (SqlConnection)_dbInfo.Server.GetConnection())
            {
                connection.Open();
                var cmd = new SqlCommand(update, connection);
                cmd.ExecuteNonQuery();
            }
        }

        private void DeleteEntriesHavingNoChildren(TableInfo tiCurrent, List<JoinInfo> joinPathToTimeTable, List<JoinInfo> joinsToProcess, MigrationColumnSetQueryHelper mcsQueryHelper)
        {
            // If there are no joins then we should delete any old updates at this level
            string deleteSql;
            if (!joinsToProcess.Any())
            {
                deleteSql = "WITH " + GetCurrentOldEntriesSQL(tiCurrent, joinPathToTimeTable) + ", EntriesToDelete AS (SELECT * FROM CurrentOldEntries)";
            }
            else
            {
                // Join on children so we can detect childless rows and delete them
                var joins = new List<string>();
                var wheres = new List<string>();

                // create sql for child joins
                foreach (var childJoin in joinsToProcess)
                {
                    var childTable = childJoin.ForeignKey.TableInfo;
                    joins.Add(string.Format("LEFT JOIN {0} {1} ON CurrentOldEntries.{2} = {1}.{3}",
                        "[" + _dbInfo.GetRuntimeName() + "]..[" + childTable.GetRuntimeName() + "]",
                        childTable.GetRuntimeName(),
                        childJoin.PrimaryKey.GetRuntimeName(),
                        childJoin.ForeignKey.GetRuntimeName()
                        ));

                    wheres.Add(childTable.GetRuntimeName() + "." + childJoin.ForeignKey.GetRuntimeName() + " IS NULL");
                }

                deleteSql = "WITH " + GetCurrentOldEntriesSQL(tiCurrent, joinPathToTimeTable) +
                            ", EntriesToDelete AS (SELECT DISTINCT CurrentOldEntries.* FROM CurrentOldEntries " + string.Join(" ", joins) +
                            " WHERE " +
                            string.Join(" AND ", wheres) + ")";
            }

            deleteSql += string.Format(@"
DELETE CurrentTable
FROM {0} CurrentTable
RIGHT JOIN EntriesToDelete {1}",
                "[" + _dbInfo.GetRuntimeName() + "]..[" + tiCurrent.GetRuntimeName() + "]",
                mcsQueryHelper.BuildJoinClause("EntriesToDelete", "CurrentTable"));

            using (var connection = (SqlConnection)_dbInfo.Server.GetConnection())
            {
                connection.Open();
                var cmd = new SqlCommand(deleteSql, connection);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// This and GetLiveDataToUpdateStaging are ugly in that they just reflect modifications to the comparison CTE. Leaving for now as a more thorough refactoring may be required once the full test suite is available.
        /// </summary>
        /// <param name="tiCurrent"></param>
        /// <param name="joinPathToTimeTable"></param>
        /// <returns></returns>
        private string GetCurrentOldEntriesSQL(TableInfo tiCurrent, List<JoinInfo> joinPathToTimeTable)
        {
            return string.Format(@"
CurrentOldEntries AS (
SELECT ToLoadWithTime.* FROM 

{0} 
",
                _sqlHelper.GetSQLComparingStagingAndLiveTables(tiCurrent, joinPathToTimeTable));
        }

        /// <summary>
        /// This and GetCurrentOldEntriesSQL are ugly in that they just reflect modifications to the comparison CTE. Leaving for now as a more thorough refactoring may be required once the full test suite is available.
        /// </summary>
        /// <param name="tiCurrent"></param>
        /// <param name="joinPathToTimeTable"></param>
        /// <returns></returns>
        private string GetLiveDataToUpdateStaging(TableInfo tiCurrent, List<JoinInfo> joinPathToTimeTable)
        {
            return string.Format(@"
LiveDataForUpdating AS (
SELECT LoadedWithTime.* FROM

{0}",
                _sqlHelper.GetSQLComparingStagingAndLiveTables(tiCurrent, joinPathToTimeTable));
        }

        public void Initialize(DiscoveredDatabase dbInfo, LoadStage loadStage)
        {
            _dbInfo = dbInfo;
        }

        public void Check(ICheckNotifier notifier)
        {
            // if we're not executing in a test context, fail the whole component: it doesn't yet have sufficient test coverage
            if (!TestContext)
            {
                notifier.OnCheckPerformed(
                    new CheckEventArgs("Don't use the StagingBackfillMutilator component for now! Does not yet have sufficient test coverage.",
                        CheckResult.Fail));
            }
        }

        

        public void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
        {
        }
    }
}