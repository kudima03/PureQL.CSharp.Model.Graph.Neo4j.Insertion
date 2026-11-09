using Neo4j.Driver;

namespace PureQL.CSharp.Model.Graph.Neo4j.Insertion.Tests;

/// <summary>A relationship as stored: its type, position and the child's element id.</summary>
internal sealed record StoredRelationship(string Type, long? Position, string To);

/// <summary>A node as stored, with its outgoing relationships.</summary>
internal sealed record StoredNode(
    string ElementId,
    IReadOnlyList<string> Labels,
    IReadOnlyDictionary<string, object> Properties,
    IReadOnlyList<StoredRelationship> Relationships
)
{
    /// <summary>The record label: neither <c>Node</c> nor <c>Query</c>.</summary>
    public string Label =>
        Labels.Single(label => label is not (GraphLabels.Node or GraphLabels.Query));
}

/// <summary>Every node of one query read back from Neo4j, and the query rebuilt.</summary>
internal sealed record StoredGraph(IReadOnlyList<StoredNode> Nodes)
{
    private const string Cypher = """
        MATCH (n:Node {queryId: $id})
        RETURN elementId(n) AS id, labels(n) AS labels, properties(n) AS props,
            [(n)-[r]->(m) | {type: type(r), position: r.position, to: elementId(m)}]
                AS out
        """;

    public static async Task<StoredGraph> ReadAsync(Neo4jFixture neo4j, Guid id)
    {
        List<StoredNode> nodes = [];
        foreach (IRecord record in await neo4j.RunAsync(Cypher, id))
        {
            nodes.Add(
                new StoredNode(
                    record["id"].As<string>(),
                    record["labels"].As<List<string>>(),
                    record["props"].As<Dictionary<string, object>>(),
                    [
                        .. record["out"]
                            .As<List<Dictionary<string, object>>>()
                            .Select(r => new StoredRelationship(
                                (string)r["type"],
                                r["position"] is long position ? position : null,
                                (string)r["to"]
                            )),
                    ]
                )
            );
        }

        return new StoredGraph(nodes);
    }

    public StoredNode Root => Nodes.Single(n => n.Labels.Contains(GraphLabels.Query));

    /// <summary>The stored root as the Graph package reads it.</summary>
    public IReadNode ReadRoot()
    {
        Dictionary<string, Dictionary<string, IReadOnlyList<IReadNode>>> children = [];
        Dictionary<string, IReadNode> read = [];
        foreach (StoredNode node in Nodes)
        {
            Dictionary<string, IReadOnlyList<IReadNode>> lists =
            [
                with(StringComparer.Ordinal),
            ];
            children[node.ElementId] = lists;
            read[node.ElementId] = new ReadNode(
                node.Label,
                node.Properties.ToDictionary(p => p.Key, p => (object?)p.Value),
                lists
            );
        }

        foreach (StoredNode node in Nodes)
        {
            foreach (
                IGrouping<string, StoredRelationship> type in node.Relationships.GroupBy(
                    r => r.Type
                )
            )
            {
                children[node.ElementId][type.Key] =
                [
                    .. type.OrderBy(r => r.Position ?? 0).Select(r => read[r.To]),
                ];
            }
        }

        return read[Root.ElementId];
    }
}
