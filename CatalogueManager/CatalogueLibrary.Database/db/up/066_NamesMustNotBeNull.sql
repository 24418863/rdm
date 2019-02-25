--Version:2.12.0.1
--Description: Database constraints Name properties are never null
update Catalogue set Name = 'NoName' where Name is null
alter table Catalogue alter column Name varchar(1000) not null

update CatalogueItemIssue set Name = 'NoName' where Name is null
alter table CatalogueItemIssue		  alter column Name varchar(1000) not null

update ColumnInfo set Name = 'NoName' where Name is null
alter table ColumnInfo				  alter column Name varchar(1000) not null

update ExternalDatabaseServer set Name = 'NoName' where Name is null
alter table ExternalDatabaseServer	  alter column Name varchar(1000) not null

update TableInfo set Name = 'NoName' where Name is null
alter table TableInfo				  alter column Name varchar(1000) not null

update PermissionWindow set Name = 'NoName' where Name is null
alter table PermissionWindow		  alter column Name varchar(1000) not null