# .NET 10 / Linux / Docker Migration Plan

**Status:** In progress
**Branch:** `update-and-prep-for-prod`
**Last updated:** Phase 0 kickoff

> **For future agents / anyone resuming this work:** Read this document in full before making changes. It is the single source of truth for scope, locked-in architecture decisions, and what has/hasn't been done yet. Update the "Phase Status" and "Decisions Log" sections after completing any phase, even a partial one. If the session is interrupted mid-phase, leave a note under that phase describing exactly what's done and what's left.

---

## 1. Why this migration exists

The `DavidsonRFB.Attendance.Web` app (a timeclock/kiosk system for Davidson Rural Fire Brigade) is a classic ASP.NET MVC 5 application on **.NET Framework 4.8**, using:
- `System.Web` / `System.Web.Mvc` (IIS-hosted, Windows-only)
- OWIN/Katana (`Microsoft.Owin.*`) for Azure AD sign-in via **ADAL** (now fully retired by Microsoft)
- A hand-rolled `System.Web.Security.FormsAuthentication`-ticket cookie for a "kiosk mode" bypass (see section 4)
- **EntityFramework 6** (`System.Data.Entity`) against SQL Server via Windows Integrated Security
- `System.Web.Optimization`/WebGrease bundling
- `Microsoft.Azure.ActiveDirectory.GraphClient` calling the Azure AD Graph API (**retired June 2023** — already dead in production regardless of anything else)
- CI: Windows-only MSBuild/NuGet GitHub Actions workflow, no automated deploy

The owner (a systems/DevOps-minded stakeholder) wants this rebuilt to run on **Linux, in Docker, on .NET 10 (latest LTS)**, both to modernize the stack and to unblock a straightforward container-based CI/CD pipeline to Azure.

**Important side-note (explicitly out of scope for this migration):** earlier in this project's history, significant time was spent diagnosing a broken "kiosk cookie" auth flow and a UTC/week-hours calculation bug. It was later clarified those bugs belong to a **different repository** (a separate .NET Core rewrite attempt by someone else), not this one. **Do not re-open that investigation here.** However, since this migration reimplements authentication from scratch anyway, the *design intent* uncovered during that investigation (see section 4) is being carried forward faithfully into the new implementation.

---

## 2. Locked-in architecture decisions

These were explicitly decided with the repo owner and should **not** be revisited without checking with them again:

| Area | Decision | Rationale |
|---|---|---|
| Target framework | `net10.0`, SDK-style `.csproj` | Latest LTS, cross-platform |
| Data access | **Keep EF6** (netstandard2.1-compatible package), **not** EF Core | Owner wants **100% database parity** with the existing production schema for a clean cutover — no migrations, no changed conventions, no risk of EF Core translating LINQ differently |
| DB schema changes | **None.** The app must never attempt to create/alter schema against the real DB (no `DropCreateDatabaseIfModelChanges`) | Same reason — clean cutover to the existing prod DB |
| DB authentication | **SQL Authentication** (username/password), replacing Windows Integrated Security everywhere | Integrated Security doesn't work transparently from Linux without Kerberos/keytab setup; SQL auth is simplest and was already used in the old Release config anyway |
| Identity/Auth | `Microsoft.Identity.Web` for Entra ID (Azure AD) sign-in, replacing OWIN + ADAL entirely | ADAL is retired; `Microsoft.Identity.Web` is Microsoft's current supported library |
| Kiosk cookie mechanism | A second ASP.NET Core cookie authentication scheme (`"Kiosk"`), long-lived (10yr) + persistent, used via `[Authorize(AuthenticationSchemes = "Kiosk,AzureAD")]` | See section 4 for full design intent and why this replaces `FormsAuthentication` |
| User profile / Graph API feature | **Dropped entirely** (`UserProfileController`, its views, `AdalTokenCache`, `UserTokenCache`, `Microsoft.Azure.ActiveDirectory.GraphClient`) | Calls the retired Azure AD Graph API; not core functionality; owner chose to drop rather than replace with Microsoft Graph SDK |
| Static asset bundling | Dropped (`System.Web.Optimization`/WebGrease removed). Serve existing minified Content/Scripts files directly via `UseStaticFiles()` + plain `<link>`/`<script>` tags | Assets are already pre-minified/vendored; no need for a build-time bundler |
| Hosting | **Docker container**, deployed to **Azure Web App for Containers** (image pushed to Azure Container Registry) | Owner's explicit choice; portable, matches their existing Azure-oriented tooling |
| Local dev | `docker-compose` — app container + SQL Server (Linux) container, schema seeded via a plain SQL script that mirrors production schema exactly | Fully disposable, no shared state, no dependency on a real dev SQL Server |
| Test framework | **xUnit** (replacing the single ancient MSTest-v1 test) | Modern default, best tooling |
| CI/CD | Existing `.github/workflows/dotnet.yml` (Windows/MSBuild, build-only, no deploy) is **retired and replaced** with a Linux/`dotnet`/container workflow: build & test on `ubuntu-latest` (`dotnet-version: 10.0.x`) → build & push image to ACR → deploy to Azure Web App for Containers | Old workflow is fundamentally incompatible with the new project format |
| Branch | Work happens on `update-and-prep-for-prod` (already existed, 1 commit ahead of `master`) — **no new branch needed** | Owner's explicit instruction |
| Delivery style | **Phased.** Each phase below should be completed, validated, and committed somewhat independently rather than as one giant change | Owner's explicit instruction, for reviewability |

