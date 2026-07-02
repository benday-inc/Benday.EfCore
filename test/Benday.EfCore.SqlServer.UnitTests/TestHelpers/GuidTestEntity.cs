using System.ComponentModel.DataAnnotations;
using Benday.EfCore.Entities;

namespace Benday.EfCore.SqlServer.UnitTests.TestHelpers;

/// <summary>
/// Minimal Guid-keyed entity for testing the generic identity path.
/// </summary>
public class GuidTestEntity : CoreFieldsEntityBase<Guid>
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public List<GuidChildEntity> Children { get; set; } = new();

    public override IList<IDependentEntityCollection>? GetDependentEntities()
    {
        return new List<IDependentEntityCollection>
        {
            new DependentEntityCollection<GuidChildEntity, Guid>(Children)
        };
    }
}

/// <summary>
/// Minimal Guid-keyed child entity for testing dependent entity behavior.
/// </summary>
public class GuidChildEntity : EntityBase<Guid>
{
    public Guid ParentId { get; set; }
    public GuidTestEntity? Parent { get; set; }

    [Required]
    public string Value { get; set; } = string.Empty;
}
