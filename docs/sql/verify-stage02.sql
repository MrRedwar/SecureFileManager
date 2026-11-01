-- SSMS 19: Windows Authentication, local .\SQLEXPRESS.
-- For local Development only: encryption ON, Trust server certificate ON.
-- Read-only verification. Never select PasswordHash or connection secrets.
USE [SecureFileManagerDB];
GO
SET NOCOUNT ON;
SELECT DB_NAME() AS DatabaseName,
    CONVERT(nvarchar(40), SERVERPROPERTY('ProductVersion')) AS ProductVersion;
SELECT encrypt_option, auth_scheme FROM sys.dm_exec_connections WHERE session_id = @@SPID;
SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
SELECT t.name AS TableName, c.name AS ColumnName, ty.name AS SqlType,
    c.max_length, c.is_nullable, c.collation_name
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE t.name IN ('Users', 'Files', 'Operations')
ORDER BY t.name, c.column_id;
SELECT OBJECT_NAME(i.object_id) AS TableName, i.name, i.is_unique, i.filter_definition,
    ic.key_ordinal, COL_NAME(ic.object_id, ic.column_id) AS ColumnName
FROM sys.indexes i JOIN sys.index_columns ic
    ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE OBJECT_NAME(i.object_id) IN ('Users', 'Files', 'Operations')
ORDER BY TableName, i.name, ic.key_ordinal;
SELECT fk.name, OBJECT_NAME(fk.parent_object_id) AS ChildTable,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ChildColumn,
    OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
    fk.delete_referential_action_desc, fk.is_disabled, fk.is_not_trusted
FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
WHERE OBJECT_NAME(fk.parent_object_id) IN ('Files', 'Operations');
SELECT OBJECT_NAME(parent_object_id) AS TableName, name, definition, is_disabled, is_not_trusted
FROM sys.check_constraints
WHERE OBJECT_NAME(parent_object_id) IN ('Users', 'Files', 'Operations');
SELECT 'Users' AS TableName, COUNT_BIG(*) AS [RowCount] FROM dbo.Users
UNION ALL SELECT 'Files', COUNT_BIG(*) FROM dbo.Files
UNION ALL SELECT 'Operations', COUNT_BIG(*) FROM dbo.Operations;
-- All three queries must return zero rows:
SELECT OwnerId, RelativePath, COUNT_BIG(*) AS DuplicateCount
FROM dbo.Files WHERE IsDeleted = 0 GROUP BY OwnerId, RelativePath HAVING COUNT_BIG(*) > 1;
SELECT f.Id FROM dbo.Files f LEFT JOIN dbo.Users u ON u.Id = f.OwnerId WHERE u.Id IS NULL;
SELECT o.Id FROM dbo.Operations o LEFT JOIN dbo.Files f ON f.Id = o.FileId
WHERE o.FileId IS NOT NULL AND f.Id IS NULL;
GO
