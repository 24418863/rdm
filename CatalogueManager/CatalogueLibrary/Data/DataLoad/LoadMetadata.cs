﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using CatalogueLibrary.Repositories;
using MapsDirectlyToDatabaseTable;
using MapsDirectlyToDatabaseTable.Revertable;
using Microsoft.SqlServer.Management.Smo;
using ReusableLibraryCode;
using ReusableLibraryCode.DataAccess;
using ReusableLibraryCode.DatabaseHelpers.Discovery;

namespace CatalogueLibrary.Data.DataLoad
{
    public enum SchedulePeriod
    {
        Continuous = 0,
        Day = 1,
        Week = 2,
        Month = 3,
        Hour = 4
    }

    public enum CacheArchiveType
    {
        None = 0,
        Zip = 1,
        Tar = 2
    }



    /// <summary>
    /// Entrypoint to the loading metadata for one or more Catalogues. This includes name, description, scheduled start dates etc.  All other loading
    /// entities are attached to this entity for example each load Process (Unzip files called *.zip / Dowload all files from FTP server X) contains
    /// a reference to the LoadMetadata that it belongs in.
    /// 
    /// A LoadMetadata also allows you to override various settings such as forcing a specific alternate server to load - for when you want to overule
    /// the location that TableInfo thinks data is on e.g. into a test environment mirror of live.
    /// </summary>
    public class LoadMetadata : VersionedDatabaseEntity, IDeleteable, ILoadMetadata, IHasDependencies, IRevertable, INamed
    {


        #region Database Properties
        private string _locationOfFlatFiles;
        private string _anonymisationEngineClass;
        private string _name;
        private string _description;
        private CacheArchiveType _cacheArchiveType;
        
        [AdjustableLocation]
        public string LocationOfFlatFiles
        {
            get { return _locationOfFlatFiles; }
            set { SetField(ref _locationOfFlatFiles, value); }
        }
        public string AnonymisationEngineClass
        {
            get { return _anonymisationEngineClass; }
            set { SetField(ref _anonymisationEngineClass, value); }
        }
        public string Name
        {
            get { return _name; }
            set { SetField(ref _name, value); }
        }
        public string Description
        {
            get { return _description; }
            set { SetField(ref _description, value); }
        }
        public CacheArchiveType CacheArchiveType
        {
            get { return _cacheArchiveType; }
            set { SetField(ref _cacheArchiveType, value); }
        }

        #endregion

        
        public static int LocationOfFlatFiles_MaxLength = -1;

        #region Relationships
        [NoMappingToDatabase]
        public ILoadProgress[] LoadProgresses
        {
            get { return Repository.GetAllObjectsWithParent<LoadProgress>(this); }
        }
        [NoMappingToDatabase]
        public IOrderedEnumerable<ProcessTask> ProcessTasks
        {
            get
            {
                return
                    Repository.GetAllObjectsWithParent<ProcessTask>(this).OrderBy(pt => pt.Order);
            }
        }
        [NoMappingToDatabase]
        public LoadPeriodically LoadPeriodically {
            get { return Repository.GetAllObjectsWithParent<LoadPeriodically>(this).SingleOrDefault(); }
        }
        #endregion

        public LoadMetadata(ICatalogueRepository repository, string name = null)
        {
            if (name == null)
                name = "NewLoadMetadata" + Guid.NewGuid();
            repository.InsertAndHydrate(this,new Dictionary<string, object>()
            {
                {"Name",name}
            });
        }

        public LoadMetadata(ICatalogueRepository repository, DbDataReader r)
            : base(repository, r)
        {
            LocationOfFlatFiles = r["LocationOfFlatFiles"].ToString();
            Name = r["Name"] as string;
            AnonymisationEngineClass = r["AnonymisationEngineClass"].ToString();
            Name = r["Name"].ToString();
            Description = r["Description"] as string;//allows for nulls
            CacheArchiveType = (CacheArchiveType)r["CacheArchiveType"];
        }
        
        public override void DeleteInDatabase()
        {
            ICatalogue firstOrDefault = GetAllCatalogues().FirstOrDefault();

            if (firstOrDefault != null)
                throw new Exception("This load is used by " + firstOrDefault.Name + " so cannot be deleted (Disassociate it first)");

            base.DeleteInDatabase();
        }
        
        
        public override string ToString()
        {
            return Name;
        }
        
        public IEnumerable<ICatalogue> GetAllCatalogues()
        {
            return Repository.GetAllObjectsWithParent<Catalogue>(this);
        }

