using System.Collections;

namespace PureQL.CSharp.Model.Graph.Neo4j.Insertion.Tests;

/// <summary>
/// Nodes and relationships as sorted text, to compare a written graph with what Neo4j
/// stored regardless of order and of the CLR types the driver hands back.
/// </summary>
internal static class GraphDescriptions
{
    public static IEnumerable<string> Nodes(QueryGraph graph)
    {
        return Sorted(graph.Nodes.Select(n => Node(n.Labels, n.Properties)));
    }

    public static IEnumerable<string> Nodes(StoredGraph stored)
    {
        return Sorted(stored.Nodes.Select(n => Node(n.Labels, n.Properties)));
    }

    public static IEnumerable<string> Relationships(QueryGraph graph)
    {
        return Sorted(
            graph.Relationships.Select(r =>
                Relationship(
                    Label(graph.Nodes[r.From].Labels),
                    r.Type,
                    r.Position,
                    Label(graph.Nodes[r.To].Labels)
                )
            )
        );
    }

    public static IEnumerable<string> Relationships(StoredGraph stored)
    {
        Dictionary<string, string> labels = stored.Nodes.ToDictionary(
            n => n.ElementId,
            n => n.Label
        );
        return Sorted(
            stored.Nodes.SelectMany(n =>
                n.Relationships.Select(r =>
                    Relationship(n.Label, r.Type, r.Position, labels[r.To])
                )
            )
        );
    }

    public static string Value(object value)
    {
        return value is IEnumerable items and not string
            ? $"[{string.Join(", ", items.Cast<object>())}]"
            : $"{value.GetType().Name}:{value}";
    }

    private static string Label(IReadOnlyList<string> labels)
    {
        return labels.Single(l => l is not (GraphLabels.Node or GraphLabels.Query));
    }

    private static string Node(
        IEnumerable<string> labels,
        IReadOnlyDictionary<string, object> properties
    )
    {
        return string.Join(":", labels.Order(StringComparer.Ordinal))
            + " "
            + string.Join(
                ", ",
                properties
                    .OrderBy(p => p.Key, StringComparer.Ordinal)
                    .Select(p => $"{p.Key}={Value(p.Value)}")
            );
    }

    private static string Relationship(
        string from,
        string type,
        long? position,
        string to
    )
    {
        return position is long index
            ? $"({from})-[{type} {{position: {index}}}]->({to})"
            : $"({from})-[{type}]->({to})";
    }

    private static IEnumerable<string> Sorted(IEnumerable<string> descriptions)
    {
        return descriptions.Order(StringComparer.Ordinal);
    }
}
