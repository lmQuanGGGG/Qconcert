using System;
using System.Collections.Generic;

namespace Qconcert.Models;

public partial class Event
{
    public Event()
    {
        Tickets = new HashSet<Ticket>();
    }
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? EventInfo { get; set; }

    public DateTime Date { get; set; }

    public int? CategoryId { get; set; }



    public int Capacity { get; set; }

    public byte[]? Image9x16 { get; set; }

    public byte[]? Image16x9 { get; set; }

    public string Province { get; set; } = null!;

    public string District { get; set; } = null!;

    public string Ward { get; set; } = null!;

    public string AddressDetail { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public string? OrganizerName { get; set; }

    public string? OrganizerInfo { get; set; }

    public byte[]? OrganizerLogo { get; set; }
    public string CreatedBy { get; set; } = null!; // ID của người tạo sự kiện

    public bool IsApproved { get; set; } = false; // Trạng thái duyệt sự kiện

    public virtual Category? Category { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; }

    public ICollection<PromotionPackage> PromotionPackages { get; set; } // Liên kết với gói khuyến mãi
}
