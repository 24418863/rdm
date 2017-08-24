﻿using System;
using System.Data.SqlClient;
using System.Text;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.Repositories;
using DataLoadEngine;
using DataLoadEngine.Job.Scheduling;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DataAccess;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.Progress;

namespace LoadModules.Generic.LoadProgressUpdating
{
    public class DataLoadProgressUpdateInfo : ICustomUIDrivenClass, ICheckable
    {
        public DataLoadProgressUpdateStrategy Strategy { get; set; }
        public string ExecuteScalarSQL { get; set; }
        public int Timeout { get; set; }
        
        #region Serialization
        public void RestoreStateFrom(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
                return;

            var lines = value.Split(new []{'\n','\r'},StringSplitOptions.RemoveEmptyEntries);
            
            DataLoadProgressUpdateStrategy strat;
            if (lines.Length > 0)
            {
                var fields = lines[0].Split(';');
                if(fields.Length>0)
                    if (DataLoadProgressUpdateStrategy.TryParse(fields[0], out strat))
                        Strategy = strat;

                if (fields.Length > 1)
                    Timeout = int.Parse(fields[1]);
            }

            ExecuteScalarSQL = "";

            for (int i = 1; i < lines.Length; i++)
                ExecuteScalarSQL += lines[i] + Environment.NewLine;
        }

        public string SaveStateToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Strategy + ";" + Timeout);
            sb.AppendLine(ExecuteScalarSQL??"");
            
            return sb.ToString();
        }
        #endregion


        /// <summary>
        /// Only call this method when you hav finished populating RAW (since the strategy ExecuteScalarSQLInRAW requires to calculate date from populated RAW database right now and it is known that RAW won't even exist post load time)
        /// </summary>
        /// <param name="job"></param>
        /// <param name="rawDatabase"></param>
        public IUpdateLoadProgress AddAppropriateDisposeStep(ScheduledDataLoadJob job, DiscoveredDatabase rawDatabase)
        {
            IUpdateLoadProgress added;
            Check(new ThrowImmediatelyCheckNotifier());

            switch (Strategy)
            {
                case DataLoadProgressUpdateStrategy.UseMaxRequestedDay:
                    added = new UpdateProgressIfLoadsuccessful(job);
                    break;
                case DataLoadProgressUpdateStrategy.ExecuteScalarSQLInLIVE:

                    added = new UpdateProgressToResultOfDelegate(job, () => GetMaxDate(GetLiveServer(job), job));
                    break;
                case DataLoadProgressUpdateStrategy.ExecuteScalarSQLInRAW:
                    try
                    {
                        var dt = GetMaxDate(rawDatabase.Server,job);
                        added = new UpdateProgressToSpecificValueIfLoadsuccessful(job, dt);
                    }
                    catch (SqlException e)
                    {
                        throw new DataLoadProgressUpdateException("Failed to execute the following SQL in the RAW database:" + ExecuteScalarSQL, e);
                    }
                    break;
               case DataLoadProgressUpdateStrategy.DoNothing:
                    //Do not add any post load update i.e. do nothing
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            job.PushForDisposal(added);
            return added;
        }

        private DiscoveredServer GetLiveServer(ScheduledDataLoadJob job)
        {
            return DataAccessPortal.GetInstance().ExpectDistinctServer(job.RegularTablesToLoad.ToArray(), DataAccessContext.DataLoad, false);
        }

        private DateTime GetMaxDate(DiscoveredServer server, IDataLoadEventListener listener)
        {

            using (var con = server.GetConnection())
            {
                con.Open();

                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "About to execute SQL to determine the maximum date for data loaded:" + ExecuteScalarSQL));

                var scalarValue = server.GetCommand(ExecuteScalarSQL, con).ExecuteScalar();

                if (scalarValue == null || scalarValue == DBNull.Value)
                    throw new DataLoadProgressUpdateException("ExecuteScalarSQL specified for determining the maximum date of data loaded returned null when executed");

                DateTime dt;
                try
                {
                    dt = Convert.ToDateTime(scalarValue);
                }
                catch (Exception e)
                {
                    throw new DataLoadProgressUpdateException("ExecuteScalarSQL specified for determining the maximum date of data loaded returned a value that was not a Date:" + scalarValue, e);
                }

                return dt;
            }
        }

        public void Check(ICheckNotifier notifier)
        {
            if(Strategy == DataLoadProgressUpdateStrategy.ExecuteScalarSQLInLIVE  || Strategy == DataLoadProgressUpdateStrategy.ExecuteScalarSQLInRAW)
                if (string.IsNullOrWhiteSpace(ExecuteScalarSQL))
                    notifier.OnCheckPerformed(
                        new CheckEventArgs(
                            "Strategy is " + Strategy +
                            " but there is no ExecuteScalarSQL, ExecuteScalarSQL should be a SELECT statement that returns a specific value that reflects the maximum date in the load e.g. Select MAX(MyDate) FROM MyTable",
                            CheckResult.Fail));


            if(Strategy == DataLoadProgressUpdateStrategy.ExecuteScalarSQLInRAW)
                if (ExecuteScalarSQL.Contains("..") || ExecuteScalarSQL.Contains(".dbo."))
                    notifier.OnCheckPerformed(
                        new CheckEventArgs(
                            "Strategy is " + Strategy +
                            " but the SQL looks like it references explicit tables, In general RAW queries should use unqualified table names i.e. 'Select MAX(dt) FROM MyTable' NOT 'Select MAX(dt) FROM [MyLIVEDatabase]..[MyTable]'",
                            CheckResult.Warning));

            if (Strategy == DataLoadProgressUpdateStrategy.ExecuteScalarSQLInLIVE)
                if (!(ExecuteScalarSQL.Contains("..") || ExecuteScalarSQL.Contains(".dbo.")))
                    notifier.OnCheckPerformed(
                        new CheckEventArgs(
                            "Strategy is " + Strategy +
                            " but the SQL does not contain '..' or '.dbo.', LIVE update queries should use fully table names i.e. 'Select MAX(dt) FROM [MyLIVEDatabase]..[MyTable]' NOT 'Select MAX(dt) FROM MyTable'",
                            CheckResult.Warning));
                        
        }
    }
}
