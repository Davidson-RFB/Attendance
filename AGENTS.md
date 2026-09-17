# AGENTS.md

## What this repo is

`DavidsonRFB.Attendance` is the timeclock/attendance kiosk system for Davidson Rural Fire Brigade — a small ASP.NET web app used to clock volunteers in/out of shifts, plus basic employee/attendance maintenance screens, with Azure AD (Entra ID) sign-in for staff.

## Active migration — read this first

This repo is **mid-migration** from classic ASP.NET Framework 4.8 (MVC 5, `System.Web`, IIS/Windows-only) to **.NET 10, running in Docker on Linux**, deployed to Azure Web App for Containers.

**Before making any change to this repo, read [`docs/DOTNET10_MIGRATION_PLAN.md`](docs/DOTNET10_MIGRATION_PLAN.md) in full.** It is the authoritative, continuously-updated source of truth for:
- Why this migration is happening
- Every locked-in architecture decision (and which alternatives were explicitly rejected — don't re-suggest them)
- The full phase-by-phase breakdown and current status of each phase
- Detailed design intent for the kiosk authentication mechanism (easy to get wrong / re-litigate if you don't read the context first)
- A decisions log and open questions list

Work is happening on branch `update-and-prep-for-prod` (not `master`).

## Rules for continuing this work

1. Do not restart or second-guess a locked-in decision in the migration plan without checking with the repo owner again — several were deliberated at length (e.g. EF6 vs EF Core, SQL auth vs Integrated Security, dropping the Graph API profile feature).
2. After completing any phase (fully or partially), update the corresponding phase's **Status** and **Notes** in `docs/DOTNET10_MIGRATION_PLAN.md`, and add an entry to its **Decisions log** for any non-trivial implementation choice made along the way.
3. If a session is interrupted mid-phase, leave the plan doc in a state where the next agent can tell exactly what's done and what's left — don't leave it saying "In Progress" with no notes.
4. Follow the phased delivery approach — each phase should be a reviewable, roughly self-contained unit of work rather than one giant change.
