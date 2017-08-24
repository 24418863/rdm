﻿using System;
using System.Data;
using System.IO;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataFlowPipeline.Requirements;
using CatalogueLibrary.DataHelper;
using LoadModules.Generic.DataFlowSources;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DataTableExtension;
using ReusableLibraryCode.Progress;

namespace DataExportLibrary
{
    public class CsvDataTableHelper : DataTableHelper, ICheckable, ICheckNotifier, IDataFlowSource<DataTable>, IResetableSource
    {
        public readonly string Filename;
        
        private readonly DelimitedFlatFileDataFlowSource _hostedSource;
        
        //regular constructor
        public CsvDataTableHelper(string filename, int padCharacterFieldsOutToMinimumLength)
        {
            Filename = filename;
            _padCharacterFieldsOutToMinimumLength = padCharacterFieldsOutToMinimumLength;

            _hostedSource = new DelimitedFlatFileDataFlowSource
            {
                Separator = ",",
                IgnoreBlankLines = true,
                UnderReadBehaviour = BehaviourOnUnderReadType.AppendNextLineToCurrentRow,
                MakeHeaderNamesSane = true,
                StronglyTypeInputBatchSize = -1,
                StronglyTypeInput = true
            };

            _hostedSource.PreInitialize(new FlatFileToLoad(new FileInfo(filename)),new ToConsoleDataLoadEventReceiver());//this is the file we want to load

        }

        public int PadCharacterFieldsOutToMinimumLength
        {
            get { return _padCharacterFieldsOutToMinimumLength; }
            set { _padCharacterFieldsOutToMinimumLength = value; }
        }


        public static string GetTableName(FileInfo prospectiveFileInfo)
        {
            return SqlSyntaxHelper.GetSensibleTableNameFromString(Path.GetFileNameWithoutExtension(prospectiveFileInfo.Name));
        }

        public bool OnCheckPerformed(CheckEventArgs args)
        {
            if (args.Result == CheckResult.Fail)
                throw new Exception("Check failed:" + args.Message, args.Ex);

            return false;
        }

        
        public virtual void Check(ICheckNotifier notifier)
        {
            try
            {
                if (DataTable == null)
                    LoadDataTableFromFile();
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Error occurred loading data from the file",CheckResult.Fail, e));
                return;
            }

            notifier.OnCheckPerformed(new CheckEventArgs("DataTable exists", CheckResult.Success, null));


            foreach (DataColumn col in DataTable.Columns)
            {
                string reason;
                if (!TableInfoImporter.IsValidEntityName(col.ColumnName, out reason))
                    notifier.OnCheckPerformed(new CheckEventArgs(reason, CheckResult.Fail));

            }
        }

        public void LoadDataTableFromFile()
        {
            DataTable =  _hostedSource.GetChunk(new ToConsoleDataLoadEventReceiver(), new GracefulCancellationToken());
        }

        public void Reset()
        {
            _haveGivenTableAlready = false;
        }

        private bool _haveGivenTableAlready = false;
        
        public DataTable GetChunk(IDataLoadEventListener job, GracefulCancellationToken cancellationToken)
        {
            if(_haveGivenTableAlready)
                return null;

            if(DataTable == null)
                LoadDataTableFromFile();
            
            _haveGivenTableAlready = true;

            return DataTable;
        }

        public void Dispose(IDataLoadEventListener job, Exception pipelineFailureExceptionIfAny)
        {
            _hostedSource.Dispose(job, pipelineFailureExceptionIfAny);
        }

        public void Abort(IDataLoadEventListener listener)
        {
            _hostedSource.Abort(listener);
        }

        public DataTable TryGetPreview()
        {
            if (DataTable == null)
                LoadDataTableFromFile();

            return DataTable;
        }
   }
}