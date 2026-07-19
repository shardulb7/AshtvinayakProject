using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public int? CityId { get; set; }

    public bool IsDeleted { get; set; }

    public int? TourDestinationId { get; set; }

    public virtual City? City { get; set; }

    public virtual ICollection<History> Histories { get; set; } = new List<History>();

    public virtual ICollection<Package> Packages { get; set; } = new List<Package>();

    public virtual TourDestination? TourDestination { get; set; }

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
