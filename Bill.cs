using System;
using System.Collections.Generic;

namespace BarberShopApp.Models;

public partial class Bill
{
    public int BillId { get; set; }

    public int? CustomerId { get; set; }

    public int? AccountId { get; set; }

    public DateTime? CreateDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? Status { get; set; }

    // ── VOUCHER (mới thêm) ────────────────────────────────────
    public int? VoucherId { get; set; }

    /// <summary>Số tiền đã giảm thực tế (snapshot tại thời điểm thanh toán)</summary>
    public decimal DiscountAmount { get; set; } = 0;

    public virtual Account? Account { get; set; }
    public virtual ICollection<BillProduct> BillProducts { get; set; } = new List<BillProduct>();
    public virtual ICollection<BillService> BillServices { get; set; } = new List<BillService>();
    public virtual Customer? Customer { get; set; }
    public virtual Voucher? Voucher { get; set; }
}