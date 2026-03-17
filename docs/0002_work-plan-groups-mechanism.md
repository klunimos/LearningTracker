# Work Plan: מנגנון קבוצות (Groups)

## Overview

המנגנון מאפשר למשתמשים ליצור קבוצות, להצטרף אליהן באמצעות קוד הזמנה, להגדיר יעדים קבוצתיים (ספר בודד ליעד), להצטרף ליעדים כאלה ולדווח התקדמות. דף הבית כבר מציג יעדים קבוצתיים שהמשתמש משתתף בהם; דף "קבוצות" כרגע placeholder. התוכנית משלימה את חוויית המשתמש: רשימת קבוצות, יצירה/הצטרפות, פרטי קבוצה ויעד קבוצתי, דיווח התקדמות ביעד קבוצתי.

---

## עקרונות עיצוב

| עקרון | החלטה |
|--------|--------|
| **ספר בודד ליעד** | יעד קבוצתי מוגדר לספר אחד בלבד (כמו יעד אישי). לא יעד לקטגוריה ולא למספר ספרים. |
| **יצירת יעד קבוצתי** | שימוש **בדיוק** באותו מנגנון כמו יצירת יעד אישי: אותם שלבים (בחירת קטגוריה → ספר בודד → יחידת התחלה, תאריך יעד/קצב), אותה חוויית משתמש (כולל Dual Datepicker, חישובי קצב/תאריך). |
| **שימוש חוזר בטבלאות** | במידת האפשר – שימוש בטבלאות הקיימות (Goals, GoalBooks, ProgressEntries) גם ליעדים קבוצתיים; טבלאות ייעודיות רק שם שברור שצריך (למשל צירוף משתתף ליעד קבוצתי). |

---

## מצב נוכחי

| שכבת | מה קיים | מה חסר |
|------|---------|--------|
| **DB** | סקריפט D2: Groups, GroupMembers, GroupGoals, GroupGoalBooks, GroupGoalMembers, GroupProgressEntries, Notifications | מיגרציה ל־שימוש חוזר: Goals (GroupId), GoalBooks, ProgressEntries + טבלת השתתפות; או צמצום ל־ספר בודד ב־GroupGoals |
| **Backend** | GroupController, GroupGoalController, GroupService, GroupGoalService | התאמת Create/Report ל־ספר בודד ואותו חוזה כ־יעד אישי; יישור ל־UnitIds/From–To אם משתמשים ב־ProgressEntries |
| **Frontend** | GroupGoalService.getMyParticipatingGoals(), הצגת יעדים בדף הבית, route `/groups` → placeholder | GroupService, דפי קבוצות; **יצירת יעד קבוצתי = שימוש באותו מנגנון (create-goal) עם הקשר קבוצה** |

---

## Libraries & Tools

| Purpose | Package | Notes |
|---------|---------|-------|
| (ללא שינוי) | – | שימוש ב־HttpClient, Reactive Forms, Router הקיימים |

---

## API Contracts (קיימים – להשלמת Frontend)

### GroupController (`/Group`)

| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| GET | `GetMine` | – | `ResultData<List<GroupSummaryResponse>>` |
| POST | `Create` | `CreateGroupRequest` (Name, Description?, IsPublic) | `ResultData<GroupDetailResponse>` |
| POST | `Join` | `JoinGroupRequest` (InviteCode) | `ResultData<GroupDetailResponse>` |
| GET | `GetDetail` | `groupId` (query) | `ResultData<GroupDetailResponse>` |
| GET | `Search` | `query` (query) | `ResultData<List<GroupSummaryResponse>>` |

**GroupSummaryResponse**: Id, Name, Description, IsPublic, InviteCode, MemberCount, GoalCount, MyRole, CreatedAt  
**GroupDetailResponse**: Id, Name, Description, IsPublic, InviteCode, MyRole, Members (UserId, FullName, Role, JoinedAt), CreatedAt

### GroupGoalController (`/GroupGoal`)

חוזה יצירת יעד קבוצתי **זהה במהות** ל־CreateGoalRequest (ספר בודד, StartUnitId, TargetDate/DailyPace). דיווח התקדמות **זהה** ל־ReportProgressRequest (UnitIds, From/To).

| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| GET | `GetByGroup` | `groupId` (query) | `ResultData<List<GroupGoalSummaryResponse>>` |
| GET | `GetMyParticipatingGoals` | – | `ResultData<List<GroupGoalHomeItemResponse>>` (קיים ב־Frontend) |
| POST | `Create` | `CreateGroupGoalRequest` (GroupId, Title, **BookId** – ספר בודד, StartUnitId?, TargetDate?, DailyPace?) | `ResultData<GroupGoalDetailResponse>` |
| POST | `JoinGoal` | `JoinGroupGoalRequest` (GroupGoalId) | `ResultData<GroupGoalDetailResponse>` |
| POST | `ReportProgress` | `ReportGroupProgressRequest` (GroupGoalId, BookId, **UnitIds** – כמו יעד אישי; Note?) | `ResultData<GroupGoalDetailResponse>` |
| GET | `GetMembersProgress` | `groupGoalId` (query) | `ResultData<List<MemberProgressResponse>>` |

