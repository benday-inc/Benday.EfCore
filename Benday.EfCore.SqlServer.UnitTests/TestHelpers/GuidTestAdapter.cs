using Benday.EfCore.Adapters;

namespace Benday.EfCore.SqlServer.UnitTests.TestHelpers;

/// <summary>
/// Adapter between <see cref="GuidChildDomainModel"/> and <see cref="GuidChildEntity"/>.
/// </summary>
public class GuidChildAdapter : AdapterBase<GuidChildDomainModel, GuidChildEntity, Guid>
{
    protected override void PerformAdapt(GuidChildDomainModel fromValue, GuidChildEntity toValue)
    {
        toValue.Value = fromValue.Value;
    }

    protected override void PerformAdapt(GuidChildEntity fromValue, GuidChildDomainModel toValue)
    {
        toValue.Id = fromValue.Id;
        toValue.Value = fromValue.Value;
    }
}

/// <summary>
/// Adapter between <see cref="GuidTestDomainModel"/> and <see cref="GuidTestEntity"/>,
/// including the child collection merge.
/// </summary>
public class GuidTestAdapter : AdapterBase<GuidTestDomainModel, GuidTestEntity, Guid>
{
    private readonly GuidChildAdapter _childAdapter = new();

    protected override void PerformAdapt(GuidTestDomainModel fromValue, GuidTestEntity toValue)
    {
        toValue.Name = fromValue.Name;
        toValue.Status = fromValue.Status;
        toValue.CreatedBy = fromValue.CreatedBy;
        toValue.CreatedDate = fromValue.CreatedDate;
        toValue.LastModifiedBy = fromValue.LastModifiedBy;
        toValue.LastModifiedDate = fromValue.LastModifiedDate;
        toValue.Timestamp = fromValue.Timestamp;
        _childAdapter.Adapt(fromValue.Children, toValue.Children);
    }

    protected override void PerformAdapt(GuidTestEntity fromValue, GuidTestDomainModel toValue)
    {
        toValue.Id = fromValue.Id;
        toValue.Name = fromValue.Name;
        toValue.Status = fromValue.Status;
        toValue.CreatedBy = fromValue.CreatedBy;
        toValue.CreatedDate = fromValue.CreatedDate;
        toValue.LastModifiedBy = fromValue.LastModifiedBy;
        toValue.LastModifiedDate = fromValue.LastModifiedDate;
        toValue.Timestamp = fromValue.Timestamp;
        toValue.Children = new List<GuidChildDomainModel>();
        _childAdapter.Adapt(fromValue.Children, toValue.Children);
    }
}
