using System;
using CachingEngine.Requests;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Cache;

namespace CachingEngine.Factories
{
    public class CacheFetchRequestFactory
    {
        public ICacheFetchRequest Create(ICacheProgress cacheProgress, ILoadProgress loadProgress)
        {
            // Figure out when to start loading from
            DateTime startDate;
            if (cacheProgress.CacheFillProgress.HasValue)
                startDate = cacheProgress.CacheFillProgress.Value;
            else if (loadProgress.OriginDate.HasValue)
                startDate = loadProgress.OriginDate.Value;
            else
                throw new Exception("Don't know when to begin loading the cache from. Neither CacheProgress or LoadProgress has a relevant date.");

            var initialFetchRequest = new CacheFetchRequest(loadProgress.Repository)
            {
                CacheProgress = cacheProgress,
                ChunkPeriod = cacheProgress.ChunkPeriod,
                PermissionWindow = cacheProgress.GetPermissionWindow() ?? new PermissionWindow(),
                Start = startDate
            };

            return initialFetchRequest;
        }
    }
}