**הערה:** לא CategoryId, לא BookIds[]; דיווח עם UnitIds (וטווחים From/To בצד שרת) כמו ביעד אישי.

---

## Backend Tasks (השלמות והתאמות)

### B1: וידוא הרצת סקריפט D2 ו־DI
- **Goal**: וודא שטבלאות קבוצות קיימות (או מיגרציה ל־שימוש חוזר) והשירותים רשומים.
- **Subtasks**:
  - [ ] הרצת סקריפט DB (D2 או מיגרציית שימוש חוזר לפי D2 למטה).
  - [ ] וידוא ב־Program.cs: `IGroupService`, `IGroupGoalService` ו־Controllers רשומים.
- **Depends on**: אין.

### B2: התאמת יעד קבוצתי לספר בודד ואותו חוזה כ־יעד אישי
- **Goal**: יצירת יעד קבוצתי עם BookId בודד, StartUnitId, TargetDate/DailyPace (כמו CreateGoalRequest); הסרת CategoryId ו־BookIds[].
- **Subtasks**:
  - [ ] עדכון `CreateGroupGoalRequest`: GroupId, Title, BookId (יחיד), StartUnitId?, TargetDate?, DailyPace?; הסרת CategoryId, BookIds, CollectiveTargetUnitId (או שמירה לאופציונלי).
  - [ ] עדכון GroupGoalService.CreateGroupGoalAsync: שמירת ספר בודד (GoalBooks או עמודה BookId אם אוחד ל־Goals).
  - [ ] עדכון ReportGroupProgressRequest ל־UnitIds (כמו ReportProgressRequest); עדכון GroupGoalService.ReportProgressAsync לטפל בטווחים From/To (או שימוש ב־ProgressEntries אם אוחד).
- **Depends on**: D2 (מיגרציה).

### B3 (אם בוחרים באיחוד טבלאות): שימוש ב־Goals + ProgressEntries ליעדים קבוצתיים
- **Goal**: יעדים קבוצתיים מאוחסנים ב־Goals (עם GroupId לא null), GoalBooks (ספר בודד), ProgressEntries; טבלת השתתפות (משתתפים ביעד קבוצתי).
- **Subtasks**:
  - [ ] מיגרציית DB: הוספת GroupId (nullable) ל־Goals; טבלה GoalParticipants(GoalId, UserId); מיגרציית נתונים מ־GroupGoals/GroupGoalMembers/GroupProgressEntries; הסרת טבלאות ישנות (או השארה זמנית).
  - [ ] עדכון GoalService/GroupGoalService: קריאת יעדים קבוצתיים מ־Goals.Where(GroupId != null); דיווח התקדמות ל־ProgressEntries.
  - [ ] GroupGoalController ממשיך לחשוף API; ה־Service ממיר ל־Goals/ProgressEntries.
- **Depends on**: החלטה על איחוד; D2 מיגרציה.

---

## Frontend Tasks

### F1: GroupService ו־Models (קבוצות)
- **Goal**: שירות וממשקים לצ consumption של Group API.
- **Subtasks**:
  - [ ] מודלים: `GroupSummaryResponse`, `GroupDetailResponse`, `GroupMemberResponse`, `CreateGroupRequest`, `JoinGroupRequest` (ב־`models/group.models.ts` או דומה).
  - [ ] `GroupService`: `getMyGroups()`, `create()`, `join()`, `getDetail(groupId)`, `search(query)`.
- **API used**: GroupController contracts למעלה.
- **Depends on**: אין.

### F2: דף קבוצות – רשימה ויצירה/הצטרפות
- **Goal**: דף `/groups` עם רשימת הקבוצות שלי, כפתור "קבוצה חדשה", כפתור/מודל "הצטרף בקוד".
- **Subtasks**:
  - [ ] החלפת `GroupsPlaceholderComponent` ב־component חדש (למשל `GroupsDashboardComponent`).
  - [ ] טעינת `getMyGroups()` והצגת כרטיסים (שם, תיאור, מספר חברים, מספר יעדים, תפקיד).
  - [ ] ניווט ל־`/groups/:id` (פרטי קבוצה).
  - [ ] כפתור "קבוצה חדשה" → מודל/דף יצירה: שם, תיאור (אופציונלי), IsPublic; אחרי הצלחה → ניווט לפרטי הקבוצה או הצגת קוד ההזמנה.
  - [ ] כפתור "הצטרף עם קוד" → מודל הזנת InviteCode; קריאה ל־`join()`; אחרי הצלחה → רענון רשימה ו/או ניווט לפרטי הקבוצה.
