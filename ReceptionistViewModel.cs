using BarberShopApp.Models;
using BarberShopWPF.Helpers;
using BarberShopWPF.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BarberShopWPF.ViewModels
{
	public class CartItem : BaseViewModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		public decimal Price { get; set; }
		private int _qty = 1; public int Quantity { get => _qty; set { Set(ref _qty, value); OnPropertyChanged(nameof(Total)); } }
		public decimal Total => Price * Quantity;
		private Barber _selectedBarber; public Barber SelectedBarber { get => _selectedBarber; set => Set(ref _selectedBarber, value); }
		public bool IsService => Type == "Service";
	}

	public class BillDetailItem
	{
		public string ItemName { get; set; }
		public int Quantity { get; set; }
		public string BarberName { get; set; }
		public decimal Price { get; set; }
		public decimal Total => Price * Quantity;
	}

	public class ReceptionistViewModel : BaseViewModel
	{
		public Account CurrentAccount { get; }
		private string _section = "Thu ngân POS";
		public string Section { get => _section; set { Set(ref _section, value); UpdateVis(); } }
		private string _status = ""; public string Status { get => _status; set => Set(ref _status, value); }

		public bool ShowPos => Section == "Thu ngân POS";
		public bool ShowAppts => Section == "Lịch hẹn";
		public bool ShowBills => Section == "Hóa đơn";
		public bool ShowBarbers => Section == "Trạng thái Thợ"; // ĐÃ THÊM
		private void UpdateVis() { OnPropertyChanged(nameof(ShowPos)); OnPropertyChanged(nameof(ShowAppts)); OnPropertyChanged(nameof(ShowBills)); OnPropertyChanged(nameof(ShowBarbers)); }

		private int _pending, _confirmed, _completed;
		public int Pending { get => _pending; set => Set(ref _pending, value); }
		public int Confirmed { get => _confirmed; set => Set(ref _confirmed, value); }
		public int Completed { get => _completed; set => Set(ref _completed, value); }

		public ObservableCollection<Appointment> Appointments { get; set; } = new();
		public ObservableCollection<Bill> Bills { get; set; } = new();
		public ObservableCollection<string> Filters { get; set; } = new() { "Tất cả", "Chờ xác nhận", "Đã xác nhận", "Hoàn thành", "Đã hủy" };
		private string _filter = "Tất cả"; public string Filter { get => _filter; set { Set(ref _filter, value); LoadData(); } }
		private Appointment _selAppt; public Appointment SelAppt { get => _selAppt; set => Set(ref _selAppt, value); }
		private int? _processingApptId = null;

		public ObservableCollection<Service> AvailableServices { get; set; } = new();
		public ObservableCollection<Product> AvailableProducts { get; set; } = new();
		public ObservableCollection<Barber> Barbers { get; set; } = new();
		public ObservableCollection<CartItem> CartItems { get; set; } = new();

		private string _customerNamePOS; public string CustomerNamePOS { get => _customerNamePOS; set => Set(ref _customerNamePOS, value); }
		private string _customerPhonePOS; public string CustomerPhonePOS { get => _customerPhonePOS; set => Set(ref _customerPhonePOS, value); }
		private string _cartTotalStr = "0 đ"; public string CartTotalStr { get => _cartTotalStr; set => Set(ref _cartTotalStr, value); }
		public decimal CartTotalAmount => CartItems.Sum(x => x.Total);

		private bool _isBillDetailsOpen; public bool IsBillDetailsOpen { get => _isBillDetailsOpen; set => Set(ref _isBillDetailsOpen, value); }
		private string _currentBillTitle; public string CurrentBillTitle { get => _currentBillTitle; set => Set(ref _currentBillTitle, value); }
		public ObservableCollection<BillDetailItem> CurrentBillDetails { get; set; } = new();

		public ICommand NavPosCmd { get; }
		public ICommand NavApptsCmd { get; }
		public ICommand NavBillsCmd { get; }
		public ICommand NavBarbersCmd { get; } // ĐÃ THÊM
		public ICommand LogoutCmd { get; }
		public ICommand RefreshCmd { get; }
		public ICommand ConfirmCmd { get; }
		public ICommand PayApptCmd { get; }
		public ICommand CancelCmd { get; }
		public ICommand AddToCartCmd { get; }
		public ICommand RemoveFromCartCmd { get; }
		public ICommand ClearCartCmd { get; }
		public ICommand CheckoutCmd { get; }
		public ICommand IncreaseQtyCmd { get; }
		public ICommand DecreaseQtyCmd { get; }
		public ICommand ViewBillDetailsCmd { get; }
		public ICommand CloseBillDetailsCmd { get; }
		public ICommand ToggleBarberStatusCmd { get; } // ĐÃ THÊM

		public ReceptionistViewModel(Account acc)
		{
			CurrentAccount = acc;

			NavPosCmd = new RelayCommand(_ => Section = "Thu ngân POS");
			NavApptsCmd = new RelayCommand(_ => Section = "Lịch hẹn");
			NavBillsCmd = new RelayCommand(_ => Section = "Hóa đơn");
			NavBarbersCmd = new RelayCommand(_ => Section = "Trạng thái Thợ"); // ĐÃ THÊM

			LogoutCmd = new RelayCommand(_ => { new LoginView().Show(); Application.Current.Windows.OfType<ReceptionistView>().FirstOrDefault()?.Close(); });
			RefreshCmd = new RelayCommand(_ => LoadData());

			ConfirmCmd = new RelayCommand(_ => ChangeApptStatus("Đã xác nhận"));
			CancelCmd = new RelayCommand(_ => ChangeApptStatus("Đã hủy"));

			PayApptCmd = new RelayCommand(_ => {
				if (SelAppt == null) { MessageBox.Show("Vui lòng chọn một lịch hẹn để thanh toán!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
				if (SelAppt.Status == "Hoàn thành") { MessageBox.Show("Lịch hẹn này đã được thanh toán rồi!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

				Section = "Thu ngân POS";
				CustomerNamePOS = SelAppt.Customer?.FullName ?? ""; CustomerPhonePOS = SelAppt.Customer?.Phone ?? "";
				var bookedBarber = Barbers.FirstOrDefault(b => b.BarberId == SelAppt.BarberId);
				int parsedQty = 1;
				if (!string.IsNullOrWhiteSpace(SelAppt.Notes) && SelAppt.Notes.StartsWith("[Đặt cho "))
				{
					var parts = SelAppt.Notes.Split(' '); if (parts.Length >= 3 && int.TryParse(parts[2], out int q)) { parsedQty = q; }
				}

				CartItems.Clear();
				if (SelAppt.Service != null)
				{
					for (int i = 0; i < parsedQty; i++) { CartItems.Add(new CartItem { Id = SelAppt.Service.ServiceId, Name = SelAppt.Service.ServiceName, Type = "Service", Price = SelAppt.Service.Price, Quantity = 1, SelectedBarber = bookedBarber }); }
				}
				UpdateTotal(); _processingApptId = SelAppt.AppointmentId;
			});

			AddToCartCmd = new RelayCommand(p => {
				if (p is Service s) AddToCart(s.ServiceId, s.ServiceName, "Service", s.Price);
				else if (p is Product pr) AddToCart(pr.ProductId, pr.ProductName, "Product", pr.Price);
			});

			RemoveFromCartCmd = new RelayCommand(p => { if (p is CartItem item) { CartItems.Remove(item); UpdateTotal(); } });
			IncreaseQtyCmd = new RelayCommand(p => { if (p is CartItem item) { item.Quantity++; UpdateTotal(); } });
			DecreaseQtyCmd = new RelayCommand(p => { if (p is CartItem item && item.Quantity > 1) { item.Quantity--; UpdateTotal(); } });
			ClearCartCmd = new RelayCommand(_ => { CartItems.Clear(); CustomerNamePOS = ""; CustomerPhonePOS = ""; UpdateTotal(); _processingApptId = null; });
			CheckoutCmd = new RelayCommand(_ => ProcessCheckout());
			ViewBillDetailsCmd = new RelayCommand(p => LoadBillDetails(p as Bill));
			CloseBillDetailsCmd = new RelayCommand(_ => IsBillDetailsOpen = false);

			// LOGIC CHUYỂN TRẠNG THÁI THỢ THỦ CÔNG
			ToggleBarberStatusCmd = new RelayCommand(p => {
				if (p is Barber b)
				{
					using var db = DbContextHelper.Create();
					var dbBarber = db.Barbers.Find(b.BarberId);
					if (dbBarber != null)
					{
						dbBarber.IsBusy = !(dbBarber.IsBusy ?? false); // Đảo ngược trạng thái
						db.SaveChanges();
						LoadData(); // Cập nhật lại UI
					}
				}
			});

			LoadData();
		}

		private void LoadData()
		{
			Status = "Đang tải dữ liệu...";
			try
			{
				using var db = DbContextHelper.Create();
				var today = DateTime.Today;

				Pending = db.Appointments.Count(x => x.AppointmentTime.Date >= today && (x.Status == "Chờ xác nhận" || x.Status == "Chờ xử lý"));
				Confirmed = db.Appointments.Count(x => x.AppointmentTime.Date >= today && x.Status.Contains("Đã xác nhận"));
				Completed = db.Appointments.Count(x => x.AppointmentTime.Date >= today && x.Status.Contains("Hoàn thành"));

				var appts = db.Appointments.Include(x => x.Customer).Include(x => x.Service).Include(x => x.Barber).Where(x => x.AppointmentTime.Date >= today).AsQueryable();
				if (Filter != "Tất cả")
				{
					if (Filter == "Chờ xác nhận") appts = appts.Where(x => x.Status.Contains("Chờ xác nhận") || x.Status.Contains("Chờ xử lý"));
					else appts = appts.Where(x => x.Status.Contains(Filter));
				}
				Appointments.Clear(); foreach (var a in appts.OrderBy(x => x.AppointmentTime).ToList()) Appointments.Add(a);

				var bills = db.Bills.Include(x => x.Customer).Include(x => x.Account).OrderByDescending(x => x.CreateDate).Take(100).ToList();
				Bills.Clear(); foreach (var b in bills) Bills.Add(b);

				AvailableServices.Clear(); foreach (var s in db.Services.OrderBy(x => x.ServiceName).ToList()) AvailableServices.Add(s);
				AvailableProducts.Clear(); foreach (var p in db.Products.Where(p => p.Stock > 0).OrderBy(x => x.ProductName).ToList()) AvailableProducts.Add(p);
				Barbers.Clear(); foreach (var b in db.Barbers.OrderBy(x => x.FullName).ToList()) Barbers.Add(b);

				Status = "Cập nhật lúc " + DateTime.Now.ToString("HH:mm:ss");
			}
			catch (Exception ex) { Status = "Lỗi: " + ex.Message; }
		}

		private void ChangeApptStatus(string newStatus) { if (SelAppt == null) return; try { using var db = DbContextHelper.Create(); var appt = db.Appointments.Find(SelAppt.AppointmentId); if (appt != null) { appt.Status = newStatus; db.SaveChanges(); LoadData(); } } catch { } }

		private void AddToCart(int id, string name, string type, decimal price)
		{
			if (type == "Service") { CartItems.Add(new CartItem { Id = id, Name = name, Type = type, Price = price, Quantity = 1 }); }
			else { var existing = CartItems.FirstOrDefault(x => x.Id == id && x.Type == type); if (existing != null) existing.Quantity++; else CartItems.Add(new CartItem { Id = id, Name = name, Type = type, Price = price, Quantity = 1 }); }
			UpdateTotal();
		}

		private void UpdateTotal() => CartTotalStr = CartTotalAmount.ToString("N0") + " đ";

		private void ProcessCheckout()
		{
			if (!CartItems.Any()) return;
			if (CartItems.Any(x => x.Type == "Service" && x.SelectedBarber == null)) { MessageBox.Show("Vui lòng chọn Thợ cắt tóc cho tất cả các dịch vụ!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
			var confirm = MessageBox.Show($"Xác nhận thanh toán hóa đơn này?\n\nTổng tiền: {CartTotalStr}", "Xác nhận thanh toán", MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (confirm != MessageBoxResult.Yes) return;
			using var db = DbContextHelper.Create(); using var transaction = db.Database.BeginTransaction();
			try
			{
				int? finalCustomerId = null;
				if (!string.IsNullOrWhiteSpace(CustomerPhonePOS))
				{
					var existingCustomer = db.Customers.FirstOrDefault(c => c.Phone == CustomerPhonePOS);
					if (existingCustomer != null) { finalCustomerId = existingCustomer.CustomerId; } else { var newCustomer = new Customer { FullName = string.IsNullOrWhiteSpace(CustomerNamePOS) ? "Khách vãng lai" : CustomerNamePOS, Phone = CustomerPhonePOS }; db.Customers.Add(newCustomer); db.SaveChanges(); finalCustomerId = newCustomer.CustomerId; }
				}
				var newBill = new Bill { CustomerId = finalCustomerId, AccountId = CurrentAccount.AccountId, CreateDate = DateTime.Now, TotalAmount = CartTotalAmount, Status = "Đã thanh toán" };
				db.Bills.Add(newBill); db.SaveChanges();
				foreach (var item in CartItems)
				{
					if (item.Type == "Service")
					{
						for (int i = 0; i < item.Quantity; i++) { db.BillServices.Add(new BillService { BillId = newBill.BillId, ServiceId = item.Id, BarberId = item.SelectedBarber.BarberId, Price = item.Price }); }
						var barber = db.Barbers.Find(item.SelectedBarber.BarberId); if (barber != null) barber.IsBusy = false;
					}
					else if (item.Type == "Product")
					{
						db.BillProducts.Add(new BillProduct { BillId = newBill.BillId, ProductId = item.Id, Quantity = item.Quantity, Price = item.Price });
						var pDb = db.Products.Find(item.Id); if (pDb != null) pDb.Stock -= item.Quantity;
					}
				}
				if (_processingApptId != null) { var appt = db.Appointments.Find(_processingApptId); if (appt != null) appt.Status = "Hoàn thành"; }
				db.SaveChanges(); transaction.Commit(); MessageBox.Show($"Thanh toán thành công!\nĐã lưu hóa đơn.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information); ClearCartCmd.Execute(null); LoadData();
			}
			catch (Exception ex) { transaction.Rollback(); MessageBox.Show("Lỗi thanh toán: " + ex.Message); }
		}

		private void LoadBillDetails(Bill bill)
		{
			if (bill == null) return; CurrentBillTitle = $"Chi tiết Hóa đơn #{bill.BillId} - Khách: {(bill.Customer?.FullName ?? "Vãng lai")}"; CurrentBillDetails.Clear();
			try
			{
				using var db = DbContextHelper.Create();
				var bServices = db.BillServices.Include(x => x.Service).Include(x => x.Barber).Where(x => x.BillId == bill.BillId).ToList();
				var groupedServices = bServices.GroupBy(x => new { x.ServiceId, x.BarberId, x.Price }).ToList();
				foreach (var g in groupedServices) { CurrentBillDetails.Add(new BillDetailItem { ItemName = g.First().Service?.ServiceName ?? "(Dịch vụ)", Quantity = g.Count(), BarberName = g.First().Barber?.FullName ?? "---", Price = g.Key.Price }); }
				var bProducts = db.BillProducts.Include(x => x.Product).Where(x => x.BillId == bill.BillId).ToList();
				foreach (var p in bProducts) { CurrentBillDetails.Add(new BillDetailItem { ItemName = p.Product?.ProductName ?? "(Sản phẩm)", Quantity = p.Quantity, BarberName = "---", Price = p.Price }); }
				IsBillDetailsOpen = true;
			}
			catch (Exception ex) { MessageBox.Show("Lỗi tải chi tiết: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
		}
	}
}