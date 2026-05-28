using Benday.EfCore.SqlServer.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer.TestApi.Repositories;

/// <summary>
/// SQL Server repository for <see cref="Person"/>. Eager-loads the child
/// notes so the aggregate round-trips intact, and inherits the async CRUD +
/// dependent-entity save lifecycle from the base class.
/// </summary>
public class SqlPersonRepository :
    SqlEntityFrameworkCrudRepositoryBase<Person, TestDbContext>,
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
}
