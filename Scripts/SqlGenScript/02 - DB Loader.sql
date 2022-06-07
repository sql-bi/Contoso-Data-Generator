-- Before running the script:
--     1) fix the USE to select the target database
--     2) Uncomment only one definition of @PathBase

-- Create database before running the script
USE [#DATABASE_NAME#]
GO
SET LANGUAGE US_ENGLISH
GO

-- 
--  PARAMETERS
-- 
CREATE TABLE #variables ( VarName VARCHAR(80) PRIMARY KEY, [Value] VARCHAR(255) )
GO

DECLARE @PathBase NVARCHAR(100) = '#PATH_BASE#'
DECLARE @DatasetFolder NVARCHAR(100) = '#DATASET_FOLDER#'

-- Variables with absolute paths for following parts of the batch
INSERT INTO #variables VALUES 
( 'Customer Names', @PathBase + '\Contoso Main\Customers from Fake Name Generator' ),
( 'Currency Exchange', @PathBase + '\Contoso Main\Currency Exchange' ),
( 'UK Postcodes', @PathBase + '\Contoso Main\Customers from Fake Name Generator\UK Postcodes.csv' ),
( 'Stores', @PathBase + '\Contoso Main\Stores\Stores.csv' ),
( 'Products', @PathBase + '\Contoso Main\Products\Products.csv' ),
( 'Orders', @DatasetFolder + '\orders.csv' ),
( 'OrderRows', @DatasetFolder + '\orderRows.csv' )
GO

--
--	Stored procedure to simplify loading of flat files for customers
--
--  Note: the files received from FakeNameGenerator have been converted to ANSI
--        to preserve the extended character set. It worked, even though I have
--        no clear idea of the reason.
--
CREATE OR ALTER PROC LoadFlatFile ( @Path NVARCHAR ( 1024 ), @Name NVARCHAR ( 1024 ) ) AS

BEGIN
	DECLARE @BulkInsert NVARCHAR(1024) = 'BULK INSERT [Data].Customer FROM '
	DECLARE @WithPart NVARCHAR(1024) = ' WITH ( TABLOCK, CODEPAGE=''1252'', FORMAT=''CSV'',ROWTERMINATOR=''0x0A'',FIRSTROW=2)'

	DECLARE @SqlStatement NVARCHAR(1024) = @BulkInsert + '''' + @Path + '\' + @Name + '''' + @WithPart
	PRINT ( @SqlStatement )
	EXEC ( @SqlStatement )
END
GO

--
-- Create Demo schema if it does not exist
--
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Data')
BEGIN
    -- The schema must be run in its own batch!
    EXEC( 'CREATE SCHEMA Data' );
END

--
--	Currency Conversion: first we drop and create the table
--
IF OBJECT_ID('[Data].CurrencyExchange', 'U') IS NOT NULL 
    DROP TABLE [Data].[CurrencyExchange]
GO

CREATE TABLE [Data].[CurrencyExchange] (
	[Date] DATE,
	[FromCurrency] NCHAR ( 3 ),
	[ToCurrency] NCHAR ( 3 ),
	[Exchange] FLOAT
)
GO


CREATE OR ALTER PROC LoadCurrency ( 
    @Path NVARCHAR ( 1024 ), 
    @From NCHAR ( 3 ), 
    @To1 NCHAR ( 3 ), 
    @To2 NCHAR ( 3 ), 
    @To3 NCHAR ( 3 ), 
    @To4 NCHAR ( 3 )
) AS
BEGIN
    CREATE TABLE [Data].CurrencyTemp ( 
        [Date] DATE,
        [Original] FLOAT,
        [Foreign1] FLOAT,
        [Foreign2] FLOAT,
        [Foreign3] FLOAT,
        [Foreign4] FLOAT
    )

    DECLARE @BulkInsert NVARCHAR(1024) = 'BULK INSERT [Data].CurrencyTemp FROM ' 
	DECLARE @WithPart NVARCHAR(1024) = ' WITH ( TABLOCK, FORMAT=''CSV'', FIRSTROW=2)'
    DECLARE @FileName NVARCHAR ( 1024 ) = '''' + @Path + '\' + @From + '.CSV'''

	DECLARE @SqlStatement NVARCHAR(1024) = @BulkInsert + @FileName + @WithPart
	PRINT ( @SqlStatement )
	EXEC ( @SqlStatement )

    DECLARE @Insert NVARCHAR ( 1024 ) 
    DECLARE @Select NVARCHAR ( 1024 ) 
    DECLARE @InsertStatement NVARCHAR(1024) 

    SET @Insert = 'INSERT INTO [Data].[CurrencyExchange] ( [Date], [FromCurrency], [ToCurrency], [Exchange] ) '

    SET @Select = 'SELECT [Date], ''' + @From + ''', ''' + @To1 + ''', [Foreign1] FROM [Data].CurrencyTemp '
    SET @InsertStatement = @Insert + @Select
	PRINT ( @InsertStatement )
	EXEC ( @InsertStatement )

    SET @Select = 'SELECT [Date], ''' + @From + ''', ''' + @To2 + ''', [Foreign2] FROM [Data].CurrencyTemp '
    SET @InsertStatement = @Insert + @Select
	PRINT ( @InsertStatement )
	EXEC ( @InsertStatement )

    SET @Select = 'SELECT [Date], ''' + @From + ''', ''' + @To3 + ''', [Foreign3] FROM [Data].CurrencyTemp '
    SET @InsertStatement = @Insert + @Select
	PRINT ( @InsertStatement )
	EXEC ( @InsertStatement )

    SET @Select = 'SELECT [Date], ''' + @From + ''', ''' + @To4 + ''', [Foreign4] FROM [Data].CurrencyTemp '
    SET @InsertStatement = @Insert + @Select
	PRINT ( @InsertStatement )
	EXEC ( @InsertStatement )

    DROP TABLE [Data].CurrencyTemp
END

GO

DECLARE @Path NVARCHAR(1024)
SELECT @Path = [Value] FROM #variables WHERE VarName = 'Currency Exchange'

EXEC [dbo].[LoadCurrency] @Path, 'AUD', 'CAD', 'EUR', 'GBP', 'USD'
EXEC [dbo].[LoadCurrency] @Path, 'CAD', 'AUD', 'EUR', 'GBP', 'USD'
EXEC [dbo].[LoadCurrency] @Path, 'EUR', 'AUD', 'CAD', 'GBP', 'USD'
EXEC [dbo].[LoadCurrency] @Path, 'GBP', 'AUD', 'CAD', 'EUR', 'USD'
EXEC [dbo].[LoadCurrency] @Path, 'USD', 'AUD', 'CAD', 'EUR', 'GBP'

GO

DROP PROC LoadCurrency 

GO

--
--  Here we insert conversion 1:1 from any currency to itself on
--  any date
--
INSERT INTO 
    [Data].[CurrencyExchange] (
        [Date], [FromCurrency], [ToCurrency], Exchange )
SELECT
    DISTINCT
        [CurrencyExchange].[Date],
        [CurrencyExchange].[FromCurrency],
        [CurrencyExchange].[FromCurrency] AS [ToCurrency],
        1 AS Exchange
    FROM
        [Data].[CurrencyExchange]

GO

-----------------------------------------------------------------------

IF OBJECT_ID('[Data].Product', 'U') IS NOT NULL 
    DROP TABLE [Data].Product
GO

CREATE TABLE [Data].[Product](
	[ProductKey] [int] NOT NULL PRIMARY KEY,
	[Product Code] [nvarchar](255) NULL,
	[Product Name] [nvarchar](500) NULL,
	[Manufacturer] [nvarchar](50) NULL,
	[Brand] [nvarchar](50) NULL,
	[Color] [nvarchar](20) NOT NULL,
	[Weight Unit Measure] [nvarchar](20) NULL,
	[Weight] [float] NULL,
	[Unit Cost] [money] NULL,
	[Unit Price] [money] NULL,
	[Subcategory Code] [nvarchar](100) NULL,
	[Subcategory] [nvarchar](50) NULL,
	[Category Code] [nvarchar](100) NULL,
	[Category] [nvarchar](30) NULL
) ON [PRIMARY]
GO

DECLARE @Path NVARCHAR(1024)
DECLARE @sql NVARCHAR(MAX)
SELECT @Path = [Value] FROM #variables WHERE VarName = 'Products'
SET @sql = 'BULK INSERT [Data].[Product] FROM ''' + @Path + ''' WITH ( TABLOCK, CODEPAGE=''1252'', FORMAT=''CSV'',FIRSTROW=2)'
EXEC (@sql)
GO

--
--  Here we load the stores from the .CSV file generated starting from Excel
--
IF OBJECT_ID('[Data].Store', 'U') IS NOT NULL 
    DROP TABLE [Data].Store
GO
CREATE TABLE [Data].Store (
    StoreKey INT PRIMARY KEY,
    [Store Code] INT,
    Country NVARCHAR ( 50 ),
    State NVARCHAR ( 50 ),
    Name NVARCHAR ( 100 ),
    [Square Meters] INT,
    [Open Date] DATE,
    [Close Date] DATE,
    Status NVARCHAR ( 50 )
)

DECLARE @Path NVARCHAR(1024)
DECLARE @sql NVARCHAR(MAX)
SELECT @Path = [Value] FROM #variables WHERE VarName = 'Stores'
SET @sql = 'BULK INSERT [Data].Store FROM ''' + @Path + ''' WITH ( TABLOCK, CODEPAGE=''1252'', FORMAT=''CSV'',FIRSTROW=2)'
EXEC (@sql)
GO


--
--  Here we load the orders table from .CSV generated by the DB tool
--

IF OBJECT_ID('[Data].Orders', 'U') IS NOT NULL 
    DROP TABLE [Data].Orders
GO

CREATE TABLE [Data].Orders (
    OrderKey BIGINT NOT NULL,
    CustomerKey INT,
    StoreKey INT,
    [Order Date] DATE,
    [Delivery Date] DATE,
    [Currency Code] NCHAR ( 3 ),
	INDEX IDX_Orders_Clustered CLUSTERED COLUMNSTORE
)
GO

DECLARE @Path NVARCHAR(1024)
DECLARE @sql NVARCHAR(MAX)
SELECT @Path = [Value] FROM #variables WHERE VarName = 'Orders'
SET @sql = 'BULK INSERT [Data].Orders FROM ''' + @Path + ''' WITH ( TABLOCK, CODEPAGE=''1252'', FORMAT=''CSV'',FIRSTROW=2)'
EXEC (@sql)
GO

-- Add primary key in nonclustered index with page compression
ALTER TABLE [Data].[Orders] ADD CONSTRAINT PK_Orders_OrderKey PRIMARY KEY (OrderKey)
	WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, 
          ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, DATA_COMPRESSION = PAGE)
		  
	
--
--	Drop and create of the Customer table
--
IF OBJECT_ID('dbo.Customer', 'V') IS NOT NULL 
    DROP VIEW dbo.Customer
GO
IF OBJECT_ID('Data.Customer', 'U') IS NOT NULL 
    DROP TABLE [Data].Customer
GO

CREATE TABLE [Data].[Customer](
	[CustomerKey] INT NOT NULL IDENTITY ( 1, 1 ) CONSTRAINT PK_Customer_CustomerKey PRIMARY KEY,
	[Gender] [nvarchar](10) NULL,
	[NameSet] [nvarchar](50) NULL,
	[Title] [nvarchar](50) NULL,
	[GivenName] [nvarchar](150) NULL,
	[MiddleInitial] [nvarchar](150) NULL,
	[Surname] [nvarchar](150) NULL,
	[StreetAddress] [nvarchar](150) NULL,
	[City] [nvarchar](50) NULL,
	[State] [nvarchar](50) NULL,
	[StateFull] [nvarchar](50) NULL,
	[ZipCode] nvarchar(50) NULL,
	[Country] [nvarchar](50) NULL,
	[CountryFull] [nvarchar](50) NULL,
	[EmailAddress] [nvarchar](50) NULL,
	[Username] [nvarchar](50) NULL,
	[Password] [nvarchar](50) NULL,
	[BrowserUserAgent] [nvarchar](150) NULL,
	[TelephoneNumber] [nvarchar](50) NULL,
	[TelephoneCountryCode] [int] NULL,
	[MothersMaiden] [nvarchar](150) NULL,
	[Birthday] [datetime2](7) NULL,
	[Age] [int] NULL,
	[TropicalZodiac] [nvarchar](50) NULL,
	[CCType] [nvarchar](50) NULL,
	[CCNumber] [float] NULL,
	[CVV2] [int] NULL,
	[CCExpires] [NVARCHAR](20) NULL,
	[NationalID] [nvarchar](50) NULL,
	[UPS] [nvarchar](50) NULL,
	[WesternUnionMTCN] [float] NULL,
	[MoneyGramMTCN] [int] NULL,
	[Color] [nvarchar](50) NULL,
	[Occupation] [nvarchar](100) NULL,
	[Company] [nvarchar](50) NULL,
	[Vehicle] [nvarchar](50) NULL,
	[Domain] [nvarchar](50) NULL,
	[BloodType] [nvarchar](50) NULL,
	[Pounds] [float] NULL,
	[Kilograms] [float] NULL,
	[FeetInches] [nvarchar](50) NULL,
	[Centimeters] [int] NULL,
	[GUID] [nvarchar](50) NULL,
	[Latitude] [float] NULL,
	[Longitude] [float] NULL
) 	
ON [PRIMARY]
WITH (DATA_COMPRESSION = PAGE)
GO
 
--
--	Here we load the flat files in Customer
--
DECLARE @Path NVARCHAR(1024)
SELECT @Path = [Value] FROM #variables WHERE VarName = 'Customer Names'

EXEC [dbo].[LoadFlatFile] @Path, 'AU 01 - AU Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'AU 02 - AU Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'CA 01 - US Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'CA 02 - US Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'DE 01 - DE Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'DE 02 - DE Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'FR 01 - FR Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'IT 01 - IT Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'NL 01 - NL names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'UK 01 - UK Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'UK 02 - UK Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'UK 03 - UK Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'US 01 - US Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'US 02 - US Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'US 03 - US Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'US 04 - US Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'US 05 - US Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'US 06 - US Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'US 07 - US Names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'US 08 - Mixed names.csv'
EXEC [dbo].[LoadFlatFile] @Path, 'US 09 - Mixed names.csv'
GO

--
--  Here we remove useless customers
--
DELETE FROM [Data].Customer 
WHERE CustomerKey 
NOT IN (SELECT DISTINCT CustomerKey FROM [Data].Orders )
AND #TRIM_CUSTOMERS#
GO

-- Fix column content
ALTER TABLE [Data].[Customer]
ADD Continent [nvarchar](50) NULL 

ALTER TABLE [Data].[Customer]
DROP COLUMN [NameSet],[EmailAddress], [Username], [Password], [BrowserUserAgent],
            [TelephoneNumber], [TelephoneCountryCode], [MothersMaiden], [TropicalZodiac],
            [CCType], [CCNumber], [CVV2], [CCExpires], [NationalID], [UPS], [WesternUnionMTCN],
            [MoneyGramMTCN], [Color], [Domain], [BloodType], [Pounds], [Kilograms], 
            [FeetInches], [Centimeters], [GUID]
GO

UPDATE [Data].[Customer]
SET Gender = UPPER ( LEFT ( Gender, 1 ) ) + SUBSTRING ( Gender, 2, LEN ( Gender ) - 1 )
GO

UPDATE [Data].[Customer]
SET Continent = CASE [Country]
	WHEN 'DE' THEN 'Europe'
	WHEN 'GB' THEN 'Europe'
	WHEN 'IT' THEN 'Europe'
	WHEN 'NL' THEN 'Europe'
	WHEN 'AU' THEN 'Australia'
	WHEN 'CA' THEN 'North America'
	WHEN 'FR' THEN 'Europe'
	WHEN 'US' THEN 'North America'
END

--
--	And we can destroy the stored procedure, it is no longer usefu
--
DROP PROC LoadFlatFile 
GO

--
--  There are several customers in a state named MP (Northern Mariana Islands),
--  it is easier to delete them than to handle them
--
DELETE FROM [Data].Customer WHERE StateFull IS NULL AND CountryFull = 'United States'
GO

--
--  United Kingdom is missing the regions, we assign them based on the postal code
--
GO
IF OBJECT_ID('[Data].UkPostCodes', 'U') IS NOT NULL 
    DROP TABLE [Data].UkPostCodes
GO
CREATE TABLE [Data].UkPostCodes (
    ZipCode NVARCHAR ( 50 ),
    StateFull NVARCHAR ( 50 )
)
GO
DECLARE @Path NVARCHAR(1024)
DECLARE @sql NVARCHAR(MAX)
SELECT @Path = [Value] FROM #variables WHERE VarName = 'UK Postcodes'
SET @sql = 'BULK INSERT [Data].UkPostCodes FROM ''' + @Path + ''' WITH ( TABLOCK, CODEPAGE=''1252'', FORMAT=''CSV'',FIRSTROW=2)'
EXEC (@sql)
GO

UPDATE [Data].Customer SET 
    [State] = P.StateFull ,
    StateFull = P.StateFull 
FROM [Data].Customer C
INNER JOIN [Data].UkPostCodes P ON 
    P.ZipCode = LEFT ( C.zipcode, CHARINDEX ( ' ', C.ZipCode ) - 1 )
WHERE countryfull = 'United Kingdom'
GO

IF OBJECT_ID('[Data].UkPostCodes', 'U') IS NOT NULL 
    DROP TABLE [Data].UkPostCodes
GO

DELETE FROM [Data].Customer 
WHERE CountryFull = 'United Kingdom' AND StateFull IS NULL
GO

	  
--
--  Here we load the orderRows table from .CSV generated by the DB tool
--
IF OBJECT_ID('[Data].OrderRows', 'U') IS NOT NULL 
    DROP TABLE [Data].OrderRows
GO

CREATE TABLE [Data].OrderRows ( 
    OrderKey BIGINT,
    [Line Number] INT,
    ProductKey INT,
    Quantity INT,
    [Unit Price] MONEY,
    [Net Price] MONEY,
    [Unit Cost] MONEY,
	INDEX IDX_OrderRows_Clustered CLUSTERED COLUMNSTORE
)
-- We don't enable the primary key on OrderRows to avoid an additional index
-- PRIMARY KEY ([OrderKey], [Line Number]),

DECLARE @Path NVARCHAR(1024)
DECLARE @sql NVARCHAR(MAX)
SELECT @Path = [Value] FROM #variables WHERE VarName = 'OrderRows'
SET @sql = 'BULK INSERT [Data].OrderRows FROM ''' + @Path + ''' WITH ( TABLOCK, CODEPAGE=''1252'', FORMAT=''CSV'',FIRSTROW=2)'
EXEC (@sql)
GO


------------------------------------------------------------------------------------------
--
--	Views
--
------------------------------------------------------------------------------------------
CREATE OR ALTER VIEW dbo.Customer AS
	SELECT
		[CustomerKey],
		[Gender],
		[GivenName] + ' ' + [Surname] AS [Name],
		[StreetAddress] AS [Address],
		[City],
		[State] AS [State Code],
		StateFull AS [State],
		ZipCode AS [Zip Code],
		[Country] as [Country Code],
		[CountryFull] as [Country],
		[Continent],
		[Birthday],
		[Age] as [Age]
	FROM
		[Data].Customer
GO


CREATE OR ALTER VIEW dbo.Product AS
	SELECT
		ProductKey,
		[Product Code],
		[Product Name],
		[Manufacturer],
		[Brand],
		[Color],
		[Weight Unit Measure],
		[Weight],
		[Unit Cost],
		[Unit Price],
		[Subcategory Code],
		Subcategory,
		[Category Code],
		Category
	FROM [Data].Product

GO

CREATE OR ALTER VIEW dbo.[Currency Exchange] AS
    SELECT
	    [Date],
	    [FromCurrency],
	    [ToCurrency],
	    [Exchange]
    FROM [Data].[CurrencyExchange]
GO


CREATE OR ALTER VIEW dbo.Store AS
SELECT 
    StoreKey,
    [Store Code],
    [Country],
    [State],
    [Name],
    [Square Meters],
    [Open Date],
    [Close Date],
    [Status]
FROM
    [Data].Store
GO

CREATE OR ALTER VIEW dbo.Sales AS
SELECT 
        Orders.OrderKey AS [Order Number],
        OrderRows.[Line Number] AS [Line Number],
        Orders.[Order Date],
        Orders.[Delivery Date],
        Orders.CustomerKey,
        Orders.StoreKey,
        OrderRows.ProductKey,
        OrderRows.Quantity,
        OrderRows.[Unit Price],
        OrderRows.[Net Price],
        OrderRows.[Unit Cost],
        Orders.[Currency Code],
        [CurrencyExchange].Exchange AS [Exchange Rate]
    FROM
        [Data].Orders  
            LEFT OUTER JOIN [Data].OrderRows
                ON Orders.OrderKey = OrderRows.OrderKey
            LEFT OUTER JOIN [Data].[CurrencyExchange]
                ON 
                    [CurrencyExchange].Date = Orders.[Order Date] AND
                    [CurrencyExchange].[ToCurrency] = Orders.[Currency Code] AND
                    [CurrencyExchange].[FromCurrency] = 'USD'
                    
GO


------------------------------------------------------------------------------------------
--
--  Tables needed for the database generator
--
------------------------------------------------------------------------------------------
IF OBJECT_ID('[Data].GeoLocations', 'U') IS NOT NULL 
    DROP TABLE [Data].GeoLocations
GO
CREATE TABLE [Data].GeoLocations ( 
    GeoLocationKey INT,
	[CountryCode]  NVARCHAR ( 50 ),
	[Country]  NVARCHAR ( 50 ),
    [StateCode] NVARCHAR ( 50 ),
	[State]  NVARCHAR ( 50 ),
    [NumCustomers] INT
)
GO
--
--  GeoLocations is created starting from the customers
--
INSERT INTO [Data].GeoLocations (
    GeoLocationKey,
	[CountryCode],
	[Country],
    [StateCode],
	[State],
    NumCustomers )
SELECT 
    ROW_NUMBER () OVER ( ORDER BY 
	    [Country Code],
	    [Country],
        [State Code],
	    [State]
    ),
    * 
FROM ( 
    SELECT
	    [Country Code],
	    [Country],
        [State Code],
	    [State],
        COUNT ( CustomerKey ) AS NumCustomers
    FROM 
        dbo.Customer
    GROUP BY 
	    [Country Code],
	    [Country],
        [State Code],
	    [State]
) A

GO

------------------------------------------------------------------------------------------
--
--  Create the ONLINE dummy geo location
--
------------------------------------------------------------------------------------------
INSERT INTO [Data].GeoLocations (
    GeoLocationKey,
	[CountryCode],
	[Country],
    [StateCode],
	[State],
    NumCustomers )
SELECT
    -1,
	'',
	'',
    '',
	'',
    0

GO

------------------------------------------------------------------------------------------
--
-- Final cleanup
--
------------------------------------------------------------------------------------------

-- Remove variables table
DROP TABLE #variables
GO

-- Rebuild Customer index
ALTER TABLE [Data].[Customer] DROP CONSTRAINT PK_Customer_CustomerKey;
CREATE CLUSTERED COLUMNSTORE INDEX IDX_Customer_Columnstore ON [Data].[Customer];
ALTER TABLE [Data].[Customer]  ADD CONSTRAINT PK_Customer_CustomerKey PRIMARY KEY (CustomerKey)
	WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, 
          ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, DATA_COMPRESSION = PAGE)
GO

