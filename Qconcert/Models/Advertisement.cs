using System;
using System.Collections.Generic;

namespace Qconcert.Models;

public partial class Advertisement
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal Price { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Event Event { get; set; } = null!;
}
