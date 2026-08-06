# ComplianceGuard

A multi-tenant supply-chain compliance monitoring API modeled on cannabis track-and-trace regulatory systems. Built with .NET 8, it tracks packages from cultivation through retail sale across licensed facilities, uses AI-powered anomaly detection via both Microsoft Semantic Kernel and Microsoft Agent Framework Workflows, and includes an evaluation harness to prevent AI regressions in CI.

> **Why this project?** Cannabis regulatory platforms enforce strict chain-of-custody rules across dozens of independently regulated state markets. Each licensed facility is a tenant with its own compliance obligations — packages must be tagged (RFID), tested (lab), manifested (transfers), and tracked at every handoff. This demo models that problem: multi-tenant facility isolation, package and transfer tracking, AI-powered anomaly detection on manifests, and a test-driven development workflow that treats the AI agent as a first-class component with its own regression suite.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        ComplianceGuard.Api                          │
│  Minimal API Endpoints · Tenant Middleware · DI Composition         │
│  /transfers  /packages  /facilities  /anomalies                     │
│  POST /anomalies/scan (SK agent)                                    │
│  POST /anomalies/workflow-scan (Agent Framework Workflows)          │
└──────────┬──────────────────────────────────────┬───────────────────┘
           │                                      │
           ▼                                      ▼
┌─────────────────────────┐    ┌──────────────────────────────────────┐
│ ComplianceGuard.        │    │ ComplianceGuard.Infrastructure       │
│ Application             │    │                                      │
│                         │    │  Persistence/                        │
│  Transfer handlers      │◄───│    AppDbContext (EF Core)            │
│  AnomalyDetectionService│    │    DapperTransferRepository          │
│  ReviewAnomalyHandler   │    │  Ai/                                 │
│                         │    │    SemanticKernelFactory              │
└──────────┬──────────────┘    │    AnomalyDetectionAgent (SK)        │
           │                   │    Plugins/                          │
           ▼                   │      CustodyAnomalyPlugin            │
┌──────────────────────────┐   │    Workflows/                        │
│ ComplianceGuard.Domain   │   │      ComplianceWorkflowFactory       │
│                          │   │      RuleEngineExecutor              │
│  Entities/               │   │      RiskAssessmentExecutor          │
│    Facility, Package,    │◄──│      CleanReportExecutor             │
│    Transfer, LabTest,    │   └──────────────────────────────────────┘
│    AnomalyFlag           │
│  Abstractions/           │
│    ITenantContext,        │
│    ITransferRepository   │
└──────────────────────────┘
```

Dependencies flow inward. Domain has zero external references, Application depends only on Domain, and Infrastructure implements the interfaces that Application defines.

## Domain Model

The domain is modeled after real cannabis track-and-trace systems:

| Entity | Real-World Concept |
|---|---|
| **Facility** | A licensed cannabis business (cultivator, manufacturer, dispensary, lab) identified by a state-issued license number |
| **Package** | The core trackable unit — tagged with an RFID identifier, moves through the supply chain from cultivation to retail sale |
| **Transfer** | A manifested movement of packages between facilities, including transporter details (driver, vehicle, departure/arrival times) |
| **LabTest** | Compliance testing results (potency, contaminants, pesticides) — packages must pass before retail sale |
| **AnomalyFlag** | AI-detected compliance violations flagged on transfers or packages |

Each state market operates its own regulatory API. In our model, each licensed facility is a tenant, and all data is scoped by `FacilityId`.

## Projects

| Project | Purpose |
|---|---|
| `ComplianceGuard.Domain` | Entities and abstractions — zero dependencies |
| `ComplianceGuard.Application` | Business logic and orchestration — depends only on Domain |
| `ComplianceGuard.Infrastructure` | EF Core, Dapper, Semantic Kernel, Agent Framework Workflows — implements Application interfaces |
| `ComplianceGuard.Api` | Thin HTTP layer — endpoints, tenant middleware, DI wiring |
| `ComplianceGuard.Eval` | Golden-scenario evaluation harness — runs agent against known inputs, fails CI on regression |
| `ComplianceGuard.UnitTests` | Application + Domain tests with mocked dependencies |
| `ComplianceGuard.IntegrationTests` | Real database tests via Testcontainers |
| `ComplianceGuard.ApiTests` | End-to-end HTTP tests via WebApplicationFactory |

## Multi-Tenancy

Each licensed facility operates as an isolated tenant. Isolation is enforced at two levels:

1. **Middleware** — `TenantResolutionMiddleware` extracts the facility identifier from the `X-License-Number` request header and resolves it to a scoped `ITenantContext`.
2. **Database** — `AppDbContext` applies EF Core global query filters on `FacilityId` for every entity, so queries are automatically scoped to the requesting facility. Cross-facility data leakage is structurally prevented.

This pattern scales across state markets without code changes — each facility gets its own isolated view of packages, transfers, and lab results through the same API surface.

## AI-Powered Anomaly Detection

ComplianceGuard implements two complementary AI architectures for anomaly detection:

### Semantic Kernel Agent (`POST /anomalies/scan`)

The original detection pipeline uses Microsoft Semantic Kernel with an LLM-orchestrated agent:

- **`AnomalyDetectionAgent`** implements `IAnomalyDetectionService` (defined in Application), keeping the business logic AI-framework-agnostic.
- **`CustodyAnomalyPlugin`** exposes kernel functions that the agent orchestrates:
  - `detect_transfer_timing_gap` — suspicious delays between departure and arrival
  - `detect_facility_distance_violation` — physically impossible transit times between facilities (Haversine distance calculation)
  - `detect_package_quantity_discrepancy` — mismatches between manifested and received package counts
  - `detect_lab_test_anomaly` — missing required tests or suspicious result patterns
- **Dual-mode execution** — when an OpenAI API key is configured, the LLM orchestrates plugin calls via `FunctionChoiceBehavior.Auto()`. Without a key, plugins are invoked directly (deterministic fallback), ensuring CI runs without external dependencies.

### Agent Framework Workflows (`POST /anomalies/workflow-scan`)

A directed-graph workflow built on Microsoft's Agent Framework that models compliance checking as a multi-step pipeline with conditional routing:

```
ComplianceScanRequest
       │
       ▼
