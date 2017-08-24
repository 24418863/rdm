﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Cohort;
using CatalogueLibrary.QueryBuilding.Parameters;
using CatalogueLibrary.Repositories;
using MapsDirectlyToDatabaseTable;
using ReusableUIComponents.Annotations;

namespace CatalogueManager.ExtractionUIs.FilterUIs.ParameterUIs.Options
{
    public delegate ISqlParameter CreateNewSqlParameterHandler(ICollectSqlParameters collector);

    public class ParameterCollectionUIOptions
    {
        
        public ICollectSqlParameters Collector { get; set; }
        public ParameterLevel CurrentLevel { get; set; }
        public ParameterManager ParameterManager { get; set; }
        private CreateNewSqlParameterHandler _createNewParameterDelegate;

        public readonly ParameterRefactorer Refactorer = new ParameterRefactorer();

        public string UseCase { get; private set; }

        public readonly string[]  ProhibitedParameterNames = new string[]
        {
            
"@CohortDefinitionID",
"@ProjectNumber",
"@dateAxis",
"@currentDate",
"@dbName",
"@sql",
"@isPrimaryKeyChange",
"@Query",
"@Columns",
"@value",
"@pos",
"@len",
"@startDate",
"@endDate"};

        public ParameterCollectionUIOptions(string useCase, ICollectSqlParameters collector, ParameterLevel currentLevel, ParameterManager parameterManager ,CreateNewSqlParameterHandler createNewParameterDelegate = null)
        {

            UseCase = useCase;
            Collector = collector;
            CurrentLevel = currentLevel;
            ParameterManager = parameterManager;
            _createNewParameterDelegate = createNewParameterDelegate;

            if (_createNewParameterDelegate == null)
            {
                if (AnyTableSqlParameter.IsSupportedType(collector.GetType()))
                {
                    _createNewParameterDelegate = delegate
                    {
                        Random r = new Random();
                        var entity = (IMapsDirectlyToDatabaseTable) collector;
                        var newParam = new AnyTableSqlParameter((ICatalogueRepository)entity.Repository, entity,"DECLARE @" + r.Next(100) + " as varchar(10)");
                        newParam.Value = "'todo'";
                        newParam.SaveToDatabase();
                        return newParam;
                    };
                }
            }
        }

        public bool CanNewParameters()
        {
            return _createNewParameterDelegate != null;
        }

        public ISqlParameter CreateNewParameter()
        {
            return _createNewParameterDelegate(Collector);
        }


        public bool IsHigherLevel(ISqlParameter parameter)
        {
            return ParameterManager.GetLevelForParameter(parameter) > CurrentLevel;
        }

    }
}