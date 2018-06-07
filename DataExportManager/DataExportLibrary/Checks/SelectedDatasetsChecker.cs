using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.QueryBuilding;
using CatalogueLibrary.Repositories;
using DataExportLibrary.Data;
using DataExportLibrary.Data.LinkCreators;
using DataExportLibrary.ExtractionTime.Commands;
using DataExportLibrary.ExtractionTime.UserPicks;
using DataExportLibrary.Interfaces.Data.DataTables;
using DataExportLibrary.Interfaces.ExtractionTime.Commands;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DataAccess;

namespace DataExportLibrary.Checks
{
    /// <summary>
    /// Checks that the <see cref="SelectedDataSets"/> can be built into a valid SQL extraction Query and that the SQL generated can be executed
    /// without syntax errors.
    /// </summary>
    public class SelectedDataSetsChecker : ICheckable
    {
        private readonly IRDMPPlatformRepositoryServiceLocator _repositoryLocator;
        private readonly IExtractDatasetCommand _extractCommand;

        public ISelectedDataSets SelectedDataSet { get; private set; }

        public SelectedDataSetsChecker(ISelectedDataSets selectedDataSet,IRDMPPlatformRepositoryServiceLocator repositoryLocator)
        {
            SelectedDataSet = selectedDataSet;
            _repositoryLocator = repositoryLocator;
            var factory = new ExtractCommandCollectionFactory();
            _extractCommand = factory.Create(_repositoryLocator, SelectedDataSet);
            _extractCommand.TopX = 1;
        }

        public void Check(ICheckNotifier notifier)
        {
            var ds = SelectedDataSet.ExtractableDataSet;
            var config = SelectedDataSet.ExtractionConfiguration;
            var cohort = config.Cohort;
            var project = config.Project;
            const int timeout = 5;

            notifier.OnCheckPerformed(new CheckEventArgs("Inspecting dataset " + ds, CheckResult.Success));

            var selectedcols = new List<IColumn>(config.GetAllExtractableColumnsFor(ds));

            if (!selectedcols.Any())
            {
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "Dataset " + ds + " in configuration '" + config + "' has no selected columns",
                        CheckResult.Fail));

                return;
            }

            if (_extractCommand == null)
                throw new Exception("Invalid ExtractDatasetCommand received");

            try
            {
                if (_extractCommand.TopX == 0)
                {
                    notifier.OnCheckPerformed(new CheckEventArgs("Request did not have any 'Top X' specified, adding TOP 1 for testing",
                                              CheckResult.Success));
                }
                _extractCommand.TopX = 1;
                if (_extractCommand.QueryBuilder == null)
                    _extractCommand.GenerateQueryBuilder();
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "Could not generate valid extraction SQL for dataset " + ds +
                        " in configuration " + config, CheckResult.Fail, e));
                return;
            }

            var server = _extractCommand.Catalogue.GetDistinctLiveDatabaseServer(DataAccessContext.DataExport, false);
            bool serverExists = server.Exists();

            notifier.OnCheckPerformed(new CheckEventArgs("Server " + server + " Exists:" + serverExists,
                serverExists ? CheckResult.Success : CheckResult.Fail));
            try
            {
                using (var con = server.GetConnection())
                {
                    con.Open();
                    var transaction = con.BeginTransaction();
                    //incase user somehow manages to write a filter/transform that nukes data or something

                    var managedTransaction = new ManagedTransaction(con, transaction);

                    DbCommand cmd;

                    try
                    {
                        cmd = server.GetCommand(_extractCommand.QueryBuilder.SQL, con, managedTransaction);
                        cmd.CommandTimeout = timeout;
                        notifier.OnCheckPerformed(
                            new CheckEventArgs(
                                "/*About to send Request SQL :*/" + Environment.NewLine + _extractCommand.QueryBuilder.SQL,
                                CheckResult.Success));
                    }
                    catch (QueryBuildingException e)
                    {
                        notifier.OnCheckPerformed(new CheckEventArgs("Failed to assemble query for dataset " + ds,
                            CheckResult.Fail, e));
                        return;
                    }
                    try
                    {
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                                notifier.OnCheckPerformed(new CheckEventArgs("Read at least 1 row successfully from dataset " + ds,
                                    CheckResult.Success));
                            else
                                notifier.OnCheckPerformed(new CheckEventArgs("Dataset " + ds + " is completely empty (when linked with the cohort)",
                                    CheckResult.Fail));
                        }
                    }
                    catch (SqlException e)
                    {
                        if (e.Message.Contains("Timeout"))
                            notifier.OnCheckPerformed(new CheckEventArgs("Failed to read rows after " + timeout + "s",CheckResult.Warning));
                        else
                            notifier.OnCheckPerformed(new CheckEventArgs("Failed to execute the query (See below for query)", CheckResult.Warning,e));
                    }
                }
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Failed to execute Top 1 on dataset " + ds, CheckResult.Warning, e));
            }

            var cata = _repositoryLocator.CatalogueRepository.GetObjectByID<Catalogue>((int)ds.Catalogue_ID);
            SupportingDocumentsFetcher fetcher = new SupportingDocumentsFetcher(cata);
            fetcher.Check(notifier);

            //check catalogue locals
            foreach (SupportingSQLTable table in cata.GetAllSupportingSQLTablesForCatalogue(FetchOptions.ExtractableLocals))
                new SupportingSQLTableChecker(table).Check(notifier);
        }
    }
}