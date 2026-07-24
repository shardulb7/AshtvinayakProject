using System;

namespace AshtavinayakAPP.Models;

public partial class CommissionSetting
{
    public int Id { get; set; }

    public decimal DefaultCommissionPercentage { get; set; }

    public DateTime UpdatedAt { get; set; }
}
