using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // THÊM DÒNG NÀY ĐỂ BẮT LỖI FORM

namespace BarberShopApp.Models;

public partial class Product
{
	[Key]
	public int ProductId { get; set; }

	[Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
	[StringLength(100, ErrorMessage = "Tên sản phẩm không được quá 100 ký tự")]
	public string ProductName { get; set; } = null!;

	[Required(ErrorMessage = "Vui lòng nhập giá bán")]
	public decimal Price { get; set; }

	[Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho")]
	public int? Stock { get; set; }

	// Dòng này rất quan trọng, giữ nguyên để không bị lỗi liên kết với bảng BillProduct
	public virtual ICollection<BillProduct> BillProducts { get; set; } = new List<BillProduct>();
}