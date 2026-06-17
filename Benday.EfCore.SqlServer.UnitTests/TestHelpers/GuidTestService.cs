using Benday.Common.Interfaces;
using Benday.EfCore.Adapters;
using Benday.EfCore.ServiceLayers;

namespace Benday.EfCore.SqlServer.UnitTests.TestHelpers;

/// <summary>
/// Guid-keyed service over the generic <see cref="CoreFieldsServiceLayerBase{TModel, TEntity, TIdentity}"/>.
/// Exercises the generic identity path end-to-end.
/// </summary>
public class GuidTestService : CoreFieldsServiceLayerBase<GuidTestDomainModel, GuidTestEntity, Guid>
{
    public GuidTestService(
        IAsyncReadableRepository<GuidTestEntity, Guid> repository,
        AdapterBase<GuidTestDomainModel, GuidTestEntity, Guid> adapter,
        IValidatorStrategy<GuidTestDomainModel> validator,
        IUsernameProvider usernameProvider)
        : base(repository, adapter, validator, usernameProvider) { }
}
