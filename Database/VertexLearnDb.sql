/* ============================================================================
   VertexLearn — Web-Based Learning System
   Database : Microsoft SQL Server (LocalDB / Express / Developer)
   Module   : CT050-3-2-WAPP

   This script is IDEMPOTENT: it only creates what does not exist yet, so it
   can be run many times. The web app runs it automatically on start-up
   (see Data/DbInitializer.cs). You can also run it yourself in SSMS or in
   Visual Studio's SQL Server Object Explorer.

   Seeded logins (for the team / lecturer only — never shown on the website):
     Admin   : admin@vertexlearn.com / Admin@123
     Student : jasmi@student.com     / Student@123
     Student : rohan@student.com     / Student@123
   ============================================================================ */

IF DB_ID(N'VertexLearnDb') IS NULL
    CREATE DATABASE VertexLearnDb;
GO

USE VertexLearnDb;
GO

/* ----------------------------------------------------------------------------
   1. TABLES
   ---------------------------------------------------------------------------- */

-- Registered members and administrators
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users (
    UserId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    FullName      NVARCHAR(100)     NOT NULL,
    Email         NVARCHAR(150)     NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
    PasswordHash  NVARCHAR(255)     NOT NULL,          -- salted PBKDF2 hash, never plain text
    Role          NVARCHAR(20)      NOT NULL CONSTRAINT DF_Users_Role DEFAULT N'Student'
                                    CONSTRAINT CK_Users_Role CHECK (Role IN (N'Student', N'Admin')),
    Bio           NVARCHAR(500)     NULL,
    CreatedAt     DATETIME2         NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME()
);
GO

-- Course categories (e.g. Web Development, Databases)
IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
CREATE TABLE dbo.Categories (
    CategoryId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name          NVARCHAR(60)      NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE,
    Description   NVARCHAR(250)     NULL
);
GO

-- Courses (a category cannot be deleted while it still has courses)
IF OBJECT_ID(N'dbo.Courses', N'U') IS NULL
CREATE TABLE dbo.Courses (
    CourseId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Courses PRIMARY KEY,
    CategoryId    INT               NOT NULL CONSTRAINT FK_Courses_Categories
                                    REFERENCES dbo.Categories(CategoryId),
    Title         NVARCHAR(150)     NOT NULL,
    Description   NVARCHAR(1000)    NOT NULL,
    Instructor    NVARCHAR(100)     NOT NULL,
    LessonCount   INT               NOT NULL CONSTRAINT CK_Courses_Lessons CHECK (LessonCount BETWEEN 1 AND 200),
    Level         NVARCHAR(20)      NOT NULL CONSTRAINT DF_Courses_Level DEFAULT N'Beginner'
                                    CONSTRAINT CK_Courses_Level CHECK (Level IN (N'Beginner', N'Intermediate', N'Advanced')),
    ImageUrl      NVARCHAR(300)     NULL,
    CreatedAt     DATETIME2         NOT NULL CONSTRAINT DF_Courses_CreatedAt DEFAULT SYSDATETIME()
);
GO

-- Learning materials (multimedia) attached to a course
IF OBJECT_ID(N'dbo.Resources', N'U') IS NULL
CREATE TABLE dbo.Resources (
    ResourceId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Resources PRIMARY KEY,
    CourseId      INT               NOT NULL CONSTRAINT FK_Resources_Courses
                                    REFERENCES dbo.Courses(CourseId) ON DELETE CASCADE,
    Title         NVARCHAR(150)     NOT NULL,
    Type          NVARCHAR(20)      NOT NULL
                                    CONSTRAINT CK_Resources_Type CHECK (Type IN (N'Video', N'PDF', N'Link', N'Notes', N'Image', N'Audio')),
    Url           NVARCHAR(500)     NOT NULL,
    Description   NVARCHAR(500)     NULL,
    SortOrder     INT               NOT NULL CONSTRAINT DF_Resources_Sort DEFAULT 0
);
GO

