USE [#DATABASE_NAME#]
GO
SET STATISTICS IO ON 
GO 
SET DATEFIRST 7, -- Default is 7 (US / Sunday)  
    DATEFORMAT mdy, 
    LANGUAGE   US_ENGLISH;
GO 

CREATE OR ALTER FUNCTION [dbo].[fn_IsHoliday]
(
    @date  date
)
RETURNS bit
AS
BEGIN
    -- for ease of typing
    DECLARE @year  int = DATEPART(YEAR, @date);
    DECLARE @month int = DATEPART(MONTH,@date);
    DECLARE @day   int = DATEPART(DAY, @date);
    DECLARE @dayName varchar(12) = DATENAME(DW, @date );

    DECLARE @nthWeekDay int = ceiling(@day / 7.0);
    DECLARE @isThursday bit = CASE WHEN @dayName LIKE 'Thursday' THEN 1 ELSE 0 END;
    DECLARE @isFriday   bit = CASE WHEN @dayName LIKE 'Friday' THEN 1 ELSE 0 END;
    DECLARE @isSaturday bit = CASE WHEN @dayName LIKE 'Saturday' THEN 1 ELSE 0 END;
    DECLARE @isSunday   bit = CASE WHEN @dayName LIKE 'Sunday' THEN 1 ELSE 0 END;
    DECLARE @isMonday   bit = CASE WHEN @dayName LIKE 'Monday' THEN 1 ELSE 0 END;
    DECLARE @isWeekend  bit = CASE WHEN @isSaturday = 1 OR @isSunday = 1 THEN 1 ELSE 0 END;
     
    ---- New Years Day
    if (@month = 12 AND @day = 31 AND @isFriday=1) return 1;
    if (@month = 1 AND @day = 1 AND @isWeekend=0) return 1;
    if (@month = 1 AND @day = 2 AND @isMonday=1) return 1;

    ---- MLK day
    if (@month = 1 AND @isMonday = 1 AND @nthWeekDay = 3) return 1;

    ------ President’s Day ( 3rd Monday in February )
    if (@month = 2 AND @isMonday = 1 AND @nthWeekDay = 3) return 1;

    ------ Memorial Day ( Last Monday in May )
    if (@month = 5 AND @isMonday = 1 AND DATEPART(MONTH, DATEADD(DAY, 7, @Date)) = 6) return 1;

    ------ Independence Day ( July 4 )
    if (@month = 7 AND @day = 3 AND @isFriday = 1) return 1;
    if (@month = 7 AND @day = 4 AND @isWeekend = 0) return 1;
    if (@month = 7 AND @day = 5 AND @isMonday = 1) return 1;

    ------ Labor Day ( 1st Monday in September )
    if (@month = 9 AND @isMonday = 1 AND @nthWeekDay = 1) return 1;

    ------ Columbus Day ( 2nd Monday in October )
    if (@month = 10 AND @isMonday = 1 AND @nthWeekDay = 2) return 1;

    ------ Veteran’s Day ( November 11 )
    if (@month = 11 AND @day = 10 AND @isFriday = 1) return 1;
    if (@month = 11 AND @day = 11 AND @isWeekend = 0) return 1;
    if (@month = 11 AND @day = 12 AND @isMonday = 1) return 1;

    ------ Thanksgiving Day ( 4th Thursday in November )
    if (@month = 11 AND @isThursday = 1 AND @nthWeekDay = 4) return 1;

    ------ Christmas Day ( December 25 )
    if (@month = 12 AND @day = 24 AND @isFriday = 1) return 1;
    if (@month = 12 AND @day = 25 AND @isWeekend = 0) return 1;
    if (@month = 12 AND @day = 25 AND @isMonday = 1) return 1;

    return 0;
END

GO

DECLARE @StartYear AS INT DECLARE @EndYear AS INT 
SET @StartYear = (SELECT YEAR ( MIN ( [Order Date] ) ) FROM [Data].Orders)
SET @EndYear = (SELECT YEAR ( MAX ( [Delivery Date] ) ) FROM [Data].Orders);
-- NOTE: we don't use the Holidays table
WITH 
Years AS (
  SELECT YYYY = @StartYear 
  UNION ALL 
  SELECT YYYY + 1 FROM Years WHERE YYYY < @EndYear
), 
Months AS (
  SELECT MM = 1 
  UNION ALL 
  SELECT MM + 1 FROM Months WHERE MM < 12
), 
Days AS (
  SELECT DD = 1 
  UNION ALL 
  SELECT DD + 1 FROM Days WHERE DD < 31
), 
DatesRaw AS (
  SELECT 
    YYYY = YYYY, 
    MM = MM, 
    DD = DD, 
    ID_Date = YYYY * 10000 + MM * 100 + DD, 
    DateString = CAST(YYYY * 10000 + MM * 100 + DD AS VARCHAR), 
    Date = CASE WHEN ISDATE(YYYY * 10000 + MM * 100 + DD) = 1 THEN CAST(
      CAST(YYYY * 10000 + MM * 100 + DD AS VARCHAR) AS DATETIME
    ) ELSE NULL END 
  FROM 
    Years 
    CROSS JOIN Months 
    CROSS JOIN Days 
  WHERE 
    ISDATE(YYYY * 10000 + MM * 100 + DD) = 1
) 
SELECT 
  DatesRaw.*, 
  DayOfWeek = DATEPART(dw, DatesRaw.Date), 
  CalendarDaySequential = CAST(DatesRaw.Date AS INT), 
  WorkingDay = CAST(
    CASE DATEPART(dw, DatesRaw.Date) 
        WHEN 1 THEN 0 -- Sunday 
        WHEN 7 THEN 0 -- Saturday
        ELSE CASE WHEN [dbo].[fn_IsHoliday]( DatesRaw.[Date] ) = 1 THEN 0 ELSE 1 END
    END AS BIT
  ) INTO #Calendar     
FROM 
  DatesRaw
ORDER BY ID_Date 
GO 

-----------------------------------------------------------------------------------------------------------

IF OBJECT_ID('[Data].[Date]', 'U') IS NOT NULL 
    DROP TABLE [Data].[Date]
GO

SELECT 
    [Date] = [Date],
    DateKey = ID_Date,
    [Year] = YEAR([Date]),
    [Year Quarter] = CAST ( 'Q' + CAST(DATEPART(QUARTER,[Date]) AS CHAR(1)) + FORMAT([Date],'-yyyy', 'en-US') AS NVARCHAR(30)),
    [Year Quarter Number] = DATEPART(QUARTER,[Date]) + 4 * YEAR([Date]),
    [Quarter] = 'Q' + CAST(DATEPART(QUARTER,[Date]) AS CHAR(1)),
    [Year Month] = CAST ( FORMAT([Date],'MMMM yyyy') AS NVARCHAR(30)),
    [Year Month Short] = CAST ( FORMAT([Date],'MMM yyyy') AS NVARCHAR(30)),
    [Year Month Number] = DATEPART(MONTH,[Date]) + 12 * YEAR([Date]),
    [Month] = CAST ( FORMAT([Date],'MMMM') AS NVARCHAR(30)),
    [Month Short] = CAST ( FORMAT([Date],'MMM') AS NVARCHAR(30)),
    [Month Number] = MONTH([Date]),
    [Day of Week] = CAST ( FORMAT([Date],'dddd') AS NVARCHAR(30)),
    [Day of Week Short] = CAST ( FORMAT([Date],'ddd') AS NVARCHAR(30)),
    [Day of Week Number] = DATEPART(WEEKDAY,[Date]),
    [Working Day] = [WorkingDay],
    [Working Day Number] = ( SELECT COUNT(WorkingDay) FROM #Calendar wd3 WHERE wd3.CalendarDaySequential <= wd1.CalendarDaySequential AND wd3.WorkingDay = 1 )
INTO [Data].[Date]
FROM #Calendar wd1
GO

DROP TABLE #Calendar
GO

ALTER TABLE [Data].[Date]
ALTER COLUMN [Date] DATE NOT NULL
GO

ALTER TABLE [Data].[Date]
ADD PRIMARY KEY ( [Date] )
GO

CREATE OR ALTER VIEW dbo.Date AS
    SELECT 
      [Date], 
      -- [DateKey],  -- We do not import DateKey in the view
      [Year], 
      [Year Quarter], 
      [Year Quarter Number], 
      [Quarter], 
      [Year Month], 
      [Year Month Short], 
      [Year Month Number], 
      [Month], 
      [Month Short], 
      [Month Number], 
      [Day of Week], 
      [Day of Week Short], 
      [Day of Week Number], 
      [Working Day], 
      [Working Day Number] 
    FROM 
      [Data].[Date]
GO
