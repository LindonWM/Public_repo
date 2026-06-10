# SkillSnap

SkillSnap is a portfolio showcase app that helps you present people, projects, and skills in one place. It has a clean Blazor front end, a secure ASP.NET Core API, and an admin area for managing content without having to edit the database by hand.

## Project Summary

The application is designed as a simple portfolio showcase with a secure admin workflow.

The backend exposes REST-style endpoints for:
- authentication and registration
- listing portfolio users
- listing, creating, updating, and deleting projects
- listing, creating, updating, and deleting skills
- seeding sample data for development and demos

The client is a Blazor WebAssembly UI that consumes those endpoints and renders portfolio cards, project cards, and skill tags. Admin users can manage content directly from the interface without leaving the app.

## Key Features

### Authentication and Security

- Users can register and log in with ASP.NET Core Identity.
- After login, the app stores a JWT token in the browser so the session survives page refreshes.
- The client restores that session on startup and automatically sends the token with API calls.
- Admin-only actions are locked behind role checks, so regular users can browse but not edit content.
- The app also creates a default admin role and admin account during startup for local development.

### CRUD Support

Projects:
- browse the full project list
- add a new project as an admin
- edit a project directly in the UI
- delete a project when it is no longer needed

Skills:
- browse all skills attached to portfolio users
- add a new skill as an admin
- edit a skill inline without leaving the page
- delete a skill from the same card view

Portfolio users:
- view the people behind the projects and skills
- pick the right owner when creating or editing related items

### Caching

- The project and skill lists are cached in memory on the API side.
- This keeps repeated reads fast, especially when the same page is opened more than once.
- Any create, update, or delete action clears the cache so the UI stays accurate.
- Cache hit and miss logs make it easy to see when the app is serving fresh data versus cached data.

### Client State

- `AuthService` keeps track of whether the user is logged in and which roles they have.
- `UserSessionService` shares that session information with the rest of the Blazor app.
- The UI uses role checks to decide when to show admin controls.
- Edit forms keep their own temporary state so changes can be reviewed before saving.

### UI Behavior

- Profile cards introduce each portfolio owner with a photo, name, and short bio.
- Project cards show the project details together with the person connected to that work.
- Skill cards show the skill name, level, and who it belongs to.
- Admin actions appear right on the card, so editing feels quick and direct.
- Scoped CSS files keep each component’s styling neatly separated.

## Architecture Overview

The solution is split into three main projects:

- `SkillSnap` - server/API and shared hosting shell
- `SkillSnap.Client` - Blazor WebAssembly front end
- `SkillSnap.Shared` - shared models used by both sides

Important backend pieces:
- `AuthController` for register/login
- `ProjectsController` for project CRUD
- `SkillsController` for skill CRUD
- `PortfolioUsersController` for listing portfolio owners
- `SeedController` for development seed data

Important client pieces:
- `ProjectList.razor`
- `SkillTags.razor`
- `ProfileCard.razor`
- `AuthService`
- `ProjectService`
- `SkillService`
- `PortfolioUserService`
- `UserSessionService`

## Development Process

This project was developed iteratively.

1. Started with the core data model and API controllers.
2. Added authentication with JWT and seeded admin access for local development.
3. Built the Blazor client to consume the API.
4. Added in-memory caching for the list endpoints to improve repeated reads.
5. Extended the UI with inline admin edit/delete flows.
6. Refined the display so related names are shown instead of raw foreign key IDs.
7. Moved component styling into scoped `.razor.css` files to keep markup cleaner.
8. Added explanatory comments to the more complex pieces of caching, auth, and update flows.

## Use of Copilot

GitHub Copilot was used throughout the project as a coding assistant to speed up implementation and cleanup.

Where it helped most:
- generating initial controller and service patterns
- proposing consistent CRUD method shapes
- filling in repetitive validation and error-handling code
- helping refactor UI pages into reusable helper methods
- suggesting scoped CSS organization for Blazor components
- drafting documentation structure for this README

