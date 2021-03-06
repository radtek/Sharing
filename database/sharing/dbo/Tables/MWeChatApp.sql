﻿CREATE TABLE [dbo].[MWeChatApp]
(
	[MerchantId] BIGINT NOT NULL CONSTRAINT [FK_MWeChatApp_MerchantId] FOREIGN KEY REFERENCES [dbo].[Merchant] ([Id]),
	[AppType] INT NOT NULL,
	[AppId] NVARCHAR(32) NOT NULL,
	[Secret] NVARCHAR(32) NOT NULL,
	[OriginalId] NVARCHAR(32) NOT NULL CONSTRAINT [UK_WeChatApp_OriginalId] UNIQUE([OriginalId]) ,
	[Payment] NVARCHAR(MAX),
	[CreatedBy] NVARCHAR(50) NULL, 
	[CreatedDateTime] BIGINT NULL,         
	[LastUpdatedBy] NVARCHAR(50) NULL,
	[LastUpdatedDateTime] BIGINT NULL, 
	[ScenarioId] UNIQUEIDENTIFIER NULL
)
