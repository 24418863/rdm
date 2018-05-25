--Version:2.9.0.1
--Description: Removes support for automation
if exists (select 1 from sys.tables where name ='AutomationServiceSlot')
begin
    drop table AutomateablePipeline
	drop table AutomationLockedCatalogues
	drop table AutomationJob
    drop table AutomationServiceException
	drop table AutomationServiceSlot
	drop table LoadPeriodically
end
  GO


if exists (select 1 from sys.columns where name like '%Locked%')
begin
  alter table LoadProgress drop constraint DF_LoadSchedule_CachingInProgress
  alter table LoadProgress drop constraint DF_LoadProgress_AllowAutomation  
  alter table LoadProgress drop column LockedBecauseRunning
  alter table LoadProgress drop column LockHeldBy
  alter table LoadProgress drop column AllowAutomation
  alter table PermissionWindow drop constraint DF_PermissionWindow_IsLocked
  alter table PermissionWindow drop column LockedBecauseRunning
  alter table PermissionWindow drop column LockHeldBy
end