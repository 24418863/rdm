﻿using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataFlowPipeline.Requirements;
using DataLoadEngine.Attachers;
using DataLoadEngine.DataFlowPipeline.Destinations;
using DataLoadEngine.DataFlowPipeline.Sources;
using DataLoadEngine.Job;
using DataLoadEngine.Job.Scheduling;
using HIC.Logging;
using LoadModules.Generic.LoadProgressUpdating;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.Progress;

namespace LoadModules.Generic.Attachers
{
    public abstract class RemoteTableAttacher: Attacher, IPluginAttacher
    {
        private const string FutureLoadMessage = "Cannot load data from the future";

        public RemoteTableAttacher(): base(true)
        {
            
        }

        [DemandsInitialization("The DataSource (Server) connect to in order to read data.  Note that this may be MyFriendlyServer (SqlServer) or something like '192.168.56.101:1521/TRAININGDB'(Oracle)",Mandatory=true)]
        public string RemoteServer { get; set; }

        [DemandsInitialization("The database on the remote host containg the table we will read data from", Mandatory = true)]
        public string RemoteDatabaseName { get; set; }

        [DemandsInitialization("The table on the remote host from which data will be read.")]
        public string RemoteTableName { get; set; }

        [DemandsInitialization("When provided this OVERIDES RemoteTableName and is intended for running a complicated query on the remote machine in order to pull data in a suitable format.",DemandType.SQL)]
        public string RemoteSelectSQL { get; set; }

        [DemandsInitialization("The table in RAW that you want to load the remote database data into.  This must (currently) match the TableInfo you are ultimately going to load exactly including having the same number of columns - if you need to run CREATE and ALTER scripts to accommodate dodgy source data formats then you should do that in either Mounting or AdjustRAW", Mandatory = true)]
        public string RAWTableName { get; set; }

        [DemandsInitialization("Optionally gives you access to two parameters " + StartDateParameter + " and " + EndDateParameter + " for use in your RemoteSelectSQL.  This requires that you create a load schedule specifically associated with the LoadMetadata, this ties you contractually to actually respect the dates correctly in your query.")]
        public LoadProgress Progress { get; set; }

        [DemandsInitialization("Indicates how you want to update the Progress.DataLoadProgress value on successful load batches (only required if you have a LoadProgress)")]
        public DataLoadProgressUpdateInfo ProgressUpdateStrategy{ get; set; }

        [DemandsInitialization("The length of time in seconds to allow for data to be completely read from the destination before giving up (0 for no timeout)")]
        public int Timeout { get; set; }

        [DemandsInitialization("Terminates the currently executing data load with LoadNotRequired if the remote table is empty / RemoteSelectSQL returns no rows")]
        public bool LoadNotRequiredIfNoRowsRead { get; set; }


        protected const string StartDateParameter = "@startDate";
        protected const string EndDateParameter = "@endDate";

        protected const string DeclareStartDateParameter = "declare @startDate datetime";
        protected const string DeclareEndDateParameter = "declare @endDate datetime";

        protected string _remoteUsername { get; set; }
        protected string _remotePassword { get; set; }
        protected bool _setupDone { get; set; }

        public enum PeriodToLoad
        {
            Month,
            Year,
            Decade
        }

        protected void ThrowIfInvalidRemoteTableName()
        {
            //this overrides the remote table
            if(!string.IsNullOrWhiteSpace(RemoteSelectSQL))
                return;

            const string patternForValidTableNames = "^[0-9A-Za-z_]+$";
            
            
            if (string.IsNullOrWhiteSpace(RemoteTableName))
                throw new Exception("RemoteTableName is null, you need to give ProcessTaskArgument a value of a table on the remote server " + RemoteServer);


            if (!Regex.IsMatch(RemoteTableName, patternForValidTableNames))
                throw new Exception("RemoteTableName argument was rejected because it contained freaky characters (could be just be spaces).  Value was " + RemoteTableName + " expected regex to match with was: " + patternForValidTableNames);
        }


