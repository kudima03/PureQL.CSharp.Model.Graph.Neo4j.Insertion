using System.Reflection;
using PureQL.CSharp.Model.Samples.Queries.Joins;

namespace PureQL.CSharp.Model.Graph.Neo4j.Insertion.Tests;

/// <summary>Every query of <c>PureQL.CSharp.Model.Samples</c>, by name.</summary>
internal static class QuerySamples
{
    public static IReadOnlyDictionary<string, PureQLQuery> All { get; } = Load();

    public static TheoryData<string> Names()
    {
        TheoryData<string> names = [];
        foreach (string name in All.Keys)
        {
            names.Add(name);
        }

        return names;
    }

    private static SortedDictionary<string, PureQLQuery> Load()
    {
        SortedDictionary<string, PureQLQuery> samples = [with(StringComparer.Ordinal)];
        foreach (Type type in typeof(InnerJoinQuery).Assembly.GetExportedTypes())
        {
            PropertyInfo? value = type.GetProperty("Value");
            if (
                type.IsClass
                && type.IsSealed
                && (value?.PropertyType == typeof(PureQLQuery))
            )
            {
                samples[type.FullName!] = (PureQLQuery)
                    value.GetValue(Activator.CreateInstance(type))!;
            }
        }

        return samples;
    }
}
