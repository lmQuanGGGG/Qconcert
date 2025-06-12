using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Qconcert.Models;

public partial class Event
{
    public Event()
    {
        Tickets = new HashSet<Ticket>();
    }
    public int Id { get; set; }
    [Required(ErrorMessage = "Tên sự kiện là bắt buộc")]
    public string Name { get; set; } = null!;
 
    public string? Description { get; set; }

    public string? EventInfo { get; set; }
    [Required(ErrorMessage = "Ngày tổ chức là bắt buộc")]
    public DateTime Date { get; set; }

    public int? CategoryId { get; set; }


    [Required(ErrorMessage = "Sức chứa là bắt buộc")]
    public int Capacity { get; set; }

    public byte[]? Image9x16 { get; set; }

    public byte[]? Image16x9 { get; set; }
    [Required(ErrorMessage = "Tỉnh/Thành là bắt buộc")]
    public string Province { get; set; } = null!;
    [Required(ErrorMessage = "Quận/Huyện là bắt buộc")]
    public string District { get; set; } = null!;
    [Required(ErrorMessage = "Phường/Xã là bắt buộc")]
    public string Ward { get; set; } = null!;
    [Required(ErrorMessage = "Địa chỉ chi tiết là bắt buộc")]
    public string AddressDetail { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }
    [Required(ErrorMessage = "Tên người tổ chức là bắt buộc")]
    public string? OrganizerName { get; set; }
    [Required(ErrorMessage = "Thông tin người tổ chức là bắt buộc")]
    public string? OrganizerInfo { get; set; }


    public byte[]? OrganizerLogo { get; set; }

    public string CreatedBy { get; set; } = null!; 

    public bool IsApproved { get; set; } = false; 
    public bool IsPaid { get; set; } = false; 
    public virtual Category? Category { get; set; }
    public virtual IdentityUser? Creator { get; set; } 
    public virtual ICollection<Ticket> Tickets { get; set; }

    public ICollection<PromotionPackage> PromotionPackages { get; set; } // Liên kết với gói khuyến mãi
    public ICollection<PaymentInfo> PaymentInfos { get; set; } 


}
