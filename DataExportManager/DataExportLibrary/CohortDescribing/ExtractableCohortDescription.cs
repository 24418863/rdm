﻿using System;
using System.Data;
using System.Linq;
using DataExportLibrary.Data.DataTables;
using HIC.Common.Validation.Constraints.Primary;
using Microsoft.Office.Interop.Excel;
using ReusableLibraryCode;

namespace DataExportLibrary.CohortDescribing
{
    public class ExtractableCohortDescription
    {
        public readonly ExtractableCohort Cohort;
        public readonly CohortDescriptionDataTableAsyncFetch Fetch;
        public int Count;
        public int CountDistinct;

        public string SourceName;
        public string ReleaseIdentifier;
        public string PrivateIdentifier;

        public int ProjectNumber;
        public int Version;
        public string Description;

        public int OriginID;

        public string CustomTables;

        public Exception Exception { get; private set; }

        public DateTime? CreationDate { get; set; }


        /// <summary>
        /// Creates a non async cohort description, this will block until counts are available for the cohort
        /// </summary>
        /// <param name="cohort"></param>
        public ExtractableCohortDescription(ExtractableCohort cohort)
        {
            Cohort = cohort;

            try
            {
                Count = cohort.Count;
            }
            catch (Exception e)
            {
                Exception = e;
                Count = -1;
            }

            try
            {
                CountDistinct = cohort.CountDistinct;
            }
            catch (Exception e)
            {
                CountDistinct = -1;
                Exception = e;
                throw;
            }
            OriginID = cohort.OriginID;

            

            try
            {
                ReleaseIdentifier = cohort.GetReleaseIdentifier();
            }
            catch (Exception e)
            {
                ReleaseIdentifier = "Unknown";
            }

            CustomTables = string.Join(",",cohort.GetCustomTableNames());

            try
            {
                PrivateIdentifier = cohort.GetPrivateIdentifier();
            }
            catch (Exception e)
            {
                PrivateIdentifier = "Unknown";
            }

            var externalData = cohort.GetExternalData();
            SourceName = externalData.ExternalCohortTableName;
            Version = externalData.ExternalVersion;
            CreationDate = externalData.ExternalCohortCreationDate;
            ProjectNumber = externalData.ExternalProjectNumber;
            Description = externalData.ExternalDescription;

        }


        /// <summary>
        /// Creates a new description based on the async fetch request for all cohorts including row counts etc (which might have already completed btw).  If you use this constructor
        /// then the properties will start out with text like "Loading..." but it will perform much faster, when the fetch completes the values will be populated.  In general if you
        /// want to use this feature you should probably use CohortDescriptionFactory and only use it if you are trying to get all the cohorts at once.
        /// 
        /// </summary>
        /// <param name="cohort"></param>
        /// <param name="fetch"></param>
        public ExtractableCohortDescription(ExtractableCohort cohort, CohortDescriptionDataTableAsyncFetch fetch)
        {
            Cohort = cohort;
            Fetch = fetch;
            OriginID = cohort.OriginID;
            Count = -1;
            CountDistinct = -1;
            SourceName = fetch.Source.Name;
            
            try
            {
                ReleaseIdentifier = SqlSyntaxHelper.GetRuntimeName(fetch.Source.GetReleaseIdentifier(cohort));
            }
            catch (Exception e)
            {
                ReleaseIdentifier = "Unknown";
            }
            
            CustomTables = "Loading...";
            
            try
            {
                PrivateIdentifier = SqlSyntaxHelper.GetRuntimeName(fetch.Source.PrivateIdentifierField);
            }
            catch (Exception e)
            {
                PrivateIdentifier = "Unknown";
            }
            
            ProjectNumber = -1;
            Version = -1;
            Description = "Loading...";

            //if it's already finished
            if (fetch.Task != null && fetch.Task.IsCompleted)
                FetchOnFinished();
            else
                fetch.Finished += FetchOnFinished;
        }

        

        private void FetchOnFinished()
        {
            try
            {
                if (Fetch.Task.IsFaulted)
                    throw new Exception("Fetch cohort data failed for source " + Fetch.Source + " see inner Exception for details" ,Fetch.Task.Exception);

                if (Fetch.DataTable == null)
                    throw new Exception("IsFaulted was false but DataTable was not populated for fetch " + Fetch.Source);
            
                if(Fetch.CustomDataTable == null)
                    throw new Exception("IsFaulted was false but CustomDataTable was not populated for fetch " + Fetch.Source);

                var row = Fetch.DataTable.Rows.Cast<DataRow>().FirstOrDefault(r => Convert.ToInt32(r["OriginID"]) == OriginID);
             
                if(row == null)
                    throw new Exception("No row found for Origin ID " + OriginID + " in fetched cohort description table for source " + Fetch.Source);

            

                //it's overriden ugh, got to go the slow way
                if (!string.IsNullOrWhiteSpace(Cohort.OverrideReleaseIdentifierSQL))
                {
                    Count = Cohort.Count;
                    CountDistinct = Cohort.CountDistinct;
                }
                else
                {
                    //it's a proper not overriden release identifier so we can use the DataTable value
                    Count = Convert.ToInt32(row["Count"]);
                    CountDistinct = Convert.ToInt32(row["CountDistinct"]);
                
                }

                ProjectNumber = Convert.ToInt32(row["ProjectNumber"]);
                Version = Convert.ToInt32(row["Version"]); ;
                Description =  row["Description"] as string;
                CreationDate = ObjectToNullableDateTime(row["dtCreated"]);

                var rows = Fetch.CustomDataTable.Rows.Cast<DataRow>().Where(r => Convert.ToInt32(r["OriginID"]) == OriginID).ToArray();
            
                CustomTables = !rows.Any() ? "" : string.Join(",", rows.Select(r => r["CustomTableName"]));
            }
            catch (Exception e)
            {
                Exception = e;
            }
        }

        
        public override string ToString()
        {
            return Cohort.ToString();
        }

        private DateTime? ObjectToNullableDateTime(object o)
        {
            if (o == null || o == DBNull.Value)
                return null;

            return (DateTime)o;
        }

    }
}