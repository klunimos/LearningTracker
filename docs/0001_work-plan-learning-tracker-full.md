# Work Plan: LearningTracker — Full System

## Skills loaded for this plan

- `agent-frontend-dev` — Angular (components, services, routing, forms)
- `agent-backend-dev` — .NET 10 ASP.NET Core Web API (controllers, services, EF Core)
- `agent-dba` — MSSQL schema design (SQL scripts, indexes)

## Overview

מערכת למעקב לימוד אישי וקבוצתי. משתמש מגדיר יעדי לימוד לספרי קודש, מדווח התקדמות, מצטרף לקבוצות, ורואה דאשבורד עם מיקומו ביחס ליעדיו.

---

## Libraries & Tools

- **Authentication**: `Microsoft.AspNetCore.Authentication.JwtBearer` (JWT) + `Google.Apis.Auth` (Google OAuth)
- **ORM**: EF Core 10 + `Microsoft.EntityFrameworkCore.SqlServer` + `Microsoft.EntityFrameworkCore.Proxies` (lazy loading)
- **Logging**: ElmahCore (already in project)
- **Password hashing**: `BCrypt.Net-Next`
- **Frontend UI**: PrimeNG (already in project per Angular setup)
- **Frontend auth**: `@auth0/angular-jwt` for JWT token handling

---

## Data Model

### Hierarchy for Books

כל קטגוריה מגדירה שמות לשתי רמות ויחידה:

