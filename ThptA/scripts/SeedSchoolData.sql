
USE ThptA_FullDev;
GO

SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- 1. Tạo năm học 2026-2027 nếu chưa tồn tại
IF NOT EXISTS (
    SELECT 1
    FROM dbo.AcademicYears
    WHERE Code = '2026-2027'
)
BEGIN
    INSERT INTO dbo.AcademicYears
        (Code, StartDate, EndDate)
    VALUES
        ('2026-2027', '2026-09-01', '2027-05-31');
END;

-- 2. Lấy ID của năm học
DECLARE @YearId INT;

SELECT @YearId = Id
FROM dbo.AcademicYears
WHERE Code = '2026-2027';

-- 3. Tạo lớp 10A1
IF NOT EXISTS (
    SELECT 1 FROM dbo.SchoolClasses
    WHERE Name = '10A1' AND AcademicYearId = @YearId
)
BEGIN
    INSERT INTO dbo.SchoolClasses
        (Name, Grade, AcademicYearId)
    VALUES ('10A1', 10, @YearId);
END;

-- 4. Tạo lớp 11A1
IF NOT EXISTS (
    SELECT 1 FROM dbo.SchoolClasses
    WHERE Name = '11A1' AND AcademicYearId = @YearId
)
BEGIN
    INSERT INTO dbo.SchoolClasses
        (Name, Grade, AcademicYearId)
    VALUES ('11A1', 11, @YearId);
END;

-- 5. Tạo lớp 12A1
IF NOT EXISTS (
    SELECT 1 FROM dbo.SchoolClasses
    WHERE Name = '12A1' AND AcademicYearId = @YearId
)
BEGIN
    INSERT INTO dbo.SchoolClasses
        (Name, Grade, AcademicYearId)
    VALUES ('12A1', 12, @YearId);
END;

COMMIT TRANSACTION;

-- 6. Xem kết quả
SELECT
    c.Id,
    c.Name AS TenLop,
    c.Grade AS Khoi,
    y.Code AS NamHoc
FROM dbo.SchoolClasses c
JOIN dbo.AcademicYears y
    ON c.AcademicYearId = y.Id
ORDER BY c.Grade, c.Name;
