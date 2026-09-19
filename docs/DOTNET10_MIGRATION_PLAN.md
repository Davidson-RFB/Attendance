# .NET 10 / Linux / Docker Migration Plan

**Status:** In progress
**Branch:** `update-and-prep-for-prod`
**Last updated:** Phase 2 complete

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
**Status:** ✅ Done
**Deliverables:** New SDK-style `.csproj`s (web + tests), `Program.cs` skeleton, `appsettings.json`/`appsettings.Development.json`, `Dockerfile` (multi-stage), `docker-compose.yml` (app + SQL Server Linux container), `.dockerignore`. Remove `packages.config`, old `App_Start/*`, `Global.asax*`.
**Notes:**
- **Structural approach:** the entire old Framework 4.8 codebase was moved to `legacy/DavidsonRFB.Attendance.Web/` and `legacy/DavidsonRFB.Attendance.Web.Tests/` (via `git mv`, so history is preserved) rather than trying to transform the existing project folders in place. Reasoning: SDK-style projects use implicit file globbing, so leaving old `System.Web.Mvc`-based `.cs`/`.cshtml` files in the new project directories would break the build immediately. Keeping `legacy/` around lets later phases port logic from a working reference until cutover (Phase 8), at which point `legacy/` is deleted entirely. **`legacy/` is excluded from the Docker build context (`.dockerignore`) and is not part of the active solution** — it's reference-only, not built by CI.
- New `DavidsonRFB.Attendance.Web/` and `DavidsonRFB.Attendance.Web.Tests/` were scaffolded via `dotnet new mvc`/`dotnet new xunit` (net10.0) rather than hand-written from scratch, to get a known-good, idiomatic starting skeleton (correct `Program.cs` wiring, `appsettings.json` layout, etc.) which was then customized.
- **Solution file format:** `dotnet new sln` on the .NET 10 SDK defaults to the new `.slnx` (XML) format, not the classic `.sln`. Went with it (`DavidsonRFB.Attendance.slnx`) rather than forcing the legacy format — it's fully supported by `dotnet build`/CI and is simpler. Anything referencing "the solution file" from here on means `DavidsonRFB.Attendance.slnx`, not `.sln`.
- `appsettings.json` has placeholder sections stubbed in for `ConnectionStrings:AttendanceContext`, `AzureAd` (for `Microsoft.Identity.Web`, wired in Phase 2), and `ApplicationInsights` (wired in Phase 5) — all empty, **no real secrets committed**. `appsettings.Development.json` has a real (but throwaway, local-only) SQL auth connection string matching the `docker-compose.yml` `db` service's SA password, since that's not a secret worth protecting.
- Template's default `wwwroot/lib/*` (libman-fetched bootstrap/jquery/jquery-validation) was deleted — Phase 4 will bring in our own already-vendored, already-minified assets from `legacy/.../Content` and `legacy/.../Scripts` instead of re-fetching different versions via libman.
- The template's placeholder `HomeController`/`Views/Home/{Index,Privacy}.cshtml`/`Views/Shared/Error.cshtml` are still the generic scaffolded ones (not yet the real app) — these get replaced with the ported versions in Phase 3. Right now the app is a "does it boot and serve a 200" skeleton, not yet functional Attendance app.
- **Verified:** `dotnet build DavidsonRFB.Attendance.slnx` succeeds (0 warnings/errors); `dotnet run` serves HTTP 200 on `/`; `docker build` against `DavidsonRFB.Attendance.Web/Dockerfile` succeeds and the resulting container serves HTTP 200 on port 8080. A Data Protection warning about non-persisted keys appears in the container logs as expected — this is exactly the concern Phase 2 addresses (key ring needs to be persisted to a mounted volume for the Kiosk cookie to survive container restarts).
- Docker image build context is the **repo root** (not the project folder), so the `Dockerfile` can `COPY` the solution file and both project folders for restore; `docker-compose.yml`'s `build.context` reflects this.

