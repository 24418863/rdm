﻿using System;
using System.Collections.Generic;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using DataExportLibrary.Data.DataTables;
using ReusableLibraryCode;

namespace DataExportLibrary.Data.Hierarchy
{

    /// <summary>
    /// Provides a memory based efficient (in terms of the number of database queries sent) way of finding all containers and subcontainers and filters in the entire DataExportManager
    /// database at once rather than using the methods on IContainer and IFilter which send individual database queries for relevant subcontainers etc.
    /// </summary>
    public class DataExportFilterHierarchy
    {
        public Dictionary<int,FilterContainer> AllContainers;
        private DeployedExtractionFilter[] _allFilters;
        public DeployedExtractionFilterParameter[] _allParameters;

        readonly Dictionary<int, List<FilterContainer>> _subcontainers = new Dictionary<int, List<FilterContainer>>();

        public List<ExtractionFilterUser>  Users = new List<ExtractionFilterUser>();

        

        public DataExportFilterHierarchy(IDataExportRepository repository)
        {
            AllContainers = repository.GetAllObjects<FilterContainer>().ToDictionary(o=>o.ID,o=>o);
            _allFilters = repository.GetAllObjects<DeployedExtractionFilter>();
            _allParameters = repository.GetAllObjects<DeployedExtractionFilterParameter>();
            
            var server = repository.DiscoveredServer;
            using (var con = repository.GetConnection())
            {
                var r = server.GetCommand("SELECT *  FROM FilterContainerSubcontainers", con).ExecuteReader();
                while(r.Read())
                {

                    var parentId = Convert.ToInt32(r["FilterContainer_ParentID"]);
                    var subcontainerId = Convert.ToInt32(r["FilterContainerChildID"]);

                    if(!_subcontainers.ContainsKey(parentId))
                        _subcontainers.Add(parentId,new List<FilterContainer>());

                    _subcontainers[parentId].Add(AllContainers[subcontainerId]);

                    
                }
                r.Close();


                r = server.GetCommand("select * from SelectedDataSets where RootFilterContainer_ID is not null", con).ExecuteReader();
                while (r.Read())
                {
                    int containerId = Convert.ToInt32(r["RootFilterContainer_ID"]);
                    var container = AllContainers[containerId];

                    Users.Add(new ExtractionFilterUser(
                       Convert.ToInt32(r["ExtractionConfiguration_ID"]),
                       Convert.ToInt32(r["ExtractableDataset_ID"]),
                       containerId,
                       container
                        ));
                }
                r.Close();
            }
        }
        public IEnumerable<DeployedExtractionFilter> GetFilters(FilterContainer filterContainer)
        {
            return _allFilters.Where(f => f.FilterContainer_ID == filterContainer.ID);
        }
        

        public IEnumerable<FilterContainer> GetSubcontainers(FilterContainer filterContainer)
        {
            if (!_subcontainers.ContainsKey(filterContainer.ID))
                return new FilterContainer[0];

            return _subcontainers[filterContainer.ID];
        }

        public IEnumerable<DeployedExtractionFilterParameter> GetParameters(DeployedExtractionFilter filter)
        {
            return _allParameters.Where(p => p.ExtractionFilter_ID == filter.ID);
        }
        
    }
}