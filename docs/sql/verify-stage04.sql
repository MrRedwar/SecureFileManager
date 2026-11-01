-- SSMS 19: select SecureFileManagerDB before running. Read-only; no password/hash output.
IF DB_NAME() <> N'SecureFileManagerDB'
    THROW 51004, 'Select SecureFileManagerDB explicitly.', 1;
SET NOCOUNT ON;

SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
SELECT name, TYPE_NAME(user_type_id) AS DataType, is_nullable
FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'FailedLoginWindowStartUtc';
SELECT (SELECT COUNT(*) FROM dbo.Users) AS UsersCount,
       (SELECT COUNT(*) FROM dbo.Files) AS FilesCount,
       (SELECT COUNT(*) FROM dbo.Operations) AS OperationsCount;
SELECT encrypt_option FROM sys.dm_exec_connections WHERE session_id = @@SPID;

-- Never select PasswordHash itself or export credential material.
SELECT Id, Username, IsActive, CreatedAtUtc, FailedLoginCount,
       FailedLoginWindowStartUtc, LockoutUntilUtc,
       CASE WHEN LEN(PasswordHash) > 0 THEN N'Hash present' ELSE N'Missing' END AS PasswordStorage
FROM dbo.Users ORDER BY CreatedAtUtc;
SELECT TOP (100) Id, UserId, ActorNameSnapshot, FileId, OccurredAtUtc, OperationType, Result, Details
FROM dbo.Operations
WHERE OperationType IN (N'LOGIN_SUCCESS', N'LOGIN_FAILED', N'LOGOUT', N'ACCESS_DENIED', N'OPERATION_FAILED', N'DIRECTORY_CREATED')
ORDER BY OccurredAtUtc DESC;
SELECT name, delete_referential_action_desc FROM sys.foreign_keys
WHERE parent_object_id IN (OBJECT_ID(N'dbo.Files'), OBJECT_ID(N'dbo.Operations'));