        public IEnumerable<ILoadProgress> GetLoadProgresses()
        {
            return LoadProgresses;
        }

        public IEnumerable<ProcessTask> GetAllProcessTasks(bool includeDisabled)
        {
            if (includeDisabled)
                return ProcessTasks;

            return ProcessTasks.Where(pt => pt.IsDisabled == false);
        }

        public DiscoveredServer GetDistinctLoggingDatabaseSettings(out IExternalDatabaseServer serverChosen,bool testserver = false)
        {
            var loggingServers = GetLoggingServers(testserver);

            var loggingServer = loggingServers.FirstOrDefault();

            //get distinct connection
            var toReturn = DataAccessPortal.GetInstance().ExpectDistinctServer(loggingServers, DataAccessContext.Logging, true);

            serverChosen = (IExternalDatabaseServer)loggingServer;
            return toReturn;
        }

        public DiscoveredServer GetDistinctLoggingDatabaseSettings(bool testserver = false)
        {
            IExternalDatabaseServer whoCares;
            return GetDistinctLoggingDatabaseSettings(out whoCares, testserver);
        }

        private IDataAccessPoint[] GetLoggingServers(bool testServer)
        {
            ICatalogue[] catalogue = GetAllCatalogues().ToArray();

            if (!catalogue.Any())
                throw new NotSupportedException("LoadMetaData '" + ToString() + " (ID=" + ID + ") does not have any Catalogues associated with it so it is not possible to fetch it's LoggingDatabaseSettings");

            return catalogue.Select(c => c.GetLoggingServer(testServer)).ToArray();
        }

        public string GetDistinctLoggingTask()
        {
            var catalogueMetadatas = GetAllCatalogues().ToArray();

            if(!catalogueMetadatas.Any())
                throw new Exception("There are no Catalogues associated with load metadata (ID=" +this.ID+")");

            var cataloguesWithoutLoggingTasks = catalogueMetadatas.Where(c => String.IsNullOrWhiteSpace(c.LoggingDataTask)).ToArray();

            if(cataloguesWithoutLoggingTasks.Any())
                throw new Exception("The following Catalogues do not have a LoggingDataTask specified:" + cataloguesWithoutLoggingTasks.Aggregate("",(s,n)=>s + n.ToString() + "(ID="+n.ID+"),"));
            
            string[] distinctLoggingTasks = catalogueMetadatas.Select(c => c.LoggingDataTask).Distinct().ToArray();
            if(distinctLoggingTasks.Count()>= 2)
                throw new Exception("There are " + distinctLoggingTasks.Length + " logging tasks in Catalogues belonging to this metadata (ID=" +this.ID+")");

            return distinctLoggingTasks[0];
        }


        public List<TableInfo> GetDistinctTableInfoList(bool includeLookups)
        {
            List<TableInfo> toReturn = new List<TableInfo>();

            foreach (ICatalogue catalogueMetadata in GetAllCatalogues())
                foreach (TableInfo tableInfo in catalogueMetadata.GetTableInfoList(includeLookups))
                    if (!toReturn.Contains(tableInfo))
                        toReturn.Add(tableInfo);

            return toReturn;
        }
        
        public bool AreLiveAndTestLoggingDifferent()
        {
            Catalogue[] catalogues = GetAllCatalogues().Cast<Catalogue>().ToArray();

            int? liveID = catalogues.Select(c => c.LiveLoggingServer_ID).Distinct().Single();
            int? testID = catalogues.Select(c => c.TestLoggingServer_ID).Distinct().Single();

            //theres a live configured but no test so we should just use the live one
            if (liveID != null && testID == null)
                return false;
            
            return liveID != testID ;
        }
        public DiscoveredServer GetDistinctLiveDatabaseServer()
        {
            var tableInfos = this.GetAllCatalogues().SelectMany(c => c.GetTableInfoList(false)).Distinct().ToArray();

            if (!tableInfos.Any())
                throw new Exception("LoadMetadata " + this + " has no TableInfos configured (or possibly the tables have been deleted resulting in MISSING ColumnInfos?)");

            var toReturn = DataAccessPortal.GetInstance().ExpectDistinctServer(tableInfos, DataAccessContext.DataLoad,true);

            return toReturn;
        }

        public string GetDistinctDatabaseName()
        {
            return GetDistinctLiveDatabaseServer().GetCurrentDatabase().GetRuntimeName();
        }

        public IHasDependencies[] GetObjectsThisDependsOn()
        {
            return null;
        }

        public IHasDependencies[] GetObjectsDependingOnThis()
        {
            return GetAllCatalogues().ToArray();
        }
    }
}