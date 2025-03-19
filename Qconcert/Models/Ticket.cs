using System;
using System.Collections.Generic;

namespace Qconcert.Models;

public partial class Ticket
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string TicketType { get; set; } = null!;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
