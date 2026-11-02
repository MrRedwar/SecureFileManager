-- SSMS 19, read-only. Select SecureFileManagerDB explicitly. Never output password hashes.
IF DB_NAME() <> N'SecureFileManagerDB' THROW 51005, 'Select the application database explicitly.', 1;
DECLARE @UserId uniqueidentifier = NULL; -- replace with your User.Id; no passwords needed
SELECT Id, Username, IsActive FROM dbo.Users;
SELECT Id, OwnerId, Name, RelativePath, Kind, Extension, SizeBytes, CreatedAtUtc,
       ModifiedAtUtc, IsDeleted, DeletedAtUtc
FROM dbo.Files WHERE OwnerId = @UserId ORDER BY RelativePath, CreatedAtUtc;
SELECT TOP (100) Id, UserId, FileId, OperationType, Result, TargetPathSnapshot,
       Details, CorrelationId, OccurredAtUtc
FROM dbo.Operations WHERE UserId = @UserId ORDER BY OccurredAtUtc DESC;
SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
