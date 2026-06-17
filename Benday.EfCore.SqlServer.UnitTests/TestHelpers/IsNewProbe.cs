using Benday.EfCore.Adapters;
using Benday.EfCore.DomainModels;
using Benday.EfCore.Entities;

namespace Benday.EfCore.SqlServer.UnitTests.TestHelpers;

/// <summary>Minimal domain model used only to satisfy adapter constraints.</summary>
public class IdentityProbeModel<TIdentity> : DomainModelBase<TIdentity>
    where TIdentity : IEquatable<TIdentity>
{
}

/// <summary>Minimal entity used only to satisfy adapter constraints.</summary>
public class IdentityProbeEntity<TIdentity> : EntityBase<TIdentity>
    where TIdentity : IEquatable<TIdentity>
{
}

/// <summary>
/// Thin adapter subclass that exposes the protected <c>IsNew</c> seam so the
/// default identity-default detection can be tested for any key type.
/// </summary>
public class IsNewProbeAdapter<TIdentity>
    : AdapterBase<IdentityProbeModel<TIdentity>, IdentityProbeEntity<TIdentity>, TIdentity>
    where TIdentity : IEquatable<TIdentity>
{
    public bool CallIsNew(TIdentity id) => IsNew(id);

    protected override void PerformAdapt(
        IdentityProbeModel<TIdentity> fromValue, IdentityProbeEntity<TIdentity> toValue) { }

    protected override void PerformAdapt(
        IdentityProbeEntity<TIdentity> fromValue, IdentityProbeModel<TIdentity> toValue) { }
}