-- Upgrade for databases created by an older version of this script:
-- resource type 'Article' became 'Link', and 'Notes' was added.
IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE name = N'CK_Resources_Type' AND definition NOT LIKE N'%Notes%')
BEGIN
    ALTER TABLE dbo.Resources DROP CONSTRAINT CK_Resources_Type;
    UPDATE dbo.Resources SET Type = N'Link' WHERE Type = N'Article';
    ALTER TABLE dbo.Resources ADD CONSTRAINT CK_Resources_Type
        CHECK (Type IN (N'Video', N'PDF', N'Link', N'Notes', N'Image', N'Audio'));
END
GO

-- Which student is enrolled in which course (many-to-many bridge table)
IF OBJECT_ID(N'dbo.Enrollments', N'U') IS NULL
CREATE TABLE dbo.Enrollments (
    EnrollmentId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Enrollments PRIMARY KEY,
    UserId        INT               NOT NULL CONSTRAINT FK_Enrollments_Users
                                    REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
    CourseId      INT               NOT NULL CONSTRAINT FK_Enrollments_Courses
                                    REFERENCES dbo.Courses(CourseId) ON DELETE CASCADE,
    EnrolledAt    DATETIME2         NOT NULL CONSTRAINT DF_Enrollments_At DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_Enrollments_UserCourse UNIQUE (UserId, CourseId)
);
GO

-- Self-assessment quizzes
IF OBJECT_ID(N'dbo.Quizzes', N'U') IS NULL
CREATE TABLE dbo.Quizzes (
    QuizId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Quizzes PRIMARY KEY,
    CourseId      INT               NOT NULL CONSTRAINT FK_Quizzes_Courses
                                    REFERENCES dbo.Courses(CourseId) ON DELETE CASCADE,
    Title         NVARCHAR(150)     NOT NULL,
    Description   NVARCHAR(500)     NULL,
    PassPercent   INT               NOT NULL CONSTRAINT DF_Quizzes_Pass DEFAULT 50
                                    CONSTRAINT CK_Quizzes_Pass CHECK (PassPercent BETWEEN 1 AND 100)
);
GO

-- Multiple-choice questions (A–D)
IF OBJECT_ID(N'dbo.Questions', N'U') IS NULL
CREATE TABLE dbo.Questions (
    QuestionId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Questions PRIMARY KEY,
    QuizId        INT               NOT NULL CONSTRAINT FK_Questions_Quizzes
                                    REFERENCES dbo.Quizzes(QuizId) ON DELETE CASCADE,
    QuestionText  NVARCHAR(500)     NOT NULL,
    OptionA       NVARCHAR(200)     NOT NULL,
    OptionB       NVARCHAR(200)     NOT NULL,
    OptionC       NVARCHAR(200)     NOT NULL,
    OptionD       NVARCHAR(200)     NOT NULL,
    CorrectOption CHAR(1)           NOT NULL CONSTRAINT CK_Questions_Correct CHECK (CorrectOption IN ('A','B','C','D'))
);
GO

-- One row per quiz submission
IF OBJECT_ID(N'dbo.QuizAttempts', N'U') IS NULL
CREATE TABLE dbo.QuizAttempts (
    AttemptId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_QuizAttempts PRIMARY KEY,
    UserId         INT               NOT NULL CONSTRAINT FK_QuizAttempts_Users
                                     REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
    QuizId         INT               NOT NULL CONSTRAINT FK_QuizAttempts_Quizzes
                                     REFERENCES dbo.Quizzes(QuizId) ON DELETE CASCADE,
    Score          INT               NOT NULL,
    TotalQuestions INT               NOT NULL,
    AttemptedAt    DATETIME2         NOT NULL CONSTRAINT DF_QuizAttempts_At DEFAULT SYSDATETIME(),
    CONSTRAINT CK_QuizAttempts_Score CHECK (Score >= 0 AND Score <= TotalQuestions)
);
GO

