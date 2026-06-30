using Benday.EfCore.Adapters;
using Benday.EfCore.SqlServer.TestApi.DomainModels;

namespace Benday.EfCore.SqlServer.TestApi.Adapters;

/// <summary>
/// Maps between <see cref="PersonNoteDomainModel"/> and the <c>PersonNote</c> entity.
/// </summary>
public class PersonNoteAdapter : AdapterBase<PersonNoteDomainModel, PersonNote>
{
    /// <inheritdoc />
    protected override void PerformAdapt(PersonNoteDomainModel fromValue, PersonNote toValue)
    {
        // Id is managed by the collection-merge logic and the database;
        // don't copy it from the model onto the entity.
        toValue.NoteText = fromValue.NoteText;
    }

    /// <inheritdoc />
    protected override void PerformAdapt(PersonNote fromValue, PersonNoteDomainModel toValue)
    {
        toValue.Id = fromValue.Id;
        toValue.NoteText = fromValue.NoteText;
    }
}

/// <summary>
/// Maps between <see cref="PersonDomainModel"/> and the <c>Person</c> aggregate root,
/// including merge logic over the child notes collection. Stateless — safe to
/// register as a singleton.
/// </summary>
public class PersonAdapter : CoreFieldsAdapterBase<PersonDomainModel, Person>
{
    private readonly PersonNoteAdapter _noteAdapter = new();

    /// <inheritdoc />
    protected override void PerformAdapt(PersonDomainModel fromValue, Person toValue)
    {
        // Status, audit fields, and the concurrency token are copied by
        // CoreFieldsAdapterBase before this runs.
        toValue.FirstName = fromValue.FirstName;
        toValue.LastName = fromValue.LastName;

        // Merge the child collection: match by Id, add new, mark missing for delete.
        _noteAdapter.Adapt(fromValue.Notes, toValue.Notes);
    }

    /// <inheritdoc />
    protected override void PerformAdapt(Person fromValue, PersonDomainModel toValue)
    {
        // Id, Status, audit fields, and the concurrency token are copied by
        // CoreFieldsAdapterBase before this runs.
        toValue.FirstName = fromValue.FirstName;
        toValue.LastName = fromValue.LastName;

        toValue.Notes = new List<PersonNoteDomainModel>();
        _noteAdapter.Adapt(fromValue.Notes, toValue.Notes);
    }
}
