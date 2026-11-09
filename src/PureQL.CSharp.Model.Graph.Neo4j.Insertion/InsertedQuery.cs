using Neo4j.Driver;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;

namespace PureQL.CSharp.Model.Graph.Neo4j.Insertion;

/// <summary>
/// A stored query written to Neo4j as a graph, on a runner the caller owns: a session,
/// where the statement commits on its own, or a transaction, where it commits with the
/// transaction's other work.
/// </summary>
/// <param name="Runner">The session or transaction to run the statement on.</param>
/// <param name="Query">The query to write, with its id.</param>
/// <remarks>
/// The graph is <see cref="QueryGraphs.Of(PureQLQuery, Guid)"/> of the query and its id,
/// written by one <see cref="InsertStatement"/>. Writing does not replace a query
/// already stored under the same id: with a uniqueness constraint on the
/// <see cref="GraphLabels.Query"/> node's <see cref="GraphProperties.Id"/>,
/// <see cref="ApplyAsync"/> fails with the driver's <see cref="ClientException"/>
/// whose <see cref="Neo4jException.Code"/> is
/// <c>Neo.ClientError.Schema.ConstraintValidationFailed</c>, and the statement writes
/// nothing; without one, a second copy of the query is written.
/// </remarks>
public sealed record InsertedQuery(IAsyncQueryRunner Runner, StoredQuery Query)
{
    /// <summary>Writes the query.</summary>
    /// <param name="cancellationToken">
    /// Checked before the statement is sent; the driver cannot cancel a running
    /// statement.
    /// </param>
    /// <returns>A task that completes once the statement has run.</returns>
    /// <exception cref="ClientException">
    /// The id is already stored and unique (code
    /// <c>Neo.ClientError.Schema.ConstraintValidationFailed</c>), or the server rejects
    /// the statement.
    /// </exception>
    /// <exception cref="GraphMappingException">
    /// The query holds a default union or a null required value.
    /// </exception>
    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(Runner);
        ArgumentNullException.ThrowIfNull(Query);
        cancellationToken.ThrowIfCancellationRequested();
        InsertStatement statement = new InsertStatement(
            QueryGraphs.Of(Query.Query, Query.Id)
        );
        IResultCursor cursor = await Runner.RunAsync(statement.Value);
        _ = await cursor.ConsumeAsync();
    }
}
