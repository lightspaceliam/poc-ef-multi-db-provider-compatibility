
using System.ComponentModel.DataAnnotations;
using POC.Entities.Abstract;

namespace POC.Entities;

public enum Use
{
    Official,
    Secondary,
    Temp,
    Usual,
    Old
}

public class Identifier : EntityBase
{
    /// <summary>
    /// Description
    /// Constraints:
    ///     Nullable
    ///     Max length of 4000 characters
    /// </summary>
    [Required(ErrorMessage = "Code is required")]
    [StringLength(75, ErrorMessage = "Code exceeds {1} characters")]
    public string? Code { get; set; }
    
    /// <summary>
    /// Description
    /// Constraints:
    ///     Nullable
    ///     Max length of 4000 characters
    /// </summary>
    [StringLength(4000, ErrorMessage = "Description exceeds {1} characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Use
    /// Constraints:
    ///     Required
    ///     Accepts only: Official, Secondary, Temp, Usual, Old
    /// </summary>
    [Required(ErrorMessage = "Use is required")]
    public Use Use { get; set; }

    //  Navigational Properties.

    /// <summary>
    /// Foreign Key
    ///     references Trial.Id
    /// Constraints:
    ///     Required and must reference an existing trial primary key
    /// </summary>
    [Required(ErrorMessage = "Trial is required")]
    public int PatientId { get; set; }
    public Patient Patient { get; set; }
}
