using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Insertion;

/// <summary>
/// The Cypher statement that writes a whole query graph at once: every node of
/// <see cref="Graph"/>, then every relationship between them.
/// </summary>
/// <param name="Graph">The query graph to write, such as one of
/// <see cref="QueryGraphs.Of(PureQLQuery, Guid)"/>.</param>
/// <remarks>
/// <para>
/// The statement is
/// <c>UNWIND $nodes AS n CREATE (x:$(n.labels)) SET x = n.props WITH collect(x) AS xs
/// UNWIND $rels AS r WITH xs[r.f] AS a, xs[r.t] AS b, r CREATE (a)-[e:$(r.type)]-&gt;(b)
/// SET e.position = r.position</c>, with two parameters. <c>$nodes</c> holds one map
/// per <see cref="WrittenNode"/>, in index order, with its <c>labels</c> (a list of
/// strings) and its <c>props</c> (the node's properties, whose values pass through
/// unchanged: strings, longs, booleans and arrays of them). <c>$rels</c> holds one map
/// per <see cref="WrittenRelationship"/> with the indexes of its nodes, <c>f</c> and
/// <c>t</c>, its <c>type</c> and, on a list item only, its <c>position</c>; setting a
/// missing position stores nothing, so a single child's relationship has no
/// <see cref="GraphProperties.Position"/> property.
/// </para>
/// <para>
/// Labels and relationship types are dynamic (<c>$(...)</c>), which needs Neo4j 5.26 or
/// later. The statement only creates: run it once per query id. With a uniqueness
/// constraint on the <see cref="GraphLabels.Query"/> node's
/// <see cref="GraphProperties.Id"/>, writing an id that is already stored fails with the
/// driver's <see cref="ClientException"/> whose <see cref="Neo4jException.Code"/> is
/// <c>Neo.ClientError.Schema.ConstraintValidationFailed</c>, and nothing is written.
/// </para>
/// </remarks>
public sealed record InsertStatement(QueryGraph Graph)
{
    private const string Text = """
        UNWIND $nodes AS n
        CREATE (x:$(n.labels))
        SET x = n.props
        WITH collect(x) AS xs
        UNWIND $rels AS r
        WITH xs[r.f] AS a, xs[r.t] AS b, r
        CREATE (a)-[e:$(r.type)]->(b)
        SET e.position = r.position
        """;

    /// <summary>The statement: its Cypher text and the graph as its parameters.</summary>
    public global::Neo4j.Driver.Query Value =>
        new global::Neo4j.Driver.Query(Text, Parameters(Graph));

    private static Dictionary<string, object> Parameters(QueryGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        List<object> nodes = [with(graph.Nodes.Count)];
        foreach (WrittenNode node in graph.Nodes)
        {
            nodes.Add(
                new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["labels"] = node.Labels.ToArray(),
                    ["props"] = new Dictionary<string, object>(
                        node.Properties,
                        StringComparer.Ordinal
                    ),
                }
            );
        }

        List<object> relationships = [with(graph.Relationships.Count)];
        foreach (WrittenRelationship relationship in graph.Relationships)
        {
            Dictionary<string, object> map = new Dictionary<string, object>(
                StringComparer.Ordinal
            )
            {
                ["f"] = (long)relationship.From,
                ["t"] = (long)relationship.To,
                ["type"] = relationship.Type,
            };
            if (relationship.Position is long position)
            {
                map[GraphProperties.Position] = position;
            }

            relationships.Add(map);
        }

        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["nodes"] = nodes,
            ["rels"] = relationships,
        };
    }
}
