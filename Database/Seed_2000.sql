/* English Center sample data
   Adds 2,000 student records (and linked users/enrollments) without deleting
   existing data. Run this file in the target SQL Server database.
   Seed student password: password
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @PasswordHash nvarchar(200) = N'$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy';

;WITH N AS (
    SELECT TOP (2000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS No
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT INTO Users (UserName, Email, PasswordHash, Role, IsDeleted)
SELECT CONCAT(N'seed_student_', RIGHT('0000' + CAST(No AS varchar(4)), 4)),
       CONCAT(N'seed.student', RIGHT('0000' + CAST(No AS varchar(4)), 4), N'@englishcenter.test'),
       @PasswordHash, N'Student', 0
FROM N
WHERE NOT EXISTS (
    SELECT 1 FROM Users u WHERE u.UserName = CONCAT(N'seed_student_', RIGHT('0000' + CAST(N.No AS varchar(4)), 4))
);

;WITH N AS (
    SELECT TOP (2000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS No
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT INTO Students (FullName, DateOfBirth, Email, Phone, Address, UserId, IsDeleted)
SELECT CONCAT(N'Học viên mẫu ', RIGHT('0000' + CAST(No AS varchar(4)), 4)),
       DATEADD(day, -(No % 7000), CAST('2005-01-01' AS date)),
       CONCAT(N'seed.student', RIGHT('0000' + CAST(No AS varchar(4)), 4), N'@englishcenter.test'),
       CONCAT(N'090', RIGHT('0000000' + CAST(No AS varchar(7)), 7)),
       CONCAT(N'Địa chỉ mẫu số ', No), u.Id, 0
FROM N
INNER JOIN Users u ON u.UserName = CONCAT(N'seed_student_', RIGHT('0000' + CAST(N.No AS varchar(4)), 4))
WHERE NOT EXISTS (SELECT 1 FROM Students s WHERE s.UserId = u.Id);

;WITH N AS (
    SELECT TOP (30) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS No
    FROM sys.all_objects
)
INSERT INTO Courses (CourseCode, CourseName, Description, Duration, TuitionFee, Status, IsDeleted)
SELECT CONCAT(N'SEED-C', RIGHT('00' + CAST(No AS varchar(2)), 2)),
       CONCAT(N'Khóa tiếng Anh mẫu ', No), N'Dữ liệu seed phục vụ kiểm thử',
       24 + (No % 12), 2000000 + (No * 100000), N'Active', 0
FROM N
WHERE NOT EXISTS (SELECT 1 FROM Courses c WHERE c.CourseCode = CONCAT(N'SEED-C', RIGHT('00' + CAST(N.No AS varchar(2)), 2)));

;WITH N AS (
    SELECT TOP (2000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS No
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT INTO Enrollments (StudentId, CourseId, EnrollmentDate, Status, IsDeleted)
SELECT s.Id, c.Id, DATEADD(day, -(N.No % 180), GETDATE()), N'Active', 0
FROM N
INNER JOIN Students s ON s.Email = CONCAT(N'seed.student', RIGHT('0000' + CAST(N.No AS varchar(4)), 4), N'@englishcenter.test')
INNER JOIN Courses c ON c.CourseCode = CONCAT(N'SEED-C', RIGHT('00' + CAST(((N.No - 1) % 30) + 1 AS varchar(2)), 2))
WHERE NOT EXISTS (SELECT 1 FROM Enrollments e WHERE e.StudentId = s.Id AND e.CourseId = c.Id AND e.IsDeleted = 0);

;WITH N AS (
    SELECT TOP (1000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS No
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT INTO Invoices (StudentId, EnrollmentId, Amount, InvoiceDate, Status, IsDeleted)
SELECT e.StudentId, e.Id, c.TuitionFee, DATEADD(day, -(N.No % 90), GETDATE()),
       CASE WHEN N.No % 3 = 0 THEN N'Paid' ELSE N'Unpaid' END, 0
FROM N
INNER JOIN Students s ON s.Email = CONCAT(N'seed.student', RIGHT('0000' + CAST(N.No AS varchar(4)), 4), N'@englishcenter.test')
INNER JOIN Enrollments e ON e.StudentId = s.Id AND e.IsDeleted = 0
INNER JOIN Courses c ON c.Id = e.CourseId
WHERE NOT EXISTS (SELECT 1 FROM Invoices i WHERE i.EnrollmentId = e.Id AND i.IsDeleted = 0);

COMMIT TRANSACTION;

SELECT
    (SELECT COUNT(*) FROM Users WHERE UserName LIKE 'seed_student_%') AS SeedUsers,
    (SELECT COUNT(*) FROM Students WHERE Email LIKE 'seed.student%@englishcenter.test') AS SeedStudents,
    (SELECT COUNT(*) FROM Enrollments e INNER JOIN Students s ON s.Id = e.StudentId WHERE s.Email LIKE 'seed.student%@englishcenter.test') AS SeedEnrollments,
    (SELECT COUNT(*) FROM Invoices i INNER JOIN Students s ON s.Id = i.StudentId WHERE s.Email LIKE 'seed.student%@englishcenter.test') AS SeedInvoices;
