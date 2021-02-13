USE [dftb]
GO

DROP PROCEDURE IF EXISTS [Journal].[RegisterItemEvent]
GO

DROP PROCEDURE IF EXISTS [Journal].[RegisterItemTemplateEvent]
GO

DROP PROCEDURE IF EXISTS [UserData].[CreateItem]
GO

DROP PROCEDURE IF EXISTS [UserData].[UpdateItem]
GO

DROP PROCEDURE IF EXISTS [UserData].[DeleteItem]
GO

DROP TABLE IF EXISTS [UserData].[ListAssignment]
GO

DROP TABLE IF EXISTS [UserData].[Item]
GO

DROP TABLE IF EXISTS [UserData].[ItemState]
GO

DROP TABLE IF EXISTS [Journal].[ItemTemplate]
GO

DROP TABLE IF EXISTS [Journal].[Item]
GO

DROP TABLE IF EXISTS [Journal].[Operation]
GO

DROP TABLE IF EXISTS [UserData].[ItemTemplate]
GO

DROP TABLE IF EXISTS [UserData].[List]
GO

DROP TABLE IF EXISTS [UserDefinitions].[Account]
GO

DROP SCHEMA IF EXISTS UserDefinitions
GO

CREATE SCHEMA UserDefinitions
GO

DROP SCHEMA IF EXISTS UserData
GO

CREATE SCHEMA UserData
GO

DROP SCHEMA IF EXISTS Journal
GO

CREATE SCHEMA Journal
GO

CREATE TABLE [UserDefinitions].[Account]
(
    AccountId  UNIQUEIDENTIFIER NOT NULL,
    EmailAddress NVARCHAR(1024),
    CONSTRAINT PK_UserDefinitions_Account_AccountId PRIMARY KEY (AccountId)
)
GO

CREATE TABLE [UserData].[List]
(
    ListId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(2048) NOT NULL,
    CONSTRAINT PK_UserData_List_ListId PRIMARY KEY (ListId)
)
GO

CREATE TABLE [UserData].[ListAssignment]
(
    ListId UNIQUEIDENTIFIER NOT NULL,
    AccountId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_UserData_ListAssignment PRIMARY KEY (ListId, AccountId),
    CONSTRAINT FK_UserData_ListAssignment_ListId FOREIGN KEY (ListId) REFERENCES [UserData].[List] (ListId),
    CONSTRAINT FK_UserData_ListAssignment_AccountId FOREIGN KEY (AccountId) REFERENCES [UserDefinitions].[Account] (AccountId)
)
GO

CREATE TABLE [UserData].[ItemTemplate]
(
    ItemTemplateId UNIQUEIDENTIFIER NOT NULL,
    AccountId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(1024) NOT NULL,
    ImageUrl NVARCHAR(2048) NOT NULL,
    UPC NVARCHAR(24) NOT NULL,
    Created DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_UserData_ItemTemplate_ItemTemplateId PRIMARY KEY (ItemTemplateId),
    CONSTRAINT FK_UserData_ItemTemplate_ItemTemplate_AccountId FOREIGN KEY (AccountId) REFERENCES [UserDefinitions].[Account] (AccountId)
)
GO

CREATE INDEX IX_ItemTemplate_Created ON [UserData].[ItemTemplate] ( [Created] DESC )
GO

CREATE TABLE [UserData].[Item]
(
    ItemId UNIQUEIDENTIFIER NOT NULL,
    ListId UNIQUEIDENTIFIER NOT NULL,
    TemplateId UNIQUEIDENTIFIER NOT NULL,
    DemandQuantity INT NOT NULL,
    AcquiredQuantity INT NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    Created DATETIME NOT NULL,
    Deleted DATETIME,
    CONSTRAINT PK_UserData_Item_ItemId PRIMARY KEY (ItemId),
    CONSTRAINT FK_UserData_Item_TemplateId FOREIGN KEY (TemplateId) REFERENCES [UserData].[ItemTemplate] (ItemTemplateId),
    CONSTRAINT FK_UserData_Item_ListId FOREIGN KEY (ListId) REFERENCES [UserData].[List] (ListId)
)
GO

CREATE TABLE [Journal].[Operation]
(
    OperationId INT NOT NULL,
    Name NVARCHAR(10),
    CONSTRAINT PK_Journal_Operation_OperationId PRIMARY KEY (OperationId)
)
GO

CREATE UNIQUE INDEX IX_Journal_Operation_Name ON [Journal].[Operation](Name)
GO

INSERT INTO [Journal].[Operation] (OperationId, Name) VALUES (1, 'Create');
INSERT INTO [Journal].[Operation] (OperationId, Name) VALUES (2, 'Update');
INSERT INTO [Journal].[Operation] (OperationId, Name) VALUES (3, 'Delete');
GO

CREATE TABLE [Journal].[ItemTemplate]
(
    JournalId UNIQUEIDENTIFIER NOT NULL,
    OperationId INT NOT NULL,
    ItemTemplateId UNIQUEIDENTIFIER NOT NULL,
    AccountId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(1024) NOT NULL,
    ImageUrl NVARCHAR(2048) NOT NULL,
    UPC NVARCHAR(24) NOT NULL,
    Created DATETIME NOT NULL,
    CONSTRAINT PK_Journal_ItemTemplate_JournalId PRIMARY KEY (JournalId),
    CONSTRAINT FK_Journal_ItemTemplate_OperationId FOREIGN KEY (OperationId) REFERENCES [Journal].[Operation] (OperationId),
    CONSTRAINT FK_Journal_ItemTemplate_AccountId FOREIGN KEY (AccountId) REFERENCES [UserDefinitions].[Account] (AccountId)
)
GO