- L1Name, L2Name, UnitName (כגון: "דף", "עמוד" בתלמוד — "סימן", "סעיף" בשו"ע)
- ספר שייך לקטגוריה (ואופציונלית ל-SeriesName כגון "אורח חיים")
- BookUnits מייצג את כל היחידות הלמידה הנפרדות בספר, ממוספרות לפי SortOrder

### Goal Target — Category / Collection / Single Book

יעד יכול לכלול:

- **ספר בודד** (BookId)
- **אוסף ספרים** (רשימת BookIds)
- **קטגוריה שלמה** (CategoryId — כולל את כל הספרים שלה)

מיושם דרך טבלת junction: `GoalBooks (GoalId, BookId)`. ה-service מרחיב קטגוריה לרשימת ספרים בעת יצירת היעד. ה-Progress מצטבר על פני כל הספרים ביעד.

### Group Goal Membership

הצטרפות ליעד קבוצתי היא **אקטיבית ואינה אוטומטית**. חבר קבוצה שרואה יעד קבוצתי חייב ללחוץ "הצטרף ליעד" כדי להתחיל לדווח ולהופיע בדאשבורד הקבוצתי. מיושם דרך טבלת junction: `GroupGoalMembers (GroupGoalId, UserId, JoinedAt)`.

### Progress Reporting Model

כל דיווח מוסיף שורה חדשה ב-`ProgressEntries` (לא מחליף). המיקום הנוכחי = השורה האחרונה לפי (GoalId, BookId, UserId). HistoryEndpoint מחזיר את כל השורות — מאפשר גרף התקדמות לאורך זמן.

---

## DB Schema (D-tasks)

### D1 — Core Schema (Users, Catalog, Goals, Progress)

**Tables:**

- `Users` (Id, Email, PasswordHash, FullName, IsAdmin, GoogleId, ProfilePicture, CreatedAt, UpdatedAt)
- `Categories` (Id, Name, L1Name, L2Name, UnitName, CreatedByUserId, CreatedAt)
- `Books` (Id, CategoryId, Name, SeriesName, CreatedByUserId, CreatedAt)
- `BookUnits` (Id, BookId, L1Label, L1Order, UnitLabel, UnitOrder, DisplayName, SortOrder)
- `Goals` (Id, UserId, CategoryId nullable, Title, StartUnitId, TargetDate, DailyPace, IsCompleted, CreatedAt, UpdatedAt)
- `GoalBooks` (GoalId, BookId) — composite PK, junction table (ריק = כל ספרי הקטגוריה)
- `ProgressEntries` (Id, GoalId, UserId, BookId, UnitId, Note, ReportedAt)

**Indexes:** FK on every FK column; `IX_Goals_UserId`; `IX_ProgressEntries_GoalId`; `IX_GoalBooks_GoalId`; `IX_BookUnits_BookId_SortOrder`

### D2 — Groups & Notifications Schema

**Tables:**

- `Groups` (Id, Name, Description, InviteCode, IsPublic, CreatedByUserId, CreatedAt)
- `GroupMembers` (GroupId, UserId, Role, JoinedAt) — composite PK
- `GroupGoals` (Id, GroupId, CategoryId nullable, Title, TargetDate, CollectiveTargetUnitId, CreatedAt, CreatedByUserId)
- `GroupGoalBooks` (GroupGoalId, BookId) — composite PK, junction (ריק = כל ספרי הקטגוריה)
- `GroupGoalMembers` (GroupGoalId, UserId, JoinedAt) — composite PK, הצטרפות אקטיבית ליעד
- `GroupProgressEntries` (Id, GroupGoalId, UserId, BookId, UnitId, IsCollectiveTarget, ReportedAt)
- `Notifications` (Id, UserId, Message, Type, IsRead, RelatedEntityType, RelatedEntityId, CreatedAt)

**Indexes:** FK on every FK column; `IX_Groups_InviteCode` (unique); `IX_Notifications_UserId_IsRead`; `IX_GroupGoalMembers_GroupGoalId`

---

## API Contracts

### Auth

- `POST /Auth/Register` — `RegisterRequest { Email, Password, FullName }` → `AuthResponse { Token, User }`
- `POST /Auth/Login` — `LoginRequest { Email, Password }` → `AuthResponse`
- `POST /Auth/GoogleLogin` — `GoogleLoginRequest { GoogleToken }` → `AuthResponse`

### Users

- `GET /Users/Me` → `UserResponse { Id, Email, FullName, IsAdmin, ProfilePicture }`
- `PUT /Users/UpdateProfile` — `UpdateProfileRequest { FullName, ProfilePicture }`

### Catalog

- `GET /Catalog/Categories` → `List<CategoryResponse>`
- `POST /Catalog/CreateCategory` (admin) — `CreateCategoryRequest { Name, L1Name, L2Name, UnitName }`
- `GET /Catalog/Books` — query: `?categoryId=` → `List<BookResponse>`
- `POST /Catalog/CreateBook` (admin) — `CreateBookRequest { CategoryId, Name, SeriesName }`
- `POST /Catalog/AddBookUnits` (admin) — `AddBookUnitsRequest { BookId, Units: [{ L1Label, L1Order, UnitLabel, UnitOrder, DisplayName }] }`
- `GET /Catalog/BookUnits/{bookId}` → `List<BookUnitResponse>`

### Goals

- `GET /Goals/My` → `List<GoalSummaryResponse>`
- `GET /Goals/Detail/{id}` → `GoalDetailResponse`
- `POST /Goals/Create` — `CreateGoalRequest { CategoryId?, BookIds?: number[], Title, StartUnitId?, TargetDate?, DailyPace? }` — אחד מ-CategoryId או BookIds חייב להיות מסופק
- `PUT /Goals/Update/{id}` — `UpdateGoalRequest { Title, TargetDate?, DailyPace? }`
- `DELETE /Goals/Delete/{id}`
- `POST /Goals/CalcSchedule` — `CalcScheduleRequest { CategoryId?, BookIds?: number[], StartUnitId?, TargetDate?, DailyPace? }` → `ScheduleResponse { SuggestedTargetDate, SuggestedDailyPace, TotalUnits }`

### Progress

- `POST /Progress/Report` — `ReportProgressRequest { GoalId, BookId, UnitId, Note? }` → `ProgressEntryResponse`
- `GET /Progress/GoalProgress/{goalId}` → `GoalProgressResponse { BooksProgress: [{ BookId, BookName, CurrentUnitDisplay, TotalUnits, Percentage }], OverallPercentage, IsOnTrack, DaysAhead, DaysBehind }`
- `GET /Progress/History/{goalId}` → `List<ProgressEntryResponse>`

### Groups

- `GET /Groups/My` → `List<GroupSummaryResponse>`
- `GET /Groups/Search?query=` → `List<GroupSummaryResponse>`
- `POST /Groups/Create` — `CreateGroupRequest { Name, Description, IsPublic }` → `GroupResponse { ..., InviteCode }`
- `POST /Groups/JoinByCode` — `{ InviteCode }` → `GroupResponse`
- `POST /Groups/JoinById` — `{ GroupId }` → `GroupResponse` (קבוצות פומביות)
- `GET /Groups/Detail/{id}` → `GroupDetailResponse { Members, Goals }`
- `POST /Groups/CreateGoal` — `CreateGroupGoalRequest { GroupId, CategoryId?, BookIds?: number[], Title, TargetDate?, CollectiveTargetUnitId? }`
- `POST /Groups/JoinGoal` — `{ GroupGoalId }` → הצטרפות אקטיבית של חבר ליעד הקבוצתי
- `POST /Groups/ReportProgress` — `ReportGroupProgressRequest { GroupGoalId, BookId, UnitId, IsCollectiveTarget }`
- `GET /Groups/GoalProgress/{groupGoalId}` → `GroupGoalProgressResponse { JoinedMembersProgress: [{ UserId, FullName, CurrentUnitDisplay, OverallPercentage }], CollectiveCurrentUnit }`

### Notifications

- `GET /Notifications/My` → `List<NotificationResponse>`
- `GET /Notifications/UnreadCount` → `{ Count }`
- `POST /Notifications/MarkRead` — `{ NotificationIds: number[] }`
- `POST /Notifications/MarkAllRead`

### Admin

- `GET /Admin/Reports/Usage` → `UsageReportResponse { TotalUsers, ActiveUsers, TotalGoals, ActiveGoals, TopBooks }`

---

## Backend Tasks

### B1 — Auth Module

- **Goal**: הרשמה + כניסה עם JWT + Google OAuth
- **Subtasks**:
  - Entity: `User`, `AppDbContext`, `DbSet<User>`
  - `IAuthService` + `AuthService` (Register, Login, GoogleLogin, GenerateToken)
  - `BCrypt.Net-Next` לגיבוב סיסמאות
  - `Google.Apis.Auth` לאימות Google token
  - `AuthController` עם 3 endpoints
  - JWT setup ב-`Program.cs` (Bearer scheme + validation)
- **Depends on**: D1

### B2 — Users Module

- **Goal**: פרופיל משתמש, `UserId` נגיש מ-`GlobalController`
- **Subtasks**:
  - `IUserService` + `UserService` (GetMe, UpdateProfile)
  - `UsersController`
- **Depends on**: B1

### B3 — Catalog Module (Admin)

- **Goal**: ניהול קטגוריות, ספרים ויחידות
- **Subtasks**:
  - Entities: `Category`, `Book`, `BookUnit`
  - `ICatalogService` + `CatalogService`
  - `CatalogController` — בדיקת `IsAdmin` בתוך ה-service לפעולות כתיבה
- **Depends on**: D1, B1

### B4 — Goals Module

- **Goal**: יצירה, עדכון, מחיקה של יעדים + חישוב קצב/תאריך
- **Subtasks**:
  - Entities: `Goal`, `GoalBook`
  - `IGoalService` + `GoalService` (CRUD + `CalcSchedule` logic)
  - לוגיקה: אם הגיע `CategoryId` — service מרחיב לכל `BookId` שלה ומאכלס `GoalBooks`
  - `GoalsController`
- **Depends on**: D1, B3

### B5 — Progress Module

- **Goal**: דיווח התקדמות + שאילתת סטטוס
- **Subtasks**:
  - Entity: `ProgressEntry` (כולל `BookId` לדיווח פר-ספר)
  - `IProgressService` + `ProgressService` (Report, GetProgress, GetHistory)
  - `ProgressController`
  - **מודל שמירה**: כל דיווח מוסיף שורה חדשה ב-`ProgressEntries` (לא מחליף). המיקום הנוכחי = השורה האחרונה לפי (GoalId, BookId). HistoryEndpoint מחזיר את כל השורות — מאפשר גרף התקדמות לאורך זמן.
  - לוגיקה: GetProgress מחזיר התקדמות לכל ספר ביעד + OverallPercentage מצטבר
  - לוגיקה: IsOnTrack, DaysAhead/Behind לפי TargetDate + DailyPace כולל על פני כלל הספרים
- **Depends on**: D1, B4

### B6 — Groups Module

- **Goal**: יצירה, הצטרפות, ניהול קבוצות ויעדים קבוצתיים
- **Subtasks**:
  - Entities: `Group`, `GroupMember`, `GroupGoal`, `GroupGoalBook`, `GroupGoalMember`, `GroupProgressEntry`
  - `IGroupService` + `GroupService`
  - `GroupsController`
  - יצירת `InviteCode` אוטומטי (GUID קצר)
  - לוגיקה: `JoinGoal` — מוסיף `GroupGoalMember`; דיווח מאומת רק אם קיים `GroupGoalMember`
  - לוגיקה: GoalProgress מציג רק את החברים שהצטרפו ליעד (לא כל חברי הקבוצה)
- **Depends on**: D2, B4

### B7 — Notifications Module

- **Goal**: יצירת והגשת התראות in-app; מנגנון עידוד אוטומטי
- **Subtasks**:
  - Entity: `Notification`
  - `INotificationService` + `NotificationService` (Create, GetMy, MarkRead)
  - `NotificationsController`
  - קריאה ל-`NotificationService` מתוך `ProgressService` בכל דיווח
- **Depends on**: D2, B5

### B8 — Admin Reports Module

- **Goal**: דוחות שימוש למשתמש ראשי
- **Subtasks**:
  - `IAdminService` + `AdminService` (GetUsageReport)
  - `AdminController` — בדיקת `IsAdmin`
- **Depends on**: B5, B6

---

## Frontend Tasks

### F1 — Auth Pages

- Login page, Register page, Google login button
- JWT token stored in `TokenStorageService`
- `AuthGuard` מגן על routes
- **API**: B1 contracts
- **Depends on**: API contracts from B1

### F2 — Home / Dashboard

- דאשבורד ראשי: רשימת יעדים אישיים עם % התקדמות
- ווידג'ט התראות (unread count בתפריט)
- **API**: B4, B5, B7 contracts

### F3 — Catalog Browse + Admin Panel

- עמוד גלישה בקטגוריות וספרים (לכל משתמש)
- Admin panel: טפסי יצירת קטגוריה / ספר / יחידות
- **API**: B3 contracts

### F4 — Goal Wizard

- אשף יצירת יעד: בחירת **קטגוריה שלמה / אוסף ספרים / ספר בודד** → הגדרת תאריך/קצב → תצוגת לוח זמנים מחושב
- UI מאפשר לסמן כל ספרי הקטגוריה בלחיצה אחת, או לבחור ספרים ספציפיים מתוכה
- **API**: B4 (Create + CalcSchedule)

### F5 — Progress Reporting

- טופס דיווח: בחירת יחידה מתוך הספר OR "עד יחידה X"
- תצוגת גרף התקדמות לפר-יעד
- **API**: B5 contracts

### F6 — Groups

- חיפוש/הצטרפות לקבוצה, יצירת קבוצה
- עמוד קבוצה: חברים, יעדים קבוצתיים, דאשבורד קבוצתי
- כל יעד קבוצתי מציג כפתור "הצטרף ליעד" למי שטרם הצטרף; רק לאחר הצטרפות ניתן לדווח ומוצגת ההתקדמות
- דיווח בהקשר קבוצתי (breakdown לפי ספרים כמו ביעד אישי)
- **API**: B6 contracts

### F7 — Notifications Center

- פאנל התראות, סימון כנקרא
- **API**: B7 contracts

### F8 — Admin Reports

- עמוד דוחות: נתוני שימוש בגרפים/טבלאות
- **API**: B8 contracts

---

## Execution Order (Backend + Frontend per step)

כל שלב כולל את הצד שרת **והצד לקוח** יחד לפני שמתקדמים לשלב הבא.

| שלב | Backend | Frontend | סטטוס |
|-----|---------|----------|-------|
| 0 | D1, D2 — DB Schema | — | ✅ הושלם |
| 1 | B1 — Auth, B2 — Users | F1 — Auth pages | B1+B2 ✅, F1 pending |
| 2 | B3 — Catalog | F3 — Catalog + Admin panel | pending |
| 3 | B4 — Goals | F4 — Goal wizard | pending |
| 4 | B5 — Progress | F5 — Progress reporting | pending |
| 5 | B6 — Groups | F6 — Groups UI | pending |
| 6 | B7 — Notifications | F7 — Notifications center | pending |
| 7 | B8 — Admin Reports | F8 — Reports page | pending |
| 8 | — | F2 — Dashboard (אחרי F4+F5) | pending |


---

## Deferred Tasks

### DEFER-1 — Email Infrastructure (אימות מייל + שכחתי סיסמה)
נדחה עד לבחירת שירות מיילים (SMTP / SendGrid).
כשמממשים:
- הוספת `IsEmailVerified BIT DEFAULT 0` ו-`EmailVerificationToken` לטבלת `Users`
- אחרי הרשמה — שליחת מייל עם קישור אימות + endpoint `GET /Auth/VerifyEmail?token=xxx`
- Login לפני אימות — `Fail("יש לאמת את כתובת המייל תחילה")`
- endpoint `POST /Auth/ForgotPassword` — שליחת מייל עם קישור איפוס
- endpoint `POST /Auth/ResetPassword` — איפוס סיסמה לפי token

### DEFER-2 — דרישות מינימום לסיסמה
כרגע: אין validation על הסיסמה. להוסיף לפני production:
- מינימום 8 תווים
- לפחות ספרה אחת
- Regex validation בשרת ב-`AuthService.RegisterAsync`

---

## Acceptance Criteria

- משתמש יכול להירשם ולהתחבר עם מייל/סיסמה ועם Google
- משתמש ראשי יכול ליצור קטגוריה, ספר, ולהגדיר את יחידותיו
- משתמש יכול להגדיר יעד על קטגוריה שלמה, אוסף ספרים, או ספר בודד
- משתמש יכול ליצור יעד עם/בלי תאריך יעד ועם/בלי קצב; המערכת מחשבת את החסר
- משתמש יכול לדווח "הגעתי ליחידה X" וגם "סימנתי יחידות בודדות"
- דאשבורד מציג % התקדמות (כולל פר-ספר) ואם המשתמש לפני/אחרי לוח הזמנים
- משתמש יכול ליצור קבוצה, להצטרף בקוד או בחיפוש
- חבר קבוצה מצטרף ליעד קבוצתי אקטיבית; רק מצטרפים מופיעים בדאשבורד הקבוצתי
- בקבוצה ניתן לראות את ההתקדמות של כל חבר שהצטרף ליעד הקבוצתי
- בכל דיווח מוצלח מופיעה התראת עידוד in-app
- משתמש ראשי יכול לראות דוח שימוש

