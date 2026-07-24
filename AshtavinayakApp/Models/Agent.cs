using System;
using System.Collections.Generic;

namespace AshtavinayakAPP.Models;

public partial class Agent
{
    public int AgentId { get; set; }

    public string FullName { get; set; } = null!;

    public string BusinessName { get; set; } = null!;

    public string MobileNumber { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string AadhaarDocumentPath { get; set; } = null!;

    public string ShopActLicenseDocumentPath { get; set; } = null!;

    public string UdyamCertificatePath { get; set; } = null!;

    public string ApprovalStatus { get; set; } = "Pending";

    public bool IsActive { get; set; }

    public string? RejectionRemarks { get; set; }

    public decimal? CommissionPercentage { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
