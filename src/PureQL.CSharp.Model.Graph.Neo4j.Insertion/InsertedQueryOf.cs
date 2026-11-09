using Neo4j.Driver;
using PureQL.CSharp.Model.Graph.Neo4j.Abstractions;

namespace PureQL.CSharp.Model.Graph.Neo4j.Insertion;

/// <summary>
/// A stored query written to the Neo4j database of <see cref="Store"/> in a transaction
/// of its own.
/// </summary>
/// <param name="Store">The database to write to.</param>
/// <param name="Query">The query to write, with its id.</param>
/// <remarks>
/// Opens a session on the store's <see cref="INeo4jStoredQueries.Driver"/> against its
/// <see cref="INeo4jStoredQueries.Database"/> and runs <see cref="InsertedQuery"/> in a
/// managed write transaction (<c>IAsyncSession.ExecuteWriteAsync</c>), which the
/// driver retries on transient errors. To write together with other operations,
/// run <see cref="InsertedQuery"/> on a transaction of your own instead.
/// </remarks>
public sealed record InsertedQueryOf(INeo4jStoredQueries Store, StoredQuery Query)
{
    /// <summary>Writes the query and commits.</summary>
    /// <param name="cancellationToken">
    /// Checked before the session is opened and before the statement is sent; the
    /// driver cannot cancel a running transaction.
    /// </param>
    /// <returns>A task that completes once the transaction has committed.</returns>
    /// <exception cref="ClientException">
    /// The id is already stored and unique (code
    /// <c>Neo.ClientError.Schema.ConstraintValidationFailed</c>), or the server rejects
    /// the statement. Nothing is written.
    /// </exception>
    /// <exception cref="GraphMappingException">
    /// The query holds a default union or a null required value.
    /// </exception>
    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(Store);
        ArgumentNullException.ThrowIfNull(Query);
        cancellationToken.ThrowIfCancellationRequested();
        await using IAsyncSession session = Store.Driver.AsyncSession(builder =>
            builder.WithDatabase(Store.Database)
        );
        await session.ExecuteWriteAsync(runner =>
            new InsertedQuery(runner, Query).ApplyAsync(cancellationToken)
        );
    }
}
