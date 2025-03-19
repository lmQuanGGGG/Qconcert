using System;
using System.Collections.Generic;

namespace Qconcert.Models;

public partial class CheckIn
{
    public int Id { get; set; }

    public int OrderDetailId { get; set; }

    public DateTime? ScannedAt { get; set; }

    public int? ScannedBy { get; set; }

    public virtual OrderDetail OrderDetail { get; set; } = null!;

    public virtual User? ScannedByNavigation { get; set; }
}