- **API used**: GetMine, Create, Join.
- **Depends on**: F1.

### F3: דף פרטי קבוצה
- **Goal**: דף `/groups/:id` – פרטי קבוצה, רשימת חברים, רשימת יעדים קבוצתיים.
- **Subtasks**:
  - [ ] קומפוננטה `GroupDetailComponent`; route `groups/:id`.
  - [ ] טעינת `getDetail(groupId)`; הצגת שם, תיאור, קוד הזמנה (למנהלים), תפקיד המשתמש.
  - [ ] רשימת חברים (שם, תפקיד, תאריך הצטרפות).
  - [ ] רשימת יעדים: `GetByGroup(groupId)`; לכל יעד – כותרת, scope, תאריך יעד, האם אני משתתף; כפתור "הצטרף ליעד" אם לא משתתף.
  - [ ] כפתור "יעד קבוצתי חדש" (רק למנהל) → ניווט ל־`/groups/:groupId/goals/create` או מודל יצירת יעד.
- **API used**: GetDetail, GetByGroup; JoinGoal.
- **Depends on**: F1, F2.

### F4: יצירת יעד קבוצתי – אותו מנגנון כמו יעד אישי
- **Goal**: יצירת יעד קבוצתי באמצעות **אותו מנגנון** של יצירת יעד אישי: אותם שלבים (קטגוריה → ספר בודד → יחידת התחלה, תאריך יעד/קצב יומי), אותה חוויית משתמש.
- **Subtasks**:
  - [ ] **שימוש חוזר** ב־CreateGoalComponent (או קומפוננטת שלבים זהה) עם הקשר "קבוצה": קבלת groupId ב־route/state; שלב 1 – קטגוריה + **ספר בודד**; שלב 2 – StartUnit, TargetDate/DailyPace (כולל Dual Datepicker וחישובי קצב/תאריך כמו ביעד אישי).
  - [ ] שליחת טופס ל־GroupGoal/Create עם: GroupId, Title, BookId (יחיד), StartUnitId?, TargetDate?, DailyPace? (חוזה תואם ל־Backend לאחר B2).
  - [ ] אחרי הצלחה → ניווט לפרטי היעד הקבוצתי או לפרטי הקבוצה.
- **API used**: GroupGoal/Create (חוזה מעודכן), CatalogService, חישובי קצב/תאריך (אותם כ־create goal).
- **Depends on**: F3, B2.

### F5: דף פרטי יעד קבוצתי
- **Goal**: דף יעד קבוצתי – סיכום התקדמות, רשימת משתתפים והתקדמותם, דיווח התקדמות.
- **Subtasks**:
  - [ ] קומפוננטה `GroupGoalDetailComponent`; route למשל `groups/:groupId/goals/:goalId` או `group-goals/:id`.
  - [ ] טעינת פרטי היעד (מהתשובה של GetByGroup או endpoint עתידי GetGoalDetail אם יוגדר).
  - [ ] הצגת התקדמות המשתמש (אם משתתף), גרף/סטטיסטיקות.
  - [ ] טבלה/רשימת חברים עם התקדמות: `GetMembersProgress(groupGoalId)`.
  - [ ] כפתור "דיווח התקדמות" → **אותו מנגנון** כמו ביעד אישי (בחירת יחידות / טווח רצוף, UnitIds); קריאה ל־ReportProgress (חוזה מעודכן).
  - [ ] אם המשתמש לא משתתף – כפתור "הצטרף ליעד" (JoinGoal).
- **API used**: GetByGroup (או GetGoalDetail), GetMembersProgress, JoinGoal, ReportProgress.
- **Depends on**: F3, F4.

### F6: אינטגרציה בדף הבית וניווט
- **Goal**: לחיצה על יעד קבוצתי בדף הבית מנווטת לדף היעד הקבוצתי.
- **Subtasks**:
  - [ ] ב־Home: קישור/ניווט מ־group goal item ל־`/groups/:groupId/goals/:goalId` (או לנתיב שנבחר ב־F5).
  - [ ] וידוא ש־GroupGoalHomeItemResponse מספיק לניווט (יש id, groupId).
- **Depends on**: F5.

---

## DB Tasks

