using Benday.EfCore.Testing.Fakes;
using Benday.EfCore.SqlServer.TestApi;
using Benday.EfCore.SqlServer.TestApi.Repositories;

namespace Benday.EfCore.SqlServer.UnitTests.TestHelpers;

/// <summary>
/// In-memory <see cref="IPersonRepository"/> for service-layer tests. Inherits
/// all behavior (id assignment, child handling, call tracking) from
/// <see cref="InMemoryRepository{T}"/> and just adds the app's marker interface.
/// This is the intended pattern for testing a service without a database.
/// </summary>
public class InMemoryPersonRepository : InMemoryRepository<Person>, IPersonRepository
{
    /// <inheritdoc />
    public Task<IList<Person>> SearchByLastNameAsync(string lastName)
    {
        IList<Person> results = Items
            .Where(p => p.LastName == lastName)
            .ToList();

        return Task.FromResult(results);
    }

    /// <inheritdoc />
    public Task<IList<Person>> SearchByNoteTextAsync(string searchText)
    {
        IList<Person> results = Items
            .Where(p => p.Notes.Any(n => n.NoteText.Contains(searchText)))
            .ToList();

        return Task.FromResult(results);
    }
}
