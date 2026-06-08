# Local development

## Aspire

The local development entrypoint is the Aspire AppHost at `src/WorldCupBets.AppHost`.

Required local secrets before starting it:

- `DB_PASSWORD`
- `JWT_SECRET` with at least 32 UTF-8 bytes, for example `local-dev-jwt-secret-32-chars`

Set them once with .NET user-secrets:

```bash
dotnet user-secrets set DB_PASSWORD "your-db-password" --project src/WorldCupBets.AppHost
dotnet user-secrets set JWT_SECRET "local-dev-jwt-secret-32-chars" --project src/WorldCupBets.AppHost
```

## AI Insights (optional)

The "Match AI Insights" feature calls OpenCode Go (a low-cost subscription
running through the OpenCode Zen gateway, via its dedicated `/zen/go/` endpoint).
It is
optional — when no API key is configured, the backend falls back to an empty
provider and the feature quietly self-disables (no errors, no UI shown).

To enable it locally, set the API key as a user secret on the Web API project
(the same convention as `ApiSportsFootball:ApiKey` — never commit it to
`appsettings.json`):

```bash
dotnet user-secrets set "AiInsights:ApiKey" "your-opencode-zen-api-key" --project src/WorldCupBets.WebApi
```

Non-secret settings (`BaseUrl`, `Model`, `TimeoutSeconds`, `MaxTokens`) live in
`appsettings.json` under the `AiInsights` section and can be overridden per
environment.

Optional environment variables:

- `DB_USERNAME` defaults to `app`
- `GOOGLE_CLIENT_ID` defaults to empty, which leaves only development login available
- `ENABLE_DEV_LOGIN` defaults to `true`

Start the full local stack with:

```bash
dotnet run --project src/WorldCupBets.AppHost
```

What Aspire starts:

- PostgreSQL on `5432`
- Redis on `6379`
- Web API on `5000`
- Angular frontend dev server on `4200`

Notes:

- The Angular frontend now runs as a Node development process under Aspire instead of the local nginx container.
- The frontend still calls relative `/api/...` URLs; the Angular dev server proxies those requests to the Aspire-managed API endpoint.
- EF Core migrations run automatically when the API starts in development.
- Flyway stays manual for local development when SQL artifacts such as views or stored procedures need to be applied.
- Docker Compose and local application Dockerfiles are no longer part of the development workflow.
- Production deployment remains intentionally undecided and should be designed separately when needed.

## Rider

If Rider starts the AppHost but the frontend fails with `exec: "npm": executable file not found in $PATH`, Rider is launching the process without the shell environment that loads Node through `nvm`.

Until Rider is configured with the correct Node/npm environment, start Aspire from a terminal where `npm` is already available:

```bash
dotnet run --project src/WorldCupBets.AppHost
```
