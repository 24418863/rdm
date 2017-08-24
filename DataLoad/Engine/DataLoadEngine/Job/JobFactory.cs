using CatalogueLibrary;
using CatalogueLibrary.Data.DataLoad;
using DataLoadEngine.DataProvider;
using HIC.Logging;
using ReusableLibraryCode.Progress;

namespace DataLoadEngine.Job
{
    public class JobFactory : IJobFactory
    {
        private readonly ILoadMetadata _loadMetadata;
        private readonly ILogManager _logManager;

        public JobFactory(ILoadMetadata loadMetadata, ILogManager logManager)
        {
            _loadMetadata = loadMetadata;
            _logManager = logManager;
        }

        public IDataLoadJob Create(IDataLoadEventListener listener)
        {
            var description = _loadMetadata.Name;
            var hicProjectDirectory = new HICProjectDirectory(_loadMetadata.LocationOfFlatFiles, false);
            return new DataLoadJob(description, _logManager, _loadMetadata, hicProjectDirectory, listener);
        }
    }
}