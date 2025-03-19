using System;
using System.Collections.Generic;

namespace Qconcert.Models;

public partial class Review
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public int UserId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
