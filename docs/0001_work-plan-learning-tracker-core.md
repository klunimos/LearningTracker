# Work Plan: LearningTracker – Core System

## Overview
מערכת מעקב לימוד אישי וקבוצתי. המשתמש מגדיר יעדי לימוד ממגוון ספרי קודש וקטגוריות,
מדווח התקדמות, מקבל עידוד, ויכול להצטרף לקבוצות עם מעקב משותף.

---

## הנחות עיצוביות (בהיעדר תשובות לשאלות הבהרה)

| נושא | הנחה |
|------|------|
| אימות | Email + Password עם JWT ו-Refresh Token |
| משתמש ראשי | דגל `IsAdmin` ב-DB, שינוי ידני |
| נוטיפיקציות | נדחה לפאזה הבאה – לא בתוכנית זו |
| היררכיית תוכן | מבנה גמיש: עד 2 רמות קיבוץ + יחידה (ראה מטה) |
| יעד קבוצתי | שני מצבים: Shared (כולם לומדים אותו חלק) ו-Divided (חלוקת יחידות) |

---

## Libraries & Tools

| Purpose | Package | Notes |
|---------|---------|-------|
| JWT | `Microsoft.AspNetCore.Authentication.JwtBearer` | NuGet |
| Password hashing | `BCrypt.Net-Next` | NuGet |
| EF Core | `Microsoft.EntityFrameworkCore.SqlServer` | NuGet |
| EF Lazy Loading | `Microsoft.EntityFrameworkCore.Proxies` | NuGet |
| Angular UI | `primeng` + `@primeuix/themes` | npm – כבר בסטנדרט |
| HTTP | `Angular HttpClient` | מובנה |

---

## מבנה נתונים – סכמה כללית

### תוכן (ניהול ע"י Admin)

```
ContentCategory        (e.g., "תלמוד בבלי", "שולחן ערוך")
  └── ContentBook      (e.g., "מסכת ברכות", "אורח חיים חלק א")
        ├── SeriesName    (nullable grouping – e.g., "אורח חיים" לשו"ע)
        ├── GroupingLabel (label לרמת קיבוץ ראשונה – e.g., "דף", "סימן")
        ├── UnitLabel     (label ליחידה – e.g., "עמוד", "סעיף")
        └── ContentSection   (רמת קיבוץ ראשונה – e.g., "דף ב", "סימן כב")
              └── ContentUnit  (יחידה עלה – e.g., "עמוד א", "סעיף יג")
```

**לדוגמה – תלמוד בבלי / מסכת ברכות:**
- Book: "מסכת ברכות" | GroupingLabel="דף" | UnitLabel="עמוד"
- Section: "דף ב" | Units: "עמוד א", "עמוד ב"
- Section: "דף ג" | Units: "עמוד א", "עמוד ב"

**לדוגמה – שולחן ערוך / אורח חיים חלק ג:**
- Book: "אורח חיים חלק ג" | SeriesName="אורח חיים" | GroupingLabel="סימן" | UnitLabel="סעיף"
- Section: "סימן כב" | Units: "סעיף א", "סעיף ב", ... "סעיף יג"

> הערה: רמת שולחן ערוך מוסיפה לכאורה רמת "חלק" בנוסף. הטיפול: כל "חלק" הוא Book נפרד
> עם אותו SeriesName (e.g., "אורח חיים"). כך הספרייה מייצגת 5 Books עבור אורח חיים.

### משתמשים וקבוצות

```
User
  ├── IsAdmin (bool)
  └── Goals[]

Goal  (יעד אישי)
  ├── UserId
  ├── BookId (nullable)
  ├── CategoryId (nullable – אם מגדיר יעד על קטגוריה שלמה)
  ├── StartUnitId
  ├── CurrentUnitId
  ├── TargetUnitId (nullable)
  ├── TargetDate (nullable)
  ├── DailyPace (nullable – יחידות ליום)
  └── ProgressEntries[]

ProgressEntry
  ├── GoalId
  ├── UserId
  ├── UnitId (עד איפה הגיע)
  └── ReportedAt

Group
  ├── CreatedByUserId
  └── Members[]

GroupMember → UserId + GroupId + Role (Admin/Member)

GroupGoal
  ├── GroupId
  ├── BookId
  ├── Mode (Shared / Divided)
  ├── TargetDate (nullable)
  └── GroupGoalAssignments[] (לדיוויד: UnitFrom, UnitTo, MemberId)
```

---

## API Contracts

