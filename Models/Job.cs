using System;

public class Job
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public string Education { get; set; }
    public string CTC { get; set; }
    public string Company { get; set; }
    public string Department { get; set; }
    public string Designation { get; set; }
    public string ImageUrl { get; set; } // Base64 string
    public string Posted { get; set; }
    public bool IsActive { get; set; }

    // Add these properties
    public int CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
}
