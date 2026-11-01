IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [Username] nvarchar(50) COLLATE Latin1_General_100_CI_AS NOT NULL,
    [PasswordHash] nvarchar(1024) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [FailedLoginCount] int NOT NULL,
    [LockoutUntilUtc] datetime2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Users_FailedLoginCount] CHECK ([FailedLoginCount] >= 0),
    CONSTRAINT [CK_Users_PasswordHash] CHECK (LEN([PasswordHash]) > 0),
    CONSTRAINT [CK_Users_Username] CHECK (LEN(LTRIM(RTRIM([Username]))) BETWEEN 3 AND 50 AND DATALENGTH([Username]) = DATALENGTH(LTRIM(RTRIM([Username]))))
);

CREATE TABLE [Files] (
    [Id] uniqueidentifier NOT NULL,
    [OwnerId] uniqueidentifier NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [RelativePath] nvarchar(400) COLLATE Latin1_General_100_CI_AS NOT NULL,
    [Kind] nvarchar(16) NOT NULL,
    [Extension] nvarchar(32) NULL,
    [SizeBytes] bigint NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ModifiedAtUtc] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAtUtc] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Files] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Files_Dates] CHECK ([ModifiedAtUtc] >= [CreatedAtUtc] AND ([DeletedAtUtc] IS NULL OR [DeletedAtUtc] >= [CreatedAtUtc])),
    CONSTRAINT [CK_Files_Kind] CHECK ([Kind] IN ('File', 'Directory')),
    CONSTRAINT [CK_Files_Name] CHECK (LEN([Name]) > 0),
    CONSTRAINT [CK_Files_RelativePath] CHECK (LEN([RelativePath]) > 0),
    CONSTRAINT [CK_Files_SizeBytes] CHECK ([SizeBytes] >= 0),
    CONSTRAINT [CK_Files_SoftDelete] CHECK (([IsDeleted] = 0 AND [DeletedAtUtc] IS NULL) OR ([IsDeleted] = 1 AND [DeletedAtUtc] IS NOT NULL)),
    CONSTRAINT [FK_Files_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Operations] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NULL,
    [ActorNameSnapshot] nvarchar(50) NULL,
    [FileId] uniqueidentifier NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    [OperationType] nvarchar(32) NOT NULL,
    [Result] nvarchar(16) NOT NULL,
    [TargetPathSnapshot] nvarchar(400) NULL,
    [Details] nvarchar(512) NULL,
    [CorrelationId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_Operations] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Operations_Result] CHECK ([Result] IN ('Success', 'Denied', 'Failed')),
    CONSTRAINT [CK_Operations_Type] CHECK ([OperationType] IN ('LOGIN_SUCCESS', 'LOGIN_FAILED', 'LOGOUT', 'FILE_CREATED', 'FILE_READ', 'FILE_UPDATED', 'FILE_DELETED', 'DIRECTORY_CREATED', 'JSON_VALIDATED', 'XML_VALIDATED', 'ZIP_CREATED', 'ZIP_EXTRACTED', 'ACCESS_DENIED', 'OPERATION_FAILED')),
    CONSTRAINT [FK_Operations_Files_FileId] FOREIGN KEY ([FileId]) REFERENCES [Files] ([Id]),
    CONSTRAINT [FK_Operations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE INDEX [IX_Files_OwnerId] ON [Files] ([OwnerId]);

CREATE UNIQUE INDEX [IX_Files_OwnerId_RelativePath] ON [Files] ([OwnerId], [RelativePath]) WHERE [IsDeleted] = 0;

CREATE INDEX [IX_Operations_CorrelationId] ON [Operations] ([CorrelationId]);

CREATE INDEX [IX_Operations_FileId] ON [Operations] ([FileId]);

CREATE INDEX [IX_Operations_OperationType_Result] ON [Operations] ([OperationType], [Result]);

CREATE INDEX [IX_Operations_UserId_OccurredAtUtc] ON [Operations] ([UserId], [OccurredAtUtc]);

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261101094955_InitialCreate', N'9.0.22');

COMMIT;
GO

