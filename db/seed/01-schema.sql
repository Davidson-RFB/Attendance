-- Local development schema ONLY.
--
-- This is a best-effort reconstruction of the production schema, derived
-- from the EF6 model in DavidsonRFB.Attendance.Web/Models/AttendanceModels.cs
-- and its conventions (PluralizingTableNameConvention removed, so table
-- names match class names singular; EF6's default column type/nullability
-- conventions for the annotations present on each property).
--
-- IMPORTANT: this has NOT been diffed against the real production database
-- (no access to it during this migration). Before relying on this for an
-- actual cutover, validate it against the real schema - see Phase 8 in
-- docs/DOTNET10_MIGRATION_PLAN.md ("Open questions").
--
-- The application itself NEVER creates or alters schema (see
-- AttendanceDbConfiguration - Database.SetInitializer<AttendanceContext>(null)).
-- This script exists purely to give local docker-compose dev a usable,
-- disposable database.

IF OBJECT_ID(N'dbo.Attendance', N'U') IS NOT NULL DROP TABLE dbo.Attendance;
IF OBJECT_ID(N'dbo.Employee', N'U') IS NOT NULL DROP TABLE dbo.Employee;
IF OBJECT_ID(N'dbo.Job', N'U') IS NOT NULL DROP TABLE dbo.Job;
IF OBJECT_ID(N'dbo.Rank', N'U') IS NOT NULL DROP TABLE dbo.Rank;
IF OBJECT_ID(N'dbo.Brigade', N'U') IS NOT NULL DROP TABLE dbo.Brigade;
GO

CREATE TABLE dbo.Brigade
(
    Id       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name     NVARCHAR(MAX) NULL,
    District NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL
);
GO

CREATE TABLE dbo.Rank
(
    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Description NVARCHAR(MAX) NULL,
    ImageSource NVARCHAR(MAX) NULL,
    IsActive    BIT NOT NULL
);
GO

CREATE TABLE dbo.Job
(
    Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Description NVARCHAR(MAX) NULL,
    BrigadeId   INT NOT NULL,
    IsActive    BIT NOT NULL,
    CONSTRAINT FK_Job_Brigade FOREIGN KEY (BrigadeId) REFERENCES dbo.Brigade (Id)
);
GO

-- Employee.Id is [DatabaseGenerated(DatabaseGeneratedOption.None)] in the
-- model (Firezone Number, assigned by the brigade, not auto-generated).
CREATE TABLE dbo.Employee
(
    Id        INT NOT NULL PRIMARY KEY,
    Name      NVARCHAR(MAX) NOT NULL,
    BrigadeId INT NOT NULL,
    RankId    INT NULL,
    IsActive  BIT NOT NULL,
    Email     NVARCHAR(MAX) NOT NULL,
    CONSTRAINT FK_Employee_Brigade FOREIGN KEY (BrigadeId) REFERENCES dbo.Brigade (Id),
    CONSTRAINT FK_Employee_Rank FOREIGN KEY (RankId) REFERENCES dbo.Rank (Id)
);
GO

CREATE TABLE dbo.Attendance
(
    Id            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    EmployeeId    INT NOT NULL,
    JobId         INT NULL,
    StartDateTime DATETIME NOT NULL,
    EndDateTime   DATETIME NULL,
    CONSTRAINT FK_Attendance_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.Employee (Id),
    CONSTRAINT FK_Attendance_Job FOREIGN KEY (JobId) REFERENCES dbo.Job (Id)
);
GO
