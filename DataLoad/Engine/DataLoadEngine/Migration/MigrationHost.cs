using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngine.DatabaseManagement;
using DataLoadEngine.DatabaseManagement.EntityNaming;
using DataLoadEngine.Job;
using DataLoadEngine.LoadExecution;
using DataLoadEngine.LoadProcess;
using HIC.Logging;
using ReusableLibraryCode;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.Progress;

namespace DataLoadEngine.Migration
{
    public class MigrationHost 
    {
        private readonly List<ICatalogue> _cataloguesToLoad;  
        private readonly DiscoveredDatabase _sourceDbInfo;
        private readonly DiscoveredDatabase _destinationDbInfo;
        private readonly HICDatabaseConfiguration _databaseConfiguration;
        private OverwriteMigrationStrategy _migrationStrategy;
        private readonly MigrationConfiguration _migrationConfig;

        public MigrationHost(List<ICatalogue> cataloguesToLoad, DiscoveredDatabase sourceDbInfo, DiscoveredDatabase destinationDbInfo, HICDatabaseConfiguration databaseConfiguration, MigrationConfiguration migrationConfig)
        {
            _sourceDbInfo = sourceDbInfo;
            _destinationDbInfo = destinationDbInfo;
            _databaseConfiguration = databaseConfiguration;
            _migrationConfig = migrationConfig;
            _cataloguesToLoad = cataloguesToLoad;
        }

        public void Migrate(HICLoadConfigurationFlags loadConfigurationFlags, ILogManager logManager, IDataLoadJob job, GracefulCancellationToken cancellationToken)
        {
            if (DatabaseOperations.CheckTablesAreEmptyInDatabaseOnServer(_sourceDbInfo))
                throw new Exception("The source database '" + _sourceDbInfo.GetRuntimeName()+ "' on " + _sourceDbInfo.Server.Name + " is empty. There is nothing to migrate.");

            using (var managedConnectionToDestination = _destinationDbInfo.Server.BeginNewTransactedConnection())
            {
                
                try
                {
                    // This will eventually be provided by factory/externally based on LoadMetadata (only one strategy for now)
                    _migrationStrategy = new OverwriteMigrationStrategy(_sourceDbInfo.GetRuntimeName(), managedConnectionToDestination);
                    _migrationStrategy.TableMigrationCompleteHandler += (name, inserts, updates) =>
                        job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Migrate table "+ name +" from STAGING to " + _destinationDbInfo.GetRuntimeName() + ": " + inserts + " inserts, " + updates + " updates"));

                    //migrate all tables (both lookups and live tables in the same way)
                    var dataColsToMigrate = _migrationConfig.CreateMigrationColumnSetFromTableInfos(job.RegularTablesToLoad, job.LookupTablesToLoad, new StagingToLiveMigrationFieldProcessor());

                    // Migrate the data columns
                    _migrationStrategy.Execute(dataColsToMigrate, job.DataLoadInfo, logManager, cancellationToken);

                    managedConnectionToDestination.ManagedTransaction.CommitAndCloseConnection();
                    job.DataLoadInfo.CloseAndMarkComplete();

                }
                catch (OperationCanceledException)
                {
                    managedConnectionToDestination.ManagedTransaction.AbandonAndCloseConnection();
                }
                catch (Exception ex)
                {
                    try
                    {
                        managedConnectionToDestination.ManagedTransaction.AbandonAndCloseConnection();
                    }
                    catch (Exception)
                    {
                        throw new Exception("Failed to rollback after exception, see inner exception for details of original problem",ex);
                    }
                    throw;
                }
            }
        }
    }
}