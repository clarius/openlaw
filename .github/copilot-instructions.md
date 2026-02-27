# Copilot Instructions for OpenLaw

## Build, test, and lint commands

- Build (same shape as CI): `dotnet build -m:1 -bl:build.binlog`
- Run tests (local): `dotnet test src\Tests\Tests.csproj`
- Run tests (CI runner pattern): `dnx --yes retest -- --no-build`
- Run a single test: `dotnet test src\Tests\Tests.csproj --filter "FullyQualifiedName~Clarius.OpenLaw.DisplayValueTests.CanParseFromDisplay"`
- Format/lint checks used in CI:
  - `dotnet format whitespace --verify-no-changes -v:diag --exclude ~/.nuget`
  - `dotnet format style --verify-no-changes -v:diag --exclude ~/.nuget`

## High-level architecture

- `src/dotnet-openlaw` is the CLI entrypoint (`openlaw` global tool) and wires command execution/update checks.
- `src/OpenLaw.Commands` contains CLI behavior and SAIJ synchronization/conversion logic:
  - `App` registers `convert`, `format`, and Argentina commands (`sync`, `syncitem`) via `UseArgentina`.
  - `SaijClient` + embedded JQ resources (`Argentina/*.jq`) normalize SAIJ payloads.
  - `SyncCommand`/`SyncItemCommand` use `FileDocumentRepository` to persist raw JSON in `data\` and Markdown files at the target root.
- `src/OpenLaw` contains shared infrastructure (serialization helpers, ID mapping, chat client adapters, complexity routing).
- `src/Api` is an Azure Functions host:
  - `Program.cs` wires OpenAI clients, chat pipelines, WhatsApp handlers, and storage clients.
  - `BlobStorage` ingests EventGrid blob events, uploads legal files to OpenAI vector stores, and writes back `FileId`/`StoreId` metadata.
  - `VectorStoreService` resolves which vector store to use from OpenAI metadata (`agent`, `from`, `to`).
- `src/Tests` is xUnit coverage for core logic, Argentina sync/parsing, and selected OpenAI/integration scenarios.

## Key repository conventions

- DI registration relies on `[Service]` attributes with generated `AddServices()` calls (see `src\Api\Program.cs`), not only explicit `AddSingleton` calls.
- Keyed services are important wiring points: `"oai"` (OpenAI client) and `"complexity"` (model used for complexity scoring).
- Configuration is layered from environment variables, user secrets, and dotnet config (`AddDotNetConfig` in CLI app setup).
- SAIJ type and filter mapping uses `DisplayValue` attributes/enums; prefer `DisplayValue.Parse/TryParse` over custom string switches.
- Document metadata is carried in front matter (`---` YAML or `<!-- -->` HTML-style blocks) and parsed through `ContentInfo.ReadFrontMatter`.
- Sync workflows are built for transient failures (Polly + retries + requeue) and write poison/error artifacts under `.github\.openlaw\...` when needed.
- Tests use custom xUnit attributes in `src\Tests\Attributes.cs` (`SecretsFact`, `LocalFact`, `CIFact`, runtime variants) to gate CI-only, local-only, and secret-dependent tests.
