﻿using System.Collections.Generic;
using System.Data;
using CatalogueLibrary.Data.Aggregation;
using QueryCaching.Aggregation;
using QueryCaching.Aggregation.Arguments;

namespace CohortManagerLibrary.Execution
{
    public abstract class CachableTask : Compileable, ICachableTask
    {
        protected CachableTask(CohortCompiler compiler) : base(compiler)
        {
        }

        public abstract AggregateConfiguration GetAggregateConfiguration();
        public abstract CacheCommitArguments GetCacheArguments(string sql, DataTable results, Dictionary<string, string> explicitTypingDictionary);
        public abstract void ClearYourselfFromCache(CachedAggregateConfigurationResultsManager manager);

        public bool IsCacheableWhenFinished()
        {
            if (!_compiler.Tasks.ContainsKey(this))
                return false;

            return _compiler.Tasks[this].SubQueries > _compiler.Tasks[this].SubqueriesCached;
        }

        public bool CanDeleteCache()
        {
            return _compiler.Tasks[this].SubqueriesCached > 0;
        }

    }
}