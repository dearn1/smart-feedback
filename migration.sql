IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(128) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'00000000000000_CreateIdentitySchema', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021020612_student'
)
BEGIN
    CREATE TABLE [Student] (
        [Id] int NOT NULL IDENTITY,
        [StudentId] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Student] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021020612_student'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251021020612_student', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021085524_rubrics'
)
BEGIN
    CREATE TABLE [Rubrics] (
        [RubricsId] int NOT NULL IDENTITY,
        [RubricName] nvarchar(max) NOT NULL,
        [Institution] nvarchar(max) NOT NULL,
        [Programme] nvarchar(max) NOT NULL,
        [CourseCode] nvarchar(max) NOT NULL,
        [CourseName] nvarchar(max) NOT NULL,
        [TotalMarks] int NOT NULL,
        [SourceFile] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Rubrics] PRIMARY KEY ([RubricsId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251021085524_rubrics'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251021085524_rubrics', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251022234501_rubricsTask'
)
BEGIN
    CREATE TABLE [RubricTask] (
        [RubricTaskId] int NOT NULL IDENTITY,
        [RubricsId] int NOT NULL,
        [TaskTitle] nvarchar(max) NOT NULL,
        [TaskDescription] nvarchar(max) NOT NULL,
        [MaxMarks] int NOT NULL,
        CONSTRAINT [PK_RubricTask] PRIMARY KEY ([RubricTaskId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251022234501_rubricsTask'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251022234501_rubricsTask', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026185625_rubricsAddTermName'
)
BEGIN
    ALTER TABLE [Rubrics] ADD [TermName] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026185625_rubricsAddTermName'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251026185625_rubricsAddTermName', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251027010139_rubricCriteria'
)
BEGIN
    CREATE TABLE [RubricCriteria] (
        [RubricCriteriaId] int NOT NULL IDENTITY,
        [RubricTaskId] int NOT NULL,
        [CriterionTitle] nvarchar(max) NOT NULL,
        [Weight] float NOT NULL,
        [MaxScore] int NOT NULL,
        CONSTRAINT [PK_RubricCriteria] PRIMARY KEY ([RubricCriteriaId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251027010139_rubricCriteria'
)
BEGIN
    CREATE TABLE [RubricCriteriaScore] (
        [RubricCriteriaScoreId] int NOT NULL IDENTITY,
        [RubricCriteriaId] int NOT NULL,
        [CriterionScore] int NOT NULL,
        [ScoreDescription] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_RubricCriteriaScore] PRIMARY KEY ([RubricCriteriaScoreId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251027010139_rubricCriteria'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251027010139_rubricCriteria', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102191302_courserole'
)
BEGIN
    CREATE TABLE [CourseRoles] (
        [CourseRolesId] int NOT NULL IDENTITY,
        [CourseCode] nvarchar(max) NOT NULL,
        [CourseName] nvarchar(max) NOT NULL,
        [TermName] nvarchar(max) NOT NULL,
        [RoleLecturer] nvarchar(max) NOT NULL,
        [RoleModerator] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_CourseRoles] PRIMARY KEY ([CourseRolesId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251102191302_courserole'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251102191302_courserole', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251103183730_addScoreTitle'
)
BEGIN
    ALTER TABLE [RubricCriteriaScore] ADD [ScoreTitle] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251103183730_addScoreTitle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251103183730_addScoreTitle', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106183015_uniquekeyStudentId'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Student]') AND [c].[name] = N'StudentId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Student] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Student] ALTER COLUMN [StudentId] nvarchar(450) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106183015_uniquekeyStudentId'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Student_StudentId] ON [Student] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106183015_uniquekeyStudentId'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251106183015_uniquekeyStudentId', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251110210614_courseAdditional'
)
BEGIN
    ALTER TABLE [CourseRoles] ADD [Institution] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251110210614_courseAdditional'
)
BEGIN
    ALTER TABLE [CourseRoles] ADD [Programme] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251110210614_courseAdditional'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251110210614_courseAdditional', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113085127_AddAssessmentTables'
)
BEGIN
    CREATE TABLE [Assessments] (
        [AssessmentId] int NOT NULL IDENTITY,
        [AssessmentName] nvarchar(max) NOT NULL,
        [CourseCode] nvarchar(max) NOT NULL,
        [TermName] nvarchar(max) NOT NULL,
        [RubricsId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Assessments] PRIMARY KEY ([AssessmentId]),
        CONSTRAINT [FK_Assessments_Rubrics_RubricsId] FOREIGN KEY ([RubricsId]) REFERENCES [Rubrics] ([RubricsId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113085127_AddAssessmentTables'
)
BEGIN
    CREATE TABLE [StudentAssessmentScores] (
        [StudentAssessmentScoreId] int NOT NULL IDENTITY,
        [AssessmentId] int NOT NULL,
        [StudentId] int NOT NULL,
        [RubricCriteriaId] int NOT NULL,
        [Score] int NOT NULL,
        [CustomComment] nvarchar(max) NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_StudentAssessmentScores] PRIMARY KEY ([StudentAssessmentScoreId]),
        CONSTRAINT [FK_StudentAssessmentScores_Assessments_AssessmentId] FOREIGN KEY ([AssessmentId]) REFERENCES [Assessments] ([AssessmentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_StudentAssessmentScores_RubricCriteria_RubricCriteriaId] FOREIGN KEY ([RubricCriteriaId]) REFERENCES [RubricCriteria] ([RubricCriteriaId]) ON DELETE CASCADE,
        CONSTRAINT [FK_StudentAssessmentScores_Student_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Student] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113085127_AddAssessmentTables'
)
BEGIN
    CREATE INDEX [IX_Assessments_RubricsId] ON [Assessments] ([RubricsId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113085127_AddAssessmentTables'
)
BEGIN
    CREATE INDEX [IX_StudentAssessmentScores_AssessmentId] ON [StudentAssessmentScores] ([AssessmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113085127_AddAssessmentTables'
)
BEGIN
    CREATE INDEX [IX_StudentAssessmentScores_RubricCriteriaId] ON [StudentAssessmentScores] ([RubricCriteriaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113085127_AddAssessmentTables'
)
BEGIN
    CREATE INDEX [IX_StudentAssessmentScores_StudentId] ON [StudentAssessmentScores] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113085127_AddAssessmentTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251113085127_AddAssessmentTables', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225081526_AddCustomUserProperties'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Department] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225081526_AddCustomUserProperties'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [FullName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225081526_AddCustomUserProperties'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [JobTitle] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251225081526_AddCustomUserProperties'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251225081526_AddCustomUserProperties', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251228214335_AddAssessmentStatus'
)
BEGIN
    ALTER TABLE [StudentAssessmentScores] ADD [ModeratorComments] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251228214335_AddAssessmentStatus'
)
BEGIN
    ALTER TABLE [Assessments] ADD [Status] nvarchar(50) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251228214335_AddAssessmentStatus'
)
BEGIN
    ALTER TABLE [Assessments] ADD [StatusChangedBy] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251228214335_AddAssessmentStatus'
)
BEGIN
    ALTER TABLE [Assessments] ADD [StatusChangedDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251228214335_AddAssessmentStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251228214335_AddAssessmentStatus', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260105070522_AddCourseStudentTable'
)
BEGIN
    CREATE TABLE [CourseStudent] (
        [CourseStudentId] int NOT NULL IDENTITY,
        [CourseRolesId] int NOT NULL,
        [StudentId] int NOT NULL,
        [EnrolledDate] datetime2 NOT NULL,
        CONSTRAINT [PK_CourseStudent] PRIMARY KEY ([CourseStudentId]),
        CONSTRAINT [FK_CourseStudent_CourseRoles_CourseRolesId] FOREIGN KEY ([CourseRolesId]) REFERENCES [CourseRoles] ([CourseRolesId]) ON DELETE CASCADE,
        CONSTRAINT [FK_CourseStudent_Student_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Student] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260105070522_AddCourseStudentTable'
)
BEGIN
    CREATE INDEX [IX_CourseStudent_CourseRolesId] ON [CourseStudent] ([CourseRolesId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260105070522_AddCourseStudentTable'
)
BEGIN
    CREATE INDEX [IX_CourseStudent_StudentId] ON [CourseStudent] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260105070522_AddCourseStudentTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260105070522_AddCourseStudentTable', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216071009_AddProgrammeYearTrimesterToStudent'
)
BEGIN
    ALTER TABLE [Student] ADD [Programme] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216071009_AddProgrammeYearTrimesterToStudent'
)
BEGIN
    ALTER TABLE [Student] ADD [TrimesterEnrolled] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216071009_AddProgrammeYearTrimesterToStudent'
)
BEGIN
    ALTER TABLE [Student] ADD [YearEnrolled] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216071009_AddProgrammeYearTrimesterToStudent'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260216071009_AddProgrammeYearTrimesterToStudent', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218211422_ReplaceTermName'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Rubrics]') AND [c].[name] = N'TermName');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Rubrics] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Rubrics] DROP COLUMN [TermName];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218211422_ReplaceTermName'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CourseRoles]') AND [c].[name] = N'TermName');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [CourseRoles] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [CourseRoles] DROP COLUMN [TermName];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218211422_ReplaceTermName'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Assessments]') AND [c].[name] = N'TermName');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Assessments] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Assessments] DROP COLUMN [TermName];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218211422_ReplaceTermName'
)
BEGIN
    ALTER TABLE [Rubrics] ADD [Trimester] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218211422_ReplaceTermName'
)
BEGIN
    ALTER TABLE [Rubrics] ADD [Year] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218211422_ReplaceTermName'
)
BEGIN
    ALTER TABLE [CourseRoles] ADD [Trimester] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218211422_ReplaceTermName'
)
BEGIN
    ALTER TABLE [CourseRoles] ADD [Year] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218211422_ReplaceTermName'
)
BEGIN
    ALTER TABLE [Assessments] ADD [Trimester] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218211422_ReplaceTermName'
)
BEGIN
    ALTER TABLE [Assessments] ADD [Year] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218211422_ReplaceTermName'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260218211422_ReplaceTermName', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218214715_AddTotalAssessmentAndStatusToCourseRoles'
)
BEGIN
    ALTER TABLE [CourseRoles] ADD [Status] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218214715_AddTotalAssessmentAndStatusToCourseRoles'
)
BEGIN
    ALTER TABLE [CourseRoles] ADD [TotalAssessment] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260218214715_AddTotalAssessmentAndStatusToCourseRoles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260218214715_AddTotalAssessmentAndStatusToCourseRoles', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260224222449_AddAssessmentProportionalMarks'
)
BEGIN
    ALTER TABLE [Assessments] ADD [ProportionalMarks] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260224222449_AddAssessmentProportionalMarks'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260224222449_AddAssessmentProportionalMarks', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303082346_AddStudentOverallFeedback'
)
BEGIN
    CREATE TABLE [StudentOverallFeedback] (
        [StudentOverallFeedbackId] int NOT NULL IDENTITY,
        [AssessmentId] int NOT NULL,
        [StudentId] int NOT NULL,
        [OverallFeedback] nvarchar(max) NOT NULL,
        [GeneratedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NULL,
        CONSTRAINT [PK_StudentOverallFeedback] PRIMARY KEY ([StudentOverallFeedbackId]),
        CONSTRAINT [FK_StudentOverallFeedback_Assessments_AssessmentId] FOREIGN KEY ([AssessmentId]) REFERENCES [Assessments] ([AssessmentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_StudentOverallFeedback_Student_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Student] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303082346_AddStudentOverallFeedback'
)
BEGIN
    CREATE INDEX [IX_StudentOverallFeedback_AssessmentId] ON [StudentOverallFeedback] ([AssessmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303082346_AddStudentOverallFeedback'
)
BEGIN
    CREATE INDEX [IX_StudentOverallFeedback_StudentId] ON [StudentOverallFeedback] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303082346_AddStudentOverallFeedback'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260303082346_AddStudentOverallFeedback', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306110648_AddStudentScoreTables'
)
BEGIN
    CREATE TABLE [StudentOverallScores] (
        [StudentOverallScoreId] int NOT NULL IDENTITY,
        [AssessmentId] int NOT NULL,
        [StudentId] int NOT NULL,
        [TotalActualScore] float NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_StudentOverallScores] PRIMARY KEY ([StudentOverallScoreId]),
        CONSTRAINT [FK_StudentOverallScores_Assessments_AssessmentId] FOREIGN KEY ([AssessmentId]) REFERENCES [Assessments] ([AssessmentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_StudentOverallScores_Student_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Student] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306110648_AddStudentScoreTables'
)
BEGIN
    CREATE TABLE [StudentTaskScores] (
        [StudentTaskScoreId] int NOT NULL IDENTITY,
        [AssessmentId] int NOT NULL,
        [StudentId] int NOT NULL,
        [RubricTaskId] int NOT NULL,
        [ActualScore] float NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_StudentTaskScores] PRIMARY KEY ([StudentTaskScoreId]),
        CONSTRAINT [FK_StudentTaskScores_Assessments_AssessmentId] FOREIGN KEY ([AssessmentId]) REFERENCES [Assessments] ([AssessmentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_StudentTaskScores_RubricTask_RubricTaskId] FOREIGN KEY ([RubricTaskId]) REFERENCES [RubricTask] ([RubricTaskId]) ON DELETE CASCADE,
        CONSTRAINT [FK_StudentTaskScores_Student_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Student] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306110648_AddStudentScoreTables'
)
BEGIN
    CREATE INDEX [IX_StudentOverallScores_AssessmentId] ON [StudentOverallScores] ([AssessmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306110648_AddStudentScoreTables'
)
BEGIN
    CREATE INDEX [IX_StudentOverallScores_StudentId] ON [StudentOverallScores] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306110648_AddStudentScoreTables'
)
BEGIN
    CREATE INDEX [IX_StudentTaskScores_AssessmentId] ON [StudentTaskScores] ([AssessmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306110648_AddStudentScoreTables'
)
BEGIN
    CREATE INDEX [IX_StudentTaskScores_RubricTaskId] ON [StudentTaskScores] ([RubricTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306110648_AddStudentScoreTables'
)
BEGIN
    CREATE INDEX [IX_StudentTaskScores_StudentId] ON [StudentTaskScores] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306110648_AddStudentScoreTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260306110648_AddStudentScoreTables', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260307085855_AddProportionalFieldsToStudentOverallScore'
)
BEGIN
    ALTER TABLE [StudentOverallScores] ADD [ProportionalFinalScore] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260307085855_AddProportionalFieldsToStudentOverallScore'
)
BEGIN
    ALTER TABLE [StudentOverallScores] ADD [ProportionalMarks] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260307085855_AddProportionalFieldsToStudentOverallScore'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260307085855_AddProportionalFieldsToStudentOverallScore', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327184214_AddCoursesTable'
)
BEGIN
    CREATE TABLE [Courses] (
        [Id] int NOT NULL IDENTITY,
        [CourseCode] nvarchar(20) NOT NULL,
        [CourseName] nvarchar(max) NOT NULL,
        [Programme] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Courses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327184214_AddCoursesTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260327184214_AddCoursesTable', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260328142658_AddProgrammeTable'
)
BEGIN
    CREATE TABLE [Programmes] (
        [Id] int NOT NULL IDENTITY,
        [ProgrammeName] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_Programmes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260328142658_AddProgrammeTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260328142658_AddProgrammeTable', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331231001_AddAuditLog'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [AuditLogId] int NOT NULL IDENTITY,
        [TableName] nvarchar(max) NOT NULL,
        [Action] nvarchar(max) NOT NULL,
        [KeyValues] nvarchar(max) NOT NULL,
        [OldValues] nvarchar(max) NOT NULL,
        [NewValues] nvarchar(max) NOT NULL,
        [ChangedBy] nvarchar(max) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331231001_AddAuditLog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260331231001_AddAuditLog', N'8.0.15');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331231829_update-auditlogs'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLogs]') AND [c].[name] = N'OldValues');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [AuditLogs] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [AuditLogs] ALTER COLUMN [OldValues] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331231829_update-auditlogs'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLogs]') AND [c].[name] = N'NewValues');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [AuditLogs] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [AuditLogs] ALTER COLUMN [NewValues] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331231829_update-auditlogs'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260331231829_update-auditlogs', N'8.0.15');
END;
GO

COMMIT;
GO

