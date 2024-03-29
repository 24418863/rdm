--Version:1.19.0.0
--Description: Forces all suffixes in ANOTable to be unique

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_suffixMustBeUnique')
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [ix_suffixMustBeUnique] ON [dbo].[ANOTable]
(
	[Suffix] ASC
)
END
