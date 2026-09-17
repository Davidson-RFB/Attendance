-- Local development seed data ONLY - mirrors the original app's
-- DAL/AttendanceInitializer.cs seed (see legacy/DavidsonRFB.Attendance.Web),
-- so local dev has the same starting brigade/ranks/jobs/employees as a
-- freshly-provisioned instance of the original app did.

SET IDENTITY_INSERT dbo.Brigade OFF;

INSERT INTO dbo.Brigade (Name, District, IsActive)
VALUES (N'Davidson RFB', N'Warringah/Pittwater', 1);

DECLARE @BrigadeId INT = (SELECT Id FROM dbo.Brigade WHERE Name = N'Davidson RFB');

INSERT INTO dbo.Rank (Description, ImageSource, IsActive)
VALUES
    (N'Administration', NULL, 1),
    (N'Cadet', NULL, 1),
    (N'Captain', NULL, 1),
    (N'Deputy Captain', NULL, 1),
    (N'Deputy Group Captain', NULL, 1),
    (N'Firefighter', NULL, 1),
    (N'Group Captain', NULL, 1),
    (N'Senior Deputy Captain', NULL, 1),
    (N'Trainee', NULL, 1);

INSERT INTO dbo.Job (Description, BrigadeId, IsActive)
VALUES
    (N'Community Engagement', @BrigadeId, 1),
    (N'Duty Crew', @BrigadeId, 1),
    (N'Fire Call', @BrigadeId, 1),
    (N'Flyer', @BrigadeId, 1),
    (N'Hazard Reduction', @BrigadeId, 1),
    (N'Meeting', @BrigadeId, 1),
    (N'Training', @BrigadeId, 1);

INSERT INTO dbo.Employee (Id, Name, BrigadeId, RankId, IsActive, Email)
VALUES
    (93247, N'Jason King', @BrigadeId, (SELECT Id FROM dbo.Rank WHERE Description = N'Deputy Captain'), 1, N'jason.king@davidsonrfb.org'),
    (1234, N'Rolf Krolke', @BrigadeId, (SELECT Id FROM dbo.Rank WHERE Description = N'Deputy Group Captain'), 1, N'rolf.krolke@davidsonrfb.org'),
    (105544, N'Blake Dutton', @BrigadeId, (SELECT Id FROM dbo.Rank WHERE Description = N'Firefighter'), 1, N'blake.dutton@davidsonrfb.org'),
    (214874, N'James McCutcheon', @BrigadeId, (SELECT Id FROM dbo.Rank WHERE Description = N'Firefighter'), 1, N'jamesmccutcheon@davidsonrfb.org'),
    (95478, N'Tim Eliot', @BrigadeId, (SELECT Id FROM dbo.Rank WHERE Description = N'Deputy Captain'), 1, N'tim.eliot@davidsonrfb.org');
GO
