using PureQL.CSharp.Model.Samples.Queries.Joins;

namespace PureQL.CSharp.Model.Graph.Neo4j.Insertion.Tests;

public sealed record InsertStatementTests
{
    private static readonly Guid Id = Guid.Parse("3c9e2b7a-58d1-4f06-9a4e-1b7d0c62e8f5");

    private static readonly string[] RootLabels = ["MainPlainQuery", "Node", "Query"];

    private static readonly string[] ChildLabels = ["FromEntity", "Node"];

    private static readonly long[] Numbers = [1, 2];

    private static readonly QueryGraph Graph = new QueryGraph(
        [
            new WrittenNode(
                0,
                RootLabels,
                new Dictionary<string, object>
                {
                    ["id"] = Id.ToString("D"),
                    ["queryId"] = Id.ToString("D"),
                    ["distinct"] = true,
                }
            ),
            new WrittenNode(
                1,
                ChildLabels,
                new Dictionary<string, object>
                {
                    ["queryId"] = Id.ToString("D"),
                    ["entity"] = "users",
                    ["values"] = Numbers,
                    ["take"] = 10L,
                }
            ),
            new WrittenNode(
                2,
                ChildLabels,
                new Dictionary<string, object> { ["queryId"] = Id.ToString("D") }
            ),
        ],
        [
            new WrittenRelationship(0, 1, "From", null),
            new WrittenRelationship(0, 2, "Columns", 0),
            new WrittenRelationship(0, 1, "Columns", 1),
        ]
    );

    [Fact]
    public void TextCreatesEveryNodeAndThenEveryRelationshipInOneStatement()
    {
        Assert.Equal(
            """
            UNWIND $nodes AS n
            CREATE (x:$(n.labels))
            SET x = n.props
            WITH collect(x) AS xs
            UNWIND $rels AS r
            WITH xs[r.f] AS a, xs[r.t] AS b, r
            CREATE (a)-[e:$(r.type)]->(b)
            SET e.position = r.position
            """,
            new InsertStatement(Graph).Value.Text
        );
    }

    [Fact]
    public void ParametersAreTheNodesAndTheRelationships()
    {
        Assert.Equal(
            ["nodes", "rels"],
            new InsertStatement(Graph).Value.Parameters.Keys.Order(StringComparer.Ordinal)
        );
    }

    [Fact]
    public void NodesHoldTheLabelsOfEveryNodeInIndexOrder()
    {
        Assert.Equal(
            [RootLabels, ChildLabels, ChildLabels],
            Nodes(new InsertStatement(Graph)).Select(node => (string[])node["labels"])
        );
    }

    [Fact]
    public void NodesHoldThePropertiesOfEveryNode()
    {
        Assert.Equal(
            Graph.Nodes.Select(node => node.Properties.OrderBy(p => p.Key)),
            Nodes(new InsertStatement(Graph))
                .Select(node => (IDictionary<string, object>)node["props"])
                .Select(properties => properties.OrderBy(p => p.Key))
        );
    }

    [Fact]
    public void NodePropertyValuesPassThroughUnchanged()
    {
        IDictionary<string, object> properties =
            (IDictionary<string, object>)Nodes(new InsertStatement(Graph))[1]["props"];

        Assert.Same(Numbers, properties["values"]);
    }

    [Fact]
    public void RelationshipsHoldTheIndexesOfTheirNodesAndTheirType()
    {
        Assert.Equal(
            [(0L, 1L, "From"), (0L, 2L, "Columns"), (0L, 1L, "Columns")],
            Relationships(new InsertStatement(Graph))
                .Select(r => ((long)r["f"], (long)r["t"], (string)r["type"]))
        );
    }

    [Fact]
    public void OnlyListRelationshipsHoldAPosition()
    {
        Assert.Equal(
            [null, 0L, 1L],
            Relationships(new InsertStatement(Graph))
                .Select(r => r.TryGetValue("position", out object? p) ? (long?)p : null)
        );
    }

    [Fact]
    public void SampleQueryGraphIsPassedWhole()
    {
        QueryGraph graph = QueryGraphs.Of(new InnerJoinQuery().Value, Id);

        InsertStatement statement = new InsertStatement(graph);

        Assert.Equal(
            (graph.Nodes.Count, graph.Relationships.Count),
            (Nodes(statement).Count, Relationships(statement).Count)
        );
    }

    [Fact]
    public void EqualGraphsGiveEqualStatements()
    {
        Assert.Equal(new InsertStatement(Graph), new InsertStatement(Graph));
    }

    [Fact]
    public void ValueThrowsOnANullGraph()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new InsertStatement(null!).Value);
    }

    private static List<IDictionary<string, object>> Nodes(InsertStatement statement)
    {
        return Maps(statement, "nodes");
    }

    private static List<IDictionary<string, object>> Relationships(
        InsertStatement statement
    )
    {
        return Maps(statement, "rels");
    }

    private static List<IDictionary<string, object>> Maps(
        InsertStatement statement,
        string parameter
    )
    {
        return
        [
            .. ((IEnumerable<object>)statement.Value.Parameters[parameter]).Cast<
                IDictionary<string, object>
            >(),
        ];
    }
}
