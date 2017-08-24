﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Aggregation;
using CatalogueLibrary.Repositories;
using MapsDirectlyToDatabaseTable;

namespace CatalogueLibrary.FilterImporting.Construction
{
    public class AggregateFilterFactory :IFilterFactory
    {
        private readonly ICatalogueRepository _repository;

        public AggregateFilterFactory(ICatalogueRepository repository)
        {
            _repository = repository;
        }

        public IFilter CreateNewFilter(string name)
        {
            return new AggregateFilter(_repository,name);
        }

        public ISqlParameter CreateNewParameter(IFilter filter, string parameterSQL)
        {
            return new AggregateFilterParameter(_repository,parameterSQL,filter);
        }

        public Type GetRootOwnerType()
        {
            return typeof (AggregateConfiguration);
        }

        public Type GetIContainerTypeIfAny()
        {
            return typeof (AggregateFilterContainer);
        }
    }
}