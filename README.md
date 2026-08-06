# ComplianceGuard

A multi-tenant supply-chain compliance monitoring API modeled on cannabis track-and-trace regulatory systems. Built with .NET 8, it tracks packages from cultivation through retail sale across licensed facilities, uses a Semantic Kernel agent to detect anomalies in transfer manifests, and includes an evaluation harness to prevent AI regressions in CI.

> **Why this project?** Cannabis regulatory platforms enforce strict chain-of-custody rules across dozens of independently regulated state markets. Each licensed facility is a tenant with its own compliance obligations — packages must be tagged (RFID), tested (lab), manifested (transfers), and tracked at every handoff. This demo models that problem: multi-tenant facility isolation, package and transfer tracking, AI-powered anomaly detection on manifests, and a test-driven development workflow that treats the AI agent as a first-class component with its own regression suite.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      ComplianceGuard.Api                        │
│  Minimal API Endpoints · Tenant Middleware · DI Composition     │
│  /transfers  /packages  /facilities  /anomalies                 │
└──────────┬──────────────────────────────────┬───────────────────┘
           │                                  │
           ▼                                  ▼
┌─────────────────────────┐    ┌──────────────────────────────────┐
│ ComplianceGuard.         │    │ ComplianceGuard.Infrastructure   │
│ Application              │    │                                  │
│                          │    │  Persistence/                    │
│  Transfer handlers       │◄───│    AppDbContext (EF Core)        │
│  AnomalyDetectionService │    │    DapperTransferRepository      │
│  ReviewAnomalyHandler    │    │  Ai/                             │
│                          │    │    SemanticKernelFactory          │
└──────────┬───────────────┘    │    AnomalyDetectionAgent         │
           │                    │    Plugins/                       │
           ▼                    │      CustodyAnomalyPlugin        │
┌──────────────────────────┐    └──────────────┬───────────────────┘
│ ComplianceGuard.Domain   │                   │
│                          │◄──────────────────┘
│  Entities/               │
│    Facility, Package,    │
│    Transfer, LabTest,    │
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
| `ComplianceGuard.Infrastructure` | EF Core, Dapper, Semantic Kernel — implements Application interfaces |
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

The anomaly-detection pipeline uses Microsoft Semantic Kernel to identify compliance violations in the supply chain:

- **`AnomalyDetectionAgent`** implements `IAnomalyDetectionService` (defined in Application), keeping the business logic AI-framework-agnostic.
- **`CustodyAnomalyPlugin`** exposes kernel functions that the agent orchestrates:
  - `detect_transfer_timing_gap` — suspicious delays between departure and arrival
  - `detect_facility_distance_violation` — physically impossible transit times between facilities
  - `detect_package_quantity_discrepancy` — mismatches between manifested and received package counts
  - `detect_lab_test_anomaly` — missing required tests or suspicious result patterns
- **`SemanticKernelFactory`** builds and configures the kernel with registered plugins.

The Application layer calls `IAnomalyDetectionService` without knowing it's backed by Semantic Kernel — the agent could be swapped for a different LLM framework or a rules engine without touching business logic.

## Eval Harness

AI features need regression testing beyond unit tests. `ComplianceGuard.Eval` is a standalone CLI that:

1. Loads golden scenarios from JSON files — each represents a real compliance risk:
   - **Transfer timing gap** — 72-hour delay on a 2-hour route
   - **Facility distance violation** — package received 400 miles away in 30 minutes
   - **Package quantity discrepancy** — 3 packages missing from a 50-package manifest
   - **Missing lab test** — package transferred to dispensary without passing lab results
2. Runs the anomaly-detection agent against each scenario
3. Scores results against expected outcomes
4. Compares to `BaselineResults.json` (committed to the repo)
5. Fails the CI build if scores regress

```bash
dotnet run --project src/ComplianceGuard.Eval
```

## Getting Started

```bash
dotnet restore
dotnet build
dotnet run --project src/ComplianceGuard.Api
dotnet test
dotnet run --project src/ComplianceGuard.Eval
```

## CI Pipeline

The GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every push and PR:

1. **Build** — compile all projects
2. **Unit Tests** — fast, mocked, covers Application + Domain logic
3. **Integration Tests** — real SQL Server via service container, validates facility isolation
4. **API Tests** — full HTTP round-trips via WebApplicationFactory
5. **Eval Harness** — runs AI agent against golden scenarios, fails on regression vs. baseline

## Key Design Decisions

| Decision | Rationale |
|---|---|
| **Domain modeled on cannabis track-and-trace** | Entities (Facility, Package, Transfer, LabTest) mirror the real regulatory domain — manifests, RFID tags, lab compliance, licensed facilities |
| **Dapper for reads, EF Core for writes** | Transfer history queries can span many records — Dapper keeps reads fast; EF Core provides change tracking and migrations for writes |
| **Semantic Kernel behind an interface** | Application layer stays AI-framework-agnostic; the agent is a swappable infrastructure detail |
| **Eval harness as a CI gate** | LLM-powered features need deterministic quality gates — golden scenarios with real compliance violations catch regressions that unit tests can't |
| **Global query filters for facility isolation** | Structurally prevents cross-facility data leakage; harder to accidentally bypass than per-query filtering |
| **Clean architecture with dependency inversion** | Domain and Application layers are testable in isolation; Infrastructure can be swapped without ripple effects |