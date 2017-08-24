using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using DataLoadEngine.Attachers;
using DataLoadEngine.Job;
using LoadModules.Generic.Exceptions;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.Progress;
using DataTable = System.Data.DataTable;


namespace LoadModules.Generic.Attachers
{
    [Description(
        @"Base class for an Attacher which expects to be passed a Filepath which is the location of a textual file in which values for a single DataTable are stored (e.g. csv or fixed width etc).  This attacher requires that the RAW database server be setup and contain the correct tables for loading (it is likely that the DataLoadEngine handles all this - as a user you dont need to worry about this)."
        )]
    public abstract class FlatFileAttacher : Attacher, IPluginAttacher
    {

        [DemandsInitialization("The file to attach, e.g. \"*hic*.csv\" - this is NOT a Regex", Mandatory = true)]
        public string FilePattern { get; set; }

        [DemandsInitialization("The table name to load e.g. \"My Table1\" (should not contain wrappers such as square brackets)",Mandatory=true)]
        public string TableName { get; set; }

        [DemandsInitialization("Determines the behaviour of the system when no files are matched by FilePattern.  If true the entire data load process immediately stops with exit code LoadNotRequired, if false then the load proceeds as normal (useful if for example if you have multiple Attachers and some files are optional)")]
        public bool SendLoadNotRequiredIfFileNotFound { get; set; }
        
        public FlatFileAttacher()
            : base(true)
        {
            
        }

        public override ExitCodeType Attach(IDataLoadJob job)
        {
            var baseResult = base.Attach(job);

            if (baseResult != ExitCodeType.Success)
                throw new Exception("Base class for "+this.GetType().FullName+" failed to return ExitCodeType.Success");

            Stopwatch timer = new Stopwatch();
            timer.Start();


            if(string.IsNullOrWhiteSpace(TableName))
                throw new ArgumentNullException("TableName has not been set, set it in the DataCatalogue");

            DiscoveredTable table = _dbInfo.ExpectTable(TableName);

            //table didnt exist!
            if (!table.Exists())
                if (!_dbInfo.DiscoverTables(false).Any())//maybe no tables existed
                    throw new FlatFileLoadException("Raw database had 0 tables we could load");
                else//no there are tables just not the one we were looking for
                    throw new FlatFileLoadException("RAW database did not have a table called:" + TableName);

            
            //load the flat file
            var filepattern = FilePattern ?? "*";

            var filesToLoad = HICProjectDirectory.ForLoading.EnumerateFiles(filepattern).ToList();

            if (!filesToLoad.Any())
            {
                job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Warning,  "Did not find any files matching pattern " + filepattern + " in forLoading directory"));
                
                if(SendLoadNotRequiredIfFileNotFound)
                    return ExitCodeType.OperationNotRequired;

                return ExitCodeType.Success;
            }

            foreach (var fileToLoad in filesToLoad)
                LoadFile(table, fileToLoad, _dbInfo, timer, job);

            timer.Stop();

