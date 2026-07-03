using Benday.Common.Interfaces;

namespace Benday.EfCore.SqlServer.TestApi.Repositories;

/// <summary>
/// Repository contract for <see cref="Person"/>. Extends the shared
/// <see cref="IAsyncReadableRepository{T, TKey}"/> so the service layer
/// depends only on the storage-agnostic interface.
/// </summary>
public interface IPersonRepository : IAsyncReadableRepository<Person, int>
{
    /// <summary>
    /// Returns all people with the supplied last name, notes eager-loaded.
    /// </summary>
    Task<IList<Person>> SearchByLastNameAsync(string lastName);

    /// <summary>
    /// Returns all people who have at least one note whose text contains the
    /// supplied search term, notes eager-loaded.
    /// </summary>
    Task<IList<Person>> SearchByNoteTextAsync(string searchText);
}
