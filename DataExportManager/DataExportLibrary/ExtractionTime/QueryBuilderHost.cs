﻿using System;
using System.Collections.Generic;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.QueryBuilding;
using CatalogueLibrary.Repositories;
using DataExportLibrary.Interfaces.Data.DataTables;
using DataExportLibrary.Data;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.ExtractionTime.Commands;
using DataExportLibrary.ExtractionTime.ExtractionPipeline;
using MapsDirectlyToDatabaseTable;

namespace DataExportLibrary.ExtractionTime
{
    public class QueryBuilderHost
    {
        private readonly IDataExportRepository _repository;

        public QueryBuilderHost(IDataExportRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// This produces the SQL that would retrieve the specified dataset columns including any JOINS 
        /// 
        /// It uses:
        /// QueryBuilder in the data CatalogueLibrary and then it adds some custom lines for linking to the cohort
        /// </summary>
        /// <returns></returns>
        public QueryBuilder GetSQLCommandForFullExtractionSet(ExtractDatasetCommand request, out List<ReleaseIdentifierSubstitution> substitutions)
        {
            if(request.QueryBuilder != null)
                throw new Exception("Creation of a QueryBuilder from a request can only happen once, to access the results of the creation use the cached answer in the request.QueryBuilder property");
            
            if (!request.ColumnsToExtract.Any())
                throw new Exception("No columns are marked for extraction in this configuration");

            if(request.ExtractableCohort == null)
                throw new NullReferenceException("No Cohort selected");

            substitutions = new List<ReleaseIdentifierSubstitution>();
            
            switch (request.ColumnsToExtract.Count(c => c.IsExtractionIdentifier))
            { 
                //no extraction identifiers
                case 0: throw new Exception("There are no Columns in this dataset marked as IsExtractionIdentifier"); 
                    
                //a single extraction identifier e.g. CHI X died on date Y with conditions a,b and c
                case 1: substitutions.Add(new ReleaseIdentifierSubstitution(request.ColumnsToExtract.FirstOrDefault(c => c.IsExtractionIdentifier), request.ExtractableCohort, false));
                    break;

                //multiple extraction identifiers e.g. Mother X had Babies A, B, C where A,B and C are all CHIs that must be subbed for ProCHIs
                default:
                    foreach (IColumn columnToSubstituteForReleaseIdentifier in request.ColumnsToExtract.Where(c=>c.IsExtractionIdentifier))
                        substitutions.Add(new ReleaseIdentifierSubstitution(columnToSubstituteForReleaseIdentifier, request.ExtractableCohort, true));
                    break;
            }

            var configurationProperties = new ConfigurationProperties(false, _repository);
            
            string hashingAlgorithm = configurationProperties.TryGetValue(ConfigurationProperties.ExpectedProperties.HashingAlgorithmPattern);
            if (string.IsNullOrWhiteSpace(hashingAlgorithm))
                hashingAlgorithm = null;
            
            QueryBuilder queryBuilder = new QueryBuilder("DISTINCT " + request.LimitationSql,hashingAlgorithm);
         
            queryBuilder.SetSalt(request.Salt.GetSalt());

            //add the constant parameters
            foreach (ConstantParameter parameter in GetConstantParameters(request.Configuration, request.ExtractableCohort))
                queryBuilder.ParameterManager.AddGlobalParameter(parameter);

            //add the global parameters
            foreach (var globalExtractionFilterParameter in request.Configuration.GlobalExtractionFilterParameters)
                queryBuilder.ParameterManager.AddGlobalParameter(globalExtractionFilterParameter);

            //remove the identification column from the query
            request.ColumnsToExtract.RemoveAll(c=>c.IsExtractionIdentifier);

            //add in the ReleaseIdentifier in place of the identification column
            queryBuilder.AddColumnRange(substitutions.ToArray());

            //add the rest of the columns to the query
            queryBuilder.AddColumnRange(request.ColumnsToExtract.Cast<IColumn>().ToArray());

            //add the users selected filters
            queryBuilder.RootFilterContainer = request.Configuration.GetFilterContainerFor(request.DatasetBundle.DataSet);
            
            ExternalCohortTable externalCohortTable = _repository.GetObjectByID<ExternalCohortTable>(request.ExtractableCohort.ExternalCohortTable_ID);

            if (request.ExtractableCohort != null)
            {
                //the JOIN with the cohort table:
                string cohortJoin;

                if (substitutions.Count == 1)
                    cohortJoin = " INNER JOIN " + externalCohortTable.TableName + " ON " + substitutions.First().JoinSQL;
                else
                {
                    cohortJoin = substitutions.Aggregate(" INNER JOIN " + externalCohortTable.TableName + " ON ",
                                                         (seed, next) => seed + next.JoinSQL + " OR ");

                    cohortJoin = cohortJoin.Substring(0, cohortJoin.Length - " OR ".Length);

                }
                //add the JOIN in after any other joins
                queryBuilder.AddCustomLine(cohortJoin, QueryComponent.JoinInfoJoin);

                //if there is a custom table then make sure to also join to that (e.g. date registered in study)
                foreach (string customTableJoinSql in request.ExtractableCohort.GetCustomTableJoinSQLIfExists(queryBuilder))
                    queryBuilder.AddCustomLine(customTableJoinSql, QueryComponent.JoinInfoJoin);


                //add the filter cohortID because our new Cohort system uses ID number and a giant combo table with all the cohorts in it we need to say Select XX from XX join Cohort Where Cohort number = Y
                queryBuilder.AddCustomLine(request.ExtractableCohort.WhereSQL(), QueryComponent.Filter);
            }

            request.QueryBuilder = queryBuilder;
            return queryBuilder;
        }

        public static List<ConstantParameter> GetConstantParameters(IExtractionConfiguration configuration, IExtractableCohort extractableCohort)
        {
            List<ConstantParameter> toReturn = new List<ConstantParameter>();

            IProject project = configuration.Project;

            if (project.ProjectNumber == null)
                throw new Exception("Project number has not been entered, cannot create constant paramaters");
            
            if(extractableCohort == null)
                throw new Exception("Cohort has not been selected, cannot create constant parameters");

            IExternalCohortTable externalCohortTable = extractableCohort.ExternalCohortTable;

            toReturn.Add(new ConstantParameter("DECLARE @CohortDefinitionID AS int", extractableCohort.OriginID.ToString(), "The ID of the cohort in " + externalCohortTable.TableName));
            toReturn.Add(new ConstantParameter("DECLARE @ProjectNumber as int", project.ProjectNumber.ToString(),"The project number of project " + project.Name));

            return toReturn;
        }

        public static List<ConstantParameter> PreviewConstantParameters(ExtractionConfiguration configuration)
        {
            if (configuration.Cohort_ID != null)
                return GetConstantParameters(configuration, configuration.Cohort);

            List<ConstantParameter> toReturn = new List<ConstantParameter>();

            IProject project = configuration.Project;

            if (project.ProjectNumber == null)
                throw new Exception("Project number has not been entered, cannot create constant paramaters");
            
            toReturn.Add(new ConstantParameter("DECLARE @CohortDefinitionID AS int", "<<CohortNotYetSelected>>", "The ID of the cohort table once you have picked it"));
            toReturn.Add(new ConstantParameter("DECLARE @ProjectNumber as int", project.ProjectNumber.ToString(), "The project number of project " + project.Name));

            return toReturn;
        }

   
   
    }
}