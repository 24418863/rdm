﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using DataExportLibrary.Interfaces.Data.DataTables;
using MapsDirectlyToDatabaseTable;

namespace DataExportLibrary.Data.DataTables
{
    /// <summary>
    /// All extractions through DataExportManager must be done through Projects.  A Project has a name, extraction directory and optionally Tickets (if you have a ticketing system 
    /// configured) and DataUsers.  A Project should never be deleted even after all ExtractionConfigurations have been executed as it serves as an audit and a cloning point if you 
    /// ever need to clone any of the ExtractionConfigurations (e.g. to do an update of project data 5 years on).
    /// 
    /// The ProjectNumber must match the project number of the cohorts in your cohort database.  Therefore it is not possible to share a single cohort between multiple Projects. 
    /// </summary>
    public class Project : VersionedDatabaseEntity, IProject,INamed
    {
        #region Database Properties
        private string _name;
        private string _masterTicket;
        private string _extractionDirectory;
        private int? _projectNumber;

        public string Name
        {
            get { return _name; }
            set { SetField(ref _name, value); }
        }
        public string MasterTicket
        {
            get { return _masterTicket; }
            set { SetField(ref _masterTicket, value); }
        }
        [AdjustableLocation]
        public string ExtractionDirectory
        {
            get { return _extractionDirectory; }
            set { SetField(ref _extractionDirectory, value); }
        }
        public int? ProjectNumber
        {
            get { return _projectNumber; }
            set { SetField(ref _projectNumber, value); }
        }

        #endregion
        public static int Name_MaxLength = -1;
        public static int MasterTicket_MaxLength = -1;
        public static int ExtractionDirectory_MaxLength = -1;

        #region Relationships
        [NoMappingToDatabase]
        public IEnumerable<IDataUser> DataUsers
        {
            get
            {
                return Repository.SelectAll<DataUser>("SELECT * FROM Project_DataUser WHERE Project_ID=" + ID, "DataUser_ID");
            }
        }

        [NoMappingToDatabase]
        public IExtractionConfiguration[] ExtractionConfigurations
        {
            get
            {
                return Repository.GetAllObjectsWithParent<ExtractionConfiguration>(this)
                    .Cast<IExtractionConfiguration>()
                    .ToArray();
            }
        }
        #endregion

        /// <summary>
        /// Defines a new extraction project this is stored in the DataExportManager2 database and the ID of the new record is returned by this
        ///  method, use GetProjectByID to get the object back from the database
        /// </summary>
        public Project(IDataExportRepository repository, string name)
        {
            Repository = repository;

            try
            {
                Repository.InsertAndHydrate(this, new Dictionary<string, object>
                {
                    {"Name", name}
                });
            }
            catch (Exception ex)
            {
                //sometimes the user tries to create multiple Projects without fully populating the last one (with a project number)
                if (ex.Message.Contains("idx_ProjectNumberMustBeUnique"))
                {
                    Project offender;
                    try
                    {
                        //find the one with the unset project number
                        offender = Repository.GetAllObjects<Project>().Single(p => p.ProjectNumber == null);
                    }
                    catch (Exception)
                    {
                        throw ex;
                    }
                    throw new Exception("Could not create a new Project because there is already another Project in the system (" + offender + ") which is missing a Project Number.  All projects must have a ProjectNumber, there can be 1 Project at a time which does not have a number and that is one that is being built by the user right now.  Either delete Project " + offender + " or give it a project number", ex);

                }

                throw;
            }
        }

        public Project(IDataExportRepository repository, DbDataReader r)
            : base(repository, r)
        {
            MasterTicket = r["MasterTicket"].ToString();
            Name = r["Name"] as string;
            ExtractionDirectory = r["ExtractionDirectory"] as string;

            if (r["ProjectNumber"] == null || r["ProjectNumber"] == DBNull.Value)
                ProjectNumber = null;
            else
                ProjectNumber = int.Parse(r["ProjectNumber"].ToString());
        }

        public override string ToString()
        {
            if (ProjectNumber == null)
                return Name;
            
            return ProjectNumber + "_"+ Name;
        }
        
        #region Stuff for updating our internal database records

        public int CountCohorts()
        {
            //get those which have cohorts and get the unique cohort ids amongsth them
            return ExtractionConfigurations.Where(config => config.Cohort_ID != null).Select(c => c.Cohort_ID).Distinct().Count();
        }
        #endregion
    }
}