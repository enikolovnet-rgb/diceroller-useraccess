# PLAN.md — diceroller-useraccess (Phase 1)

## Goal

Users can register with a photo and exchange email + password for a signed JWT.
Token **generation is mocked** behind `ITokenIssuer`; credential checking is real.

Depends on `DiceRoller.BuildingBlocks.*` v0.1.0 (local feed or GitHub Packages).

## 1. Repo setup (service template)

- [x] `DiceRoller.UserAccess.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props` (BuildingBlocks pinned to `0.1.0`), `nuget.config` (GitHub Packages + `~/local-nuget`), `.editorconfig`, `.gitignore`
- [x] Projects:
  - `src/DiceRoller.UserAccess.Domain` → BuildingBlocks.Domain
  - `src/DiceRoller.UserAccess.Application` → Domain, BuildingBlocks.Contracts, FluentValidation
  - `src/DiceRoller.UserAccess.Infrastructure` → Application, EF Core SqlServer, Identity.Core
  - `src/DiceRoller.UserAccess.Api` → Application, Infrastructure, BuildingBlocks.Web
  - `tests/DiceRoller.UserAccess.UnitTests`
  - `tests/DiceRoller.UserAccess.IntegrationTests`
- [x] Builds green with empty projects before any feature work

## 2. Domain

- [x] `PersonName` value object: first and last name, trimmed, 1–100 chars each; throws `DomainException` otherwise
- [x] `Email` value object: trimmed, lower-cased, valid format, ≤ 256 chars; throws `DomainException` otherwise
- [x] `User` aggregate: `Id` (Guid v7), `Name`, `Email`, `PasswordHash`, `PhotoKey`, `CreatedAtUtc`; private setters
- [x] `User.Register(name, email, passwordHash, photoKey, TimeProvider)` factory guarding invariants
- [x] `UserErrors`: `EmailTaken`, `InvalidCredentials`, `NotFound`, `Forbidden`, plus domain codes (`InvalidEmail`, `InvalidName`)

## 3. Application

- [ ] Interfaces: `IUserRepository`, `IPasswordHasher`, `IPhotoStorage`, `ITokenIssuer`, `ICurrentUser`, `IUnitOfWork` (or `SaveChangesAsync` on repository)
- [ ] `RegisterUserRequest` (FirstName, LastName, Email, Password, Photo as stream + file name + content type + length)
- [ ] `RegisterUserRequestValidator`:
  - first/last name required, 1–100 chars, letters, spaces, hyphens, apostrophes
  - email required, valid, ≤ 256
  - password 8–128 chars, at least one upper, one lower, one digit
  - photo required, ≤ 2 MB, `image/jpeg` | `image/png` | `image/webp`, magic bytes match the declared type
  - every rule has `.WithErrorCode(...)` and `.WithMessage(...)`
- [ ] `RegisterUserHandler` → `Result<UserDto>`: email unique (else `UserErrors.EmailTaken`) → hash password → store photo → `User.Register` → save; delete the photo if saving fails
- [ ] `CreateTokenRequest` + validator (email and password required)
- [ ] `IssueTokenHandler` → `Result<TokenDto>`: find by email → verify hash → `ITokenIssuer.Issue(user)`; unknown email and wrong password both return `UserErrors.InvalidCredentials`
- [ ] `GetUserHandler` → `Result<UserDto>`: `NotFound` if missing, `Forbidden` if not the current user
- [ ] DTOs: `UserDto { Id, FirstName, LastName, Email, PhotoUrl }`, `TokenDto { AccessToken, TokenType = "Bearer", ExpiresIn }`
- [ ] `AddApplication()` registration extension (handlers + validators)

## 4. Infrastructure

- [ ] `UserAccessDbContext`, `UserConfiguration`: table `Users`, unique index on `Email`, max lengths, owned `PersonName`, `Email` value conversion
- [ ] Initial migration `InitialCreate`
- [ ] `UserRepository`
- [ ] `AspNetPasswordHasher` wrapping `PasswordHasher<User>`
- [ ] `LocalPhotoStorage`: configurable root folder, random file name + original extension, `SaveAsync`, `DeleteAsync`, `GetUrl`
- [ ] `DevJwtTokenIssuer` (the mock) using `JsonWebTokenHandler`, HS256:
  - claims `sub`, `email`, `given_name`, `family_name`, `jti`, `iat`
  - issuer, audience, signing key, expiry (default 60 min) from `JwtOptions`
- [ ] `AddInfrastructure(IConfiguration)` registration extension; DB health check

## 5. Api

- [ ] `Program.cs`: `AddServiceDefaults()`, `AddApplication()`, `AddInfrastructure()`, `AddJwtAuthentication()`; migrations applied at startup in Development and in the container
- [ ] `UsersController`
  - `POST /api/v1/users` — `[AllowAnonymous]`, `[Consumes("multipart/form-data")]`, request size limit 3 MB → `201` + `Location`
  - `GET /api/v1/users/{id:guid}` — authenticated → `200`
- [ ] `TokensController`
  - `POST /api/v1/tokens` — `[AllowAnonymous]`, rate limited (fixed window, 10 per minute per IP) → `200`
- [ ] Static file serving or a `GET /api/v1/users/{id}/photo` endpoint so `PhotoUrl` resolves (pick one, document it)
- [ ] `ICurrentUser` implementation reading `sub` from `HttpContext.User`
- [ ] `appsettings.json` with non-secret defaults; secrets via user-secrets / env
- [ ] `DiceRoller.UserAccess.http` with register, token, get-user examples

## 6. Tests

- [x] Unit: `Email`, `PersonName`, `User.Register` invariants throw `DomainException` with the right code
- [ ] Unit: each validator rule, using `TestValidate(...)` and asserting error codes
- [ ] Unit: `RegisterUserHandler` — email taken, photo deleted when save fails, success
- [ ] Unit: `IssueTokenHandler` — unknown email, wrong password (same error), success
- [ ] Unit: `DevJwtTokenIssuer` output validates with the same `TokenValidationParameters` `AddJwtAuthentication` uses
- [ ] Integration (Testcontainers SQL Server + `WebApplicationFactory`) — when adding the first test, remove `--ignore-exit-code 8` from the IntegrationTests csproj:
  - register → 201 with `Location`
  - duplicate email → 409, `errorCode = User.EmailTaken`
  - invalid input (bad email, short password, wrong photo type) → 400 with per-field `errors`
  - token happy path → 200, token decodes with expected claims
  - wrong password → 401, standard body
  - get own user → 200; another user's id → 403; no token → 401

## 7. Container and CI

- [ ] Multi-stage `Dockerfile` (`sdk:10.0` → `aspnet:10.0`), non-root user, `HEALTHCHECK` on `/health/live`, photo folder as a volume
- [ ] `docker-compose.yml`: this service + its own SQL Server 2022 (healthcheck, named volume) + photo volume; settings from `.env`
- [ ] `.env.example` with every variable; `.env` git-ignored
- [ ] GitHub Actions `ci.yml`: restore (package feed token) → build → unit + integration tests → on `main`, build and push image `ghcr.io/<owner>/diceroller-useraccess` tagged with commit SHA and version
- [ ] `README.md`: purpose, run locally, configuration, endpoints, the token contract it issues

## Done when

- [ ] `dotnet build` with 0 warnings, `dotnet test` green
- [ ] `docker compose up` from a fresh clone of this repo alone works, and every request in the `.http` file succeeds
- [ ] Every error response (400, 401, 403, 404, 409, 500) has the standard body with `errorCode` and `traceId`
