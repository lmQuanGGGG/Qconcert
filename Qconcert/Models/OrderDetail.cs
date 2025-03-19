using System;
using System.Collections.Generic;

namespace Qconcert.Models;

public partial class OrderDetail
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int TicketId { get; set; }

    public int Quantity { get; set; }

    public decimal Subtotal { get; set; }

    public string? Qrcode { get; set; }

    public virtual ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();

    public virtual Order Order { get; set; } = null!;

    public virtual Ticket Ticket { get; set; } = null!;
}