### Explicitly rejected/deferred alternatives (don't re-suggest these without new information)
- EF Core migration — rejected in favor of EF6 parity (see above).
- Keeping Windows Integrated Security via Kerberos keytab on Linux — rejected in favor of SQL auth (simpler).
- Replacing the Graph API profile feature with the modern Microsoft Graph SDK — rejected; the feature is being dropped instead.
- Plain Linux VM / Kubernetes / ACI hosting — rejected in favor of Azure Web App for Containers.
- MSTest for the ported test project — rejected in favor of xUnit.

---

## 3. Full inventory of what changes (reference)

This was compiled from a full read-through of the pre-migration codebase. Kept here so nothing gets missed.

**Project/hosting:**
- `DavidsonRFB.Attendance.Web.csproj`, `DavidsonRFB.Attendance.Web.Tests.csproj` → SDK-style, `net10.0`, `PackageReference` (no more `packages.config`)
- `Global.asax.cs`, `Startup.cs`, `App_Start/{RouteConfig,FilterConfig,BundleConfig,Startup.Auth}.cs` → collapse into `Program.cs`
- `Web.config`, `Web.Debug.config`, `Web.Release.config`, `Views/Web.config` → `appsettings.json` / `appsettings.Development.json` / `_ViewImports.cshtml`

**Auth (full rewrite — see section 4):**
- `App_Start/Startup.Auth.cs`, `Attributes/LocalAuthorizationFilter.cs`, `Helpers/Brigade.cs`, `Controllers/AccountController.cs`, `Models/AdalTokenCache.cs` (drop), `Models/UserTokenCache.cs` (drop), `Controllers/UserProfileController.cs` (drop), `Views/UserProfile/*` (drop), `Views/Manage/*` (drop — leftover from pre-AAD ASP.NET Identity, already unused)

**Data layer (port near-verbatim, EF6 kept):**
- `DAL/AttendanceContext.cs`, `DAL/AttendanceInitializer.cs` (repurpose as local-dev seed only), `Models/AttendanceModels.cs`

**Controllers & views (mechanical port to `Microsoft.AspNetCore.Mvc`):**
- `Controllers/{HomeController,AttendanceController,EmployeeController,ErrorController}.cs`
- `Views/{Home,Attendance,Employee,Error,Account,Shared}/**`

**Static assets:**
- `App_Start/BundleConfig.cs` → deleted
- `Content/**`, `Scripts/**` → served as-is via static files

**Observability:**
- `Microsoft.ApplicationInsights.Web` → `Microsoft.ApplicationInsights.AspNetCore`

**Tests:**
- `DavidsonRFB.Attendance.Web.Tests/Controllers/HomeControllerTest.cs` → xUnit, SDK-style test project

**CI/CD:**
- `.github/workflows/dotnet.yml` → replaced with Linux/container-based workflow

---

## 4. Kiosk authentication — design intent (important context, don't relitigate)

This was worked out in detail with the owner before the migration was scoped. The **intended behavior** (which must be preserved in the new implementation) is:

1. **`Home/Index` and `ClockOut`** (the actual clock-in/out screen) must be accessible via **either**:
   - A valid Azure AD (Entra ID) session, **or**
   - A long-lived (10-year) persistent "kiosk" cookie — this lets a physically-secured kiosk terminal stay "signed in" indefinitely without ever re-prompting for AAD login.
2. **`CreateCookie` and `RemoveCookie`** (the actions that mint/revoke the kiosk cookie) must **strictly require AAD authentication**. This is intentional and must not be relaxed — only an authenticated staff member should be able to provision a kiosk.
3. **Every other page** (`Employee`, `Attendance` maintenance, etc.) requires AAD authentication only — the kiosk cookie must never grant access beyond the Home/ClockOut pages.

