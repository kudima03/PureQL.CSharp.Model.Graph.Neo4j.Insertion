using System.Text.Json;
using System.Text.Json.Serialization;
using PureQL.CSharp.Model.Serialization;

namespace PureQL.CSharp.Model.Graph.Neo4j.Insertion.Tests;

/// <summary>JSON through every PureQL converter, to compare models.</summary>
internal static class PureQLJson
{
    /// <summary>The deepest queries nest past the default limit of 64.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static string Serialize(object value, Type type)
    {
        return JsonSerializer.Serialize(value, type, Options);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions { MaxDepth = 256 };
        foreach (JsonConverter converter in new PureQLConverters())
        {
            options.Converters.Add(converter);
        }

        return options;
    }
}
