# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All `dotnet` commands must be run from the `./src` directory. The projects need the .NET 11 SDK (for the model's C# 15 unions); where the system SDK is older, use `~/.dotnet/dotnet` or put it first on `PATH` with `DOTNET_ROOT=$HOME/.dotnet`.

```bash
dotnet restore
dotnet build --no-restore -warnaserror
dotnet format --verify-no-changes          # check code style (CI enforces this)
dotnet format                              # auto-fix code style
dotnet test --no-build --verbosity normal  # run tests
dotnet csharpier check .                   # check formatting (CI enforces this)
dotnet pack --configuration Release -p:Version=<version> --output .
```

CI additionally passes `-p:AssemblyVersion` (pinned to the major) and `-p:FileVersion`; see `.github/workflows/publish-nuget.yml`.

## Architecture

This is the **insertion package** of the `PureQL.CSharp.Model.Graph` family: it writes a query graph to Neo4j in one statement. It depends on `PureQL.CSharp.Model.Graph` `0.1.0-preview.1.1.0`, `PureQL.CSharp.Model.Graph.Neo4j.Abstractions` `0.1.0-preview.1.1.0` and `Neo4j.Driver` `6.5.1`, by published NuGet version, and on no other operation package. Namespace `PureQL.CSharp.Model.Graph.Neo4j.Insertion`, one public type per file:

- `InsertStatement` — `sealed record (QueryGraph Graph)`; `Value` is the `Neo4j.Driver.Query` that creates every node and then every relationship of the graph: `UNWIND $nodes AS n CREATE (x:$(n.labels)) SET x = n.props WITH collect(x) AS xs UNWIND $rels AS r WITH xs[r.f] AS a, xs[r.t] AS b, r CREATE (a)-[e:$(r.type)]->(b) SET e.position = r.position`. `$nodes` maps hold `labels` and `props` (the `WrittenNode` values, unchanged); `$rels` maps hold `f`, `t`, `type` and `position` only for list items, so `SET e.position = null` leaves single children without one. Dynamic labels need Neo4j 5.26+.
- `InsertedQuery` — `sealed record (IAsyncQueryRunner Runner, StoredQuery Query)`; `ApplyAsync` runs the statement of `QueryGraphs.Of(Query.Query, Query.Id)` on the runner and consumes the result. A session commits it on its own; a transaction commits it with its other work (the Updating package composes it after a deletion).
- `InsertedQueryOf` — `sealed record (INeo4jStoredQueries Store, StoredQuery Query)`; `ApplyAsync` opens a session on `Store.Driver` for `Store.Database` and runs `InsertedQuery` inside `ExecuteWriteAsync`.

The driver's `RunAsync` and `ExecuteWriteAsync` take no cancellation token, so `ApplyAsync` checks its token before sending. A duplicate id under the `Query.id` uniqueness constraint fails with the driver's `ClientException` (`Neo.ClientError.Schema.ConstraintValidationFailed`): it is documented and propagates unwrapped. Insertion never replaces a stored query.

The enclosing namespace `PureQL.CSharp.Model` has its own `Query` (a subquery), and `Neo4j` inside this namespace means `PureQL.CSharp.Model.Graph.Neo4j`: write the driver's query type as `global::Neo4j.Driver.Query`.

**Test project:** `PureQL.CSharp.Model.Graph.Neo4j.Insertion.Tests` targets `net11.0` and uses xunit with coverlet. `InsertStatementTests` checks the Cypher text and parameters without a server. The integration tests share one `neo4j:5-community` container (`Neo4jFixture`, `Testcontainers.Neo4j`, random host port, no authentication; Docker must be running) through the `Neo4jServer` collection, and give every insert a fresh id, so they never clean up. For each of the 86 `PureQL.CSharp.Model.Samples` queries they compare the stored node and relationship counts, the root's labels and ids, every node's labels and properties and every relationship's type and position with the `QueryGraph`, and read the nodes back (`StoredGraph`) into `ReadNode`s, `QueryGraphs.Read` them and compare JSON through the `PureQL.CSharp.Model.Serialization` converters. Other tests create the `pureql_query_id` uniqueness constraint and insert a duplicate id, roll back a transaction holding two inserts, and run `InsertedQueryOf` end to end.

**Target framework:** net11.0 only, as the model. `IsAotCompatible` is `true`.

**Package validation:** enabled against `PackageValidationBaselineVersion` `0.1.0-preview.0.2.0`, the previous release; the `nuget-publish` skill moves it after each release, and intentional breaks go to `CompatibilitySuppressions.xml` next to the csproj.

**Mutation testing:** the Stryker step of CI is commented out.

**Publishing:** triggered by pushing a semver tag (pattern `*.*.*`). The tag becomes the `PackageVersion`. The workflow publishes to both GitHub Packages and NuGet.org.

## Code Style

Enforced via `.editorconfig` and `dotnet format --verify-no-changes` in CI:

- No `var` — always use explicit types
- No expression-bodied methods or constructors — use block bodies
- Expression-bodied properties and accessors are required
- File-scoped namespace declarations (`namespace Foo.Bar;`)
- Private fields prefixed with `_` in camelCase (e.g., `_options`)
- Braces always required, even for single-statement blocks
- Max line length: 90 characters
- Allman brace style — opening brace on its own line for all blocks
- `System.*` using directives sorted first, no blank line between using groups
- Parentheses required in arithmetic and relational binary operators for clarity

## Commit Messages

Do not mention Claude or AI assistance in commit messages.
