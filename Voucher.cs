using System;

namespace BarberShopApp.Models;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Giá trị giảm cố định (VD: 50000 đ)</summary>
    public decimal DiscountAmount { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Số lần sử dụng tối đa (null = không giới hạn)</summary>
    public int? MaxUsage { get; set; }

    /// <summary>Số lần đã sử dụng</summary>
    public int UsedCount { get; set; } = 0;
}