-- The answer chosen for each question in an attempt (used for the result breakdown).
-- QuestionId uses NO ACTION to avoid multiple cascade paths; the app deletes these rows first.
IF OBJECT_ID(N'dbo.AttemptAnswers', N'U') IS NULL
CREATE TABLE dbo.AttemptAnswers (
    AttemptAnswerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AttemptAnswers PRIMARY KEY,
    AttemptId       INT               NOT NULL CONSTRAINT FK_AttemptAnswers_Attempts
                                      REFERENCES dbo.QuizAttempts(AttemptId) ON DELETE CASCADE,
    QuestionId      INT               NOT NULL CONSTRAINT FK_AttemptAnswers_Questions
                                      REFERENCES dbo.Questions(QuestionId),
    SelectedOption  CHAR(1)           NULL,
    IsCorrect       BIT               NOT NULL
);
GO

-- Course discussion board (asynchronous online discussion)
IF OBJECT_ID(N'dbo.DiscussionPosts', N'U') IS NULL
CREATE TABLE dbo.DiscussionPosts (
    PostId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DiscussionPosts PRIMARY KEY,
    CourseId      INT               NOT NULL CONSTRAINT FK_Posts_Courses
                                    REFERENCES dbo.Courses(CourseId) ON DELETE CASCADE,
    UserId        INT               NOT NULL CONSTRAINT FK_Posts_Users
                                    REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
    Title         NVARCHAR(150)     NOT NULL,
    Body          NVARCHAR(4000)    NOT NULL,
    CreatedAt     DATETIME2         NOT NULL CONSTRAINT DF_Posts_CreatedAt DEFAULT SYSDATETIME(),
    UpdatedAt     DATETIME2         NULL
);
GO

