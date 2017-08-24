﻿using System;
using CachingEngine.BasicCache;
using CachingEngine.Layouts;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Cache;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngine.Job;
using DataLoadEngine.Job.Scheduling;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using ReusableLibraryCode.Progress;

namespace DataLoadEngine.DataProvider.FromCache
{
    public class BasicCacheDataProvider : CachedFileRetriever
    {
        public override void Initialize(IHICProjectDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
        {
            
        }

        public override ExitCodeType Fetch(IDataLoadJob job, GracefulCancellationToken cancellationToken)
        {
            var scheduledJob = ConvertToScheduledJob(job);
            
            var workload = GetDataLoadWorkload(scheduledJob);
            ExtractJobs(scheduledJob);

            job.PushForDisposal(new DeleteCachedFilesOperation(scheduledJob, workload));
            return ExitCodeType.Success;
        }
    }
}