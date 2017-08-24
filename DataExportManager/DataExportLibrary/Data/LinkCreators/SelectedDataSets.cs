﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.Interfaces.Data;
using DataExportLibrary.Interfaces.Data.DataTables;
using MapsDirectlyToDatabaseTable;
using Microsoft.Office.Interop.Excel;

namespace DataExportLibrary.Data.LinkCreators
{
    /// <summary>
    /// Usually when creating an ExtractionConfiguration you do not want to extract all the datasets (Catalogues).  SelectedDataSets represents the desire to extract a
    /// given dataset for a given ExtractableDataSet for a given ExtractionConfiguration.  
    /// </summary>
    public class SelectedDataSets : DatabaseEntity,ISelectedDataSets
    {
        #region Database Properties
        
        private int _extractionConfiguration_ID;
        private int _extractableDataSet_ID;
        private int? _rootFilterContainer_ID;

        private ExtractableDataSet _knownDataSet;

        public int ExtractionConfiguration_ID
        {
            get { return _extractionConfiguration_ID; }
            set { SetField(ref _extractionConfiguration_ID, value); }
        }
        public int ExtractableDataSet_ID
        {
            get { return _extractableDataSet_ID; }
            set { SetField(ref _extractableDataSet_ID, value); }
        }
        public int? RootFilterContainer_ID
        {
            get { return _rootFilterContainer_ID; }
            set { SetField(ref _rootFilterContainer_ID, value); }
        }


        #endregion

        #region Relationships

        [NoMappingToDatabase]
        public IContainer RootFilterContainer
        {
            get
            {
                return RootFilterContainer_ID == null
                    ? null
                    : Repository.GetObjectByID<FilterContainer>(RootFilterContainer_ID.Value);
            }
        }

        [NoMappingToDatabase]
        public ExtractionConfiguration ExtractionConfiguration { get
        {
            return Repository.GetObjectByID<ExtractionConfiguration>(ExtractionConfiguration_ID);
        } }

        [NoMappingToDatabase]
        public ExtractableDataSet ExtractableDataSet {
            get
            {
                return Repository.GetObjectByID<ExtractableDataSet>(ExtractableDataSet_ID); 
            }
        }

        #endregion

        public SelectedDataSets(IDataExportRepository repository, DbDataReader r)
            : base(repository, r)
        {
            ExtractionConfiguration_ID = Convert.ToInt32(r["ExtractionConfiguration_ID"]);
            ExtractableDataSet_ID = Convert.ToInt32(r["ExtractableDataSet_ID"]);
            RootFilterContainer_ID = ObjectToNullableInt(r["RootFilterContainer_ID"]);
        }

        public SelectedDataSets(IDataExportRepository repository, ExtractionConfiguration configuration, IExtractableDataSet dataSet, FilterContainer rootContainerIfAny)
        {
            repository.InsertAndHydrate(this,new Dictionary<string, object>()
            {
                {"ExtractionConfiguration_ID",configuration.ID},
                {"ExtractableDataSet_ID",dataSet.ID},
                {"RootFilterContainer_ID",rootContainerIfAny != null?(object) rootContainerIfAny.ID:DBNull.Value}
            });
        }

        public void SetKnownDataSet(ExtractableDataSet ds)
        {
            if(ds.ID != ExtractableDataSet_ID)
                throw new ArgumentException("That is not our dataset, our dataset has ID " +ExtractableDataSet_ID,"ds");

            _knownDataSet = ds;
        }

        public override string ToString()
        {
            if(_knownDataSet == null)
                return base.ToString();

            return _knownDataSet.ToString();
        }
    }
}
