using Neo4j.Driver;
using Testcontainers.Neo4j;

namespace PureQL.CSharp.Model.Graph.Neo4j.Insertion.Tests;

/// <summary>
/// One Neo4j server for every integration test, started from <c>neo4j:5-community</c>
/// on a random host port, without authentication.
/// </summary>
public sealed class Neo4jFixture : IAsyncLifetime
{
    private readonly Neo4jContainer _container = new Neo4jBuilder(
        "neo4j:5-community"
    ).Build();

    private IDriver? _driver;

    public IDriver Driver =>
        _driver ?? throw new InvalidOperationException("The server is not started.");

    public IAsyncSession Session()
    {
        return Driver.AsyncSession();
    }

    /// <summary>The uniqueness constraint the Creation package creates.</summary>
    public async Task ConstrainQueryIdsAsync()
    {
        _ = await RunAsync(
            "CREATE CONSTRAINT pureql_query_id IF NOT EXISTS "
                + "FOR (q:Query) REQUIRE q.id IS UNIQUE"
        );
    }

    public async Task<(long Nodes, long Relationships)> CountAsync(Guid id)
    {
        IRecord record = (
            await RunAsync(
                """
                MATCH (n:Node {queryId: $id})
                RETURN count(n) AS nodes, sum(COUNT { (n)-->() }) AS relationships
                """,
                id
            )
        ).Single();
        return (record["nodes"].As<long>(), record["relationships"].As<long>());
    }

    public async Task<List<IRecord>> RunAsync(string cypher, Guid? id = null)
    {
        Dictionary<string, object> parameters = [];
        if (id is Guid value)
        {
            parameters["id"] = value.ToString("D");
        }

        await using IAsyncSession session = Session();
        IResultCursor cursor = await session.RunAsync(cypher, parameters);
        return await cursor.ToListAsync();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _driver = GraphDatabase.Driver(_container.GetConnectionString(), AuthTokens.None);
    }

    public async Task DisposeAsync()
    {
        if (_driver is not null)
        {
            await _driver.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}
