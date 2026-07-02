using Benday.Common.Testing;
using Benday.EfCore.SqlServer.TestApi;
using Benday.EfCore.SqlServer.TestApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Benday.EfCore.SqlServer.IntegrationTests;

/// <summary>
/// SQL Server-backed tests for behavior that only a real database can prove:
/// add-vs-attach round-trip, eager loading, dependent-entity delete, cascade
/// delete, and [Timestamp] optimistic concurrency.
/// </summary>
public class PersonRepositoryIntegrationTests : IntegrationTestBase
{
    public PersonRepositoryIntegrationTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task SaveAsync_NewPerson_InsertsAndAssignsId()
    {
        await EnsureCleanDatabaseAsync();

        // arrange
        var person = new Person { FirstName = "Ada", LastName = "Lovelace" };

        // act
        using (var repo = new SqlPersonRepository(CreateDbContext()))
        {
            await repo.SaveAsync(person);
        }

        // assert — reload in a fresh context
        await using var context = CreateDbContext();
        var reloaded = await context.Persons.SingleAsync(p => p.Id == person.Id);
        AssertThat.IsTrue(person.Id > 0, "Insert should assign an identity");
        reloaded.FirstName.ShouldEqual("Ada", "Persisted first name should match");
    }

    [Fact]
    public async Task SaveAsync_ExistingPerson_UpdatesLoadedEntity()
    {
        await EnsureCleanDatabaseAsync();

        // arrange — seed
        int id;
        using (var repo = new SqlPersonRepository(CreateDbContext()))
        {
            var person = new Person { FirstName = "Grace", LastName = "Hopper" };
            await repo.SaveAsync(person);
            id = person.Id;
        }

        // act — load (so the entity carries its [Timestamp] concurrency token),
        // modify, then save. This is how the service layer updates an aggregate.
        using (var repo = new SqlPersonRepository(CreateDbContext()))
        {
            var loaded = await repo.GetByIdAsync(id);
            loaded!.LastName = "Murray Hopper";
            await repo.SaveAsync(loaded);
        }

        // assert
        await using var context = CreateDbContext();
        var reloaded = await context.Persons.SingleAsync(p => p.Id == id);
        reloaded.LastName.ShouldEqual("Murray Hopper", "Update of a loaded entity should persist");
    }

    [Fact]
    public async Task GetByIdAsync_EagerLoadsNotes()
    {
        await EnsureCleanDatabaseAsync();

        // arrange
        var person = new Person
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Notes = { new PersonNote { NoteText = "note one" }, new PersonNote { NoteText = "note two" } }
        };
        using (var repo = new SqlPersonRepository(CreateDbContext()))
        {
            await repo.SaveAsync(person);
        }

        // act — fresh repo/context; AddIncludes should eager-load Notes
        using var readRepo = new SqlPersonRepository(CreateDbContext());
        var loaded = await readRepo.GetByIdAsync(person.Id);

        // assert
        AssertThat.IsNotNull(loaded, "Person should be found");
        loaded.Notes.Count.ShouldEqual(2, "Notes should be eager-loaded via Include");
    }

    [Fact]
    public async Task SaveAsync_NoteMarkedForDelete_RemovesChildRow()
    {
        await EnsureCleanDatabaseAsync();

        // arrange — person with two notes
        var person = new Person
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Notes = { new PersonNote { NoteText = "keep" }, new PersonNote { NoteText = "remove" } }
        };
        using (var repo = new SqlPersonRepository(CreateDbContext()))
        {
            await repo.SaveAsync(person);
        }

        // act — load, mark one note for delete, save
        using (var repo = new SqlPersonRepository(CreateDbContext()))
        {
            var loaded = await repo.GetByIdAsync(person.Id);
            var noteToRemove = loaded!.Notes.Single(n => n.NoteText == "remove");
            noteToRemove.IsMarkedForDelete = true;
            await repo.SaveAsync(loaded);
        }

        // assert — only one note row remains for this person
        await using var context = CreateDbContext();
        var remaining = await context.PersonNotes.CountAsync(n => n.PersonId == person.Id);
        remaining.ShouldEqual(1, "Note marked for delete should be removed from the database");
    }

    [Fact]
    public async Task DeleteAsync_Person_CascadeDeletesNotes()
    {
        await EnsureCleanDatabaseAsync();

        // arrange
        var person = new Person
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Notes = { new PersonNote { NoteText = "a" }, new PersonNote { NoteText = "b" } }
        };
        using (var repo = new SqlPersonRepository(CreateDbContext()))
        {
            await repo.SaveAsync(person);
        }

        // act — delete the aggregate root
        using (var repo = new SqlPersonRepository(CreateDbContext()))
        {
            var loaded = await repo.GetByIdAsync(person.Id);
            await repo.DeleteAsync(loaded!);
        }

        // assert — child rows are cascade-deleted
        await using var context = CreateDbContext();
        var noteCount = await context.PersonNotes.CountAsync(n => n.PersonId == person.Id);
        noteCount.ShouldEqual(0, "Deleting the person should cascade-delete its notes");
    }

    [Fact]
    public async Task Save_ConflictingConcurrentUpdate_ThrowsConcurrencyException()
    {
        await EnsureCleanDatabaseAsync();

        // arrange — seed a person
        int id;
        using (var context = CreateDbContext())
        {
            var person = new Person { FirstName = "Ada", LastName = "Lovelace" };
            context.Persons.Add(person);
            await context.SaveChangesAsync();
            id = person.Id;
        }

        // act — load the same row into two contexts
        await using var contextOne = CreateDbContext();
        await using var contextTwo = CreateDbContext();
        var copyOne = await contextOne.Persons.SingleAsync(p => p.Id == id);
        var copyTwo = await contextTwo.Persons.SingleAsync(p => p.Id == id);

        // first writer wins
        copyOne.FirstName = "First Writer";
        await contextOne.SaveChangesAsync();

        // assert — second writer's stale [Timestamp] triggers a concurrency failure
        copyTwo.FirstName = "Second Writer";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => contextTwo.SaveChangesAsync());
    }
}