### Auth
| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| POST | `Auth/Register` | `RegisterRequest` (userName, email, password) | `ResultData<TokenData>` |
| POST | `Auth/Login` | `LoginRequest` (email, password) | `ResultData<TokenData>` |
| POST | `Auth/Refresh` | `RefreshRequest` (refreshToken) | `ResultData<TokenData>` |

`TokenData`: `{ token: string, refreshToken: string }`

### Content Catalog (Admin)
| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| GET | `ContentCategory/GetAll` | – | `ResultData<CategoryResponse[]>` |
| POST | `ContentCategory/Create` | `CreateCategoryRequest` | `ResultData<CategoryResponse>` |
| GET | `ContentBook/GetByCategory` | `categoryId` | `ResultData<BookSummaryResponse[]>` |
| POST | `ContentBook/Create` | `CreateBookRequest` | `ResultData<BookResponse>` |
| POST | `ContentBook/AddSectionsBulk` | `AddSectionsRequest` (bookId, sections[]) | `ResultData` |
| GET | `ContentBook/GetUnits` | `bookId` | `ResultData<UnitResponse[]>` |

`CategoryResponse`: `{ id, name }`
`BookSummaryResponse`: `{ id, categoryId, name, seriesName, totalUnits }`
`BookResponse`: includes `sections[]` with units
`CreateBookRequest`: `{ categoryId, name, seriesName?, groupingLabel, unitLabel }`
`AddSectionsRequest`: `{ bookId, sections: [{ name, units: string[] }] }`
`UnitResponse`: `{ id, bookId, sectionId, sectionName, name, orderIndex }`

### Goals (User)
| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| GET | `Goal/GetMine` | – | `ResultData<GoalSummaryResponse[]>` |
| POST | `Goal/Create` | `CreateGoalRequest` | `ResultData<GoalResponse>` |
| PUT | `Goal/UpdatePace` | `UpdatePaceRequest` | `ResultData<GoalResponse>` |
| POST | `Goal/ReportProgress` | `ReportProgressRequest` | `ResultData<GoalResponse>` |
| GET | `Goal/CalculatePace` | `bookId, targetDate` | `ResultData<PaceCalculationResponse>` |
| GET | `Goal/CalculateTargetDate` | `bookId, startUnitId, dailyPace` | `ResultData<TargetDateResponse>` |

`CreateGoalRequest`: `{ bookId?, categoryId?, startUnitId?, targetUnitId?, targetDate?, dailyPace? }`
`ReportProgressRequest`: `{ goalId, unitId, mode: "UpTo" | "MarkUnit" }`
`GoalSummaryResponse`: `{ id, bookName, currentUnitName, progressPercent, targetDate?, dailyPace?, isOnTrack }`
`PaceCalculationResponse`: `{ requiredDailyPace, totalUnitsRemaining }`

### Groups
| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| GET | `Group/GetMine` | – | `ResultData<GroupSummaryResponse[]>` |
| POST | `Group/Create` | `CreateGroupRequest` | `ResultData<GroupResponse>` |
| POST | `Group/Join` | `JoinGroupRequest` (groupCode) | `ResultData<GroupResponse>` |
| GET | `GroupGoal/GetByGroup` | `groupId` | `ResultData<GroupGoalResponse[]>` |
| POST | `GroupGoal/Create` | `CreateGroupGoalRequest` | `ResultData<GroupGoalResponse>` |
| POST | `GroupGoal/ReportProgress` | `GroupProgressRequest` | `ResultData<GroupGoalResponse>` |
| GET | `GroupGoal/GetMembersProgress` | `groupGoalId` | `ResultData<MemberProgressResponse[]>` |

`CreateGroupRequest`: `{ name, description? }` → generates unique `joinCode`
`CreateGroupGoalRequest`: `{ groupId, bookId, mode, targetDate?, assignedUnits[]? (for Divided) }`
`MemberProgressResponse`: `{ userId, userName, progressPercent, currentUnitName, isOnTrack }`

### Admin Reports
| Method | Endpoint | Response |
|--------|----------|----------|
| GET | `AdminReport/UsageStats` | `ResultData<UsageStatsResponse>` |
| GET | `AdminReport/ActiveGoals` | `ResultData<ActiveGoalStatsResponse>` |

---

## DB Tasks

### D1: Auth & Users
- **Entities**: `User`, `RefreshToken`
- **Relationships**: User 1→N RefreshToken
- **Indexes**: `Email` (unique), `RefreshToken.Token` (unique), `RefreshToken.UserId`

### D2: Content Catalog
- **Entities**: `ContentCategory`, `ContentBook`, `ContentSection`, `ContentUnit`
- **Relationships**:
  - Category 1→N Book
  - Book 1→N Section
  - Section 1→N Unit
