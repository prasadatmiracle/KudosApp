/*
  Starter SQL schema for production migration (SQL Server).
  The running API currently uses in-memory persistence for fast bootstrap.
*/

CREATE TABLE Teams (
    TeamId INT IDENTITY PRIMARY KEY,
    TeamName NVARCHAR(200) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Users (
    UserId INT IDENTITY PRIMARY KEY,
    EmployeeId NVARCHAR(50) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Email NVARCHAR(256) NOT NULL UNIQUE,
    Role NVARCHAR(50) NOT NULL,
    TeamId INT NOT NULL,
    ManagerId INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Users_Team FOREIGN KEY (TeamId) REFERENCES Teams(TeamId),
    CONSTRAINT FK_Users_Manager FOREIGN KEY (ManagerId) REFERENCES Users(UserId)
);

CREATE TABLE Projects (
    ProjectId INT IDENTITY PRIMARY KEY,
    ProjectCode NVARCHAR(50) NOT NULL,
    ProjectName NVARCHAR(200) NOT NULL,
    ClientName NVARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE ResourceAllocations (
    ResourceAllocationId INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    ProjectId INT NOT NULL,
    BillingType NVARCHAR(50) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_ResourceAllocations_User FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_ResourceAllocations_Project FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId)
);

CREATE TABLE DailyUpdates (
    DailyUpdateId INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    ProjectId INT NOT NULL,
    WorkDate DATE NOT NULL,
    TicketNumber NVARCHAR(100) NOT NULL,
    Description NVARCHAR(2000) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_DailyUpdates_User FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_DailyUpdates_Project FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId)
);

CREATE TABLE Tasks (
    TaskId INT IDENTITY PRIMARY KEY,
    Title NVARCHAR(250) NOT NULL,
    Description NVARCHAR(2000) NOT NULL,
    TaskType NVARCHAR(50) NOT NULL,
    State NVARCHAR(50) NOT NULL,
    CreatedByUserId INT NOT NULL,
    ProjectId INT NULL,
    DueAtUtc DATETIME2 NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Tasks_Creator FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Tasks_Project FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId)
);

