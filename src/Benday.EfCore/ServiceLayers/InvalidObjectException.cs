namespace Benday.EfCore.ServiceLayers;

/// <summary>
/// Invalid object exception thrown when validation fails in the service layer.
/// </summary>
public class InvalidObjectException : InvalidOperationException
{
    /// <summary>
    /// Creates an exception describing an invalid object.
    /// </summary>
    public InvalidObjectException(string message) : base(message) { }
}
