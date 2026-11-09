using Neo4j.Driver;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;
using PureQL.CSharp.Model.Samples.Queries.Joins;
using PureQL.CSharp.Model.Samples.Queries.Select;

namespace PureQL.CSharp.Model.Graph.Neo4j.Insertion.Tests;

[Collection(Neo4jServer.Name)]
public sealed class InsertedQueryTests(Neo4jFixture neo4j)
{
    private readonly Neo4jFixture _neo4j = neo4j;

    [Fact]
    public void EverySampleIsLoaded()
    {
        Assert.Equal(86, QuerySamples.All.Count);
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public async Task StoresAsManyNodesAndRelationshipsAsTheGraph(string name)
    {
        StoredQuery query = new StoredQuery(Guid.NewGuid(), QuerySamples.All[name]);
        QueryGraph graph = QueryGraphs.Of(query.Query, query.Id);

        await InsertAsync(query);

        Assert.Equal(
            (graph.Nodes.Count, graph.Relationships.Count),
            await _neo4j.CountAsync(query.Id)
        );
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public async Task StoresTheRootWithItsLabelsAndId(string name)
    {
        StoredQuery query = new StoredQuery(Guid.NewGuid(), QuerySamples.All[name]);
        QueryGraph graph = QueryGraphs.Of(query.Query, query.Id);

        await InsertAsync(query);
        StoredNode root = (await StoredGraph.ReadAsync(_neo4j, query.Id)).Root;

        Assert.Equal(
            [graph.Nodes[0].Labels[0], GraphLabels.Node, GraphLabels.Query],
            root.Labels.Order(StringComparer.Ordinal)
                .OrderBy(l => l is GraphLabels.Node or GraphLabels.Query)
        );
        Assert.Equal(query.Id.ToString("D"), root.Properties[GraphProperties.Id]);
        Assert.Equal(query.Id.ToString("D"), root.Properties[GraphProperties.QueryId]);
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public async Task StoresEveryNodeWithItsLabelsAndProperties(string name)
    {
        StoredQuery query = new StoredQuery(Guid.NewGuid(), QuerySamples.All[name]);

        await InsertAsync(query);

        Assert.Equal(
            GraphDescriptions.Nodes(QueryGraphs.Of(query.Query, query.Id)),
            GraphDescriptions.Nodes(await StoredGraph.ReadAsync(_neo4j, query.Id))
        );
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public async Task StoresAPositionOnListRelationshipsOnly(string name)
    {
        StoredQuery query = new StoredQuery(Guid.NewGuid(), QuerySamples.All[name]);

        await InsertAsync(query);

        Assert.Equal(
            GraphDescriptions.Relationships(QueryGraphs.Of(query.Query, query.Id)),
            GraphDescriptions.Relationships(await StoredGraph.ReadAsync(_neo4j, query.Id))
        );
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public async Task ReadsBackAsTheSameQuery(string name)
    {
        StoredQuery query = new StoredQuery(Guid.NewGuid(), QuerySamples.All[name]);

        await InsertAsync(query);
        StoredGraph stored = await StoredGraph.ReadAsync(_neo4j, query.Id);

        Assert.Equal(
            PureQLJson.Serialize(query.Query, typeof(PureQLQuery)),
            PureQLJson.Serialize(QueryGraphs.Read(stored.ReadRoot()), typeof(PureQLQuery))
        );
    }

    [Fact]
    public async Task StoresLiteralValuesAsTheGraphHoldsThem()
    {
        StoredQuery query = new StoredQuery(
            Guid.NewGuid(),
            new LiteralOfEveryTypeNextToFieldQuery().Value
        );

        await InsertAsync(query);
        List<IRecord> records = await _neo4j.RunAsync(
            "MATCH (n:Node {queryId: $id}) WHERE n.value IS NOT NULL RETURN n.value AS v",
            query.Id
        );

        Assert.Equal(
            QueryGraphs
                .Of(query.Query, query.Id)
                .Nodes.Where(n => n.Properties.ContainsKey("value"))
                .Select(n => GraphDescriptions.Value(n.Properties["value"]))
                .Order(StringComparer.Ordinal),
            records
                .Select(r => GraphDescriptions.Value(r["v"]))
                .Order(StringComparer.Ordinal)
        );
        Assert.Contains(records, r => r["v"] is string);
        Assert.Contains(records, r => r["v"] is long);
        Assert.Contains(records, r => r["v"] is bool);
    }

    [Fact]
    public async Task DuplicateIdFailsWithTheConstraintViolationAndWritesNothing()
    {
        await _neo4j.ConstrainQueryIdsAsync();
        StoredQuery query = new StoredQuery(Guid.NewGuid(), new InnerJoinQuery().Value);
        await InsertAsync(query);
        (long Nodes, long Relationships) before = await _neo4j.CountAsync(query.Id);

        ClientException exception = await Assert.ThrowsAnyAsync<ClientException>(() =>
            InsertAsync(query with { Query = new LeftJoinNullableFieldsQuery().Value })
        );

        Assert.Equal("Neo.ClientError.Schema.ConstraintValidationFailed", exception.Code);
        Assert.Equal(before, await _neo4j.CountAsync(query.Id));
    }

    [Fact]
    public async Task WritesWithTheTransactionItRunsOn()
    {
        StoredQuery first = new StoredQuery(Guid.NewGuid(), new InnerJoinQuery().Value);
        StoredQuery second = new StoredQuery(Guid.NewGuid(), new FullJoinQuery().Value);
        await using IAsyncSession session = _neo4j.Session();

        await using (
            IAsyncTransaction transaction = await session.BeginTransactionAsync()
        )
        {
            await new InsertedQuery(transaction, first).ApplyAsync();
            await new InsertedQuery(transaction, second).ApplyAsync();
            await transaction.RollbackAsync();
        }

        Assert.Equal((0L, 0L), await _neo4j.CountAsync(first.Id));
        Assert.Equal((0L, 0L), await _neo4j.CountAsync(second.Id));
    }

    [Fact]
    public async Task CancelledTokenWritesNothing()
    {
        StoredQuery query = new StoredQuery(Guid.NewGuid(), new InnerJoinQuery().Value);
        await using IAsyncSession session = _neo4j.Session();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new InsertedQuery(session, query).ApplyAsync(new CancellationToken(true))
        );

        Assert.Equal((0L, 0L), await _neo4j.CountAsync(query.Id));
    }

    [Fact]
    public async Task NullRunnerOrQueryThrows()
    {
        await using IAsyncSession session = _neo4j.Session();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new InsertedQuery(
                null!,
                new StoredQuery(Guid.NewGuid(), new InnerJoinQuery().Value)
            ).ApplyAsync()
        );
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new InsertedQuery(session, null!).ApplyAsync()
        );
    }

    public static TheoryData<string> Samples()
    {
        return QuerySamples.Names();
    }

    private async Task InsertAsync(StoredQuery query)
    {
        await using IAsyncSession session = _neo4j.Session();
        await new InsertedQuery(session, query).ApplyAsync();
    }
}