        public override void Initialize(IHICProjectDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
        {
            base.Initialize(hicProjectDirectory,dbInfo);

            try
            {
                SetupUsernameAndPassword();
            }
            catch (Exception)
            {
                //use integrated security if this fails
            }
        }
     

        

        public override void Check(ICheckNotifier notifier)
        {
            //if we have been initialized
            if (HICProjectDirectory != null)
            {
                try
                {
                    ThrowIfInvalidRemoteTableName();
                }
                catch (Exception e)
                {
                    notifier.OnCheckPerformed(new CheckEventArgs("Failed to find username and password for RemoteTableAttacher",
                        CheckResult.Fail, e));
                }

                try
                {
                    
                    try
                    {
                        SetupUsernameAndPassword();

                        //if there is a username and password
                        if(!string.IsNullOrWhiteSpace(_remoteUsername) && !string.IsNullOrWhiteSpace(_remotePassword))
                            notifier.OnCheckPerformed(new CheckEventArgs("Found username and password to use with RemoteTableAttacher",CheckResult.Success, null));
                    }
                    catch (Exception e)
                    {
                        notifier.OnCheckPerformed(new CheckEventArgs("Failed to setup username/password - proceeding with Integrated Security", CheckResult.Warning, e));
                    }
                    
                    CheckTablesExist(GetConnectionString(), notifier);
                }
                catch (Exception e)
                {
                    notifier.OnCheckPerformed(new CheckEventArgs("Program crashed while trying to inspect remote server " + RemoteServer + "  for presence of tables/databases specified in the load configuration.",
                        CheckResult.Fail, e));
                }

            }
            else
                notifier.OnCheckPerformed(new CheckEventArgs(
                    "HICProjectDirectory was null in Check() for class RemoteTableAttacher",
                    CheckResult.Warning, null));


            if (Progress != null)
            {
                if (!Progress.DataLoadProgress.HasValue)
                {

                    if (Progress.OriginDate.HasValue)
                    {
                        var fixDate = Progress.OriginDate.Value.AddDays(-1);

                        bool setDataLoadProgressDateToOriginDate = notifier.OnCheckPerformed(new CheckEventArgs("LoadProgress '" + Progress + "' does not have a DataLoadProgress value, you must set this to something to start loading data from that date", CheckResult.Fail, null, "Set the data load progress date to the OriginDate minus one Day? " + Environment.NewLine + "Set DataLoadProgress = " + Progress.OriginDate + " -1 day = " + fixDate)); 
                        if(setDataLoadProgressDateToOriginDate)
                        {
                            Progress.DataLoadProgress = fixDate;
                            Progress.SaveToDatabase();
                        }
                        else
                           notifier.OnCheckPerformed(new CheckEventArgs("User decided not to apply suggested fix so stopping checking",CheckResult.Fail, null));
                    }
                    else
                        notifier.OnCheckPerformed(new CheckEventArgs("LoadProgress '" + Progress + "' does not have a DataLoadProgress value, you must set this to something to start loading data from that date",CheckResult.Fail, null));
                    
                }

                if (ProgressUpdateStrategy == null)
                    notifier.OnCheckPerformed(
                        new CheckEventArgs(
                            "Progress is specified '" + Progress +
                            "' but there is no ProgressUpdateStrategy specified (if you have one you must have both)",
                            CheckResult.Fail));
                else
                    ProgressUpdateStrategy.Check(notifier);

                if (!LoadNotRequiredIfNoRowsRead)
                    notifier.OnCheckPerformed(new CheckEventArgs("LoadNotRequiredIfNoRowsRead is false but you have a Progress '" + Progress +"', this means that when the data being loaded is fully exhausted for a given range of days you will probably get an error instead of a clean shutdown",CheckResult.Warning));

                
                if (Progress.LockedBecauseRunning)
                    notifier.OnCheckPerformed(new CheckEventArgs("LoadProgress '" + Progress + "'  is currently locked, lock holder is listed as '" + (Progress.LockHeldBy ?? "unspecified") + "'", CheckResult.Fail, null));

                if(string.IsNullOrWhiteSpace(RemoteSelectSQL))
                    notifier.OnCheckPerformed(new CheckEventArgs("A LoadProgress has been configured but the RemoteSelectSQL is empty, how are you respecting the schedule without tailoring your query?", CheckResult.Fail, null));
                else
                {
                    foreach (string expectedParameter in new[] {StartDateParameter, EndDateParameter})
                        if (RemoteSelectSQL.Contains(expectedParameter))
                            notifier.OnCheckPerformed(new CheckEventArgs("Found " + expectedParameter + " in the RemoteSelectSQL",
                                CheckResult.Success, null));
                        else
                            notifier.OnCheckPerformed(new CheckEventArgs(
                                "Could not find any reference to parameter " + expectedParameter +
                                " in the RemoteSelectSQL, how do you expect to respect the LoadProgress you have configured without a reference to this date?", CheckResult.Fail, null));

                    foreach (string prohibitedSQL in new[]{DeclareEndDateParameter,DeclareStartDateParameter})
                    {
                        if (RemoteSelectSQL.Contains(prohibitedSQL))
                            notifier.OnCheckPerformed(new CheckEventArgs("Do not include '" + prohibitedSQL + "' in the RemoteSelectSQL, this global parameter will automatically be added to your query and populated with the current LoadProgress",CheckResult.Fail, null));
                 
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(RAWTableName))
                notifier.OnCheckPerformed(new CheckEventArgs("RAWTableName has not been set for " + GetType().Name, CheckResult.Fail));
        }

        protected abstract DbConnectionStringBuilder GetConnectionString();
        protected abstract DbConnection GetConnection();
        protected void CheckTablesExist(DbConnectionStringBuilder builder, ICheckNotifier notifier)
        {
            try
            {
                var db = new DiscoveredServer(builder).ExpectDatabase(RemoteDatabaseName);

                if (!db.Exists())
                    throw new Exception("Database " + RemoteDatabaseName + " did not exist on the remote server");

                //still worthwhile doing this incase we cannot connect to the server
                var tables = db.DiscoverTables(true).Select(t => t.GetRuntimeName()).ToArray();
                
                //overrides table level checks
                if (!string.IsNullOrWhiteSpace(RemoteSelectSQL))
                    return;

                //user has just picked a table to copy exactly so we can precheck for it
                if (tables.Contains(RemoteTableName))
                    notifier.OnCheckPerformed(new CheckEventArgs(
                        "successfully found table " + RemoteTableName + " on server " + RemoteServer + " on database " +
                        RemoteDatabaseName,
                        CheckResult.Success, null));
                else
                    notifier.OnCheckPerformed(new CheckEventArgs(
                        "Could not find table called '" + RemoteTableName + "' on server " + RemoteServer + " on database " +
                        RemoteDatabaseName +Environment.NewLine+"(The following tables were found:"+string.Join(",",tables)+")",
                        CheckResult.Fail, null));
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Problem occurred when trying to enumerate tables on server " + RemoteServer + " on database " +RemoteDatabaseName, CheckResult.Fail, e));
            }
        }


        private void SetupUsernameAndPassword()
        {

            if (HICProjectDirectory.GetTagFromConfigurationDataXML("password")==null)
                throw new Exception("No account details found in HIC Project Directory, expected a file called ConfigurationDetails.xml");

            var usernameNodeList = HICProjectDirectory.GetTagFromConfigurationDataXML("username");

            if (usernameNodeList.Count != 1)
                throw new Exception("When looking for Tag called username in ConfigurationDetails.xml we found " + usernameNodeList.Count + " tags, expected 1");

            _remoteUsername = usernameNodeList[0].InnerText;

            var passwordNodeList = HICProjectDirectory.GetTagFromConfigurationDataXML("password");

            if (passwordNodeList.Count != 1)
                throw new Exception("When looking for Tag called password in ConfigurationDetails.xml we found " + passwordNodeList.Count + " tags, expected 1");

            _remotePassword = passwordNodeList[0].InnerText;

            _setupDone = true;
        }
        public override ExitCodeType Attach(IDataLoadJob job)
        {
            base.Attach(job);


            if (job == null)
                throw new Exception("Job is Null, we require to know the job to build a DataFlowPipeline");

            
            DbConnection con = GetConnection();
            con.Open();

            ThrowIfInvalidRemoteTableName();
            
            string sql;

            if (!string.IsNullOrWhiteSpace(RemoteSelectSQL))
                sql = RemoteSelectSQL;
            else
                sql = "Select * from " + RemoteTableName;
            
            bool scheduleMismatch = false;

            //if there is a load progress 
            if (Progress != null)
                try
                {
                    //get appropriate date declaration SQL if any
                    sql = GetScheduleParameterDeclarations(job, out scheduleMismatch) + sql;
                }
                catch (Exception e)
                {
                    //if the date range is in the future then GetScheduleParameterDeclarations will throw Exception about future dates
                    if(e.Message.StartsWith(FutureLoadMessage))
                        return ExitCodeType.OperationNotRequired;//if this is the case then don't bother with the data load

                    throw;
                }
            if (scheduleMismatch)
            {
                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "Skipping LoadProgress '" + Progress + "' because it is not the correct Schedule for this component"));
                return ExitCodeType.Success;
            }

            job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "About to execute SQL:" + Environment.NewLine + sql));