            return ExitCodeType.Success;
        }

        private void LoadFile(DiscoveredTable tableToLoad, FileInfo fileToLoad, DiscoveredDatabase dbInfo, Stopwatch timer, IDataLoadJob job)
        {
            using (var con = dbInfo.Server.GetConnection())
            {
                DataTable dt = CreateDataTableForTarget(tableToLoad,job);

                // setup bulk insert it into destination
                SqlBulkCopy insert = new SqlBulkCopy((SqlConnection) con);
                insert.BulkCopyTimeout = 500000;

                //bulk insert ito destination
                insert.DestinationTableName = tableToLoad.GetRuntimeName();

                job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "About to open file " + fileToLoad.FullName));
                OpenFile(fileToLoad,job);

                //confirm the validity of the headers
                ConfirmFlatFileHeadersAgainstDataTable(dt,job);

                //work out mappings
                foreach (DataColumn column in dt.Columns)
                    insert.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                con.Open();

                //now we will read data out of the file in batches
                int batchNumber = 1;
                int maxBatchSize = 10000;
                int recordsCreatedSoFar = 0;
                

                try
                {
                    //while there is data to be loaded into table 
                    while (IterativelyBatchLoadDataIntoDataTable(dt, maxBatchSize) != 0)
                    {
                        DropEmptyColumns(dt);
                        ConfirmFitToDestination(dt, tableToLoad, job);
                        try
                        {
                            recordsCreatedSoFar += UsefulStuff.BulkInsertWithBetterErrorMessages(insert, dt, dbInfo.Server);
                            dt.Rows.Clear(); //very important otherwise we add more to the end of the table but still insert last batches records resulting in exponentially multiplying upload sizes of duplicate records!

                            job.OnProgress(this,
                                new ProgressEventArgs(dbInfo.GetRuntimeName(),
                                    new ProgressMeasurement(recordsCreatedSoFar, ProgressType.Records), timer.Elapsed));
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Error processing batch number " + batchNumber + " (of batch size " + maxBatchSize+")",e);
                        } 
                    }
                }
                catch (Exception e)
                {
                    throw new FlatFileLoadException("Error processing file " + fileToLoad, e);
                }
                finally
                {
                    CloseFile();
                }
            }
        }

        protected abstract void OpenFile(FileInfo fileToLoad,IDataLoadEventListener listener);
        protected abstract void CloseFile();
        
        public override void Check(ICheckNotifier notifier)
        {
            if (string.IsNullOrWhiteSpace(TableName))
                notifier.OnCheckPerformed(new CheckEventArgs("Argument TableName has not been set on " + this + ", you should specify this value in the LoadMetadataUI" ,CheckResult.Fail));

            if (string.IsNullOrWhiteSpace(FilePattern))
                notifier.OnCheckPerformed(new CheckEventArgs("Argument FilePattern has not been set on " + this + ", you should specify this value in the LoadMetadataUI", CheckResult.Fail));
        }

        private DataTable CreateDataTableForTarget(DiscoveredTable table,IDataLoadJob job)
        {
            DataTable dt = new DataTable();

            DiscoveredColumn[] columns = table.DiscoverColumns().ToArray();
            
            //add the columns found on the database by name to the DataTable
            foreach (var listColumn in columns.Select(c => c.GetRuntimeName()))
                dt.Columns.Add(listColumn);
            
            //now setup the types for those columns
            SetupTypes(dt,columns, table,job);
            
            return dt;
        }
        

        private void ConfirmFitToDestination(DataTable dt, DiscoveredTable tableToLoad,IDataLoadJob job)
        {

            var columnsAtDestination = tableToLoad.DiscoverColumns().Select(c=>c.GetRuntimeName()).ToArray();

            //see if there is a shape problem between stuff that is on the server and stuff that is in the flat file
            if (dt.Columns.Count != columnsAtDestination.Length)
                job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Warning,"There was a mismatch between the number of columns in the flat file (" +
                    columnsAtDestination.Aggregate((s, n) => s + Environment.NewLine + n) +
                    ") and the number of columns in the RAW database table (" + dt.Columns.Count + ")"));
            
            foreach (DataColumn column in dt.Columns)
                if (!columnsAtDestination.Contains(column.ColumnName))
                    throw new FlatFileLoadException("Column in flat file called " + column.ColumnName +
                                                    " does not appear in the RAW database table (after fixing potentially silly names)");

       }

        protected void
            SetupTypes(DataTable dt, DiscoveredColumn[] columnsAtDestination, DiscoveredTable table, IDataLoadJob job)
        {
            foreach (DiscoveredColumn columnAtDestination in columnsAtDestination)
            {
                string type = columnAtDestination.DataType.SQLType;
                string name = columnAtDestination.GetRuntimeName();

                //remove (x) from varchar(x)
                if (type.Contains("("))
                    type = type.Substring(0, type.IndexOf("("));

                if (dt.Columns[name] == null)//missing column
                {
                    job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Warning, "Column missing from DataTable (loaded from flat file) " + columnAtDestination + " (Column is present at destination database but not in datatable, so skipping it)"));
                    continue;//skip it
                }
                
                switch (type)
                {
                    case "decimal":
                        dt.Columns[name].DataType = typeof(decimal);
                        dt.Columns[name].AllowDBNull = true;
                        break;
                    case "numeric":
                        dt.Columns[name].DataType = typeof(decimal);
                        dt.Columns[name].AllowDBNull = true;
                        break;
                    case "char":
                        dt.Columns[name].DataType = typeof(string);
                        dt.Columns[name].AllowDBNull = true;
                        break;
                    case "nchar":
                        dt.Columns[name].DataType = typeof(string);
                        dt.Columns[name].AllowDBNull = true;
                        break;
                    case "varchar":
                        dt.Columns[name].DataType = typeof (string);
                        dt.Columns[name].AllowDBNull = true;
                        break;
                    case "nvarchar":
                        dt.Columns[name].DataType = typeof(string);
                        dt.Columns[name].AllowDBNull = true;
                        break;
                  case "datetime2":
                        dt.Columns[name].DataType = typeof(DateTime);
                        dt.Columns[name].AllowDBNull = true;
                        break;
                  case "datetime":
                        dt.Columns[name].DataType = typeof (DateTime);
                        dt.Columns[name].AllowDBNull = true;
                        break;
                    case "date":
                        dt.Columns[name].DataType = typeof(DateTime);
                        dt.Columns[name].AllowDBNull = true;
                        break;
                    case "text":
                        dt.Columns[name].DataType = typeof(string);
                        dt.Columns[name].AllowDBNull = true;
                        break;
                    case "int":
                        dt.Columns[name].DataType = typeof(int);
                        dt.Columns[name].AllowDBNull = true;
                        break;
                    case "float":
                        dt.Columns[name].DataType = typeof(decimal);
                        dt.Columns[name].AllowDBNull = true;
                        break;
               
                    case "bit":
                        dt.Columns[name].DataType = typeof(Boolean);
                        dt.Columns[name].AllowDBNull = true;
                        break;
                    default:
                        job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Warning,"SetupTypes is not sure what type of data table column to use for SQL type :" + type));
                        break;
                }
            }

        }

        /// <summary>
        /// DataTable dt is a copy of what is in RAW, your job (if you choose to accept it) is to look in your file and work out what headers you can see
        /// and then complain to job (or throw) if what you see in the file does not match the RAW target
        /// </summary>
        protected abstract void ConfirmFlatFileHeadersAgainstDataTable(DataTable loadTarget,IDataLoadJob job);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="reader"></param>
        /// <param name="maxBatchSize"></param>
        /// <returns>return the number of rows read, if you return >0 then you will be called again to get more data (if during this second or subsequent call there is no more data to read from source, return 0)</returns>
        protected abstract int IterativelyBatchLoadDataIntoDataTable(DataTable dt, int maxBatchSize);
        

        private void DropEmptyColumns(DataTable dt)
        {
            Regex emptyColumnsSyntheticNames = new Regex("^Column[0-9]+$");

            //deal with any ending columns which have nothing but whitespace
            for (int i = dt.Columns.Count - 1; i >= 0; i--)
            {
                if (emptyColumnsSyntheticNames.IsMatch(dt.Columns[i].ColumnName) || string.IsNullOrWhiteSpace(dt.Columns[i].ColumnName)) //is synthetic column or blank, nuke it
                {
                    bool foundValue = false;
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dr.ItemArray[i] == null)
                            continue;

                        if (string.IsNullOrWhiteSpace(dr.ItemArray[i].ToString()))
                            continue;

                        foundValue = true;
                        break;
                    }
                    if (!foundValue)
                        dt.Columns.Remove(dt.Columns[i]);
                }
            }
        }
        
        protected virtual object HackValueReadFromFile(string s)
        {
            
            return s;
        }

        public override void LoadCompletedSoDispose(ExitCodeType exitCode,IDataLoadEventListener postLoadEventListener)
        {
            
        }
    }
}