- **Indexes**: `ContentUnit.BookId+OrderIndex` (composite), `ContentUnit.SectionId`, `ContentBook.CategoryId`, `ContentBook.SeriesName+CategoryId`

### D3: Goals & Progress
- **Entities**: `Goal`, `ProgressEntry`
- **Relationships**:
  - User 1→N Goal
  - Goal 1→N ProgressEntry
  - Goal N→1 ContentUnit (CurrentUnit, StartUnit, TargetUnit – 3 FKs)
- **Indexes**: `Goal.UserId`, `Goal.BookId`, `ProgressEntry.GoalId`, `ProgressEntry.ReportedAt`

### D4: Groups & Group Goals
- **Entities**: `Group`, `GroupMember`, `GroupGoal`, `GroupGoalAssignment`, `GroupProgressEntry`
- **Relationships**:
  - Group 1→N GroupMember
  - Group 1→N GroupGoal
  - GroupGoal 1→N GroupGoalAssignment (Divided mode)
  - GroupGoal 1→N GroupProgressEntry
- **Indexes**: `GroupMember.UserId+GroupId` (unique composite), `Group.JoinCode` (unique), `GroupProgressEntry.GroupGoalId+UserId`

---

## Backend Tasks

### B1: Auth Module
- **Goal**: רישום, התחברות, JWT + Refresh Token
- **Subtasks**:
  - [ ] Entity: `User`, `RefreshToken`
  - [ ] `IAuthService` + `AuthService` (Register, Login, Refresh, Hash passwords with BCrypt)
  - [ ] `AuthController`: Register, Login, Refresh
  - [ ] `[Authorize]` middleware + JWT config ב-`Program.cs`
  - [ ] `UserId` claim מתוך JWT ב-`GlobalController`
- **Endpoints**: `Auth/Register`, `Auth/Login`, `Auth/Refresh`
- **Depends on**: D1

### B2: Content Catalog (Admin)
- **Goal**: Admin מגדיר קטגוריות, ספרים, מקטעים ויחידות
- **Subtasks**:
  - [ ] Entities: `ContentCategory`, `ContentBook`, `ContentSection`, `ContentUnit`
  - [ ] `IContentCatalogService` + implementation
  - [ ] `ContentCategoryController`, `ContentBookController`
  - [ ] Endpoint `AddSectionsBulk`: ולידציה שה-Book שייך ל-Admin + bulk insert
  - [ ] `[Authorize]` + `[AdminOnly]` filter לכל Admin endpoints
- **Endpoints**: כנ"ל ב-Contracts
- **Depends on**: B1 (auth), D2

### B3: Personal Goals & Progress
- **Goal**: משתמש מגדיר יעד לימוד ומדווח התקדמות
- **Subtasks**:
  - [ ] Entities: `Goal`, `ProgressEntry`
  - [ ] `IGoalService` + implementation:
    - `CreateGoal`, `UpdatePace`
    - `ReportProgress` – מעדכן `CurrentUnitId` ב-Goal
    - `CalculatePace(bookId, startUnit, targetDate)` → יחידות ליום
    - `CalculateTargetDate(bookId, startUnit, dailyPace)` → תאריך
    - `IsOnTrack(goal)` → bool
  - [ ] `GoalController`
- **Depends on**: B2, D3

### B4: Groups
- **Goal**: יצירת קבוצה, הצטרפות, ניהול יעדים קבוצתיים
- **Subtasks**:
  - [ ] Entities: `Group`, `GroupMember`, `GroupGoal`, `GroupGoalAssignment`, `GroupProgressEntry`
  - [ ] `IGroupService`, `IGroupGoalService`
  - [ ] `GroupController`, `GroupGoalController`
  - [ ] לוגיקת Divided mode: חישוב חלוקה שווה / ידנית של יחידות
  - [ ] `GetMembersProgress`: הצגת כל חבר + אחוז + האם בתכנית
- **Depends on**: B3, D4

### B5: Admin Reports
- **Goal**: Admin רואה נתוני שימוש
- **Subtasks**:
  - [ ] `IAdminReportService`: `GetUsageStats`, `GetActiveGoals`
  - [ ] `AdminReportController` עם `[AdminOnly]`
- **Depends on**: B3, B4

---

## Frontend Tasks

### F1: Auth Pages
- **Goal**: מסך התחברות ורישום
- **Subtasks**:
  - [ ] `AuthService` (אין – HTTP ישיר ב-Component לפי SKILL)
  - [ ] `LoginComponent`, `RegisterComponent`
  - [ ] Routes: `/login`, `/register`
  - [ ] שמירת Token ב-`TokenStorageService` (כבר קיים)
  - [ ] `authGuard` על כל נתיבים מוגנים (כבר קיים)