            var source = new DbDataCommandDataFlowSource(sql, "Fetch data from " + RemoteServer + " to populate RAW table " + RemoteTableName, GetConnectionString(), Timeout ==0?50000:Timeout);

            var destination = new SqlBulkInsertDestination(_dbInfo, RAWTableName, Enumerable.Empty<string>());

            var contextFactory = new DataFlowPipelineContextFactory<DataTable>();
            var context = contextFactory.Create(PipelineUsage.LogsToTableLoadInfo | PipelineUsage.FixedDestination);

            var engine = new DataFlowPipelineEngine<DataTable>(context, source, destination, job);

            ITableLoadInfo loadInfo = job.DataLoadInfo.CreateTableLoadInfo("Truncate RAW table " + RAWTableName,
                _dbInfo.Server.Name + "." + _dbInfo.GetRuntimeName(),
                new DataSource[]
                {
                    new DataSource(
                        "Remote SqlServer Servername=" + RemoteServer + "Database=" + _dbInfo.GetRuntimeName() +
                        
                        //Either list the table or the query depending on what is populated
                        (RemoteTableName != null?" Table=" + RemoteTableName
                        :" Query = " + sql), DateTime.Now)
                }, -1);

            engine.Initialize(loadInfo);
            engine.ExecutePipeline(new GracefulCancellationToken());

