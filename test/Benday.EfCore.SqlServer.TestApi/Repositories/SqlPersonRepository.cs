using Benday.EfCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer.TestApi.Repositories;

/// <summary>
/// SQL Server repository for <see cref="Person"/>. Eager-loads the child
/// notes so the aggregate round-trips intact, and inherits the async CRUD +
/// dependent-entity save lifecycle from the base class.
/// </summary>
public class SqlPersonRepository :
    EfCoreCrudRepositoryBase<Person, TestDbContext>,
    IPersonRepository
{
    /// <summary>
    /// Creates the repository over the supplied <see cref="TestDbContext"/>.
    /// </summary>
    public SqlPersonRepository(TestDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    protected override DbSet<Person> EntityDbSet => Context.Persons;

    /// <inheritdoc />
    protected override IQueryable<Person> AddIncludes(IQueryable<Person> queryable)
    {
        return queryable.Include(p => p.Notes);
    }

    /// <inheritdoc />
    protected override IQueryable<Person> AddDefaultSort(IQueryable<Person> queryable)
    {
        return queryable.OrderBy(p => p.LastName).ThenBy(p => p.FirstName);
    }

    /// <inheritdoc />
    public async Task<IList<Person>> SearchByLastNameAsync(string lastName)
    {
        // Diagnostics come for free: Tag(...) captures this method's name via
        // [CallerMemberName] and the interceptor derives Source from the tag —
        // no hand-written strings, no correlation scope needed for a read.
        var query = Tag(AddIncludes(EntityDbSet.AsQueryable())
            .Where(p => p.LastName == lastName));

        return await query.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IList<Person>> SearchByNoteTextAsync(string searchText)
    {
        // A subquery across the child collection — the tag rides through the
        // generated SQL join just the same, so this shows up in diagnostics as
        // "SqlPersonRepository.SearchByNoteTextAsync" with no extra ceremony.
        var query = Tag(AddIncludes(EntityDbSet.AsQueryable())
            .Where(p => p.Notes.Any(n => n.NoteText.Contains(searchText))));

        return await query.ToListAsync();
    }
}
