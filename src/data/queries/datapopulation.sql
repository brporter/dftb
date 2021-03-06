DECLARE @AccountID UNIQUEIDENTIFIER
DECLARE @ItemID_1 UNIQUEIDENTIFIER
DECLARE @ItemID_2 UNIQUEIDENTIFIER

SET @AccountID = NEWID()
SET @ItemID_1 = NEWID()
SET @ItemID_2 = NEWID()

INSERT INTO [UserDefinitions].[Account] (AccountId, EmailAddress) VALUES ('00000000-0000-0000-0000-000000000000', 'System');
INSERT INTO [UserDefinitions].[Account] (AccountId, EmailAddress) VALUES (@AccountID, 'bryan@bryanporter.com')
SELECT * FROM [UserDefinitions].[Account]

DECLARE @ListID UNIQUEIDENTIFIER
SET @ListID = NEWID()
INSERT INTO [UserData].[List] (ListId, Name) VALUES (@ListID, 'Some List')
SELECT * FROM [UserData].[List]

INSERT INTO [UserData].[ListAssignment] (ListId, AccountId) VALUES (@ListID, @AccountID)

DECLARE @ItemTemplateID_1 UNIQUEIDENTIFIER
DECLARE @ItemTemplateID_2 UNIQUEIDENTIFIER
SET @ItemTemplateID_1 = NEWID()
SET @ItemTemplateID_2 = NEWID()

INSERT INTO [UserData].[ItemTemplate] (AccountId, ItemTemplateId, Name, ImageUrl, UPC) VALUES ('00000000-0000-0000-0000-000000000000', @ItemTemplateID_1, 'Oreos', '', '044000064969')
INSERT INTO [UserData].[ItemTemplate] (AccountId, ItemTemplateId, Name, ImageUrl, UPC) VALUES (@AccountID, @ItemTemplateID_2, 'Asparagus', '', '')
SELECT * FROM [UserData].[ItemTemplate]

INSERT INTO [UserData].[Item] (ItemId, ListId, TemplateId, DemandQuantity, AcquiredQuantity, IsDeleted, Created, Deleted) VALUES (@ItemID_1, @ListID, @ItemTemplateID_1, 6, 3, 0, GETUTCDATE(), CONVERT(DATETIME, 0))
INSERT INTO [UserData].[Item] (ItemId, ListId, TemplateId, DemandQuantity, AcquiredQuantity, IsDeleted, Created, Deleted) VALUES (@ItemID_2, @ListID, @ItemTemplateID_2, 3, 2, 0, GETUTCDATE(), CONVERT(DATETIME, 0))
SELECT * FROM [UserData].[Item]

SELECT List.*
FROM
    [UserData].[List] List
WHERE
    List.AccountId = '1a099f28-920d-45e5-aead-4645b1aac94d'

SELECT ItemTemplate.*
FROM
    [UserData].[ItemTemplate] ItemTemplate
    INNER JOIN [UserData].[Item] Item on Item.TemplateId = ItemTemplate.ItemTemplateId
    INNER JOIN [UserData].[List] List on List.ListId = Item.ListId AND List.AccountId = '1a099f28-920d-45e5-aead-4645b1aac94d'

SELECT Item.*
FROM
    [UserData].[Item] Item
    INNER JOIN [UserData].[List] List ON List.ListId = Item.ListId AND List.AccountId = '1a099f28-920d-45e5-aead-4645b1aac94d'

SELECT * FROM [Journal].[Item]
SELECT * FROM [UserData].[Item]


SELECT * FROM [UserDefinitions].[Account]
SELECT * FROM [UserData].[List]
SELECT * FROM [UserData].[ItemTemplate]

SELECT * FROM [Journal].[Item]
SELECT * FROM [Journal].[ItemTemplate]
SELECT * FROM [UserData].[Item]

DELETE FROM [Journal].[Item]
DELETE FROM [Journal].[ItemTemplate]
DELETE FROM [UserData].[Item]