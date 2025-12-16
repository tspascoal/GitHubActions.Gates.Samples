<!-- Copilot instructions for GitHubActions.Gates.Samples -->
# Repository guidance for AI coding assistants

This repo implements sample GitHub Actions "gates" as .NET 10 Azure Functions on Flex Consumption. Keep guidance concise and specific to this codebase.

## Architecture overview

Two gates (Deploy Hours and Issues) follow the same pattern:
1. **HTTP webhook** receives GitHub deployment protection rule events → enqueues to Service Bus
2. **ServiceBus-triggered function** reads YAML config from the repo and approves/rejects/delays deployment

Key files:
- Webhook handler: `src/DeployHours.Gate/WebHookFunction.cs` → calls `ProcessWebHook(..., Constants.ProcessQueueName)`
- Processing logic: `src/DeployHours.Gate/ProcessFunction.cs` (inherits `ProcessingHandler<TConfig,TRule>`)
- Framework config constants: `src/GitHubActions.Gates.Framework/Config.cs`
- IaC: `IaC/Gate.bicep` (Flex Consumption with managed identity)

## Technology stack

- **.NET 10** isolated worker model (`net10.0`, `AzureFunctionsVersion v4`)
- **Azure Functions Flex Consumption** (FC1 SKU) with managed identity for storage/Service Bus
- **Azure Service Bus** for async processing (queues: `deployHoursProcessing`, `issuesProcessing`)
- **Key Vault** for secrets (PEM certificate, webhook secret) via App Service references
- **Application Insights** via connection string (not instrumentation key)

## Environment variables / App settings

| Setting | Purpose |
|---------|---------|
| `SERVICEBUS_CONNECTION__fullyQualifiedNamespace` | Service Bus FQDN (managed identity auth) |
| `AzureWebJobsStorage__accountName` | Storage account name (managed identity auth) |
| `GHAPP_ID` | GitHub App ID |
| `GHAPP_PEMCERTIFICATE` | Key Vault reference to PEM certificate |
| `GHAPP_WEBHOOKSECRET` | Key Vault reference to webhook secret (optional) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Insights connection string |

## Developer workflows

```bash
# Build
dotnet build src/GitHubActions.Gates.Samples.sln

# Run tests (CI fails on warnings)
dotnet test src/GitHubActions.Gates.Samples.sln /p:TreatWarningsAsErrors=true

# Run locally (requires Azure Functions Core Tools v4)
cd src/DeployHours.Gate
func start
```

Local development requires:
- Service Bus namespace (or mock in tests)
- Environment variables set (see `local.settings.json` template)

## Project conventions

- **Minimal DI**: `Program.cs` uses `HostBuilder` + `ConfigureFunctionsWebApplication()`. Avoid complex containers.
- **Config centralization**: All env var names live in `Config.cs`. Changing these requires updating `IaC/Gate.bicep`.
- **YAML config per repo**: Gates read config from target repo (e.g., `.github/deployhours-gate.yml`)
- **UTC everywhere**: Time calculations use `DateTime.UtcNow` (see `GetNextDeployHour`)
- **Conditional signature validation**: Webhook accepts unauthenticated requests when `GHAPP_WEBHOOKSECRET` is unset

## IaC patterns (Bicep)

The `IaC/Gate.bicep` uses:
- **Flex Consumption**: FC1 SKU with `functionAppConfig` for runtime/scaling
- **Managed identity**: System-assigned identity for Storage, Service Bus, Key Vault
- **No shared keys**: `allowSharedKeyAccess: false` on storage, `disableLocalAuth: true` on Service Bus
- **RBAC roles**: Storage Blob Data Owner, Storage Account Contributor, Service Bus Data Owner/Sender/Receiver

When modifying IaC:
- Keep `IaC/Gate.bicep` and `Config.cs` in sync for env var names
- Flex Consumption requires `serverFarmId` (hosting plan) despite being "serverless"
- Runtime version format is `10.0` not `10`

## Testing patterns

- Unit tests in `tests/` mock Service Bus and GitHub API via `GitHubActions.TestHelpers`
- YAML parsing tests in `DeployHours.Gate.Tests/DeployHoursConfigurationTests.cs` show expected config shape
- Framework tests in `GitHubActions.Gates.Framework.Tests/` cover retry logic and handlers

## CI expectations

- CI runs on `ubuntu-latest` with .NET SDK from `global.json`
- Build with `/p:TreatWarningsAsErrors=true` — keep code warning-free
- CodeQL security scanning runs on every PR

## Adding a new gate

1. Copy `DeployHours.Gate` project structure
2. Create new queue name in `Constants.cs`
3. Implement `ProcessFunction` inheriting `ProcessingHandler<TConfig,TRule>`
4. Add queue to `IaC/Gate.bicep` allowed values
5. Add corresponding tests in `tests/`
