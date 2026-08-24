# Supabase Setup Guide

Every developer sets up their own connection string. The database itself is shared — we use one Supabase project for dev.

## 1. Get the connection string

Ask the team lead for the Supabase project URL. You need the **Session pooler** connection string:

- Go to **Settings → Database → Connection string**
- Select **Session mode** (port 5432, NOT transaction mode port 6543)
- Copy the string — it looks like:
  ```
  Host=aws-1-ap-south-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<projectref>;Password=...
  ```

> **Why session mode?** Transaction mode (pgBouncer) breaks EF Core migrations because it doesn't support advisory locks or prepared statements. Direct connection (`db.<ref>.supabase.co`) is IPv6-only without the paid add-on.

## 2. Store it via user-secrets (never commit passwords)

```bash
cd src/PayrollSaaS.API
dotnet user-secrets set "ConnectionStrings:Payroll" "Host=aws-1-ap-south-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<projectref>;Password=<your-password>;Search Path=payroll"
dotnet user-secrets set "Jwt:Key" "your-32-byte-dev-secret-key-here!!"
```

## 3. Apply migrations

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
dotnet ef database update --project src/PayrollSaaS.Infrastructure --startup-project src/PayrollSaaS.API
```

Verify tables landed in the `payroll` schema (not `public`) with:
```sql
SELECT table_name FROM information_schema.tables WHERE table_schema = 'payroll';
```

## 4. Run the API

```bash
dotnet run --project src/PayrollSaaS.API
```

Scalar UI: http://localhost:5000/scalar/v1

Seed data is auto-applied in Development. One user per role:

| Role | Email | Password |
|---|---|---|
| SuperAdmin | superadmin@theerrv.in | Password123! |
| SchoolAdmin | schooladmin@theerrv.in | Password123! |
| HR | hr@theerrv.in | Password123! |
| Finance | finance@theerrv.in | Password123! |
| Employee | employee@theerrv.in | Password123! |

## 5. Integration tests (per-developer schema)

Each developer uses a separate schema `test_<yourname>` so tests don't collide.

```bash
dotnet user-secrets set "ConnectionStrings:PayrollTest" "...;Search Path=test_harsha"
dotnet run --project tests/PayrollSaaS.IntegrationTests
```

## Schema layout

| Schema | Contents |
|---|---|
| `payroll` | All application tables |
| `hangfire` | Hangfire job storage |
| `test_<dev>` | Per-developer integration test schema |
| `public` | Left empty — keeps PostgREST from auto-exposing salary data |
