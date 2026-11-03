# VertexLearn — Web-Based Learning System

CT050-3-2-WAPP Web Applications, group assignment (APU).

VertexLearn is a web-based learning system with three kinds of users:

- **Visitors** (not logged in) can browse the course catalogue and read course outlines.
- **Registered students** can enrol, study multimedia materials, take auto-marked quizzes, join course discussion boards, track progress and manage their profile.
- **Administrators** manage users, categories, courses, resources, quizzes/questions, quiz attempts, discussions and contact messages.

---

## 1. Technology stack

| Layer | Technology |
|---|---|
| Server-side | ASP.NET Core 8 MVC, C# |
| Views | Razor (`.cshtml`) with Tag Helpers, HTML5 |
| Client-side | CSS3 (external, internal and inline), vanilla JavaScript, jQuery Validation (unobtrusive) |
| **Database** | **Microsoft SQL Server** (LocalDB by default; Express or Developer also work) |
| Data access | ADO.NET with `Microsoft.Data.SqlClient` and fully parameterised SQL |
| Authentication | ASP.NET Core cookie authentication, role-based authorisation (`Student`, `Admin`) |
| Password storage | Salted PBKDF2 hashes via ASP.NET Core `PasswordHasher` (never plain text) |

---

## 2. How to run

### Requirements
- .NET 8 SDK
- SQL Server LocalDB. It is installed with Visual Studio's **"ASP.NET and web development"** workload. Check with `sqllocaldb info`.

### Visual Studio
1. Open `VertexLearn.slnx` (or `VertexLearn.csproj`).
2. Press **F5**. NuGet restores `Microsoft.Data.SqlClient` automatically.

### VS Code / terminal
```bash
dotnet run
```

### The database is created automatically
On first launch, `Data/DbInitializer.cs` runs `Database/VertexLearnDb.sql`. That script:

1. Creates the `VertexLearnDb` database.
2. Creates all 12 tables.
3. Inserts sample data.

The script only creates what's missing, so it's safe on every start. You can also run the script yourself in SSMS or Visual Studio's SQL Server Object Explorer.

To use a different SQL Server, change `ConnectionStrings:VertexLearnDb` in `appsettings.json`. For example: `Server=.\SQLEXPRESS;Database=VertexLearnDb;Trusted_Connection=True;TrustServerCertificate=True`.

### Seeded accounts (for the team and lecturer only)
These are created by the database script so the admin area can be reached on a fresh install. They are **not** shown anywhere on the website. Change the admin password from **Profile → Change password** after the first login.

| Role | Email | Password |
|---|---|---|
| Admin | admin@vertexlearn.com | Admin@123 |
| Student | jasmi@student.com | Student@123 |
| Student | rohan@student.com | Student@123 |

To reset everything, delete the database (SSMS → right-click `VertexLearnDb` → Delete) and run the app again.

---

## 3. Database design (SQL Server)

| Table | Purpose | Key relationships |
|---|---|---|
| `Users` | Students and admins. Email is UNIQUE; Role has a CHECK constraint. | — |
| `Categories` | Course subjects. | 1 → many `Courses` (delete blocked while courses exist) |
| `Courses` | The course catalogue. | FK `CategoryId` |
| `Resources` | Multimedia materials (Video, PDF, Link, Notes, Image, Audio); admins can upload files to wwwroot/files/uploads. | FK `CourseId`, ON DELETE CASCADE |
| `Enrollments` | Bridge table for the many-to-many relationship between Users and Courses. UNIQUE (UserId, CourseId). | FK `UserId`, FK `CourseId` (cascade) |
| `Quizzes` | Self-assessment quizzes with a pass mark. | FK `CourseId` (cascade) |
| `Questions` | Multiple-choice questions (A–D). CHECK on `CorrectOption`. | FK `QuizId` (cascade) |
| `QuizAttempts` | One row per submission, with score. | FK `UserId`, FK `QuizId` (cascade) |
| `AttemptAnswers` | The answer chosen for each question in an attempt. | FK `AttemptId` (cascade), FK `QuestionId` |
| `DiscussionPosts` | Course discussion threads. | FK `CourseId`, FK `UserId` (cascade) |
| `DiscussionReplies` | Replies to threads. | FK `PostId` (cascade), FK `UserId` |
| `ContactMessages` | Messages from the public Contact form. | — |

