USE [dftb]
GO

DROP TABLE IF EXISTS [UserData].[ItemStateHistory]
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
    CONSTRAINT PK_AccountId PRIMARY KEY (AccountId)
)
GO

CREATE TABLE [UserData].[List]
(
    ListId UNIQUEIDENTIFIER NOT NULL,
    AccountId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_ListId PRIMARY KEY (ListId),
    CONSTRAINT FK_List_AccountId FOREIGN KEY (AccountId) REFERENCES [UserDefinitions].[Account] (AccountId)
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
    CONSTRAINT PK_ItemTemplateId PRIMARY KEY (ItemTemplateId),
    CONSTRAINT FK_ItemTemplate_AccountId FOREIGN KEY (AccountId) REFERENCES [UserDefinitions].[Account] (AccountId)
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
    CONSTRAINT PK_ItemId PRIMARY KEY (ItemId),
    CONSTRAINT FK_TemplateId FOREIGN KEY (TemplateId) REFERENCES [UserData].[ItemTemplate] (ItemTemplateId),
    CONSTRAINT FK_ListId FOREIGN KEY (ListId) REFERENCES [UserData].[List] (ListId)
)
GO

CREATE TABLE [Journal].[Operation]
(
    OperationId INT NOT NULL,
    Name NVARCHAR(10),
    CONSTRAINT PK_OperationId PRIMARY KEY (OperationId)
)
GO

CREATE UNIQUE INDEX IX_OperationName ON [Journal].[Operation](Name)
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
    Name NVARCHAR(1024) NOT NULL,
    ImageUrl NVARCHAR(2048) NOT NULL,
    UPC NVARCHAR(24) NOT NULL,
    Created DATETIME NOT NULL,
    CONSTRAINT PK_ItemTemplate_JournalId PRIMARY KEY (JournalId),
    CONSTRAINT FK_ItemTemplate_OperationId FOREIGN KEY (OperationId) REFERENCES [Journal].[Operation] (OperationId)
)
GO

CREATE TABLE [Journal].[Item]
(
    JournalId UNIQUEIDENTIFIER NOT NULL,
    OperationId INT NOT NULL,
    ItemId UNIQUEIDENTIFIER NOT NULL,
    ItemTemplateId UNIQUEIDENTIFIER NOT NULL,
    DemandQuantity INT NOT NULL,
    AcquiredQuantity INT NOT NULL,
    Created DATETIME NOT NULL,
    CONSTRAINT PK_Item_JournalId PRIMARY KEY (JournalId),
    CONSTRAINT FK_Item_ItemTemplateId FOREIGN KEY (ItemTemplateId) REFERENCES [UserData].[ItemTemplate] (ItemTemplateId),
    CONSTRAINT FK_Item_OperationId FOREIGN KEY (OperationId) REFERENCES [Journal].[Operation] (OperationId)
)
GO