            if (source.TotalRowsRead == 0 && LoadNotRequiredIfNoRowsRead)
            {
                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "No rows were read from the remote table and LoadNotRequiredIfNoRowsRead is true so returning ExitCodeType.LoadNotRequired"));
                return ExitCodeType.OperationNotRequired;
            }

            job.OnNotify(this, new NotifyEventArgs(source.TotalRowsRead  > 0 ? ProgressEventType.Information:ProgressEventType.Warning, "Finished after reading " + source.TotalRowsRead + " rows"));


            if (Progress != null)
            {
                if(ProgressUpdateStrategy == null)
                    throw new Exception("ProgressUpdateStrategy is null but there is a Progress");

                ProgressUpdateStrategy.AddAppropriateDisposeStep((ScheduledDataLoadJob) job,_dbInfo);

            }

            return ExitCodeType.Success;
        }

        private string GetScheduleParameterDeclarations(IDataLoadJob job, out bool scheduleMismatch)
        {

            var jobAsScheduledJob = job as ScheduledDataLoadJob;

            if(jobAsScheduledJob == null)
                throw new NotSupportedException("Job must be of type " + typeof(ScheduledDataLoadJob).Name + " because you have specified a LoadProgress");

            //if the currently scheduled job is not our Schedule then it is a mismatch and we should skip it
            scheduleMismatch = !jobAsScheduledJob.LoadProgress.Equals(Progress);


            DateTime min = jobAsScheduledJob.DatesToRetrieve.Min();
            DateTime max = jobAsScheduledJob.DatesToRetrieve.Max();

            //since it's a date time and fetch list is Dates then we should set the max to the last second of the day (23:59:59) but leave the min as the first second of the day (00:00:00).  This allows for single day loads too
            if(max.Hour == 0  && max.Minute == 0 && max.Second ==0)
            {
                max = max.AddHours(23);
                max = max.AddMinutes(59);
                max = max.AddSeconds(59);
            }


            if(min >= max)
                throw new Exception("Problematic max and min dates(" + max + " and " + min +" respectively)");

            string startSql = DeclareStartDateParameter + " = '" + min.ToString("yyyy-MM-dd HH:mm:ss") + "'" + Environment.NewLine;
            string endSQL = DeclareEndDateParameter + " = '" + max.ToString("yyyy-MM-dd HH:mm:ss") + "'" + Environment.NewLine;
            
            if(min > DateTime.Now)
                throw new Exception(FutureLoadMessage + " (min is " + min +")");
            
            return startSql + endSQL + Environment.NewLine;
        }


    }

}