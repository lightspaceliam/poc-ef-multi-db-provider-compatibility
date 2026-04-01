using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using POC.Entities.Abstract;

namespace POC.Entities;

[Table("trials")]
public class Trial : EntityBase
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(150, ErrorMessage = "Name exceeds {1} characters")]
    [Column("name")]
    public string Name { get; set; } = null!;
    
    [Column("start_date")]
    public DateTime StartDate { get; set; }
    
    [Column("end_date")]
    public DateTime EndDate { get; set; }
    
    //  Navigational Properties.
    
    // A Trial can have 0, 1 or many Criterion mapped by a foreign key constraint.
    public List<Criteria> Criterion { get; set; } = new ();
}