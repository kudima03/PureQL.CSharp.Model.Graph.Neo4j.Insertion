using Neo4j.Driver;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;
using PureQL.CSharp.Model.Samples.Queries.Dashboards;
using PureQL.CSharp.Model.Samples.Queries.Joins;

namespace PureQL.CSharp.Model.Graph.Neo4j.Insertion.Tests;

[Collection(Neo4jServer.Name)]
public sealed class InsertedQueryOfTests(Neo4jFixture neo4j)
{
    private readonly Neo4jFixture _neo4j = neo4j;

    [Fact]
    public async Task WritesTheQueryToTheStore()
    {
        StoredQuery query = new StoredQuery(
            Guid.NewGuid(),
            new ProductSalesPerformanceQuery().Value
        );
        QueryGraph graph = QueryGraphs.Of(query.Query, query.Id);

        await new InsertedQueryOf(
            new Neo4jStoredQueries(_neo4j.Driver),
            query
        ).ApplyAsync();
        StoredGraph stored = await StoredGraph.ReadAsync(_neo4j, query.Id);

        Assert.Equal(
            (graph.Nodes.Count, graph.Relationships.Count),
            await _neo4j.CountAsync(query.Id)
        );
        Assert.Equal(
            PureQLJson.Serialize(query.Query, typeof(PureQLQuery)),
            PureQLJson.Serialize(QueryGraphs.Read(stored.ReadRoot()), typeof(PureQLQuery))
        );
    }

    [Fact]
    public async Task DuplicateIdFailsWithTheConstraintViolationAndWritesNothing()
    {
        await _neo4j.ConstrainQueryIdsAsync();
        INeo4jStoredQueries store = new Neo4jStoredQueries(_neo4j.Driver);
        StoredQuery query = new StoredQuery(Guid.NewGuid(), new InnerJoinQuery().Value);
        await new InsertedQueryOf(store, query).ApplyAsync();
        (long Nodes, long Relationships) before = await _neo4j.CountAsync(query.Id);

        ClientException exception = await Assert.ThrowsAnyAsync<ClientException>(() =>
            new InsertedQueryOf(
                store,
                query with
                {
                    Query = new RightJoinQuery().Value,
                }
            ).ApplyAsync()
        );

        Assert.Equal("Neo.ClientError.Schema.ConstraintValidationFailed", exception.Code);
        Assert.Equal(before, await _neo4j.CountAsync(query.Id));
    }

    [Fact]
    public async Task CancelledTokenWritesNothing()
    {
        StoredQuery query = new StoredQuery(Guid.NewGuid(), new InnerJoinQuery().Value);

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new InsertedQueryOf(new Neo4jStoredQueries(_neo4j.Driver), query).ApplyAsync(
                new CancellationToken(true)
            )
        );

        Assert.Equal((0L, 0L), await _neo4j.CountAsync(query.Id));
    }

    [Fact]
    public async Task NullStoreOrQueryThrows()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new InsertedQueryOf(
                null!,
                new StoredQuery(Guid.NewGuid(), new InnerJoinQuery().Value)
            ).ApplyAsync()
        );
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new InsertedQueryOf(new Neo4jStoredQueries(_neo4j.Driver), null!).ApplyAsync()
        );
    }
}
