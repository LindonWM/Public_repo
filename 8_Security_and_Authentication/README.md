# SafeVault — Setup & Usage Guide

## What the app does

SafeVault is an ASP.NET Core Razor Pages web application that demonstrates secure coding practices:

- **Input sanitization** — strips HTML tags and enforces validation rules on username, email, and password before any processing occurs (`Helpers/InputSanitizer.cs`)
- **Parameterized SQL queries** — user-supplied values are never concatenated into SQL strings; they are passed as typed parameters via `MySqlConnector` (`Data/MySqlUserRepository.cs`)
- **BCrypt password hashing** — passwords are stored as BCrypt hashes (work-factor 12); the hash is never transmitted to the client and verification runs in application code, not SQL (`Helpers/PasswordHasher.cs`)
- **Cookie-based authentication** — successful login issues a signed HttpOnly session cookie; claims include username, email, and role
- **Role-based authorization** — the Privacy page is restricted to the `Admin` role via `[Authorize(Roles = "Admin")]`; the nav link is hidden for non-admins and direct URL access redirects to home with an access-denied message
- **Anti-forgery protection** — all POST forms include a hidden CSRF token automatically managed by ASP.NET Core
- **Model validation** — data annotations (`[Required]`, `[StringLength]`) validate input server-side even if client-side checks are bypassed

---

## Prerequisites

| Tool | Minimum version | Download |
|---|---|---|
| .NET SDK | 10.0 | https://dotnet.microsoft.com/download |
| Docker Desktop | any recent | https://www.docker.com/products/docker-desktop |
| MySQL Shell for VS Code | any | VS Code Extensions marketplace |

---

## 1. Clone / open the project

Open the folder `SafeVault` in VS Code. All commands below are run from a terminal inside that folder.

---

## 2. Start the MySQL database (Docker)

Run this once to create and start a MySQL 8.4 container:

```powershell
docker run --name safevault-mysql `
  --restart always `
  -e MYSQL_ROOT_PASSWORD=rootpass `
  -e MYSQL_DATABASE=SafeVault `
  -e MYSQL_USER=app_user `
  -e MYSQL_PASSWORD=change_me `
  -p 3306:3306 `
  -d mysql:8.4
```

`--restart always` means the container starts automatically whenever Docker Desktop (or Windows) starts — you do not need to run this command again.

### Verify the container is running

```powershell
docker ps --filter "name=safevault-mysql"
```

You should see `safevault-mysql` with status `Up`.

---

## 3. Create the database schema and seed data

Wait about 15–20 seconds after starting the container for MySQL to initialise, then run:

```powershell
Get-Content .\Data\database.sql | docker exec -i safevault-mysql mysql -uroot -prootpass
```

This runs `Data/database.sql`, which:
- Creates the `SafeVault` database (if it doesn't exist)
- Creates the `Users` table with columns `UserID`, `Username`, `Email`, `Role`, `PasswordHash` and unique constraints on `Username` and `Email`
- Inserts four seed users with BCrypt-hashed passwords (work-factor 12)

> **Upgrading from a previous version?** If your table still has a plaintext `Password` column, rename it first:
> ```sql
> ALTER TABLE Users CHANGE COLUMN Password PasswordHash VARCHAR(255) NOT NULL;
> ```
> Then re-run the seed script to replace plaintext values with hashes.

### Verify

```powershell
docker exec -it safevault-mysql mysql -uapp_user -pchange_me -e "USE SafeVault; SELECT * FROM Users;"
```

---

## 4. Connect with MySQL Shell for VS Code (optional)

1. Click the **MySQL Shell** icon in the VS Code Activity Bar
2. Click **+** to add a new connection with these settings:

| Field | Value |
|---|---|
| Host | `127.0.0.1` |
| Port | `3306` |
| User | `app_user` |
| Password | `change_me` |
| Default Schema | `SafeVault` |

3. Click **Test Connection** → should say "Successfully made the MySQL connection"
4. Open the SQL Editor to browse or run queries directly

> Use `127.0.0.1` instead of `localhost` to force TCP (Docker requires it on Windows).

---

## 5. Configure the connection string

The app reads its connection string from `appsettings.Development.json` when running locally:

```json
"ConnectionStrings": {
  "SafeVaultDb": "Server=localhost;Port=3306;Database=SafeVault;User ID=app_user;Password=change_me;"
}
```

This matches the Docker container created in step 2. No changes needed if you used the command above.

---

## 6. Run the app

```powershell
dotnet run
```

The terminal will print a URL such as `https://localhost:5001`. Open it in your browser.

