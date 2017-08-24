﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.QueryBuilding;
using CatalogueLibrary.Repositories;
using DataQualityEngine.Data;
using DataQualityEngine.Reports.PeriodicityHelpers;
using HIC.Common.Validation;
using HIC.Common.Validation.Constraints;
using HIC.Common.Validation.Constraints.Secondary.Predictor;
using HIC.Logging;
using HIC.Logging.Listeners;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DataAccess;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.Progress;

namespace DataQualityEngine.Reports
{
    public class CatalogueConstraintReport : DataQualityReport
    {
        private readonly string _dataLoadRunFieldName;
        //where the data is located 
        private DiscoveredServer _server;
        private QueryBuilder _queryBuilder;
        private Validator _validator;
        private bool _containsDataLoadID;

        private Dictionary<string,DQEStateTesseract> byPivotTesseracts = new Dictionary<string, DQEStateTesseract>();
        private Dictionary<string,PeriodicityHyperCube> byPivotCategoryHyperCubes = new Dictionary<string, PeriodicityHyperCube>();

        private IExternalDatabaseServer _loggingServer;
        string _loggingTask;
        LogManager _logManager;

        public CatalogueConstraintReport(Catalogue catalogue, string dataLoadRunFieldName)
        {
            _dataLoadRunFieldName = dataLoadRunFieldName;
            _catalogue = catalogue;
            
        }

        private void SetupLogging(CatalogueRepository repository,AutomationJob job = null)
        {
            //if we have already setup logging successfully then don't worry about doing it again
            if (_loggingServer != null && _logManager != null && _loggingTask != null)
                return;

            _loggingServer = new ServerDefaults(repository).GetDefaultFor(ServerDefaults.PermissableDefaults.LiveLoggingServer_ID);

            if (_loggingServer != null)
            {
                _logManager = new LogManager(_loggingServer);
                _loggingTask = _logManager.ListDataTasks().SingleOrDefault(task => task.ToLower().Equals("dqe"));

                if (job != null)
                    _logManager.DataLoadInfoCreated += (s, d) => job.SetLoggingInfo(_loggingServer, d.ID);

                if (_loggingTask == null)
                {
                    _logManager.CreateNewLoggingTaskIfNotExists("DQE");
                    _loggingTask = "DQE";
                }
            }
            else
                throw new NotSupportedException(
                    "You must set a Default LiveLoggingServer so we can audit the DQE run, do this through the ManageExternalServers dialog");
        }

        private bool haveComplainedAboutNullCategories = false;

