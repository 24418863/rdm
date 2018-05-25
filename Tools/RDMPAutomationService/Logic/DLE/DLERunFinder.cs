using System;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Cache;
using CatalogueLibrary.Repositories;
using ReusableLibraryCode.Progress;

namespace RDMPAutomationService.Logic.DLE
{
    /// <summary>
    /// Identifies data loads (LoadMetadatas) which are due to run (and not already executing/crashed/locked etc).  Used by DLEAutomationSource to 
    /// identify when a new AutomatedDLELoad can be started.  Loads can be due to run either because they have a LoadPeriodically that indicates they should
    /// be run every so often or a LoadProgress / CacheProgress which indicate that there is data available to load.
    /// 
    /// <para>Used by DLEAutomationSource to decide when a new AutomatedDLELoad/AutomatedDLELoadFromCache can be started.</para>
    /// </summary>
    public class DLERunFinder
    {
        private readonly ICatalogueRepository _catalogueRepository;
        private readonly IDataLoadEventListener _listener;

        public DLERunFinder(ICatalogueRepository catalogueRepository, IDataLoadEventListener listener)
        {
            _catalogueRepository = catalogueRepository;
            _listener = listener;
        }
        
        public ILoadProgress SuggestLoadBecauseCacheAvailable()
        {
            var cacheProgresses = _catalogueRepository.GetAllObjects<CacheProgress>();
            var lockedCatalogues = _catalogueRepository.GetAllAutomationLockedCatalogues();
            
            foreach (CacheProgress cp in cacheProgresses)
            {
                var dtCache = cp.CacheFillProgress;
                var loadProgress = cp.LoadProgress;
               
                //Cache has never been loaded
                if(dtCache == null)
                {
                    _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Debug, String.Format("Cache Progress {0}: cache has never been loaded", cp)));
                    continue;
                }

                var dtLoadProgress = loadProgress.DataLoadProgress;
                
                //Never loaded the LoadProgress... probably don't start now in automation, that's a bad idea.  
                //If user wants to load it they can run load checks which will suggest to the user they set it to the OriginDate anyway
                if(dtLoadProgress == null)
                {
                    _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Debug, String.Format("Cache Progress {0}: Never loaded the LoadProgress... ", cp)));
                    continue;
                }

                int daysToLoad = loadProgress.DefaultNumberOfDaysToLoadEachTime;

                if (daysToLoad == 0)
                {
                    _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Debug, String.Format("Load Progress {0} has 0 days to load set", loadProgress.Name)));
                    continue;
                }

                if (dtLoadProgress.Value.AddDays(daysToLoad) <= dtCache)
                    if(!LocksPreventLoading(loadProgress,lockedCatalogues))
                    {
                        _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, String.Format("Load Progress {0} has data to load and it's not locked. Firing!", loadProgress.Name)));
                        return loadProgress;
                    }
            }

            //No tasks are ready to go
            _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "No cache loading tasks are ready to go, exiting..."));
            return null;
        }

        private bool LocksPreventLoading(ILoadProgress cacheProgress, Catalogue[] lockedCatalogues)
        {
            var loadCatalogues = cacheProgress.LoadMetadata.GetAllCatalogues();

            //if any of the catalogues that participate in the load are locked in another automation task (could be DQE or cache download even!)
            return loadCatalogues.Any(lockedCatalogues.Contains);
        }
    }
}
