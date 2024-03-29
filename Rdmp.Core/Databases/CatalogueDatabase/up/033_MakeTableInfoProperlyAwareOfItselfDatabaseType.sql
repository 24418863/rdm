--Version:1.27.0.0
--Description: Makes TableInfo objects aware of database type as an enum not an abstract string that makes no sense to anyone

IF (SELECT columnproperty(object_id('TableInfo'), 'Store_type', 'AllowsNull')) = 1
BEGIN
update [TableInfo] set 
Store_type = 'MicrosoftSQLServer'
where
Store_type 
in 
('',
'SQL DB',
'NoSQL DB',
'Hadoop')
or 
Store_type is null

	alter table [TableInfo] alter column Store_type varchar(100) not null
	
	alter table [TableInfo] add constraint df_TableInfoDefaultsToMicrosoftSqlServer  default 'MicrosoftSQLServer'  for Store_type
END
GO

--rename all the Store_type fields to DatabaseType
IF exists (select 1 from sys.columns where name = 'Store_type'and OBJECT_NAME(object_id) ='TableInfo')
BEGIN
	exec sp_rename 'TableInfo.Store_type', 'DatabaseType','COLUMN'
END
 
 
