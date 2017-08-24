using System.Linq;
using CatalogueLibrary;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngine.Job.Scheduling;
using DataLoadEngine.LoadExecution;
using DataLoadEngine.LoadProcess.Scheduling.Strategy;
using HIC.Logging;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace DataLoadEngine.LoadProcess.Scheduling
{
    public class IterativeScheduledDataLoadProcess : ScheduledDataLoadProcess
    {
        // todo: refactor to cut down on ctor params
        public IterativeScheduledDataLoadProcess(ILoadMetadata loadMetadata, ICheckable preExecutionChecker, IDataLoadExecution loadExecution, JobDateGenerationStrategyFactory jobDateGenerationStrategyFactory, ILoadProgressSelectionStrategy loadProgressSelectionStrategy, int overrideNumberOfDaysToLoad, ILogManager logManager, IDataLoadEventListener dataLoadEventsreceiver)
            : base(loadMetadata, preExecutionChecker, loadExecution, jobDateGenerationStrategyFactory, loadProgressSelectionStrategy, overrideNumberOfDaysToLoad, logManager, dataLoadEventsreceiver)
        {
            
        }

        public override ExitCodeType Run(GracefulCancellationToken loadCancellationToken)
        {
            // grab all the load schedules we can and lock them
            var loadProgresses = LoadProgressSelectionStrategy.GetAllLoadProgresses();
            if (!loadProgresses.Any())
                return ExitCodeType.OperationNotRequired;

            // create job factory
            var progresses = loadProgresses.ToDictionary(loadProgress => loadProgress, loadProgress => JobDateGenerationStrategyFactory.Create(loadProgress));
            var jobProvider = new MultipleScheduleJobFactory(progresses, OverrideNumberOfDaysToLoad, LoadMetadata, LogManager);

            // check if the factory will produce any jobs, if not we can stop here
            if (!jobProvider.HasJobs())
                return ExitCodeType.OperationNotRequired;

            // Run the data load process
            JobProvider = jobProvider;
            try
            {
                //Do a data load 
                ExitCodeType result;
                while((result = base.Run(loadCancellationToken) ) == ExitCodeType.Success) //stop if it said not required
                {
                    //or if between executions the token is set
                    if(loadCancellationToken.IsAbortRequested)
                        return ExitCodeType.Abort;

                    if(loadCancellationToken.IsCancellationRequested)
                        return ExitCodeType.Success;
                }

                //should be Operation Not Required or Error since the token inside handles stopping
                return result;
            }
            finally
            {
                // Unlock all load schedules after completion
                loadProgresses.ForEach(schedule => schedule.Unlock());
            }            

            return ExitCodeType.Success;
        }
    }
}