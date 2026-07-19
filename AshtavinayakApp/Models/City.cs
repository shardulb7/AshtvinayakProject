using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class City
{
    public int CityId { get; set; }

    public string CityName { get; set; }
    public bool IsDeleted { get; set; }

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<DropUp> DropUps { get; set; } = new List<DropUp>();

    public virtual ICollection<History> Histories { get; set; } = new List<History>();

    public virtual ICollection<Package> Packages { get; set; } = new List<Package>();

    public virtual ICollection<PickupPoint> PickupPoints { get; set; } = new List<PickupPoint>();

    public virtual ICollection<TripRoute> TripRoutes { get; set; } = new List<TripRoute>();
}
