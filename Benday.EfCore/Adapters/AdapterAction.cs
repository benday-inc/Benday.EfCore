namespace Benday.EfCore.Adapters;

/// <summary>
/// Controls what happens to an individual item during adaptation.
/// Returned from BeforeAdapt hooks.
/// </summary>
public enum AdapterAction
{
    /// <summary>Proceed with the normal adapt/copy.</summary>
    Adapt,
    /// <summary>Skip this item — don't copy, don't add.</summary>
    Skip,
    /// <summary>Mark this item for deletion.</summary>
    Delete
}