### D1: וידוא סכמת קבוצות (קבוצות + חברים)
- **סטטוס**: סקריפט `2026-02-26_d2-groups-notifications-schema.sql` קיים (Groups, GroupMembers, GroupGoals, GroupGoalBooks, GroupGoalMembers, GroupProgressEntries, Notifications).
- **פעולה**: להריץ על DB LearningTracker אם טרם הורץ.
- **אינדקסים**: מוגדרים בסקריפט (InviteCode, GroupId, UserId, וכו').

### D2: שימוש חוזר בטבלאות קיימות (יעדים קבוצתיים)

**אפשרות א – צמצום ללא איחוד:** השארת GroupGoals ו־GroupProgressEntries; צמצום ל־ספר בודד ליעד.
- [ ] GroupGoals: הוספת עמודה BookId (או הגבלה ב־לוגיקה ל־שורה אחת ב־GroupGoalBooks); הסרת/אי־שימוש ב־CategoryId אם לא רלוונטי.
- [ ] GroupProgressEntries: יישור ל־FromUnitId/ToUnitId (כמו ProgressEntries) – סקריפט מיגרציה בהתאם.

**אפשרות ב – איחוד מקסימלי (שימוש בטבלאות הקיימות):**
- [ ] Goals: הוספת GroupId INT NULL; יעד קבוצתי = שורה ב־Goals עם GroupId לא null (ללא UserId בבעלות, או שמירת CreatedByUserId).
- [ ] GoalBooks: שימוש כמו ביעד אישי – שורה אחת ליעד (ספר בודד).
- [ ] ProgressEntries: שימוש גם לדיווח קבוצתי (כבר יש GoalId, UserId, BookId, FromUnitId, ToUnitId).
- [ ] טבלת השתתפות: GoalParticipants(GoalId, UserId) – אילו משתמשים מצטרפים ליעד קבוצתי (מחליף GroupGoalMembers ביחס ל־GoalId).
- [ ] מיגרציית נתונים: העברת GroupGoals → Goals (GroupId, BookId דרך GoalBooks), GroupGoalMembers → GoalParticipants, GroupProgressEntries → ProgressEntries (GoalId מהמטרה החדשה).
- [ ] לאחר מיגרציה: הסרת GroupGoals, GroupGoalBooks, GroupGoalMembers, GroupProgressEntries (או השארה ל־rollback).
- **אינדקסים**: IX_Goals_GroupId; IX_GoalParticipants_GoalId, IX_GoalParticipants_UserId.

---

## Parallel Execution Map

| Task | Can start when |
|------|-----------------|
| D1  | עכשיו (הרצת סקריפט D2) |
| D2  | אחרי D1 (מיגרציה ל־ספר בודד / איחוד טבלאות) |
| B1  | עכשיו |
| B2  | אחרי D2 |
| B3  | אם נבחרה אפשרות ב – אחרי D2 |
| F1  | עכשיו (מבוסס על API קיים) |
| F2  | אחרי F1 |
| F3  | אחרי F1 |
| F4  | אחרי F3 ו־B2 (חוזה Create מעודכן) |
| F5  | אחרי F3, רצוי אחרי F4 |
| F6  | אחרי F5 |

---

## Acceptance Criteria

- [ ] משתמש יכול לראות את רשימת הקבוצות שלו בדף "קבוצות".
- [ ] משתמש יכול ליצור קבוצה חדשה (שם, תיאור, פומבית) ולקבל קוד הזמנה.
- [ ] משתמש יכול להצטרף לקבוצה באמצעות קוד הזמנה.
- [ ] משתמש יכול להיכנס לפרטי קבוצה ולראות חברים ויעדים קבוצתיים.
- [ ] מנהל קבוצה יכול ליצור יעד קבוצתי **באמצעות אותו מנגנון כמו יעד אישי** (קטגוריה → ספר בודד → יחידת התחלה, תאריך/קצב).
- [ ] יעד קבוצתי מוגדר **לספר אחד בלבד** (לא לקטגוריה ולא למספר ספרים).
- [ ] חבר קבוצה יכול להצטרף ליעד קבוצתי ולראות התקדמות חברים.
- [ ] משתתף ביעד קבוצתי יכול לדווח התקדמות (בחירת יחידות/טווח כמו ביעד אישי).
- [ ] לחיצה על יעד קבוצתי בדף הבית מנווטת לדף היעד הקבוצתי.

---

## הערות

- **נוטיפיקציות**: טבלת Notifications קיימת בסכמה; אינטגרציה – פאזה נפרדת.
- **דיווח התקדמות קבוצתי**: יישור ל־UnitIds ו־From/To (כמו ביעדים אישיים) – חלק מ־B2.
- **טבלאות**: מומלץ לאמץ את **אפשרות ב** (איחוד ל־Goals, GoalBooks, ProgressEntries + GoalParticipants) כדי למקסם שימוש חוזר; אם מעדיפים שינוי מינימלי – **אפשרות א** (צמצום ל־ספר בודד + יישור GroupProgressEntries).
