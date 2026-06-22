using Benday.Common.Interfaces;
using Benday.EfCore.SqlServer.TestApi.DomainModels;

namespace Benday.EfCore.SqlServer.TestApi.Services;

/// <summary>
/// Service contract for <see cref="PersonDomainModel"/>. Extends the shared
/// <see cref="IAsyncService{T, TKey}"/> so callers deal only in domain models.
/// </summary>
public interface IPersonService : IAsyncService<PersonDomainModel, int>
{
}
