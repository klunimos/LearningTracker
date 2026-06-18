IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshTokens' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[RefreshTokens] (
        [Id]        INT           IDENTITY(1,1) NOT NULL,
        [UserId]    INT           NOT NULL,
        [Token]     NVARCHAR(256) NOT NULL,
        [ExpiresAt] DATETIME2     NOT NULL,
        [CreatedAt] DATETIME2     NOT NULL,
        [IsRevoked] BIT           NOT NULL DEFAULT(0),
        CONSTRAINT [PK_RefreshTokens]         PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users]   FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_RefreshTokens_Token]   UNIQUE NONCLUSTERED ([Token])
    );

    CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId] ON [dbo].[RefreshTokens] ([UserId]);
END
GO
