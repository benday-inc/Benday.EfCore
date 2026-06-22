using System.ComponentModel.DataAnnotations;
using Benday.EfCore.DomainModels;

namespace Benday.EfCore.SqlServer.UnitTests.TestHelpers;

/// <summary>
/// Guid-keyed aggregate-root domain model for the generic identity tests.
/// </summary>
public class GuidTestDomainModel : CoreFieldsDomainModelBase<Guid>
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public List<GuidChildDomainModel> Children { get; set; } = new();
}

/// <summary>
/// Guid-keyed child domain model for the generic identity tests.
/// </summary>
public class GuidChildDomainModel : DomainModelBase<Guid>
{
    [Required]
    public string Value { get; set; } = string.Empty;
}
