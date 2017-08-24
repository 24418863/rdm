using System;
using System.Collections.Generic;
using CatalogueLibrary.Data.Pipelines;
using CatalogueLibrary.DataFlowPipeline;
using MapsDirectlyToDatabaseTable;

namespace CatalogueLibrary.Data.Cache
{
    public interface ICacheProgress : ISaveable, IDeleteable, IMapsDirectlyToDatabaseTable
    {
        int LoadProgress_ID { get; set; }
        int? PermissionWindow_ID { get; set; }

        DateTime? CacheFillProgress { get; set; }
        string CacheLagPeriod { get; set; }
        //string CacheLayoutType { get; set; }
        TimeSpan ChunkPeriod { get; set; }
        int? Pipeline_ID { get; set; }

        IPipeline Pipeline { get; }
        PermissionWindow PermissionWindow { get; }

        CacheLagPeriod GetCacheLagPeriod();
        void SetCacheLagPeriod(CacheLagPeriod cacheLagPeriod);

        CacheLagPeriod GetCacheLagPeriodLoadDelay();
        void SetCacheLagPeriodLoadDelay(CacheLagPeriod cacheLagPeriod);

        ILoadProgress GetLoadProgress();
        IPermissionWindow GetPermissionWindow();
        IEnumerable<ICacheFetchFailure> GetAllFetchFailures();
        IEnumerable<ICacheFetchFailure> FetchPage(int start, int batchSize);
        TimeSpan GetShortfall();
    }
}