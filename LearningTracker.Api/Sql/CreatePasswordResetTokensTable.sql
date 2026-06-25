IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PasswordResetTokens' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[PasswordResetTokens] (
        [Id]        INT           IDENTITY(1,1) NOT NULL,
        [UserId]    INT           NOT NULL,
        [TokenHash] NVARCHAR(128) NOT NULL,
        [ExpiresAt] DATETIME2     NOT NULL,
        [CreatedAt] DATETIME2     NOT NULL,
        [UsedAt]    DATETIME2     NULL,
        CONSTRAINT [PK_PasswordResetTokens]       PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_PasswordResetTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_PasswordResetTokens_Hash]  UNIQUE NONCLUSTERED ([TokenHash])
    );

    CREATE NONCLUSTERED INDEX [IX_PasswordResetTokens_UserId] ON [dbo].[PasswordResetTokens] ([UserId]);
END
GO