Two foreign keys use NO ACTION instead of CASCADE: `AttemptAnswers.QuestionId` and `DiscussionReplies.UserId`. SQL Server rejects "multiple cascade paths", so the repositories delete those rows first, inside a transaction.

---

## 4. How each assignment requirement is met

| Requirement | Where |
|---|---|
| Interlinked web pages | Shared navigation and footer (`Views/Shared/_Layout.cshtml`), breadcrumbs, cross-links between catalogue, course, learn, quiz and discussion pages |
| HTML5 elements | `header`, `nav`, `main`, `section`, `article`, `aside`, `footer`, `figure`/`figcaption`, `time`, `details`/`summary`, `progress`, `meter`, `datalist`, `output`, `fieldset`/`legend`, `address`, `blockquote`, `video`, `audio`, `iframe`, `object`, and input types `email`, `number`, `range`, `search` |
| External CSS | `wwwroot/css/site.css`, linked from the layout |
| Internal CSS | `<style>` block in `Views/Home/About.cshtml` |
| Inline CSS | `style` attributes whose values depend on data: score colour in `Quiz/Result.cshtml`, average-score bar in `Member/Dashboard.cshtml`, plus `Admin/Questions.cshtml`. Everything else is kept in the stylesheet. |
| Multimedia | Embedded YouTube players, PDF preview (`<object>`), SVG diagrams and course covers, HTML5 `<video>` and `<audio>` support |
| **Insert** | Register, enrol, submit quiz, create thread and reply, contact form; admin adds users, categories, courses, resources, quizzes and questions |
| **Display** | Catalogue with search and filter, course details, dashboard, results, forum; every admin list |
| **Update** | Profile, change password, edit own post; admin edits every entity |
| **Delete** | Leave course, delete own post, reply or account; admin deletes every entity (always via POST + confirmation) |
| Registration page | `/Account/Register` |
| Member module (login required) | `MemberController`, `QuizController`, `DiscussionController`, all `[Authorize]` |
| Admin module (login required) | `AdminController`, `[Authorize(Roles = "Admin")]` |
| Form validation | Server: Data Annotations, custom `IValidatableObject` (distinct quiz options), duplicate-email and current-password checks. Client: jQuery unobtrusive validation, password-strength meter, character counters, "answer every question" check on quizzes. |
| Navigation support | Responsive nav with active state, mobile menu, admin sidebar, breadcrumbs, skip link, custom 403/404 pages |
| File organisation | See below |

### Security measures (useful for the presentation Q&A)
- Every SQL statement is parameterised, which prevents SQL injection.
- Passwords are stored as salted PBKDF2 hashes.
- Anti-forgery tokens are validated on every POST (global filter). Deletes are never plain GET links.
- Role-based authorisation on controllers.
- Ownership checks: users can only edit or delete their own posts. Admins can moderate everything.
- Quiz answers are marked on the server. Correct answers are never sent to the browser.
- Open-redirect protection on login (`Url.IsLocalUrl`).
- The same error message is shown for a wrong email or a wrong password.

---

## 5. File organisation

```
VertexLearn/
├── Controllers/            Home, Courses, Account, Member, Quiz, Discussion, Admin
├── Data/
│   ├── Db.cs               ADO.NET helper (Query / Execute / Scalar)
│   ├── DbInitializer.cs    Runs the SQL script on start-up
│   ├── DataReaderExtensions.cs
│   └── Repositories/       All SQL lives here, one repository per area
├── Database/
│   └── VertexLearnDb.sql   Full schema + sample data
├── Infrastructure/         Auth cookie helper, claims helpers, media (YouTube) helper
├── Models/                 Entities with validation rules
│   └── ViewModels/         Page-specific models (register, dashboard, quiz, …)
├── Views/
│   ├── Shared/             _Layout, _AdminNav, _CourseCard, _Alerts, validation scripts
│   ├── Home/ Courses/ Account/ Member/ Quiz/ Discussion/ Admin/
├── wwwroot/
│   ├── css/site.css        External stylesheet
│   ├── js/site.js, quiz.js Client-side scripts
│   ├── images/             Logo, hero, course covers, teaching diagrams (SVG)
│   └── files/              PDF handouts
├── Program.cs              Services, authentication, middleware pipeline
└── appsettings.json        Connection string
```
