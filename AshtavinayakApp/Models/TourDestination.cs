using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class TourDestination
{
    public int Id { get; set; }

    public string DestinationName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
}
