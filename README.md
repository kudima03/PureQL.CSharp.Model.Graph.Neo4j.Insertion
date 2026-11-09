# PureQL.CSharp.Model.Graph.Neo4j.Insertion

Writes a **PureQL** query to Neo4j as a property graph, in one statement.

[![.NET build & test](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Insertion/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Insertion/actions/workflows/build-and-test.yml)
[![Build and Deploy](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Insertion/actions/workflows/publish-nuget.yml/badge.svg?branch=main)](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Insertion/actions/workflows/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/PureQL.CSharp.Model.Graph.Neo4j.Insertion)](https://www.nuget.org/packages/PureQL.CSharp.Model.Graph.Neo4j.Insertion)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Overview

`PureQL.CSharp.Model.Graph.Neo4j.Insertion` is the insertion package of the `PureQL.CSharp.Model.Graph` family. It turns the `QueryGraph` that [`PureQL.CSharp.Model.Graph`](https://github.com/kudima03/PureQL.CSharp.Model.Graph) builds for a query into a single Cypher statement that creates every node and every relationship of the query at once.

## Public API

Namespace `PureQL.CSharp.Model.Graph.Neo4j.Insertion`:

| Name | Description |
|------|-------------|
| `InsertStatement` | `sealed record InsertStatement(QueryGraph Graph)`; `Value` is the `Neo4j.Driver.Query` that writes the graph |
| `InsertedQuery` | `sealed record InsertedQuery(IAsyncQueryRunner Runner, StoredQuery Query)`; `ApplyAsync` writes the query on a session or transaction you own |
| `InsertedQueryOf` | `sealed record InsertedQueryOf(INeo4jStoredQueries Store, StoredQuery Query)`; `ApplyAsync` opens a session on the store and writes the query in a managed write transaction |

## Usage

```csharp
using Neo4j.Driver;
using PureQL.CSharp.Model;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;
using PureQL.CSharp.Model.Graph.Neo4j.Insertion;

await using IDriver driver = GraphDatabase.Driver(
    "neo4j://localhost:7687",
    AuthTokens.Basic("neo4j", "password")
);
INeo4jStoredQueries store = new Neo4jStoredQueries(driver);
PureQLQuery query = /* any PureQL query */;
StoredQuery stored = new StoredQuery(Guid.NewGuid(), query);

// In a transaction of its own:
await new InsertedQueryOf(store, stored).ApplyAsync();

// Or together with other operations, in your transaction:
await using IAsyncSession session = driver.AsyncSession(
    builder => builder.WithDatabase(store.Database)
);
await session.ExecuteWriteAsync(
    async transaction =>
    {
        await new InsertedQuery(transaction, new StoredQuery(Guid.NewGuid(), query))
            .ApplyAsync();
        // ...other operations on the same transaction
    }
);
```

## How a query is written

`QueryGraphs.Of(query, id)` from [`PureQL.CSharp.Model.Graph`](https://github.com/kudima03/PureQL.CSharp.Model.Graph) gives the nodes and relationships of the query. `InsertStatement` writes them in one statement:

```cypher
UNWIND $nodes AS n
CREATE (x:$(n.labels))
SET x = n.props
WITH collect(x) AS xs
UNWIND $rels AS r
WITH xs[r.f] AS a, xs[r.t] AS b, r
CREATE (a)-[e:$(r.type)]->(b)
SET e.position = r.position
```

- `$nodes` holds each node's `labels` (the record label, `Node`, and `Query` on the root) and `props`; property values pass through unchanged (strings, longs, booleans and arrays of them). Every node has `queryId`; the root also has `id`.
- `$rels` holds each relationship's node indexes `f` and `t`, its `type` and, on a list item only, its `position`; a single child's relationship has no `position`.
- Labels and relationship types are dynamic (`$(...)`), which needs **Neo4j 5.26 or later** (`neo4j:5-community` and `neo4j:2025-community` both work).

## Duplicate ids

Inserting only creates; it never replaces a stored query (the Updating package does). When the uniqueness constraint on `Query.id` exists (the Creation package creates it as `pureql_query_id`), inserting an id that is already stored fails with the driver's `Neo4j.Driver.ClientException` whose `Code` is `Neo.ClientError.Schema.ConstraintValidationFailed`, and nothing is written. The exception is not wrapped. Without the constraint, a second copy is written.

## Cancellation

`Neo4j.Driver` 6.x takes no cancellation token for a running statement, so `ApplyAsync` checks its token before it sends the statement.

## Design Principles

- **Sealed records** with their dependencies in the constructor; no static services
- **Composable** — `InsertedQuery` runs on any `IAsyncQueryRunner`, a session or a transaction
- **AOT-compatible** — `IsAotCompatible=true`; no reflection

## Dependencies

- [`PureQL.CSharp.Model.Graph`](https://github.com/kudima03/PureQL.CSharp.Model.Graph) `0.1.0-preview.1.0.0` — `QueryGraphs`, `QueryGraph`
- [`PureQL.CSharp.Model.Graph.Neo4j.Abstractions`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Abstractions) `0.1.0-preview.1.0.0` — `INeo4jStoredQueries`, `StoredQuery`
- [`Neo4j.Driver`](https://www.nuget.org/packages/Neo4j.Driver) `6.5.1`

## Target Frameworks

- .NET 11

## Installation

```
dotnet add package PureQL.CSharp.Model.Graph.Neo4j.Insertion
```

## Related packages

- [`PureQL.CSharp.Model.Graph`](https://github.com/kudima03/PureQL.CSharp.Model.Graph) — maps a `PureQLQuery` to a property graph and back, without a driver
- [`PureQL.CSharp.Model.Graph.Neo4j.Abstractions`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Abstractions) — the stored-queries contract the Neo4j packages share
- [`PureQL.CSharp.Model.Graph.Neo4j.Creation`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Creation) — constraints and indexes
- [`PureQL.CSharp.Model.Graph.Neo4j.Querying`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Querying) — reads queries back
- [`PureQL.CSharp.Model.Graph.Neo4j.Deletion`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Deletion) — deletes queries
- [`PureQL.CSharp.Model.Graph.Neo4j.Updating`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Updating) — replaces a query in one transaction
- [`PureQL.CSharp.Model.Graph.Neo4j.Existence`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Existence) — checks the schema and queries exist
- [`PureQL.CSharp.Model.Graph.Neo4j.Destruction`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Destruction) — drops the schema and every stored query
- [`PureQL.CSharp.Model.Graph.Neo4j.Migration`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Migration) — migrates stored graphs between model versions
- [`PureQL.CSharp.Model.Serialization`](https://github.com/kudima03/PureQL.CSharp.Model.Serialization) — JSON for the same model
