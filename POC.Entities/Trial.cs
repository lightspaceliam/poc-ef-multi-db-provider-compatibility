using System.ComponentModel.DataAnnotations;
using POC.Entities.Abstract;

namespace POC.Entities;

public class Trial : EntityBase
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(150, ErrorMessage = "Name exceeds {1} characters")]
    public string Name { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    //  Navigational Properties.

    // A Trial can have 0, 1 or many Criterion mapped by a foreign key constraint.
    public List<Criteria> Criterion { get; set; } = new ();
}
