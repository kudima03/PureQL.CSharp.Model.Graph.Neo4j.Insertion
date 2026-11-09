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

## Installation

```
dotnet add package PureQL.CSharp.Model.Graph.Neo4j.Insertion
```

## Related packages

- [`PureQL.CSharp.Model.Graph`](https://github.com/kudima03/PureQL.CSharp.Model.Graph) — maps a `PureQLQuery` to a property graph and back, without a driver
- [`PureQL.CSharp.Model.Graph.Neo4j.Abstractions`](https://github.com/kudima03/PureQL.CSharp.Model.Graph.Neo4j.Abstractions) — the stored-queries contract the Neo4j packages share