**New implementation approach:**
- Two ASP.NET Core authentication schemes are registered: the AAD scheme (via `Microsoft.Identity.Web`) and a `"Kiosk"` cookie scheme (`AddCookie("Kiosk", ...)`) with a long `ExpireTimeSpan` and `IsPersistent = true`.
- `Home/Index` and `ClockOut` are decorated `[Authorize(AuthenticationSchemes = "Kiosk,AzureAD")]` — ASP.NET Core's multi-scheme authorization succeeds if **either** scheme authenticates the request, which is exactly the OR semantics the old `LocalAuthorizationFilter` was manually implementing.
- `CreateCookie`/`RemoveCookie` stay `[Authorize(AuthenticationSchemes = "AzureAD")]` only, and on success call `HttpContext.SignInAsync("Kiosk", ...)` / `SignOutAsync("Kiosk")`.
- The old ticket's `BrigadeId`-in-`Name` trick is replaced with a claim (e.g. `brigade_id`) on the Kiosk cookie's `ClaimsPrincipal`, resolved via a small helper (replacing `Helpers/Brigade.cs`) that checks either the AAD-authenticated employee's brigade or the Kiosk cookie's claim, whichever is present.
- **Data Protection key persistence matters here**: ASP.NET Core's cookie auth encrypts tickets using the Data Protection API. For the Kiosk cookie to survive container restarts/redeploys, the Data Protection key ring must be persisted to a durable location (a mounted volume in the container, at minimum) rather than the ephemeral in-container default.

This is a cleaner, more idiomatic mechanism than the old manual `FormsAuthentication.Decrypt` approach and should not need any further redesign — just correct implementation.

---

## 5. Phase breakdown & status

Update the status line and add notes under each phase as work happens.

### Phase 0 — Scaffolding
**Status:** In Progress
**Deliverables:** New SDK-style `.csproj`s (web + tests), `Program.cs` skeleton, `appsettings.json`/`appsettings.Development.json`, `Dockerfile` (multi-stage), `docker-compose.yml` (app + SQL Server Linux container), `.dockerignore`. Remove `packages.config`, old `App_Start/*`, `Global.asax*`.
**Notes:** —

### Phase 1 — Data layer
**Status:** Not started
**Deliverables:** Port `Models/AttendanceModels.cs`, `DAL/AttendanceContext.cs`, `DAL/AttendanceInitializer.cs` (repurposed as local-dev-only seed, never run against prod) to EF6-on-net10. Confirm ADO.NET provider (`Microsoft.Data.SqlClient`) works with EF6's SQL Server provider on Linux. Switch connection strings to SQL auth. Produce a plain SQL schema script for the docker-compose seed DB matching production schema exactly.
**Notes:** —

### Phase 2 — Authentication
**Status:** Not started
**Deliverables:** `Microsoft.Identity.Web` AAD wiring; `"Kiosk"` cookie scheme with Data Protection key persistence to a mounted volume; ported `CreateCookie`/`RemoveCookie`; replacement for `LocalAuthorizationFilter`/`Helpers/Brigade.cs` per section 4.
**Notes:** —

### Phase 3 — Controllers & views
**Status:** Not started
**Deliverables:** Port `HomeController`, `AttendanceController`, `EmployeeController`, `AccountController`, `ErrorController` and all `Views/**` to `Microsoft.AspNetCore.Mvc`/Razor.
**Notes:** —

### Phase 4 — Static assets
**Status:** Not started
**Deliverables:** Direct `<link>`/`<script>` references to existing minified files; remove `BundleConfig`/bundling packages.
**Notes:** —

### Phase 5 — Observability
**Status:** Not started
**Deliverables:** `Microsoft.ApplicationInsights.AspNetCore` wiring, replacing the old HTTP-module-based package.
**Notes:** —

### Phase 6 — Docker & CI/CD
**Status:** Not started
**Deliverables:** Finalized `Dockerfile`/`docker-compose.yml`; new GitHub Actions workflow (build & test on `ubuntu-latest`, `dotnet-version: 10.0.x`, build & push image to ACR, deploy to Azure Web App for Containers). Old `.github/workflows/dotnet.yml` removed.
**Notes:** —

### Phase 7 — Tests
**Status:** Not started
**Deliverables:** Port `HomeControllerTest` to xUnit on an SDK-style `net10.0` test project.
**Notes:** —

### Phase 8 — Validation & cutover
**Status:** Not started
**Deliverables:** Schema-parity check against prod DB, smoke test of clock in/out, kiosk cookie flow, AAD sign-in, Employee/Attendance maintenance pages. Cutover checklist: rotate DB credentials to the new SQL login, decommission old kiosks/servers.
**Notes:** —

---

## 6. Decisions log

Chronological record of decisions made during implementation that aren't already captured above (e.g. specific package versions chosen, deviations discovered mid-phase, etc.). Add an entry whenever a non-trivial implementation choice is made that a future agent would need to know about.

- *(none yet — add entries here as phases progress)*

---

## 7. Open questions / things to confirm later

- Exact Azure Container Registry name / resource group / Azure Web App name for this project (not yet created/confirmed).
- Whether the existing production SQL Server will get a new SQL login created for this app, or reuse an existing one.
- Final decommission timeline for the old Framework-based deployment(s) once cutover is validated.
