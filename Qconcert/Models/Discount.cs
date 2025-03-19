using System;
using System.Collections.Generic;

namespace Qconcert.Models;

public partial class Discount
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public decimal? DiscountPercentage { get; set; }

    public DateTime ExpiryDate { get; set; }
}
