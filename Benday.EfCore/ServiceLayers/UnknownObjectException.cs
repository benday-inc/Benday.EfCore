namespace Benday.EfCore.ServiceLayers;

/// <summary>
/// Exception thrown when an entity cannot be found by Id.
/// </summary>
public class UnknownObjectException : InvalidOperationException
{
    /// <summary>
    /// Creates an exception describing an object that could not be located.
    /// </summary>
    public UnknownObjectException(string typeName, object id)
        : base($"Could not locate a {typeName} with an id of '{id}'.") { }
}
