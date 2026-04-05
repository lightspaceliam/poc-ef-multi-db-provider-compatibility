using System.ComponentModel.DataAnnotations;
using POC.Entities.Abstract;

namespace POC.Entities;

public class Patient : EntityBase
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(150, ErrorMessage = "Name exceeds {1} characters")]
    public string Name { get; set; } = null!;
    
    [Required(ErrorMessage = "Birth date is required")]
    public DateTime BirthDate { get; set; }

    //  Navigational Properties.

    // A Patient can have 0, 1 or many Identifiers mapped by a foreign key constraint.
    public List<Identifier> Identifiers { get; set; } = [];
}
