BEGIN TRANSACTION;
ALTER TABLE [Users] ADD [FailedLoginWindowStartUtc] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261101122237_AddLoginFailureWindow', N'9.0.22');

COMMIT;
GO