How Copilot was used effectively:
- I kept edits incremental instead of asking for large rewrites at once.
- I verified each meaningful change with error checks.
- I used Copilot to reduce boilerplate, then adjusted the output to match the app’s actual behavior.
- I asked for cleanup suggestions after features were in place so the code stayed readable.

## Known Issues

- Portfolio users are currently list-only from the API side; there is no create/update/delete UI for them.
- The admin seed credentials are stored in configuration for local development and should be replaced with environment-based secrets for production.
- The project uses in-memory caching, so cache state is lost when the app restarts.
- Inline editing is convenient, but it is still a simple form-based workflow and does not yet include advanced validation UX.
- Delete actions do not yet show a confirmation dialog, so accidental clicks are possible.

## Future Improvements

- Add full CRUD support for portfolio users.
- Replace the seeded admin password in configuration with secure secret storage.
- Add delete confirmation dialogs and a toast/notification system.
- Introduce shared request DTOs for create/update operations instead of passing full entity models.
- Move repeated admin UI patterns into reusable Blazor components.
- Add better form validation messages and client-side field feedback.
- Add pagination or filtering if the project list grows larger.
- Add automated tests for auth, CRUD endpoints, and cache invalidation.
- Consider a more advanced cache strategy if the dataset grows or needs distributed storage.

## Setup and Run

Requirements:
- .NET 10 SDK
- SQLite

Run the app:

```powershell
dotnet run --project SkillSnap\SkillSnap.csproj
```

The app seeds an admin role and default admin user on startup if they are missing.

Default development admin:
- Email: `admin@skillsnap.dev`
- Password: `Admin@123!`

## Seed Data

You can populate or reset sample data with the seed endpoint:

```powershell
Invoke-RestMethod -Method Post http://localhost:5272/api/seed
```

This creates sample portfolio users, projects, and skills so the UI has data to display immediately.

## Notes

- Project and skill list endpoints are cached for short periods to improve repeated reads.
- Admin changes invalidate the cache so the UI stays fresh after edits.
- The current UI emphasizes readability and quick admin workflows over heavy interaction design.

## Cache Performance Verification

The following measurements were recorded while verifying the in-memory caching on the project and skill list endpoints.

### Changes Implemented During Verification

- Added Stopwatch-based duration measurement in:
	- `ProjectsController.GetProjects()`
	- `SkillsController.GetSkills()`
- Added cache hit/miss logs in both GET endpoints.
- Kept cache invalidation on create/update/delete operations using `_cache.Remove(...)`.

### Manual Test Method

1. Started the API.
2. Seeded the database.
3. Measured endpoint response times from PowerShell with `Stopwatch`.
4. Called each endpoint once for a cold read and multiple times for warm reads.

Example command used during testing:

```powershell
$base = 'http://localhost:5272'
Invoke-RestMethod -Method Post "$base/api/seed" | Out-Null

# Projects: first call (cold), then 5 warm calls
# Skills: first call (cold), then 5 warm calls
```

### Measured Load Times (Client Side)

- Projects cold call (cache miss): **129.20 ms**
- Projects warm average (cache hit): **1.46 ms**
- Projects warm samples: `4.18, 1.02, 0.77, 0.71, 0.64`

- Skills cold call (cache miss): **10.98 ms**
- Skills warm average (cache hit): **0.66 ms**
- Skills warm samples: `0.76, 0.66, 0.64, 0.61, 0.64`

### Improvement Summary

- Projects: from 129.20 ms to 1.46 ms, about **98.87% faster**
- Skills: from 10.98 ms to 0.66 ms, about **93.99% faster**

### Cache Hit/Miss Verification (Server Logs)

Observed logs confirmed cache behavior:

- Projects:
	- `Projects GET cache MISS. Count=2, DurationMs=42`
	- `Projects GET cache HIT...` repeated **5** times

- Skills:
	- `Skills GET cache MISS. Count=2, DurationMs=4`
	- `Skills GET cache HIT...` repeated **5** times

This confirmed caching was active and effective for repeated GET requests.