CREATE TABLE TaskResponses (
    TaskResponseId INT IDENTITY PRIMARY KEY,
    TaskId INT NOT NULL,
    UserId INT NOT NULL,
    [Option] NVARCHAR(100) NOT NULL,
    Remark NVARCHAR(1000) NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_TaskResponses_Task FOREIGN KEY (TaskId) REFERENCES Tasks(TaskId),
    CONSTRAINT FK_TaskResponses_User FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE Achievements (
    AchievementId INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    Title NVARCHAR(250) NOT NULL,
    Description NVARCHAR(2000) NOT NULL,
    ProofWorkDriveUrl NVARCHAR(1000) NULL,
    ValidationStatus NVARCHAR(50) NOT NULL,
    ValidatedByUserId INT NULL,
    ValidatedAtUtc DATETIME2 NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Achievements_User FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE SalesEnquiries (
    SalesEnquiryId INT IDENTITY PRIMARY KEY,
    ClientName NVARCHAR(250) NOT NULL,
    Requirement NVARCHAR(2000) NOT NULL,
    Technology NVARCHAR(250) NOT NULL,
    EnquiryDate DATE NOT NULL,
    SalesCoordinator NVARCHAR(200) NOT NULL,
    Status NVARCHAR(100) NOT NULL,
    CreatedByUserId INT NOT NULL,
    ValidationStatus NVARCHAR(50) NOT NULL,
    ValidatedByUserId INT NULL,
    ValidatedAtUtc DATETIME2 NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SalesEnquiries_User FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
);

CREATE TABLE Validations (
    ValidationRecordId INT IDENTITY PRIMARY KEY,
    EntityType NVARCHAR(50) NOT NULL,
    EntityId INT NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    ValidatedByUserId INT NULL,
    Remarks NVARCHAR(1000) NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2 NULL
);

CREATE TABLE Reports (
    ReportRecordId INT IDENTITY PRIMARY KEY,
    ReportType NVARCHAR(50) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    PayloadJson NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    GeneratedByUserId INT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Reports_User FOREIGN KEY (GeneratedByUserId) REFERENCES Users(UserId)
);

CREATE TABLE AuditEntries (
    AuditEntryId INT IDENTITY PRIMARY KEY,
    ActorUserId INT NOT NULL,
    Action NVARCHAR(150) NOT NULL,
    EntityType NVARCHAR(100) NOT NULL,
    EntityId INT NOT NULL,
    MetadataJson NVARCHAR(MAX) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AuditEntries_User FOREIGN KEY (ActorUserId) REFERENCES Users(UserId)
);

CREATE TABLE Engagements (
    EngagementId INT IDENTITY PRIMARY KEY,
    ClientName NVARCHAR(250) NOT NULL,
    ProjectName NVARCHAR(250) NOT NULL,
    NumberOfPositions INT NOT NULL,
    Details NVARCHAR(2000) NOT NULL,
    CreatedByUserId INT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Engagements_User FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
);

CREATE TABLE SalesSessions (
    SalesSessionId INT IDENTITY PRIMARY KEY,
    Title NVARCHAR(250) NOT NULL,
    SessionDate DATE NOT NULL,
    TeamId INT NOT NULL,
    Description NVARCHAR(2000) NOT NULL,
    CreatedByUserId INT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SalesSessions_Team FOREIGN KEY (TeamId) REFERENCES Teams(TeamId),
    CONSTRAINT FK_SalesSessions_User FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
);

CREATE TABLE Events (
    EventId INT IDENTITY PRIMARY KEY,
    Title NVARCHAR(250) NOT NULL,
    Description NVARCHAR(2000) NOT NULL,
    EventDate DATE NOT NULL,
    Location NVARCHAR(250) NOT NULL,
    CreatedByUserId INT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Events_User FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
);

CREATE TABLE EventMedia (
    EventMediaId INT IDENTITY PRIMARY KEY,
    EventId INT NOT NULL,
    WorkDriveFileUrl NVARCHAR(1000) NOT NULL,
    UploadedByUserId INT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_EventMedia_Event FOREIGN KEY (EventId) REFERENCES Events(EventId),
    CONSTRAINT FK_EventMedia_User FOREIGN KEY (UploadedByUserId) REFERENCES Users(UserId)
);

CREATE TABLE Meetings (
    MeetingId INT IDENTITY PRIMARY KEY,
    Title NVARCHAR(250) NOT NULL,
    MeetingAtUtc DATETIME2 NOT NULL,
    ZohoMeetingUrl NVARCHAR(1000) NOT NULL,
    TranscriptUrl NVARCHAR(1000) NULL,
    CreatedByUserId INT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Meetings_User FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
);

CREATE TABLE MomEntries (
    MomEntryId INT IDENTITY PRIMARY KEY,
    MeetingId INT NOT NULL,
    Summary NVARCHAR(MAX) NOT NULL,
    ActionItems NVARCHAR(MAX) NOT NULL,
    CreatedByUserId INT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_MomEntries_Meeting FOREIGN KEY (MeetingId) REFERENCES Meetings(MeetingId),
    CONSTRAINT FK_MomEntries_User FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
);

CREATE TABLE PointsLogs (
    PointsLogId INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    Points INT NOT NULL,
    ActivityType NVARCHAR(100) NOT NULL,
    ReferenceId INT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_PointsLogs_User FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE Badges (
    BadgeId INT IDENTITY PRIMARY KEY,
    BadgeName NVARCHAR(200) NOT NULL,
    Criteria NVARCHAR(500) NOT NULL
);

CREATE TABLE UserBadges (
    UserBadgeId INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    BadgeId INT NOT NULL,
    AwardedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_UserBadges_User FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_UserBadges_Badge FOREIGN KEY (BadgeId) REFERENCES Badges(BadgeId)
);

CREATE TABLE ReminderDispatches (
    ReminderDispatchId INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    DispatchDate DATE NOT NULL,
    Count INT NOT NULL DEFAULT 0,
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ReminderDispatches_User FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE ActionItems (
    ActionItemId         INT IDENTITY PRIMARY KEY,
    Title                NVARCHAR(300)  NOT NULL,
    Description          NVARCHAR(2000) NOT NULL DEFAULT '',
    AssignedToUserId     INT            NOT NULL,
    CreatedByUserId      INT            NOT NULL,
    DueDate              DATE           NOT NULL,
    Priority             NVARCHAR(20)   NOT NULL DEFAULT 'Medium',
    Status               NVARCHAR(30)   NOT NULL DEFAULT 'Open',
    SourceType           NVARCHAR(20)   NOT NULL DEFAULT 'Manual',
    SourceId             INT            NULL,
    FirstReminderSentDate DATE          NULL,
    EscalationSentDate    DATE          NULL,
    CreatedAtUtc         DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc         DATETIME2      NULL,
    CompletedAtUtc       DATETIME2      NULL,
    CONSTRAINT FK_ActionItems_Assignee FOREIGN KEY (AssignedToUserId) REFERENCES Users(UserId),
    CONSTRAINT FK_ActionItems_Creator  FOREIGN KEY (CreatedByUserId)  REFERENCES Users(UserId)
);

CREATE INDEX IX_DailyUpdates_WorkDate  ON DailyUpdates(WorkDate);
CREATE INDEX IX_TaskResponses_TaskId   ON TaskResponses(TaskId);
CREATE INDEX IX_Validations_Status     ON Validations(Status);
CREATE INDEX IX_Reports_TypeWindow     ON Reports(ReportType, StartDate, EndDate);
CREATE INDEX IX_PointsLogs_UserId      ON PointsLogs(UserId);
CREATE INDEX IX_EventMedia_EventId     ON EventMedia(EventId);
CREATE INDEX IX_MomEntries_MeetingId   ON MomEntries(MeetingId);
CREATE INDEX IX_ActionItems_AssignedTo ON ActionItems(AssignedToUserId);
CREATE INDEX IX_ActionItems_Status     ON ActionItems(Status);
