--Version:1.26.0.0
--Description: Adds locking fields to PermissionWindow, to guard against accidental instantiation of multiple caching services which could accidentally violate permissions.

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PermissionWindow' AND COLUMN_NAME='IsLocked')
BEGIN
  ALTER TABLE PermissionWindow ADD IsLocked bit CONSTRAINT DF_PermissionWindow_IsLocked DEFAULT ((0)) NOT NULL 
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PermissionWindow' AND COLUMN_NAME='LockingUser')
BEGIN
  ALTER TABLE PermissionWindow ADD LockingUser varchar(256)
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='PermissionWindow' AND COLUMN_NAME='LockingProcess')
BEGIN
  ALTER TABLE PermissionWindow ADD LockingProcess varchar(1024)
END