        public override void GenerateReport(Catalogue c, IDataLoadEventListener listener, CancellationToken cancellationToken,AutomationJob job = null)
        {
            SetupLogging((CatalogueRepository) c.Repository,job);

            var toDatabaseLogger = new ToLoggingDatabaseDataLoadEventListener(this, _logManager, _loggingTask, "DQE evaluation of " + c);

            var forker = new ForkDataLoadEventListener(listener, toDatabaseLogger);

            try
            {
                _catalogue = c;
                var dqeRepository = new DQERepository((CatalogueRepository) c.Repository);

                byPivotCategoryHyperCubes.Add("ALL",new PeriodicityHyperCube("ALL"));
                byPivotTesseracts.Add("ALL", new DQEStateTesseract("ALL"));

                Check(new FromDataLoadEventListenerToCheckNotifier(forker));

                if (job != null)
                    job.SetLastKnownStatus(AutomationJobStatus.Running);

                var sw = Stopwatch.StartNew();
                using (var con = _server.GetConnection())
                {
                    con.Open();
                    
                    var cmd = _server.GetCommand(_queryBuilder.SQL, con);
                    cmd.CommandTimeout = 500000;

                    var t = cmd.ExecuteReaderAsync(cancellationToken);
                    t.Wait(cancellationToken);

                    if(cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException("User cancelled DQE while fetching data");

                    var r = t.Result;

                    int progress = 0;

                    while (r.Read())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        progress++;
                        int dataLoadRunIDOfCurrentRecord = 0;//to start with assume we will pass the results for the 'unknown batch' (where data load run ID is null or not available)

                        //if the DataReader is likely to have a data load run ID column
                        if (_containsDataLoadID)
                        {
                            //get data load run id
                            int? runID = dqeRepository.ObjectToNullableInt(r[_dataLoadRunFieldName]);

                            //if it has a value use it (otherwise it is null so use 0 - ugh I know, it's a primary key constraint issue)
                            if (runID != null)
                                dataLoadRunIDOfCurrentRecord = (int) runID;
                        }

                        string pivotValue = null;

                        //if the user has a pivot category configured
                        if (_pivotCategory != null)
                        {
                            pivotValue = GetStringValueForPivotField(r[_pivotCategory], forker);

                            if (!haveComplainedAboutNullCategories && string.IsNullOrWhiteSpace(pivotValue))
                            {
                                forker.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "Found a null/empty value for pivot category '" + _pivotCategory + "', this record will ONLY be recorded under ALL and not it's specific category, you will not be warned of further nulls because there are likely to be many if there are any"));
                                haveComplainedAboutNullCategories = true;
                                pivotValue = null;
                            }
                        }
                        
                        //always increase the "ALL" category
                        ProcessRecord(dqeRepository,dataLoadRunIDOfCurrentRecord, r, byPivotCategoryHyperCubes["ALL"], byPivotTesseracts["ALL"]);
                        
                        //if there is a value in the current record for the pivot column
                        if(pivotValue != null)
                        {
                            //if it is a novel 
                            if (!byPivotCategoryHyperCubes.ContainsKey(pivotValue))
                            {
                                //we will need to expand the dictionaries 
                                if(byPivotCategoryHyperCubes.Keys.Count>30)//IMPORTANT: this value of 30 is in the documentation, dont change it without also changing UserManual.docx
                                    throw new OverflowException("Encountered more than 30 values for the pivot column " + _pivotCategory + " this will result in crazy space usage since it is a multiplicative scale of DQE tesseracts");
                                
                                //expand both the time periodicity and the state results
                                byPivotTesseracts.Add(pivotValue, new DQEStateTesseract(pivotValue));
                                byPivotCategoryHyperCubes.Add(pivotValue, new PeriodicityHyperCube(pivotValue));
                                
                            }

                            //now we are sure that the dictionaries have the category field we can increment it
                            ProcessRecord(dqeRepository, dataLoadRunIDOfCurrentRecord,r, byPivotCategoryHyperCubes[pivotValue], byPivotTesseracts[pivotValue]);
                        }

                        if (progress % 5000 == 0)
                        {
                            forker.OnProgress(this, new ProgressEventArgs("Processing " + _catalogue, new ProgressMeasurement(progress, ProgressType.Records), sw.Elapsed));
                            
                            if(job != null)
                            {
                                job.SetLastKnownStatus(AutomationJobStatus.Running);
                                job.TickLifeline();
                            }
                        }
                     }
                    //final value
                    forker.OnProgress(this, new ProgressEventArgs("Processing " + _catalogue, new ProgressMeasurement(progress, ProgressType.Records), sw.Elapsed));
                    con.Close();
                }
                sw.Stop();

                foreach (var state in byPivotTesseracts.Values)
                    state.AdjustDandyValuesDown();
                
                //now commit results
                using (var con = dqeRepository.BeginNewTransactedConnection())
                {
                    try
                    {
                        //mark down that we are beginning an evaluation on this the day of our lord etc...
                        Evaluation evaluation = new Evaluation(dqeRepository, _catalogue);
                        
                        foreach (var state in byPivotTesseracts.Values)
                            state.CommitToDatabase(evaluation,_catalogue,con.Connection,con.Transaction);
                        
                        if (_timePeriodicityField != null)
                            foreach (PeriodicityHyperCube periodicity in byPivotCategoryHyperCubes.Values)
                                periodicity.CommitToDatabase(evaluation);

                        con.ManagedTransaction.CommitAndCloseConnection();

                    }
                    catch (Exception)
                    {
                        con.ManagedTransaction.AbandonAndCloseConnection();
                        throw;
                    }
                }