CREATE INDEX IX_Journal_ItemTemplate_Created ON [Journal].[ItemTemplate] (Created DESC)
GO

CREATE TABLE [Journal].[Item]
(
    JournalId UNIQUEIDENTIFIER NOT NULL,
    OperationId INT NOT NULL,
    ItemId UNIQUEIDENTIFIER NOT NULL,
    ListId UNIQUEIDENTIFIER NOT NULL,
    ItemTemplateId UNIQUEIDENTIFIER NOT NULL,
    DemandQuantity INT NOT NULL,
    AcquiredQuantity INT NOT NULL,
    Created DATETIME NOT NULL,
    CONSTRAINT PK_Journal_Item_JournalId PRIMARY KEY (JournalId),
    CONSTRAINT FK_Journal_Item_ItemTemplateId FOREIGN KEY (ItemTemplateId) REFERENCES [UserData].[ItemTemplate] (ItemTemplateId),
    CONSTRAINT FK_Journal_Item_OperationId FOREIGN KEY (OperationId) REFERENCES [Journal].[Operation] (OperationId)
)
GO

CREATE INDEX IX_Journal_Item_Created ON [Journal].[Item] (Created DESC)
GO


-- Stored Procedures
CREATE PROCEDURE [Journal].[RegisterItemEvent]
(
    @JournalId UNIQUEIDENTIFIER, 
    @Operation INT,
    @ItemId UNIQUEIDENTIFIER,
    @ListId UNIQUEIDENTIFIER,
    @ItemTemplateId UNIQUEIDENTIFIER,
    @DemandQuantity INT,
    @AcquiredQuantity INT,
    @Created DATETIME
)
AS
    SET NOCOUNT OFF;
    SET XACT_ABORT ON;

    DECLARE @ExistingRecordCount INT

    BEGIN TRANSACTION
        SELECT
            @ExistingRecordCount = COUNT(JournalId) 
        FROM 
            [Journal].[Item] ItemJournal 
        WHERE 
            ItemJournal.ItemId = @ItemId
            AND ItemJournal.ListId = @ListId
            AND ItemJournal.Created > @Created;

        IF (@ExistingRecordCount = 0)
        BEGIN
            INSERT INTO [Journal].[Item]
                (JournalId, OperationId, ListId, ItemId, ItemTemplateId, DemandQuantity, AcquiredQuantity, Created)
            VALUES
                (@JournalId, @Operation, @ListId, @ItemId, @ItemTemplateId, @DemandQuantity, @AcquiredQuantity, @Created)
        END
    COMMIT TRANSACTION

    RETURN @@ROWCOUNT
GO

CREATE PROCEDURE [Journal].[RegisterItemTemplateEvent]
(
    @JournalId UNIQUEIDENTIFIER, 
    @Operation INT,
    @ItemTemplateId UNIQUEIDENTIFIER,
    @AccountId UNIQUEIDENTIFIER,
    @Name NVARCHAR(1024),
    @ImageUrl NVARCHAR(2048),
    @UPC NVARCHAR(24),
    @Created DATETIME
)
AS
    SET NOCOUNT OFF;
    SET XACT_ABORT ON;

    DECLARE @ExistingRecordCount INT

    BEGIN TRANSACTION
        SELECT
            @ExistingRecordCount = COUNT(JournalId) 
        FROM 
            [Journal].[ItemTemplate]
        WHERE 
            ItemTemplateId = @ItemTemplateId
            AND AccountId = @AccountId
            AND Created > @Created;

        IF (@ExistingRecordCount = 0)
        BEGIN
            INSERT INTO [Journal].[ItemTemplate]
                (JournalId, OperationId, ItemTemplateId, Name, ImageUrl, UPC, Created)
            VALUES
                (@JournalId, @Operation, @ItemTemplateId, @Name, @ImageUrl, @UPC, @Created)
        END
    COMMIT TRANSACTION

    RETURN @@ROWCOUNT
GO

CREATE PROCEDURE [UserData].[CreateItem]
(
    @ItemId UNIQUEIDENTIFIER,
    @ListId UNIQUEIDENTIFIER,
    @ItemTemplateId UNIQUEIDENTIFIER,
    @DemandQuantity INT,
    @AcquiredQuantity INT
)
AS
    SET NOCOUNT OFF;
    SET XACT_ABORT ON;

    INSERT INTO [UserData].[Item]
        (ItemId, ListId, TemplateId, DemandQuantity, AcquiredQuantity, IsDeleted, Created, Deleted)
        VALUES
        (@ItemId, @ListId, @ItemTemplateId, @DemandQuantity, @AcquiredQuantity, 0, GETUTCDATE(), NULL)
GO

CREATE PROCEDURE [UserData].[UpdateItem]
(
    @ItemId UNIQUEIDENTIFIER,
    @ListId UNIQUEIDENTIFIER,
    @ItemTemplateId UNIQUEIDENTIFIER,
    @DemandQuantity INT,
    @AcquiredQuantity INT
)
AS
    SET NOCOUNT OFF;
    SET XACT_ABORT ON;

    UPDATE [UserData].[Item]   
    SET 
        ListId = @ListId,
        TemplateId = @ItemTemplateId,
        DemandQuantity = @DemandQuantity,
        AcquiredQuantity = @AcquiredQuantity
    WHERE
        ItemId = @ItemId
GO

CREATE PROCEDURE [UserData].[DeleteItem]
(
    @ItemId UNIQUEIDENTIFIER
)
AS
    SET NOCOUNT OFF;
    SET XACT_ABORT ON;

    UPDATE [UserData].[Item]   
    SET 
        IsDeleted = 1,
        Deleted = GETUTCDATE()
    WHERE
        ItemId = @ItemId
GO