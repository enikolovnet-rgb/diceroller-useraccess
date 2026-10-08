# diceroller-useraccess

The **UserAccess** microservice of the DiceRoller system. It registers users (with a profile photo) and exchanges
email + password for a signed JWT access token. Other DiceRoller services (for example `diceroller-operative`)
validate those tokens with the same issuer, audience and signing key.

Token generation is a **mock** (`DevJwtTokenIssuer`, HS256 with a shared key) behind `ITokenIssuer`; credential
checking is real (`PasswordHasher<User>`). There are no refresh tokens and no revocation.

## Run locally

Prerequisites: .NET 10 SDK, Docker.

### With Docker Compose (service + its own SQL Server)

```bash
cp .env.example .env      # then fill in NUGET_AUTH_TOKEN, MSSQL_SA_PASSWORD and JWT_SIGNING_KEY
docker compose up -d --build
```

The service listens on `http://localhost:5080` (`USERACCESS_PORT`). Migrations are applied at startup in the
container. The compose file runs the service as `Development` (`ASPNETCORE_ENVIRONMENT`), so Scalar is at
`http://localhost:5080/scalar`. Photos are kept in the `useraccess-photos` volume, the database in `sqlserver-data`.
`docker compose down -v` removes both.

Building the image restores the `DiceRoller.BuildingBlocks.*` packages from GitHub Packages, so `NUGET_AUTH_TOKEN`
must be a GitHub personal access token with `read:packages`. It is passed to the build as a BuildKit secret and is not
stored in the image.

### With `dotnet run`

The packages come from the local feed `~/local-nuget` (see `nuget.config`). Start a SQL Server (for example
`docker compose up -d sqlserver`) and set the secrets once:

```bash
cd src/DiceRoller.UserAccess.Api
dotnet user-secrets set "ConnectionStrings:UserAccess" "Server=localhost,1433;Database=UserAccess;User Id=sa;Password=<sa password>;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:SigningKey" "<at least 32 bytes>"
dotnet run
```

In Development, migrations run at startup, and OpenAPI (`/openapi/v1.json`) and Scalar (`/scalar`) are available.
`src/DiceRoller.UserAccess.Api/DiceRoller.UserAccess.http` has a working example of every endpoint.

> The compose file doesn't publish SQL Server's port. To use it from `dotnet run`, add `ports: ["1433:1433"]` to
> the `sqlserver` service locally.

### Tests

```bash
dotnet build      # 0 warnings (TreatWarningsAsErrors)
dotnet test       # unit + integration; integration tests start SQL Server with Testcontainers, so Docker must run
```

## Configuration

Secrets come from user-secrets, environment variables or `.env`; they are never committed. Invalid options stop the
service at startup.

| Key (environment variable) | Default | Notes |
| --- | --- | --- |
| `ConnectionStrings:UserAccess` (`ConnectionStrings__UserAccess`) | — | Required. SQL Server connection string. |
| `Jwt:Issuer` (`Jwt__Issuer`) | `diceroller` | `iss` of issued tokens. |
| `Jwt:Audience` (`Jwt__Audience`) | `diceroller` | `aud` of issued tokens. |
| `Jwt:SigningKey` (`Jwt__SigningKey`) | — | Required secret, at least 32 bytes. |
| `Jwt:ExpiryMinutes` (`Jwt__ExpiryMinutes`) | `60` | Token lifetime. |
| `PhotoStorage:RootPath` (`PhotoStorage__RootPath`) | `photos` (`/data/photos` in the image) | Folder photos are written to; relative paths resolve against the content root. |
| `PhotoStorage:RequestPath` | `/photos` | URL path photos are served under. |
| `RateLimiting:Tokens:PermitLimit` / `WindowSeconds` | `10` / `60` | Fixed window per client IP for `POST /api/v1/tokens`. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | — | Enables OTLP export of traces, metrics and logs. |

`.env.example` lists the variables Docker Compose uses.

## Endpoints

| Route | Auth | Request | Success |
| --- | --- | --- | --- |
| `POST /api/v1/users` | anonymous | `multipart/form-data`: `firstName`, `lastName`, `email`, `password`, `photo` (≤ 2 MB, jpeg/png/webp) | `201` + `Location`, `UserDto` |
| `GET /api/v1/users/{id}` | Bearer, own id only | — | `200`, `UserDto` |
| `POST /api/v1/tokens` | anonymous, 10/min per IP | `{ "email", "password" }` | `200`, `{ accessToken, tokenType, expiresIn }` |
| `GET /photos/{key}` | anonymous | — | the photo (`PhotoUrl` in `UserDto`) |
| `GET /health/live`, `GET /health/ready` | anonymous | — | liveness; readiness includes the database |

`UserDto` is `{ id, firstName, lastName, email, photoUrl }`.

Every error is an RFC 9457 body (`application/problem+json`) with `status`, `title`, `detail`, `errorCode`,
`errors` (per-field messages for validation errors) and `traceId`:

| Status | `errorCode` |
| --- | --- |
| 400 | `Request.Invalid` (validation), `Request.Malformed` |
| 401 | `User.InvalidCredentials` (wrong password **or** unknown email), `Auth.Unauthorized`, `Auth.InvalidToken`, `Auth.TokenExpired` |
| 403 | `User.Forbidden` (another user's id) |
| 404 | `User.NotFound` |
| 409 | `User.EmailTaken` (emails are compared lower-cased) |
| 500 | `General.Unexpected` |

`POST /api/v1/tokens` returns `429` when the rate limit is exceeded.

## Token contract

Tokens are compact JWTs signed with **HS256** using `Jwt:SigningKey`.

| Claim | Value |
| --- | --- |
| `iss` / `aud` | `Jwt:Issuer` / `Jwt:Audience` |
| `sub` | user id (GUID) |
| `email` | lower-cased email |
| `given_name` / `family_name` | first / last name |
| `jti` | unique token id (GUID) |
| `iat`, `nbf`, `exp` | issued at, not before, expiry (`iat` + `Jwt:ExpiryMinutes`) |

Validators (`AddJwtAuthentication` in `DiceRoller.BuildingBlocks.Web`) check signature, issuer, audience and
lifetime, accept only HS256 and allow 30 seconds of clock skew.

## CI

`.github/workflows/ci.yml` restores (BuildingBlocks from GitHub Packages with the workflow's `GITHUB_TOKEN`), builds,
and runs unit and integration tests on every push and pull request. On `main` it also builds and pushes
`ghcr.io/<owner>/diceroller-useraccess`, tagged with the commit SHA and the version from `Directory.Build.props`.

The `DiceRoller.BuildingBlocks.*` packages must grant this repository read access
(package settings → *Manage Actions access*), otherwise the `GITHUB_TOKEN` can't restore them.