                forker.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "CatalogueConstraintReport completed successfully  and committed results to DQE server"));

                if (job != null)
                    job.SetLastKnownStatus(AutomationJobStatus.Finished);
            }
            catch (Exception e)
            {
                if(job != null) //Automation is on
                {
                    job.SetLastKnownStatus(e is OperationCanceledException ? AutomationJobStatus.Cancelled : AutomationJobStatus.Crashed); //record automation state

                    //dont record cancellations at automation error level just at job level
                    if (!(e is OperationCanceledException))
                        new AutomationServiceException((ICatalogueRepository) job.Repository, e);//push Exception up to automation level
                }

                if (!(e is OperationCanceledException))
                    forker.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Fatal Crash", e));
                else
                    forker.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "DQE Execution Cancelled", e));
            }

            toDatabaseLogger.FinalizeTableLoadInfos();
        }

        private bool _haveComplainedAboutTrailingWhitespaces = false;

        private string GetStringValueForPivotField(object o, IDataLoadEventListener listener)
        {
            if (o == null || o == DBNull.Value)
                return null;

            string stringValue = o.ToString();
            string trimmedValue = stringValue.Trim();

            if (!_haveComplainedAboutTrailingWhitespaces && stringValue != trimmedValue)
            {

                listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Warning, "Found trailing/leading whitespace in value in Pivot field, this will be trimmed off:'"+ o +"'"));
                _haveComplainedAboutTrailingWhitespaces = true;
            }

            return trimmedValue;
        }

        private string _timePeriodicityField;
        private string _pivotCategory;
        


        public override void Check(ICheckNotifier notifier)
        {
            
            //there is a catalogue
            if (_catalogue == null)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Catalogue has not been set, either use the constructor with Catalogue parameter or use the blank constructor and call CatalogueSupportsReport instead", CheckResult.Fail));
                return;
            }
            try
            {
                var dqeRepository = new DQERepository((CatalogueRepository)_catalogue.Repository);
                notifier.OnCheckPerformed(new CheckEventArgs("Found DQE reporting server " + dqeRepository.DiscoveredServer.Name, CheckResult.Success));
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "Failed to create DQE Repository, possibly there is no DataQualityEngine Reporting Server (ExternalDatabaseServer).  You will need to create/set one in CatalogueManager by using 'Locations=>Manage External Servers...'",
                        CheckResult.Fail,e));
            }

            try
            {
                SetupLogging((CatalogueRepository)_catalogue.Repository);
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Failed to setup logging of DQE runs", CheckResult.Fail, e));
                return;
            }

            //there is XML
            if (string.IsNullOrWhiteSpace(_catalogue.ValidatorXML))
            {
                notifier.OnCheckPerformed(new CheckEventArgs("There is no ValidatorXML specified for the Catalogue " + _catalogue + ", configure validation in CatalogueManger by right clicking a Catalogue",CheckResult.Fail));
                return;
            }
            notifier.OnCheckPerformed(new CheckEventArgs("Found ValidatorXML specified for the Catalogue " + _catalogue + ":"+Environment.NewLine + _catalogue.ValidatorXML, CheckResult.Success));
            
            //the XML is legit
            try
            {
                _validator = Validator.LoadFromXml(_catalogue.ValidatorXML);
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("ValidatorXML for Catalogue " + _catalogue + " could not be deserialized into a Validator",CheckResult.Fail,e));
                return;
            }

            notifier.OnCheckPerformed(new CheckEventArgs("Deserialized validation XML successfully", CheckResult.Success));

            //there is a server
            try
            {
                _server = _catalogue.GetDistinctLiveDatabaseServer(DataAccessContext.InternalDataProcessing, true);
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Could not get connection to Catalogue " + _catalogue,CheckResult.Fail, e));
                return;
            }
            notifier.OnCheckPerformed(new CheckEventArgs("Found connection string for Catalogue " + _catalogue, CheckResult.Success));

            //we can connect to the server
            try
            {
                _server.TestConnection();
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Could not connect to server for Catalogue " + _catalogue,CheckResult.Fail, e));
            }

            //there is extraction SQL
            try
            {
                _queryBuilder = new QueryBuilder("", "");
                _queryBuilder.AddColumnRange(_catalogue.GetAllExtractionInformation(ExtractionCategory.Any));

                var duplicates = _queryBuilder.SelectColumns.GroupBy(c => c.IColumn.GetRuntimeName()).SelectMany(grp=>grp.Skip(1)).ToArray();

                if(duplicates.Any())
                    foreach (QueryTimeColumn column in duplicates)
                    {
                        notifier.OnCheckPerformed(
                            new CheckEventArgs(
                                "The column name " + column.IColumn.GetRuntimeName() +
                                " is duplicated in the SELECT command, column names must be unique!  Most likely you have 2+ columns with the same name (from different tables) or duplicate named CatalogueItem/Aliases for the same underlying ColumnInfo",
                                CheckResult.Fail));
                    }

                notifier.OnCheckPerformed(new CheckEventArgs("Query Builder decided the extraction SQL was:" + Environment.NewLine + _queryBuilder.SQL,CheckResult.Success));

                SetupAdditionalValidationRules(notifier);

            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Failed to generate extraction SQL", CheckResult.Fail, e));
            }
            
            //for each thing we are about to try and validate
            foreach (ItemValidator itemValidator in _validator.ItemValidators)
            {
                //is there a column in the query builder that matches it
                if (
                    //there isnt!
                    !_queryBuilder.SelectColumns.Any(
                        c => c.IColumn.GetRuntimeName().Equals(itemValidator.TargetProperty)))
                    notifier.OnCheckPerformed(
                        new CheckEventArgs(
                            "Could not find a column in the extraction SQL that would match TargetProperty " +
                            itemValidator.TargetProperty, CheckResult.Fail));
                else
                    //there is that is good
                    notifier.OnCheckPerformed(
                        new CheckEventArgs("Found column in query builder columns which matches TargetProperty " +
                                           itemValidator.TargetProperty, CheckResult.Success));
            }

            _containsDataLoadID =
                _queryBuilder.SelectColumns.Any(
                    c => c.IColumn.GetRuntimeName().Equals(_dataLoadRunFieldName));

            if (_containsDataLoadID)
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "Found " + _dataLoadRunFieldName + " field in ExtractionInformation",
                        CheckResult.Success));
            else
            {
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "Did not find ExtractionInformation for a column called " + _dataLoadRunFieldName +
                        ", this will prevent you from viewing the resulting report subdivided by data load batch (make sure you have this column and that it is marked as extractable)",
                        CheckResult.Warning));
            }


            if (_catalogue.PivotCategory_ExtractionInformation_ID == null)
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "Catalogue does not have a pivot category so all records will appear as PivotCategory 'ALL'",
                        CheckResult.Warning));
            else
            {
                _pivotCategory = _catalogue.PivotCategory_ExtractionInformation.GetRuntimeName();
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "Found time Pivot Category field " + _pivotCategory + " so we will be able to generate a categorised tesseract (evaluation, periodicity, consequence, pivot category)",
                        CheckResult.Success));
            }

            var tblValuedFunctions = _catalogue.GetTableInfoList(true).Where(t => t.IsTableValuedFunction).ToArray();
            if (tblValuedFunctions.Any())
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "Catalogue contains 1+ table valued function in it's TableInfos (" +
                        string.Join(",", tblValuedFunctions.Select(t => t.ToString())), CheckResult.Fail));

            if (_catalogue.TimeCoverage_ExtractionInformation_ID == null)
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "Catalogue does not have a time periodicity field so we will be unable to generate a time coverage olap cube",
                        CheckResult.Fail));
            else
            {
                var periodicityExtractionInformation = _catalogue.TimeCoverage_ExtractionInformation;

                _timePeriodicityField = periodicityExtractionInformation.GetRuntimeName();
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "Found time periodicity field "+_timePeriodicityField+" so we will be able to generate a time coverage olap cube",
                        CheckResult.Success));

                if (!periodicityExtractionInformation.ColumnInfo.Data_type.ToLower().Contains("date"))
                    notifier.OnCheckPerformed(
                        new CheckEventArgs(
                            "Time periodicity field " + _timePeriodicityField + " was of type " +
                            periodicityExtractionInformation.ColumnInfo.Data_type +
                            " (expected the type name to contain the word 'date' - ignoring caps).  It is possible (but unlikely) that you have dealt with this by applying a transform to the underlying ColumnInfo as part of the ExtractionInformation, if so you can ignore this message.",
                            CheckResult.Warning));
                else
                    notifier.OnCheckPerformed(
                        new CheckEventArgs(
                            "Time periodicity field " + _timePeriodicityField + " is a legit date!",
                            CheckResult.Success));
            }
        }

        private void SetupAdditionalValidationRules(ICheckNotifier notifier)
        {
            //for each description
            foreach (QueryTimeColumn descQtc in _queryBuilder.SelectColumns.Where(qtc => qtc.IsLookupDescription))
            {
                try
                {
                    //if we have a the foreign key too
                    var foreignQtc = _queryBuilder.SelectColumns.SingleOrDefault(fk => fk.IsLookupForeignKey && fk.LookupTable.ID == descQtc.LookupTable.ID);
                    if (foreignQtc != null)
                    {
                        var descriptionFieldName = descQtc.IColumn.GetRuntimeName();
                        var foreignKeyFieldName = foreignQtc.IColumn.GetRuntimeName();

                        ItemValidator itemValidator = _validator.GetItemValidator(foreignKeyFieldName);

                        //there is not yet one for this field
                        if (itemValidator == null)
                        {
                            itemValidator = new ItemValidator(foreignKeyFieldName);
                            _validator.ItemValidators.Add(itemValidator);
                        }

                        //if it doesn't already have a prediction
                        if (itemValidator.SecondaryConstraints.All(constraint => constraint.GetType() != typeof(Prediction)))
                        {
                            //Add an item validator onto the fk column that targets the description column with a nullness prediction
                            var newRule = new Prediction(new ValuePredictsOtherValueNullness(), descriptionFieldName);
                            newRule.Consequence = Consequence.Missing;
                        
                            //add one that says 'if I am null my fk should also be null'
                            itemValidator.SecondaryConstraints.Add(newRule);
                        
                            notifier.OnCheckPerformed(
                                new CheckEventArgs(
                                    "Dynamically added value->value Nullnes constraint with consequence Missing onto columns " +
                                    foreignKeyFieldName + " and " + descriptionFieldName + " because they have a configured Lookup relationship in the Catalogue", CheckResult.Success));
                        }
           
                    }
                }
                catch(Exception ex)
                {
                    notifier.OnCheckPerformed(
                        new CheckEventArgs(
                            "Failed to add new lookup validation rule for column " +
                            descQtc.IColumn.GetRuntimeName(), CheckResult.Fail, ex));
                }
            }
        }

        private void ProcessRecord(DQERepository dqeRepository, int dataLoadRunIDOfCurrentRecord, DbDataReader r, PeriodicityHyperCube periodicity, DQEStateTesseract states)
        {
            //make sure all the results dictionaries
            states.AddKeyToDictionaries(dataLoadRunIDOfCurrentRecord, _validator, _queryBuilder);

            //ask the validator to validate! 
            Consequence? worstConsequence;
            _validator.ValidateVerboseAdditive(
                r,//validate the data reader
                states.ColumnValidationFailuresByDataLoadRunID[dataLoadRunIDOfCurrentRecord],//additively adjust the validation failures dictionary
                out worstConsequence);//and tell us what the worst consequence in the row was 


            //increment the time periodicity hypercube!
            if (_timePeriodicityField != null)
            {
                DateTime? dt;

                try
                {
                    dt = dqeRepository.ObjectToNullableDateTime(r[_timePeriodicityField]);
                }
                catch (InvalidCastException e)
                {
                    throw new Exception("Found value " + r[_timePeriodicityField] + " of type " +r[_timePeriodicityField].GetType().Name + " in your time periodicity field which was not a valid date time, make sure your time periodicity field is a datetime datatype",e);
                }
                if (dt != null)
                    periodicity.IncrementHyperCube(dt.Value.Year, dt.Value.Month, worstConsequence);
            }

            //now we need to update everything we know about all the columns
            foreach (var state in states.AllColumnStates[dataLoadRunIDOfCurrentRecord])
            {
                //start out by assuming everything is dandy
                state.CountCorrect++;

                if (r[state.TargetProperty] == DBNull.Value)
                    state.CountDBNull++;
            }

            //update row level dictionaries
            if (worstConsequence == null)
                states.RowsPassingValidationByDataLoadRunID[dataLoadRunIDOfCurrentRecord]++;
            else
                states.WorstConsequencesByDataLoadRunID[dataLoadRunIDOfCurrentRecord][(Consequence)worstConsequence]++;
        }
    }
}