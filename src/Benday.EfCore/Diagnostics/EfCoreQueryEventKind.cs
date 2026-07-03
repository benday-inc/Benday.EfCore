namespace Benday.EfCore.Diagnostics;

/// <summary>
/// Which kind of database command produced an
/// <see cref="EfCoreQueryDiagnostics"/> event. Parallels
/// <c>CosmosQueryEventKind</c>, mapped to EF Core's command model:
/// EF Core intercepts at the ADO.NET command layer, so the distinctions
/// are reader / scalar / non-query rather than point-op / page / total.
/// </summary>
public enum EfCoreQueryEventKind
{
    /// <summary>
    /// A command that returned a result set — a SELECT, and therefore
    /// most LINQ queries materialized via ToList/FirstOrDefault/etc.
    /// </summary>
    Reader,

    /// <summary>
    /// A scalar command executed via <c>ExecuteScalar</c>, e.g. a
    /// Count/Any/Max/Min aggregate that returns a single value.
    /// </summary>
    Scalar,

    /// <summary>
    /// A non-query command executed via <c>ExecuteNonQuery</c> —
    /// the INSERT/UPDATE/DELETE statements emitted by SaveChanges.
    /// </summary>
    NonQuery
}
