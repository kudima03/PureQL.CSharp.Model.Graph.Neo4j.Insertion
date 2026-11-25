# Changelog

All notable changes to `PureQL.CSharp.Model.Graph.Neo4j.Insertion` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Tested with every query of `PureQL.CSharp.Model.Samples` `0.1.0-preview.4.1.0`
  (386, was 151: every sample and test of the specification it mirrors, the deepest
  queries included). The package is unchanged.

## [0.1.0-preview.1.0.0] - 2026-11-25

Follows `PureQL.CSharp.Model` `0.1.0-preview.15.0.0` (PureQL specification
`0.1.0-preview.3.0.0`). The public API is unchanged. Graphs written by an earlier
version are not read back: drop them and insert the queries again.

### Changed

- **Breaking:** depends on `PureQL.CSharp.Model.Graph` `0.1.0-preview.2.0.0` and
  `PureQL.CSharp.Model.Graph.Neo4j.Abstractions` `0.1.0-preview.2.0.0`, both on
  `PureQL.CSharp.Model` `0.1.0-preview.15.0.0`. The statement writes the nodes and
  relationships of the new graph shape as it writes every other: the rows of inline rows
  (`SubqueryRows.Rows`) as nodes labelled `List` and `Node` with `queryId`, linked by
  `Rows` relationships with a `position`, and the renamed labels and relationships of
  Graph `0.1.0-preview.2.0.0`.
- Tested with every query of `PureQL.CSharp.Model.Samples` `0.1.0-preview.4.0.0` (151,
  combined, window, inline-rows and recursive queries included), read back through
  `PureQL.CSharp.Model.Serialization` `0.1.0-preview.7.0.0`.

## [0.1.0-preview.0.2.0] - 2026-11-11

### Changed

- Targets .NET 11 with the GA SDK; the SDK is installed by version in the
  build and publish workflows instead of `global.json`.
- Depends on `PureQL.CSharp.Model.Graph` `0.1.0-preview.1.1.0` and
  `PureQL.CSharp.Model.Graph.Neo4j.Abstractions` `0.1.0-preview.1.1.0`.

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