┌──────────────────┐
│RuleEngineExecutor│  Deterministic: timing, distance, quantity checks
└────────┬─────────┘
         │ ComplianceCheckResult
    ┌────┴────┐  conditional edges
    │         │
    ▼         ▼
anomalies   clean
found?      transfer?
    │         │
    ▼         ▼
┌────────────┐ ┌───────────────┐
│RiskAssess- │ │CleanReport-   │
│mentExecutor│ │Executor       │
│            │ │               │
│ LLM risk   │ │ "Compliant"   │
│ analysis   │ │ all-clear     │
└────────────┘ └───────────────┘
       │              │
       ▼              ▼
  ComplianceReport  ComplianceReport
```

**Key components:**

| Executor | Role |
|---|---|
| **RuleEngineExecutor** | Wraps `CustodyAnomalyPlugin` — runs all 4 deterministic detection functions |
| **RiskAssessmentExecutor** | LLM-powered risk analysis via `IChatClient` (Microsoft.Extensions.AI) — contextualizes anomalies, recommends enforcement actions. Falls back to rule-based assessment without an API key |
| **CleanReportExecutor** | Generates compliant status when no anomalies are found |
| **ComplianceWorkflowFactory** | Builds the workflow graph with typed conditional edges and registers it in DI |

**Why both approaches?** The Semantic Kernel agent demonstrates LLM tool-calling orchestration — the model decides which detection functions to invoke. The Agent Framework Workflow demonstrates a deterministic, graph-based pipeline where the execution path is structurally defined. In a real system, the workflow approach is better suited for auditable compliance processes because the execution path is predictable and reproducible (superstep/BSP execution model).

## Eval Harness

AI features need regression testing beyond unit tests. `ComplianceGuard.Eval` is a standalone CLI that:

1. Loads golden scenarios from JSON files — each represents a real compliance risk:
   - **Transfer timing gap** — 72-hour delay on a 2-hour route
   - **Facility distance violation** — package received 400 miles away in 30 minutes
   - **Package quantity discrepancy** — 3 packages missing from a 50-package manifest
   - **Missing lab test** — package transferred to dispensary without passing lab results
   - **Clean transfer** — false-positive check, ensures compliant transfers produce no flags
2. Runs the anomaly-detection agent against each scenario
3. Scores results against expected outcomes (type + severity matching)
4. Compares to `BaselineResults.json` (committed to the repo)
5. Fails the CI build if scores regress

```bash
dotnet run --project src/ComplianceGuard.Eval
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB for development, or Docker for integration tests)
- Docker Desktop (for Testcontainers-based integration tests)