---

## 7. Using the app

1. Open the home page (`/`)
2. Enter a **username** and **password** and click **Login**
3. The app will:
   - Sanitize inputs (strip HTML and enforce validation constraints)
   - Fetch the user row by username using a parameterized query
   - Verify the submitted password against the stored BCrypt hash (constant-time)
   - Issue a signed HttpOnly auth cookie on success, showing your username and role
4. Click **Logout** to clear the session cookie

### Test accounts

| Username | Password | Role | Privacy page access |
|---|---|---|---|
| `demo_user` | `Demo@1234` | User | No |
| `anna_kowalska` | `Anna@9471!` | Admin | Yes |
| `mike_nowak` | `Mike#3820$` | User | No |
| `sara_wisniewska` | `Sara*6159%` | User | No |

---

## Project structure

```
SafeVault/
├── Data/
│   ├── database.sql          # Bootstrap SQL (schema + seed data)
│   ├── IUserRepository.cs    # Repository interface
│   └── MySqlUserRepository.cs# Parameterized query implementation
├── Helpers/
│   ├── InputSanitizer.cs     # XSS / injection sanitization logic
│   └── PasswordHasher.cs     # BCrypt hash and verify helpers
├── Models/
│   └── UserRecord.cs         # DTO returned from database queries
├── Pages/
│   ├── Index.cshtml          # Login form / authenticated home view
│   ├── Index.cshtml.cs       # Page model — sanitizes, validates, signs in/out
│   ├── Privacy.cshtml        # Admin-only page
│   └── Privacy.cshtml.cs     # Restricted with [Authorize(Roles = "Admin")]
├── Tests/
│   └── TestInputValidation.cs# NUnit tests for SQL injection & XSS payloads
├── appsettings.json          # Default config (connection string template)
├── appsettings.Development.json # Local dev overrides
└── Program.cs                # App startup + DI registration
```

---

## Stopping and restarting the container manually

```powershell
# Stop
docker stop safevault-mysql

# Start again
docker start safevault-mysql
```

Because `--restart always` was set, Docker will also start it automatically on the next system boot.

---

## Running the tests

```powershell
dotnet test
```

The test project (`Tests/TestInputValidation.cs`) covers:
- SQL injection attack payload simulations (`OR 1=1`, `UNION SELECT`, `DROP TABLE`, time-based payloads)
- XSS attack payload simulations through form fields (`<script>`, `<img onerror>`, `<svg onload>`, `javascript:` vectors)
- End-to-end Razor Page form POST attack-flow tests that verify malicious input is neutralized or rejected before repository usage
- Valid username and email acceptance
- Username max-length truncation
- Invalid and malicious email rejection

Current baseline: 22 NUnit tests passing via `dotnet test`.

---

## Security notes

- **Passwords** are hashed with BCrypt (work-factor 12) via `BCrypt.Net-Next`. The hash is fetched from the database and verified in C# — the plaintext password never touches SQL.
- **Timing attacks** are mitigated because `BCrypt.Verify` runs in constant time regardless of whether the password is correct.
- **Auth cookie** is HttpOnly and Secure by default in ASP.NET Core cookie auth; sliding expiry is set to 8 hours.
- To increase hashing cost as hardware improves, raise `WorkFactor` in `Helpers/PasswordHasher.cs` and re-hash stored passwords on next login.
