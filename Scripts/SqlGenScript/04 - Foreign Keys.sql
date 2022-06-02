USE [#DATABASE_NAME#]
GO

DELETE FROM [Data].[CurrencyExchange]
WHERE [Date] IS NULL
GO

ALTER TABLE [Data].[CurrencyExchange]
ALTER COLUMN [Date] DATE NOT NULL

ALTER TABLE [Data].[CurrencyExchange]
ALTER COLUMN [FromCurrency] NCHAR(3) NOT NULL

ALTER TABLE [Data].[CurrencyExchange]
ALTER COLUMN [ToCurrency] NCHAR(3) NOT NULL
GO

ALTER TABLE [Data].[CurrencyExchange]
ADD PRIMARY KEY ( [Date], [FromCurrency], [ToCurrency] )
	
	
ALTER TABLE [Data].[Orders]
ADD CONSTRAINT FK_Orders_CustomerKey FOREIGN KEY ([CustomerKey]) REFERENCES [Data].[Customer]([CustomerKey])

ALTER TABLE [Data].[Orders]
ADD CONSTRAINT FK_Orders_StoreKey FOREIGN KEY ([StoreKey]) REFERENCES [Data].[Store]([StoreKey])

ALTER TABLE [Data].[Orders]
ADD CONSTRAINT FK_Orders_OrderDateKey FOREIGN KEY ([Order Date]) REFERENCES [Data].[Date]([Date])

ALTER TABLE [Data].[OrderRows]
ADD CONSTRAINT FK_OrderRows_OrderKey FOREIGN KEY ([OrderKey]) REFERENCES [Data].[Orders]([OrderKey])

ALTER TABLE [Data].[OrderRows]
ADD CONSTRAINT FK_OrderRows_ProductKey FOREIGN KEY ([ProductKey]) REFERENCES [Data].[Product]([ProductKey])
