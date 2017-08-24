﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data.Aggregation;
using CatalogueLibrary.Data.Dashboarding;
using CatalogueManager.ObjectVisualisation;
using CohortManagerLibrary.QueryBuilding;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode;
using ReusableLibraryCode.DataAccess;

namespace CatalogueManager.DataViewing.Collections
{
    public class ViewAggregateExtractUICollection : IViewSQLAndResultsCollection
    {
        public PersistStringHelper Helper { get; private set; }
        public List<IMapsDirectlyToDatabaseTable> DatabaseObjects { get; set; }

        public ViewAggregateExtractUICollection()
        {
            Helper = new PersistStringHelper();
            DatabaseObjects = new List<IMapsDirectlyToDatabaseTable>();
        }

        public ViewAggregateExtractUICollection(AggregateConfiguration config):this()
        {
            DatabaseObjects.Add(config);
        }

        public string SaveExtraText()
        {
            return "";
        }

        public void LoadExtraText(string s)
        {
            
        }

        public IHasDependencies GetAutocompleteObject()
        {
            return AggregateConfiguration;
        }

        public void SetupRibbon(RDMPObjectsRibbonUI ribbon)
        {
            ribbon.Add(AggregateConfiguration);
        }

        public IDataAccessPoint GetDataAccessPoint()
        {
            var dim = AggregateConfiguration.AggregateDimensions.FirstOrDefault();

            if (dim == null)
                return null;

            return dim.ColumnInfo.TableInfo;
        }

        public string GetSql()
        {
            string sql = "";
            var ac = AggregateConfiguration;

            if (ac.IsCohortIdentificationAggregate)
            {
                var cic = ac.GetCohortIdentificationConfigurationIfAny();
                var isJoinable = ac.IsJoinablePatientIndexTable();
                var globals = cic.GetAllParameters();

                var builder = new CohortQueryBuilder(ac, globals, isJoinable);

                sql = builder.GetDatasetSampleSQL(100);
            }
            else
            {
                var builder = ac.GetQueryBuilder();
                sql = builder.SQL;
            }

            return sql;
        }

        public string GetTabName()
        {
            return "View Top 100 " + AggregateConfiguration;
        }

        AggregateConfiguration AggregateConfiguration { get
        {
            return DatabaseObjects.OfType<AggregateConfiguration>().SingleOrDefault();
        } }
    }
}