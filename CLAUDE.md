# CLAUDE.md — diceroller-useraccess

## What this repo is

The **UserAccess** microservice of the DiceRoller system: user registration (with photo) and access-token issuing.
It is one of four repos; it builds, tests and runs on its own and never references another repo's source code.

| Repo | Role |
| --- | --- |
| `diceroller-building-blocks` | Shared plumbing as NuGet packages (`DiceRoller.BuildingBlocks.*`) |
| `diceroller-useraccess` | **This repo** — users + tokens |
| `diceroller-operative` | Dice rolls + history; validates tokens issued here |
| `diceroller-platform` | YARP gateway, system docker-compose, end-to-end tests |

The current task is in `PLAN.md`. Do only the phase you are asked to do.

## Commands

```bash
dotnet build                                   # must finish with 0 warnings (TreatWarningsAsErrors)
dotnet test                                    # unit + integration; integration needs Docker running
docker compose up -d                           # SQL Server + this service
dotnet ef migrations add <Name> \
  -p src/DiceRoller.UserAccess.Infrastructure \
  -s src/DiceRoller.UserAccess.Api
```

## Endpoints

| Route | Auth | Notes |
| --- | --- | --- |
| `POST /api/v1/users` | anonymous | `multipart/form-data`: firstName, lastName, email, password, photo → `201` + `Location` |
| `GET /api/v1/users/{id}` | Bearer, own id only | `403` for another user's id |
| `POST /api/v1/tokens` | anonymous, rate-limited | `{ email, password }` → `{ accessToken, tokenType, expiresIn }` |

## Rules specific to this service

- Token generation goes **only** through `ITokenIssuer`. The single implementation is `DevJwtTokenIssuer` (the mock): HS256, claims `sub`, `email`, `given_name`, `family_name`, `jti`, `iat`; issuer, audience, key and expiry from `JwtOptions`. Do not add refresh tokens, revocation or an identity server.
- Wrong password and unknown email return the **same** `401` error (`User.InvalidCredentials`). Never reveal which one failed.
- Passwords are hashed with `PasswordHasher<User>` behind `IPasswordHasher`. Never log, return or store a plain password.
- Email is normalized to lower case and has a unique index; a duplicate returns `409` (`User.EmailTaken`).
- Photo: required, ≤ 2 MB, jpeg/png/webp, and the file's magic bytes must match the declared type. Store it through `IPhotoStorage` under a random key; the database stores only the key. If saving the user fails, delete the stored photo.

<!-- ===== Everything below is identical in every DiceRoller repo ===== -->

## Architecture rules

- Projects: `Api → Application → Domain`; `Infrastructure → Application`. Domain references nothing except `DiceRoller.BuildingBlocks.Domain`.
- Domain: entities with private setters and factory methods (`User.Register(...)`); invariants guarded inside the domain; value objects for concepts like `Email`, `PersonName`.
- Application: one MediatR request + `IRequestHandler` per use case (MediatR 12.5.0, no pipeline behaviors; validation stays in `ValidationFilter`); handlers depend on interfaces, never on EF or ASP.NET types.
- Api: thin controllers — bind → (validation filter runs) → call one handler → map `Result` to HTTP. No business logic, no `DbContext`.
- Infrastructure: EF Core, external storage, token issuing, hashing — all behind interfaces declared in Application.
- Inject `TimeProvider` for time and interfaces for randomness. Never call `DateTime.UtcNow` or `Random` directly.

## Error handling — follow exactly

| Failure | How it is raised | Who turns it into HTTP |
| --- | --- | --- |
| Invalid input | FluentValidation validator | `ValidationFilter` → `Error.Validation` with per-field `Details` |
| Business rule (taken, not found, bad credentials) | handler returns `Result.Failure(error)` | controller via `ToActionResult()` |
| Domain invariant broken | `throw new DomainException(DomainErrors.X)` | `GlobalExceptionHandler` → `Result.Failure(ex.Error)` |
| Anything unexpected | any other exception | `GlobalExceptionHandler` → `Error.Unexpected` (500) |

- Handlers return `Result` / `Result<T>`. They never `throw` for expected failures and never `catch` `DomainException`.
- Every failure goes through `ErrorMapper`, so every error body has the same RFC 9457 shape: `status`, `title`, `detail`, `errorCode`, `errors`, `traceId`. Never build a ProblemDetails or `BadRequest(...)` by hand.
- Error codes live in static classes per aggregate (`UserErrors.EmailTaken`). Tests assert codes, not message text.

## Validation

- Every request DTO and query has a FluentValidation validator in the Application project, registered with `AddValidatorsFromAssemblyContaining<...>()`.
- No DataAnnotations on request types. Every rule sets `.WithErrorCode(...)` and `.WithMessage(...)`.
- Cross-field rules belong in the validator, not in controllers or handlers.

## API conventions

- Routes: plural nouns under `/api/v1`. Correct status codes: `201` + `Location` for creates, `400`, `401`, `403`, `404`, `409`.
- Every endpoint that returns a list takes `page` (≥ 1, default 1) and `pageSize` (1–100, default 10) and returns `PagedResponse<T>`, with a deterministic order (last sort key is `Id`).
- Endpoints are `async` and accept a `CancellationToken`; pass it all the way down.
- OpenAPI via `Microsoft.AspNetCore.OpenApi` + Scalar. Keep the `.http` file in the Api project up to date with a working example per endpoint.

## Data

- SQL Server, EF Core code-first. One database per service; never query another service's database.
- Configure entities with `IEntityTypeConfiguration<T>` classes; set max lengths, required fields and indexes explicitly.
- Reads use `AsNoTracking()` and project to DTOs. Migrations run at startup only in Development and in the container.

## Security and configuration

- Secrets (signing key, connection strings) come from user-secrets, environment variables or `.env`. Never commit them, never hardcode them, never log them.
- Options classes are validated with `ValidateOnStart()` so bad config fails at startup.
- The authorization fallback policy requires an authenticated user; anonymous endpoints opt out explicitly with `[AllowAnonymous]`.

## Testing

- xUnit v3, Moq, Shouldly. Unit tests for validators, domain factories and handlers; integration tests with `WebApplicationFactory` + Testcontainers (real SQL Server, never the EF in-memory provider).
- Name tests `Method_Scenario_ExpectedResult`. Every bug fix gets a test that fails without the fix.
- Integration tests that need a token mint one with the shared test key, issuer and audience.

## Code style

- .NET 10, C# latest, nullable enabled, file-scoped namespaces, primary constructors where they read well, `sealed` by default.
- Central Package Management: versions only in `Directory.Packages.props`.
- No commented-out code, no `TODO` without a matching PLAN.md item.

## How to work

1. Read `PLAN.md` and restate the phase's tasks before writing code. In plan mode, propose the design and wait for approval.
2. Work in small steps (domain → application → infrastructure → api → tests). After each step run `dotnet build` and `dotnet test`; fix failures before moving on.
3. Tick the matching checkbox in `PLAN.md` when a task is done.
4. Do not add features, endpoints or NuGet packages that `PLAN.md` doesn't list — ask first.
5. Do not change anything in another repo. If BuildingBlocks needs a change, stop and describe it.
6. Do not commit or push unless asked. When asked, use Conventional Commits (`feat:`, `fix:`, `test:`, `chore:`).
7. Finish by checking the phase's "Done when" list item by item and reporting anything not met.
