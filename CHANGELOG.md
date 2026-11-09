# Changelog

All notable changes to `PureQL.CSharp.Model.Graph.Neo4j.Insertion` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-preview.0.1.0] - 2026-11-09

First release: writes a stored PureQL query graph to Neo4j in one statement.

### Added

- **`InsertStatement`** — `sealed record InsertStatement(QueryGraph Graph)`:
  one batched `UNWIND` Cypher statement that creates every node of the query
  graph and then every relationship between them, with dynamic labels and
  relationship types (needs Neo4j 5.26 or later).
- **`InsertedQuery`** — runs the insert statement of a `StoredQuery` on any
  `IAsyncQueryRunner` (a session or a transaction), so an insertion composes
  inside a caller's transaction.
- **`InsertedQueryOf`** — inserts into an `INeo4jStoredQueries` store, in a
  write transaction against its database.
- A duplicate id under the `Query.id` uniqueness constraint fails with the
  driver's `ClientException`
  (`Neo.ClientError.Schema.ConstraintValidationFailed`); insertion never
  replaces a stored query.
- Depends on `PureQL.CSharp.Model.Graph` `0.1.0-preview.1.0.0`,
  `PureQL.CSharp.Model.Graph.Neo4j.Abstractions` `0.1.0-preview.1.0.0` and
  `Neo4j.Driver` `6.5.1`; targets .NET 11 and is AOT-compatible.