### Run

```bash
dotnet restore
dotnet build
dotnet run --project src/ComplianceGuard.Api
```

The API starts at `http://localhost:5012` with Swagger UI at `/swagger`.

### Test

```bash
dotnet test                                     # all tests
dotnet run --project src/ComplianceGuard.Eval   # eval harness
```

### Optional: Enable LLM-powered features

Set an OpenAI API key to enable LLM orchestration in both the SK agent and the workflow risk assessment:

```bash
# Windows
setx MetrcOpenAiKey "sk-proj-..."

# Linux/macOS
export MetrcOpenAiKey="sk-proj-..."
```

Without a key, both detection paths run in deterministic mode — all rule-based checks still execute, and CI never requires an external API.

## CI Pipeline

The GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every push and PR:

1. **Build** — compile all projects
2. **Unit Tests** — fast, mocked, covers Application + Domain logic
3. **Integration Tests** — real SQL Server via Testcontainers, validates facility isolation
4. **API Tests** — full HTTP round-trips via WebApplicationFactory with in-memory database
5. **Eval Harness** — runs AI agent against golden scenarios, fails on regression vs. baseline

API tests swap `DapperTransferRepository` with an EF Core-backed stub to avoid SQL Server platform dependencies on Linux CI runners.

## Key Design Decisions

| Decision | Rationale |
|---|---|
| **Domain modeled on cannabis track-and-trace** | Entities (Facility, Package, Transfer, LabTest) mirror the real regulatory domain — manifests, RFID tags, lab compliance, licensed facilities |
| **Dapper for reads, EF Core for writes** | Transfer history queries can span many records — Dapper keeps reads fast; EF Core provides change tracking and migrations for writes |
| **Semantic Kernel behind an interface** | Application layer stays AI-framework-agnostic; the agent is a swappable infrastructure detail |
| **Agent Framework Workflows for compliance pipeline** | Graph-based execution with typed messages, conditional routing, and superstep synchronization — deterministic and auditable, ideal for regulatory workflows |
| **Dual AI integration (SK + Agent Framework)** | Demonstrates both LLM-orchestrated tool calling and structured workflow pipelines — two complementary patterns for AI in enterprise systems |
| **IChatClient (Microsoft.Extensions.AI)** | The workflow uses the new standard AI abstraction, decoupled from any specific LLM provider |
| **Eval harness as a CI gate** | LLM-powered features need deterministic quality gates — golden scenarios with real compliance violations catch regressions that unit tests can't |
| **Global query filters for facility isolation** | Structurally prevents cross-facility data leakage; harder to accidentally bypass than per-query filtering |
| **Clean architecture with dependency inversion** | Domain and Application layers are testable in isolation; Infrastructure can be swapped without ripple effects |

## Tech Stack

| Category | Technology |
|---|---|
| **Runtime** | .NET 8, C# 12 |
| **API** | ASP.NET Core Minimal APIs |
| **ORM** | EF Core 8 (writes, multi-tenancy filters) + Dapper (optimized reads) |
| **AI — Agent** | Microsoft Semantic Kernel 1.21 (plugins, LLM tool calling) |
| **AI — Workflows** | Microsoft Agent Framework 1.17 (directed-graph execution, conditional routing) |
| **AI — Abstraction** | Microsoft.Extensions.AI (IChatClient, provider-agnostic) |
| **LLM** | OpenAI gpt-4o-mini (optional — deterministic fallback when no key) |
| **Database** | SQL Server (LocalDB dev, Testcontainers test, service container CI) |
| **Testing** | xUnit, Moq, Testcontainers, WebApplicationFactory |
| **CI** | GitHub Actions |
