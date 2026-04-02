using System.ComponentModel.DataAnnotations;
using NpgsqlTypes;
using POC.Entities.Abstract;

namespace POC.Entities;

public enum CriteriaTypes
{
    [PgName("Inclusion")]  Inclusion,
    [PgName("Exclusion")]  Exclusion,
    [PgName("Mainevent")]  Mainevent,
}

public class Criteria : EntityBase
{
    /// <summary>
    /// Description
    /// Constraints:
    ///     Nullable
    ///     Max length of 4000 characters
    /// </summary>
    [StringLength(4000, ErrorMessage = "Description exceeds {1} characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Type
    /// Constraints:
    ///     Required
    ///     Accepts only: Inclusion, Exclusion, Mainevent
    /// </summary>
    [Required(ErrorMessage = "Type is required")]
    public CriteriaTypes Type { get; set; }

    //  Navigational Properties.

    /// <summary>
    /// Foreign Key
    ///     references Trial.Id
    /// Constraints:
    ///     Required and must reference an existing trial primary key
    /// </summary>
    [Required(ErrorMessage = "Trial is required")]
    public int TrialId { get; set; }
    public Trial Trial { get; set; }
}