- **API used**: B1 contracts

### F2: Content Catalog – Admin
- **Goal**: ממשק Admin להגדרת קטגוריות וספרים
- **Subtasks**:
  - [ ] `ContentCatalogService` (shared, משמש כמה עמודים)
  - [ ] `CategoriesListComponent`, `BooksListComponent`
  - [ ] `CreateBookComponent` + `AddSectionsComponent` (bulk add)
  - [ ] Routes: `/admin/categories`, `/admin/books`, `/admin/books/:id/sections`
  - [ ] Guard: `adminGuard` (מרחיב `authGuard`, בודק claim `isAdmin`)
- **API used**: B2 contracts

### F3: Personal Goals Dashboard
- **Goal**: מסך ראשי – רשימת יעדים, הוספת יעד, דיווח התקדמות
- **Subtasks**:
  - [ ] `GoalService`
  - [ ] `GoalsDashboardComponent` – רשימת יעדים עם Progress bar
  - [ ] `CreateGoalComponent` – בחירת ספר/קטגוריה, הגדרת תאריך/קצב
  - [ ] `ReportProgressComponent` – modal לדיווח (UpTo / MarkUnit)
  - [ ] `PaceCalculatorComponent` – ממשק חישוב דו-כיווני (תאריך ↔ קצב)
  - [ ] Routes: `/goals`, `/goals/create`, `/goals/:id`
- **API used**: B3 contracts

### F4: Groups
- **Goal**: מסך קבוצות, הצטרפות, יעדים קבוצתיים
- **Subtasks**:
  - [ ] `GroupService`
  - [ ] `GroupsListComponent`, `CreateGroupComponent`
  - [ ] `GroupDetailComponent` – חברים + יעדים קבוצתיים
  - [ ] `GroupGoalProgressComponent` – טבלת מי אוחז איפה
  - [ ] `ReportGroupProgressComponent` – modal לדיווח בתוכנית קבוצתית
  - [ ] Routes: `/groups`, `/groups/create`, `/groups/:id`, `/groups/:id/goals/:goalId`
- **API used**: B4 contracts

### F5: Admin Reports
- **Goal**: דשבורד Admin
- **Subtasks**:
  - [ ] `AdminReportComponent`
  - [ ] Route: `/admin/reports`
- **API used**: B5 contracts

---

## Parallel Execution Map

| Who | Can start when |
|-----|----------------|
| D1 | מיד |
| D2 | מיד (במקביל ל-D1) |
| D3, D4 | אחרי D1, D2 |
| B1 | אחרי D1 |
| B2 | אחרי D2 + B1 (auth) |
| B3 | אחרי B2 + D3 |
| B4 | אחרי B3 + D4 |
| B5 | אחרי B3, B4 |
| F1 | אחרי הגדרת API contracts (לא אחרי implement) |
| F2 | אחרי F1 (auth) |
| F3 | אחרי F1 |
| F4 | אחרי F3 |
| F5 | אחרי F2 |

---

## Acceptance Criteria

### Auth
- [ ] משתמש יכול להירשם עם email + password
- [ ] משתמש יכול להתחבר ולקבל JWT
- [ ] Refresh Token מחדש token שפג תוקף

### Content Catalog
- [ ] Admin יכול ליצור קטגוריה
- [ ] Admin יכול ליצור ספר ולהוסיף מקטעים ויחידות בבלק
- [ ] משתמש רגיל לא יכול לגשת לendpoints של Admin

### Goals
- [ ] משתמש יכול ליצור יעד על ספר עם/בלי תאריך/קצב
- [ ] מערכת מחשבת קצב נדרש לפי תאריך יעד
- [ ] מערכת מחשבת תאריך יעד לפי קצב
- [ ] דיווח "עד יחידה X" מעדכן CurrentUnit
- [ ] Progress bar מציג אחוז נכון

### Groups
- [ ] משתמש יכול ליצור קבוצה ולקבל קוד הצטרפות
- [ ] משתמש יכול להצטרף בקוד
- [ ] Admin קבוצה יכול ליצור יעד קבוצתי (Shared או Divided)
- [ ] במצב Shared – כל חבר מדווח "עד X", ורואים את כולם
- [ ] במצב Divided – כל חבר רואה את החלק שלו

---

## פאזה הבאה (לא בתוכנית זו)
- Push notifications / Email reminders
- Google SSO
- עמוד "לוח מובילים" (leaderboard) בקבוצה
- לוח שנה להתקדמות
