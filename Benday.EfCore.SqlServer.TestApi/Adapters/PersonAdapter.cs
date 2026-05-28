using Benday.EfCore.SqlServer.Adapters;
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
public class PersonAdapter : AdapterBase<PersonDomainModel, Person>
{
    private readonly PersonNoteAdapter _noteAdapter = new();

    /// <inheritdoc />
    protected override void PerformAdapt(PersonDomainModel fromValue, Person toValue)
    {
        toValue.FirstName = fromValue.FirstName;
        toValue.LastName = fromValue.LastName;

        toValue.Status = fromValue.Status;
        toValue.CreatedBy = fromValue.CreatedBy;
        toValue.CreatedDate = fromValue.CreatedDate;
        toValue.LastModifiedBy = fromValue.LastModifiedBy;
        toValue.LastModifiedDate = fromValue.LastModifiedDate;
        toValue.Timestamp = fromValue.Timestamp;

        // Merge the child collection: match by Id, add new, mark missing for delete.
        _noteAdapter.Adapt(fromValue.Notes, toValue.Notes);
    }

    /// <inheritdoc />
    protected override void PerformAdapt(Person fromValue, PersonDomainModel toValue)
    {
        toValue.Id = fromValue.Id;
        toValue.FirstName = fromValue.FirstName;
        toValue.LastName = fromValue.LastName;

        toValue.Status = fromValue.Status;
        toValue.CreatedBy = fromValue.CreatedBy;
        toValue.CreatedDate = fromValue.CreatedDate;
        toValue.LastModifiedBy = fromValue.LastModifiedBy;
        toValue.LastModifiedDate = fromValue.LastModifiedDate;
        toValue.Timestamp = fromValue.Timestamp;

        toValue.Notes = new List<PersonNoteDomainModel>();
        _noteAdapter.Adapt(fromValue.Notes, toValue.Notes);
    }
}