-- Replies to a discussion post (UserId uses NO ACTION; the app deletes a user's replies first)
IF OBJECT_ID(N'dbo.DiscussionReplies', N'U') IS NULL
CREATE TABLE dbo.DiscussionReplies (
    ReplyId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DiscussionReplies PRIMARY KEY,
    PostId        INT               NOT NULL CONSTRAINT FK_Replies_Posts
                                    REFERENCES dbo.DiscussionPosts(PostId) ON DELETE CASCADE,
    UserId        INT               NOT NULL CONSTRAINT FK_Replies_Users
                                    REFERENCES dbo.Users(UserId),
    Body          NVARCHAR(2000)    NOT NULL,
    CreatedAt     DATETIME2         NOT NULL CONSTRAINT DF_Replies_CreatedAt DEFAULT SYSDATETIME()
);
GO

-- Messages sent through the public Contact form
IF OBJECT_ID(N'dbo.ContactMessages', N'U') IS NULL
CREATE TABLE dbo.ContactMessages (
    MessageId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContactMessages PRIMARY KEY,
    Name          NVARCHAR(100)     NOT NULL,
    Email         NVARCHAR(150)     NOT NULL,
    Subject       NVARCHAR(150)     NOT NULL,
    Message       NVARCHAR(2000)    NOT NULL,
    IsRead        BIT               NOT NULL CONSTRAINT DF_Contact_IsRead DEFAULT 0,
    CreatedAt     DATETIME2         NOT NULL CONSTRAINT DF_Contact_CreatedAt DEFAULT SYSDATETIME()
);
GO

/* ----------------------------------------------------------------------------
   2. SAMPLE DATA (only inserted into empty tables)
   ---------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
    SET IDENTITY_INSERT dbo.Users ON;
    INSERT INTO dbo.Users (UserId, FullName, Email, PasswordHash, Role, Bio) VALUES
    (1, N'System Admin',   N'admin@vertexlearn.com', N'AQAAAAIAAYagAAAAEBvh/x+0uSGlSNEXbAlo6pEjQ6InMx3G935A7kqGeuW7iGZQfGEeqBfVZ7QF3zbp5A==', N'Admin',   N'Maintains VertexLearn course content and accounts.'),
    (2, N'Jasmi Dhami',    N'jasmi@student.com',     N'AQAAAAIAAYagAAAAEG/5JNdQFyXpaM0YApwNCFAE820JOVA1fiM5SrPqCRwSA4Ntw9BKhUAhBoVNY8S98g==', N'Student', N'Second-year computing student interested in web development.'),
    (3, N'Rohan Shrestha', N'rohan@student.com',     N'AQAAAAIAAYagAAAAEG/5JNdQFyXpaM0YApwNCFAE820JOVA1fiM5SrPqCRwSA4Ntw9BKhUAhBoVNY8S98g==', N'Student', NULL);
    SET IDENTITY_INSERT dbo.Users OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
BEGIN
    SET IDENTITY_INSERT dbo.Categories ON;
    INSERT INTO dbo.Categories (CategoryId, Name, Description) VALUES
    (1, N'Web Development', N'Building websites and web applications for the browser and the server.'),
    (2, N'Databases',       N'Designing, querying and managing relational data.'),
    (3, N'Programming',     N'Core programming concepts and languages.'),
    (4, N'Design',          N'Interface design, usability and accessibility.'),
    (5, N'Learning Skills', N'Study techniques for effective online learning.');
    SET IDENTITY_INSERT dbo.Categories OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Courses)
BEGIN
    SET IDENTITY_INSERT dbo.Courses ON;
    INSERT INTO dbo.Courses (CourseId, CategoryId, Title, Description, Instructor, LessonCount, Level, ImageUrl) VALUES
    (1, 1, N'Web Development Fundamentals',
        N'Build interlinked, standards-compliant web pages with HTML5, CSS3 and JavaScript. You will learn semantic page structure, the CSS box model, responsive layouts with Flexbox and Grid, and how to validate forms in the browser.',
        N'Dr. Amina Rai', 24, N'Beginner', N'/images/courses/web-fundamentals.svg'),
    (2, 2, N'Database Systems',
        N'Design relational schemas and write SQL queries that support real applications. Covers entity-relationship modelling, normalisation, keys and constraints, joins, and safe parameterised queries from application code.',
        N'Mr. Sanjay Thapa', 18, N'Intermediate', N'/images/courses/database-systems.svg'),
    (3, 3, N'JavaScript Essentials',
        N'From variables to async/await: practical scripting for interactive pages. Work with the DOM, handle events, validate user input and fetch data without reloading the page.',
        N'Ms. Laxmi Gurung', 20, N'Beginner', N'/images/courses/javascript-essentials.svg'),
    (4, 4, N'UI/UX Design Principles',
        N'Usable, accessible interface design grounded in real evaluation methods. Learn wireframing, visual hierarchy, colour contrast, Nielsen''s usability heuristics and the WCAG accessibility guidelines.',
        N'Dr. Amina Rai', 15, N'Beginner', N'/images/courses/uiux-design.svg'),
    (5, 1, N'Server-Side Scripting with ASP.NET Core',
        N'Handle requests, sessions and data on the server with a modern MVC framework. Covers routing, controllers and views, model binding and validation, cookie authentication and role-based authorisation.',
        N'Mr. Bikash Karki', 22, N'Intermediate', N'/images/courses/server-side.svg'),
    (6, 5, N'Introduction to Discussion-Based Learning',
        N'Collaborative online learning through forums, peer review and asynchronous discussion. Learn how to ask good questions, give useful feedback and build understanding with other learners.',
        N'Ms. Laxmi Gurung', 10, N'Beginner', N'/images/courses/discussion-learning.svg');
    SET IDENTITY_INSERT dbo.Courses OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Resources)
BEGIN
    INSERT INTO dbo.Resources (CourseId, Title, Type, Url, Description, SortOrder) VALUES
    (1, N'HTML full course for beginners', N'Video', N'https://www.youtube.com/watch?v=pQN-pnXPaVg', N'Video walkthrough of HTML elements, attributes and page structure.', 1),
    (1, N'HTML5 & CSS3 quick reference',   N'PDF',   N'/files/html5-css3-quick-reference.pdf', N'Two-page handout of the semantic elements and CSS selectors used in this course.', 2),
    (1, N'The CSS box model',              N'Image', N'/images/resources/box-model.svg', N'Diagram of content, padding, border and margin.', 3),
    (1, N'MDN: HTML basics',               N'Link',    N'https://developer.mozilla.org/en-US/docs/Learn/Getting_started_with_the_web/HTML_basics', N'Mozilla''s beginner guide to writing HTML.', 4),
    (2, N'SQL tutorial: full database course', N'Video', N'https://www.youtube.com/watch?v=HXV3zeQKqGY', N'Video course covering tables, keys, SELECT, JOIN and more.', 1),
    (2, N'SQL quick reference',            N'PDF',   N'/files/sql-quick-reference.pdf', N'The core SELECT, INSERT, UPDATE and DELETE statements on one page.', 2),
    (2, N'Example ERD: learning system',   N'Image', N'/images/resources/erd-example.svg', N'A simplified entity relationship diagram for a learning platform.', 3),
    (2, N'Microsoft Learn: SELECT (Transact-SQL)', N'Link',    N'https://learn.microsoft.com/en-us/sql/t-sql/queries/select-transact-sql', N'Official reference for the SELECT statement in SQL Server.', 4),
    (3, N'Learn JavaScript: full course',  N'Video', N'https://www.youtube.com/watch?v=PkZNo7MFNFg', N'Video introduction to JavaScript fundamentals.', 1),
    (3, N'MDN JavaScript Guide',           N'Link',    N'https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide', N'Mozilla''s structured guide to the JavaScript language.', 2),
    (4, N'10 usability heuristics',        N'Link',    N'https://www.nngroup.com/articles/ten-usability-heuristics/', N'Nielsen Norman Group''s classic principles for interaction design.', 1),
    (4, N'WCAG overview',                  N'Link',    N'https://www.w3.org/WAI/standards-guidelines/wcag/', N'W3C introduction to the Web Content Accessibility Guidelines.', 2),
    (5, N'Request flow in ASP.NET Core MVC', N'Image', N'/images/resources/mvc-flow.svg', N'How a browser request travels through routing, controller, model and view.', 1),
    (5, N'Microsoft Learn: ASP.NET Core MVC overview', N'Link',    N'https://learn.microsoft.com/en-us/aspnet/core/mvc/overview', N'Official introduction to the MVC pattern in ASP.NET Core.', 2),
    (6, N'Asynchronous learning',          N'Link',    N'https://en.wikipedia.org/wiki/Asynchronous_learning', N'Background reading on learning that does not happen at the same time for everyone.', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Quizzes)
BEGIN
    SET IDENTITY_INSERT dbo.Quizzes ON;
    INSERT INTO dbo.Quizzes (QuizId, CourseId, Title, Description, PassPercent) VALUES
    (1, 1, N'HTML5 & CSS basics',           N'Check your understanding of semantic HTML5 and how CSS is applied.', 60),
    (2, 2, N'SQL fundamentals',             N'SELECT, WHERE, keys and joins.', 60),
    (3, 3, N'JavaScript core concepts',     N'Variables, types, arrays and events.', 60),
    (4, 4, N'Usability and accessibility',  N'Heuristics, wireframes and accessible design.', 50),
    (5, 5, N'MVC and web security basics',  N'Controllers, HTTP methods and safe data handling.', 60),
    (6, 6, N'Learning through discussion',  N'How to take part in online discussion effectively.', 50);
    SET IDENTITY_INSERT dbo.Quizzes OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Questions)
BEGIN
    SET IDENTITY_INSERT dbo.Questions ON;
    INSERT INTO dbo.Questions (QuestionId, QuizId, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectOption) VALUES
    -- Quiz 1: HTML5 & CSS
    (1, 1, N'What does HTML stand for?', N'HyperText Markup Language', N'High Text Machine Language', N'Hyperlink and Text Management Language', N'Home Tool Markup Language', 'A'),
    (2, 1, N'Which HTML5 element represents self-contained content, such as a blog post?', N'<section>', N'<article>', N'<div>', N'<aside>', 'B'),
    (3, 1, N'Which CSS property changes the colour of text?', N'font-color', N'text-color', N'color', N'foreground', 'C'),
    (4, 1, N'Which line correctly links an external stylesheet?', N'<style src="site.css">', N'<link rel="stylesheet" href="site.css">', N'<css href="site.css">', N'<script href="site.css">', 'B'),
    (5, 1, N'Which type of CSS has the highest priority when the same property is set in several places?', N'External stylesheet', N'Internal <style> block', N'Inline style attribute', N'Browser default', 'C'),
    -- Quiz 2: SQL
    (6, 2, N'Which SQL statement retrieves data from a table?', N'SELECT', N'GET', N'OPEN', N'FETCH ALL', 'A'),
    (7, 2, N'Which clause filters individual rows in a query?', N'ORDER BY', N'WHERE', N'GROUP BY', N'DISTINCT', 'B'),
    (8, 2, N'A primary key column must be…', N'Unique and not null', N'Always a text column', N'Allowed to repeat', N'Optional', 'A'),
    (9, 2, N'Which join returns only rows that have a match in both tables?', N'LEFT JOIN', N'FULL OUTER JOIN', N'INNER JOIN', N'CROSS JOIN', 'C'),
    (10, 2, N'Which statement removes rows from a table but keeps the table?', N'DROP TABLE', N'DELETE', N'REMOVE', N'ALTER TABLE', 'B'),
    -- Quiz 3: JavaScript
    (11, 3, N'Which keyword declares a block-scoped variable that cannot be reassigned?', N'var', N'let', N'const', N'static', 'C'),
    (12, 3, N'What does typeof null return in JavaScript?', N'"null"', N'"object"', N'"undefined"', N'"number"', 'B'),
    (13, 3, N'Which array method adds an item to the end of an array?', N'push()', N'pop()', N'shift()', N'unshift()', 'A'),
    (14, 3, N'What does the === operator compare?', N'Value only', N'Type only', N'Memory address only', N'Value and type', 'D'),
    (15, 3, N'Which event fires when a form is sent?', N'click', N'submit', N'change', N'load', 'B'),
    -- Quiz 4: UI/UX
    (16, 4, N'What is the WCAG AA minimum contrast ratio for normal-size text?', N'3:1', N'4.5:1', N'7:1', N'2:1', 'B'),
    (17, 4, N'What does a wireframe mainly show?', N'Page layout and structure', N'Final colours and photos', N'Server code', N'Database tables', 'A'),
    (18, 4, N'The heuristic "visibility of system status" means the system should…', N'Hide loading states', N'Use as few colours as possible', N'Keep users informed through timely feedback', N'Show every option at once', 'C'),
    (19, 4, N'Alternative (alt) text on images mainly helps…', N'Pages load faster', N'Images look sharper', N'Search engines rank ads', N'People using screen readers', 'D'),
    -- Quiz 5: MVC & security
    (20, 5, N'In MVC, which part receives the request and decides which view to return?', N'Model', N'Controller', N'View', N'Layout', 'B'),
    (21, 5, N'Which HTTP method should a form use when it changes data on the server?', N'GET', N'HEAD', N'POST', N'OPTIONS', 'C'),
    (22, 5, N'Parameterised SQL queries mainly protect against…', N'SQL injection', N'Slow networks', N'Broken CSS', N'Expired cookies', 'A'),
    (23, 5, N'How should user passwords be stored?', N'As plain text', N'Encrypted with a shared key in the page', N'In a browser cookie', N'As a salted hash', 'D'),
    -- Quiz 6: discussion-based learning
    (24, 6, N'What makes a discussion "asynchronous"?', N'Participants post at different times', N'Everyone must be online together', N'It only uses video', N'Only the teacher can post', 'A'),
    (25, 6, N'A helpful forum question should include…', N'Only the word "help"', N'Your password', N'What you tried and the exact problem', N'As little detail as possible', 'C'),
    (26, 6, N'Peer review helps learners mainly by…', N'Replacing the teacher', N'Learning from evaluating others'' work', N'Avoiding feedback', N'Making courses shorter', 'B');
    SET IDENTITY_INSERT dbo.Questions OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Enrollments)
BEGIN
    INSERT INTO dbo.Enrollments (UserId, CourseId, EnrolledAt) VALUES
    (2, 1, DATEADD(DAY, -20, SYSDATETIME())),
    (2, 2, DATEADD(DAY, -15, SYSDATETIME())),
    (2, 3, DATEADD(DAY, -5,  SYSDATETIME())),
    (3, 1, DATEADD(DAY, -10, SYSDATETIME())),
    (3, 4, DATEADD(DAY, -3,  SYSDATETIME()));
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.QuizAttempts)
BEGIN
    SET IDENTITY_INSERT dbo.QuizAttempts ON;
    INSERT INTO dbo.QuizAttempts (AttemptId, UserId, QuizId, Score, TotalQuestions, AttemptedAt) VALUES
    (1, 2, 1, 4, 5, DATEADD(DAY, -12, SYSDATETIME())),
    (2, 2, 2, 3, 5, DATEADD(DAY, -6,  SYSDATETIME())),
    (3, 3, 1, 2, 5, DATEADD(DAY, -2,  SYSDATETIME()));
    SET IDENTITY_INSERT dbo.QuizAttempts OFF;

    INSERT INTO dbo.AttemptAnswers (AttemptId, QuestionId, SelectedOption, IsCorrect) VALUES
    (1, 1, 'A', 1), (1, 2, 'B', 1), (1, 3, 'C', 1), (1, 4, 'B', 1), (1, 5, 'A', 0),
    (2, 6, 'A', 1), (2, 7, 'B', 1), (2, 8, 'A', 1), (2, 9, 'A', 0), (2, 10, 'A', 0),
    (3, 1, 'A', 1), (3, 2, 'C', 0), (3, 3, 'C', 1), (3, 4, 'A', 0), (3, 5, 'B', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DiscussionPosts)
BEGIN
    SET IDENTITY_INSERT dbo.DiscussionPosts ON;
    INSERT INTO dbo.DiscussionPosts (PostId, CourseId, UserId, Title, Body, CreatedAt) VALUES
    (1, 1, 2, N'When should I use <section> instead of <div>?',
        N'I understand <div> is a generic container, but I''m not sure when a block of content deserves a <section>. Is there a simple rule?',
        DATEADD(DAY, -9, SYSDATETIME())),
    (2, 1, 3, N'My footer overlaps the content on mobile',
        N'On small screens my footer sits on top of the last section. I''m using position: absolute; bottom: 0 on the footer. What am I doing wrong?',
        DATEADD(DAY, -4, SYSDATETIME())),
    (3, 2, 2, N'Why does my LEFT JOIN return NULLs?',
        N'I joined Courses to Enrollments with a LEFT JOIN and some rows have NULL in the enrollment columns. Is this a bug in my query?',
        DATEADD(DAY, -3, SYSDATETIME()));
    SET IDENTITY_INSERT dbo.DiscussionPosts OFF;

    INSERT INTO dbo.DiscussionReplies (PostId, UserId, Body, CreatedAt) VALUES
    (1, 1, N'A good rule: use <section> when the block would make sense with its own heading in the page outline. If it''s only there for styling, a <div> is fine.', DATEADD(DAY, -8, SYSDATETIME())),
    (1, 3, N'That helped, thanks. I was wrapping every card in a <section>.', DATEADD(DAY, -7, SYSDATETIME())),
    (2, 2, N'Absolute positioning takes the footer out of the normal flow. Remove it and let the footer follow the content, or use a flex column layout on body with main set to flex: 1.', DATEADD(DAY, -4, SYSDATETIME())),
    (3, 1, N'Not a bug. A LEFT JOIN keeps every course even when it has no enrolments; the columns from the right table are NULL for those rows. Use INNER JOIN if you only want matches.', DATEADD(DAY, -2, SYSDATETIME()));
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ContactMessages)
BEGIN
    INSERT INTO dbo.ContactMessages (Name, Email, Subject, Message, IsRead, CreatedAt) VALUES
    (N'Priya Adhikari', N'priya@example.com', N'Course request: Python',
     N'Hello, are there any plans to add an introductory Python course? I would enrol straight away.', 0, DATEADD(DAY, -1, SYSDATETIME()));
END
GO
