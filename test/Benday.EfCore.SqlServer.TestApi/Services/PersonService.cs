using Benday.Common.Interfaces;
using Benday.EfCore.ServiceLayers;
using Benday.EfCore.SqlServer.TestApi.Adapters;
using Benday.EfCore.SqlServer.TestApi.DomainModels;
using Benday.EfCore.SqlServer.TestApi.Repositories;

namespace Benday.EfCore.SqlServer.TestApi.Services;

/// <summary>
/// Service layer for <see cref="PersonDomainModel"/>. Inherits the audit-field
/// population and copy-back behavior from <see cref="CoreFieldsServiceLayerBase{TModel, TEntity}"/>:
/// validate → get/create entity → populate audit fields → adapt → save → copy fields back.
/// </summary>
public class PersonService :
    CoreFieldsServiceLayerBase<PersonDomainModel, Person>,
    IPersonService
{
    /// <summary>
    /// Creates the service with its repository, adapter, validator, and username provider.
    /// </summary>
    public PersonService(
        IPersonRepository repository,
        PersonAdapter adapter,
        IValidatorStrategy<PersonDomainModel> validator,
        IUsernameProvider usernameProvider)
        : base(repository, adapter, validator, usernameProvider)
    {
    }
}