### Phase 1 — Data layer
**Status:** ✅ Done
**Deliverables:** Port `Models/AttendanceModels.cs`, `DAL/AttendanceContext.cs`, `DAL/AttendanceInitializer.cs` (repurposed as local-dev-only seed, never run against prod) to EF6-on-net10. Confirm ADO.NET provider works with EF6's SQL Server provider on Linux. Switch connection strings to SQL auth. Produce a plain SQL schema script for the docker-compose seed DB matching production schema exactly.
**Notes:**
- **`Models/AttendanceModels.cs` and `DAL/AttendanceContext.cs` are ported byte-for-byte in behavior** (only `#nullable disable` added, since these are EF6 POCOs never written against nullable reference types, plus doc comments). No logic changes, including the known `DateTime.Now`-based `WeekHrs`/`TotalHours` computed properties - not in scope here, not touched.
- **ADO.NET provider decision (deviates from the original Phase-planning assumption of `Microsoft.Data.SqlClient`):** used `System.Data.SqlClient` instead. Reasoning: EF6's `System.Data.Entity.SqlServer.SqlProviderServices` is built around the classic `System.Data.SqlClient` provider; that combination is the documented, low-risk path for "EF6 without a Framework/app.config host" (see `EntityFramework` NuGet package's own transitive dependency - installing `EntityFramework` 6.5.2 pulls in `System.Data.SqlClient` 4.8.6 automatically). `System.Data.SqlClient` is in maintenance mode but fully functional on Linux (verified below). Revisit only if `System.Data.SqlClient` support actually gets pulled by Microsoft - not currently a concern.
- **EF6-without-app.config wiring** (this doesn't exist in any Microsoft doc as a clean recipe - assembled from EF6 source behavior): a `[DbConfigurationType(typeof(AttendanceDbConfiguration))]`-decorated `AttendanceContext`, where `AttendanceDbConfiguration : DbConfiguration` explicitly calls `SetProviderServices`/`SetProviderFactory`/`SetDefaultConnectionFactory` in its constructor (replacing the old `<entityFramework><providers>` Web.config section). Additionally, `Program.cs` must call `System.Data.Common.DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance)` at startup, since .NET (Core)/.NET 5+ doesn't populate `DbProviderFactories` from machine.config the way .NET Framework does.
  - **Gotcha hit and fixed:** `Database.SetInitializer<AttendanceContext>(null)` must NOT be called from inside `AttendanceDbConfiguration`'s constructor - doing so re-enters `DbConfigurationManager` while it's still being constructed and throws `InvalidOperationException: ValueFactory attempted to access the Value property of this instance`. It's called from `Program.cs` instead, after the provider registration, before any `AttendanceContext` is constructed.
- **Connection string handling:** EF6's classic `base("AttendanceContext")` constructor (resolving a *name* against `<connectionStrings>` in Web.config) has no equivalent in ASP.NET Core. `AttendanceContext` now takes the connection string *value* directly via its constructor; `Program.cs` registers it in DI as `AddScoped(...)`, resolving the value from `IConfiguration.GetConnectionString("AttendanceContext")` (i.e. `appsettings.json` / `ConnectionStrings__AttendanceContext` env var / user secrets), throwing a clear `InvalidOperationException` at first use if it's unset.
- **`DAL/AttendanceInitializer.cs` was NOT ported as a C# class.** Its job (seed data for a fresh DB) is superseded by plain SQL scripts (`db/seed/01-schema.sql`, `db/seed/02-seed-data.sql`) run by a new `db-init` one-shot service in `docker-compose.yml` (`db/init-db.sh`) - the `mssql/server` image has no built-in `docker-entrypoint-initdb.d` support like postgres/mysql images do, hence the custom wait-then-`sqlcmd` script. This keeps EF6 completely out of schema/seed responsibilities in every environment (dev and prod alike), consistent with "the app must never attempt schema changes against a real DB."
- **Schema caveat (flagged for Phase 8):** `db/seed/01-schema.sql` is a best-effort reconstruction of the production schema derived purely from the EF6 model's conventions (no access to the real prod DB during this migration). Table/column nullability and types (e.g. `NVARCHAR(MAX)`, `DATETIME`) follow EF6 defaults for the annotations present on each property. **This must be diffed against the real production schema before cutover** - added to the Open Questions list below.
- **Verified for real, not just "should work":** stood up a disposable SQL Server 2022 Linux container, applied `db/seed/*.sql` against it, then ran a throwaway console app referencing the new `AttendanceContext` (net10.0) that: queried `Employees`/`Brigades` via LINQ, inserted a new `Attendance` row via `SaveChanges()`, and re-queried `WeekHrs`/`TotalHours` (the ported computed properties) via `.Include()` - all correct. This is real proof EF6 6.5.2 works end-to-end on .NET 10 on Linux, not just "the packages restored."
- `dotnet build`/`dotnet test` both still pass after these changes.

### Phase 2 — Authentication
**Status:** ✅ Done
**Deliverables:** `Microsoft.Identity.Web` AAD wiring; `"Kiosk"` cookie scheme with Data Protection key persistence to a mounted volume; ported `CreateCookie`/`RemoveCookie`; replacement for `LocalAuthorizationFilter`/`Helpers/Brigade.cs` per section 4.
**Notes:**
- **Scheme naming, mapped onto real Microsoft.Identity.Web/ASP.NET Core constants** (the plan doc's section 4 used a placeholder name "AzureAD" when this was originally designed - the actual scheme name is `CookieAuthenticationDefaults.AuthenticationScheme` = `"Cookies"`, registered by `AddMicrosoftIdentityWebApp`, which holds the AAD session after a successful OIDC sign-in; `"OpenIdConnect"` is the separate challenge-only scheme that redirects to Entra ID). `Auth/KioskAuthentication.cs` defines `HomeAndKioskSchemes = "Kiosk,Cookies"` as the combined list used on `[Authorize(AuthenticationSchemes = ...)]`.
- **`Home/Index` now carries `[Authorize(AuthenticationSchemes = KioskAuthentication.HomeAndKioskSchemes)]`** even though Index's *body* is still the Phase 0 placeholder - the auth gate is genuinely final/Phase-2-complete; Phase 3 only needs to replace the view/logic, not touch the attribute. `ClockOut` will need the same attribute once it exists in Phase 3.
- **Real bug caught and fixed via live testing:** naming both `"Kiosk"` and `"Cookies"` in `AuthenticationSchemes` means ASP.NET Core challenges **both by name** when neither authenticates. The `"Cookies"` scheme, if not explicitly configured, has its own default (unconfigured) `LoginPath` ("/Account/Login", which doesn't exist in this app) and was clobbering the correct AAD redirect that the Kiosk scheme's own `OnRedirectToLogin` had already set up. Fixed by also configuring `CookieAuthenticationOptions.Events.OnRedirectToLogin` (via `services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, ...)`) to forward to the OpenIdConnect challenge, same as the Kiosk scheme does. Caught by literally running the app and curling `/` unauthenticated - it redirected to a dead `/Account/Login` path before the fix.
- **`CreateCookie` resolves the signed-in user's email via the `preferred_username` claim** (falling back to `User.Identity.Name`), not `User.Identity.Name` alone like the original app - `preferred_username` is the reliable AAD v2/Microsoft.Identity.Web claim for the user's UPN/email; relying on `Identity.Name`'s mapping alone was a known fragility point in the old ADAL/JWT stack (claim-type-mapping behavior changes across major library versions) that this migration deliberately avoids repeating.
- **`Helpers/Brigade.cs` replaced by `Helpers/CurrentBrigade.cs`** - a static method taking `(ClaimsPrincipal user, AttendanceContext db)` explicitly instead of reading `HttpContext.Current`/`Request.Cookies` (which don't exist in ASP.NET Core). Checks the Kiosk cookie's `davidsonrfb:brigade_id` claim first (kiosk session, no employee lookup needed/possible), then falls back to looking up the AAD-authenticated employee's `BrigadeId` by email - same precedence as the original.
- **Data Protection key persistence** is config-driven (`DataProtection:KeyRingPath`): unset in base `appsettings.json`'s committed value is `/keys` (the Docker container path, backed by a new named volume `attendance-dataprotection-keys` in `docker-compose.yml`); `appsettings.Development.json` overrides it to a local `.dataprotection-keys/` folder for plain `dotnet run` outside Docker (gitignored). `docker-compose.yml`'s `web` service explicitly sets `DataProtection__KeyRingPath=/keys` as an env var so the Development-environment override inside the container doesn't win over the mounted volume path.
- `docker-compose.yml`'s `web` service also gained dummy `AzureAd__TenantId`/`AzureAd__ClientId` env vars so the stack boots without crashing before real AAD app registration values exist - these are placeholder GUIDs, not secrets, and must be replaced with real values (via `.env`/secrets) before real sign-in will work. Flagged in Open Questions.
- **Verified for real, end-to-end, against live infrastructure (not just "should work"):**
  - `dotnet run` locally with a real, public AAD tenant alias (`common`) and a dummy client id: unauthenticated `GET /Home/CreateCookie` correctly 302-redirects to a genuine, fully-formed `https://login.microsoftonline.com/common/oauth2/v2.0/authorize?...` URL (proves the AAD-only challenge path end-to-end).
  - Unauthenticated `GET /` (Index, multi-scheme `Kiosk,Cookies`) - reproduced the clobbering bug above, fixed it, then reverified it now correctly redirects to the same genuine AAD authorize URL instead of the dead `/Account/Login` path.
  - Rebuilt the actual Docker image and re-ran the same unauthenticated `GET /` check against the container (with `/keys` as the mounted key path) - same correct redirect, and the Data Protection "keys may not persist" warning from Phase 0 is gone (confirming the key-path wiring works).
  - `Helpers/CurrentBrigade.Resolve` verified directly (no HTTP needed) with a hand-built Kiosk-scheme `ClaimsPrincipal` (returns the claimed brigade id, no DB access attempted) and an anonymous principal (returns `null`, no exception).
  - **Not yet verified (deferred to Phase 8, requires a real AAD app registration):** a full real sign-in round trip, `CreateCookie` actually minting a working Kiosk cookie, and a subsequent request succeeding via that cookie alone. The mechanics (`HttpContext.SignInAsync`/cookie auth) are standard, well-trodden ASP.NET Core APIs, but this hasn't been exercised against a real identity provider in this session.
- `dotnet build`/`dotnet test` both pass; Docker image builds and runs correctly.

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

- **Phase 0:** Old Framework 4.8 codebase moved wholesale to `legacy/` (via `git mv`, history preserved) rather than transformed in place, to avoid SDK-style implicit-glob build breakage while porting incrementally. `legacy/` is excluded from Docker build context and the active solution; deleted at Phase 8 cutover.
- **Phase 0:** Solution file is `DavidsonRFB.Attendance.slnx` (new XML format, .NET 10 SDK default), not classic `.sln`.
- **Phase 0:** New projects scaffolded via `dotnet new mvc`/`dotnet new xunit` then customized, rather than hand-authored from scratch.
- **Phase 1:** ADO.NET provider is `System.Data.SqlClient`, not `Microsoft.Data.SqlClient` - required for EF6's `SqlProviderServices` to work cleanly; see Phase 1 notes for the full reasoning.
- **Phase 1:** `AttendanceInitializer.cs` was not ported as C# - replaced by `db/seed/*.sql` + a `db-init` docker-compose service, keeping EF6 fully out of schema/seed duties in all environments.
- **Phase 1:** Connection string is passed as a literal value to `AttendanceContext`'s constructor (resolved from `IConfiguration` in `Program.cs`), not a config-section name lookup like the original `base("AttendanceContext")` - `<connectionStrings>`/`ConfigurationManager` don't exist in ASP.NET Core.
- **Phase 2:** The AAD session scheme is literally named `"Cookies"` (`CookieAuthenticationDefaults.AuthenticationScheme`, registered by `AddMicrosoftIdentityWebApp`), not `"AzureAD"` as originally sketched in section 4 - `Auth/KioskAuthentication.HomeAndKioskSchemes` is the source of truth for the actual combined scheme string used on `[Authorize]`.
- **Phase 2:** Both the `"Kiosk"` and `"Cookies"` schemes need their `Events.OnRedirectToLogin` explicitly forwarded to an `OpenIdConnect` challenge - naming a cookie scheme directly in a multi-scheme `[Authorize(AuthenticationSchemes = ...)]` causes ASP.NET Core to challenge it directly by name, bypassing the "default challenge scheme" convention that normally handles this automatically for plain `[Authorize]`. Found and fixed via live testing, not by inspection.
- **Phase 2:** `CreateCookie` resolves the signed-in user's identity via the `preferred_username` claim (falling back to `Identity.Name`), not `Identity.Name` alone, to avoid the claim-type-mapping fragility that affected the old ADAL/JWT stack.

---

## 7. Open questions / things to confirm later

- Exact Azure Container Registry name / resource group / Azure Web App name for this project (not yet created/confirmed).
- Whether the existing production SQL Server will get a new SQL login created for this app, or reuse an existing one.
- Final decommission timeline for the old Framework-based deployment(s) once cutover is validated.
- **`db/seed/01-schema.sql` has not been validated against the real production schema** (no DB access during this migration session) - it's reconstructed from the EF6 model's conventions only. Before cutover (Phase 8), diff this script's tables/columns/types against the actual production database and correct any discrepancies (nullable columns, exact data types, missing indexes/constraints, etc.) - this is the single biggest risk to "100% database parity."
- **No real Entra ID (AAD) app registration exists yet for the new app.** Should this reuse the existing `ida:ClientId`/tenant from the legacy app's `Web.config` (same tenant `74023b68-c430-409d-a0c0-76548ddc20b1`), or should a fresh app registration be created for the new .NET 10 app (recommended, since the redirect URIs/callback paths will differ from the old `https://localhost:44321/signin-oidc` mess)? Needed before Phase 8's real sign-in validation can happen.
- A full real AAD sign-in → `CreateCookie` → Kiosk-cookie-only-request round trip has not been exercised against a live tenant (only the challenge/redirect half has been verified for real - see Phase 2 notes). Do this as part of Phase 8.
