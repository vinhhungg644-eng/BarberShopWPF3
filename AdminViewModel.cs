using BarberShopApp.Models;
using BarberShopWPF.Helpers;
using BarberShopWPF.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BarberShopWPF.ViewModels
{
    public class RevenueData { public string Day { get; set; } public double Amount { get; set; } public double X { get; set; } public double Y { get; set; } public bool ShowLabel { get; set; } }
    public class CustomerReport { public string Name { get; set; } public string Phone { get; set; } public string Service { get; set; } public string Time { get; set; } public string Price { get; set; } }

    public class AdminViewModel : BaseViewModel
    {
        // ─── UI STATE ──────────────────────────────────────────────────────
        private string _section = "Dashboard";
        public string Section { get => _section; set { Set(ref _section, value); UpdateVisibility(); } }

        private bool _isDrawerOpen;
        public bool IsDrawerOpen { get => _isDrawerOpen; set => Set(ref _isDrawerOpen, value); }

        private string _drawerContent = "";
        public string DrawerContent { get => _drawerContent; set => Set(ref _drawerContent, value); }

        public bool ShowDash => Section == "Dashboard";
        public bool ShowSvc => Section == "Services";
        public bool ShowBarber => Section == "Barbers";
        public bool ShowProd => Section == "Products";
        public bool ShowVoucher => Section == "Vouchers";   // ← MỚI

        private void UpdateVisibility()
        {
            OnPropertyChanged(nameof(ShowDash));
            OnPropertyChanged(nameof(ShowSvc));
            OnPropertyChanged(nameof(ShowBarber));
            OnPropertyChanged(nameof(ShowProd));
            OnPropertyChanged(nameof(ShowVoucher));
        }

        // ─── DASHBOARD ──────────────────────────────────────────────────────
        public Account CurrentAccount { get; }
        private string _todayRevenue = "0 đ"; public string TodayRevenue { get => _todayRevenue; set => Set(ref _todayRevenue, value); }
        private int _freeBarberCount; public int FreeBarberCount { get => _freeBarberCount; set => Set(ref _freeBarberCount, value); }
        private int _todayCustomerCount; public int TodayCustomerCount { get => _todayCustomerCount; set => Set(ref _todayCustomerCount, value); }

        private string _selectedChartPeriod = "1 tuần qua";
        public string SelectedChartPeriod
        {
            get => _selectedChartPeriod;
            set { Set(ref _selectedChartPeriod, value); UpdateChartData(); }
        }
        public ObservableCollection<string> ChartPeriods { get; } = new() { "1 tuần qua", "1 tháng qua", "1 năm qua" };
        public string ChartPoints { get; set; } = "";
        public string YMax { get; set; } = "0 đ";
        public string YMid { get; set; } = "0 đ";
        public ObservableCollection<RevenueData> RevenueChart { get; set; } = new();
        public ObservableCollection<CustomerReport> DashboardCustomerList { get; set; } = new();
        public ObservableCollection<Barber> DashboardFreeBarberList { get; set; } = new();

        // ─── COLLECTIONS ────────────────────────────────────────────────────
        public ObservableCollection<Service> Services { get; set; } = new();
        public ObservableCollection<Barber> Barbers { get; set; } = new();
        public ObservableCollection<Product> Products { get; set; } = new();
        public ObservableCollection<Voucher> Vouchers { get; set; } = new();   // ← MỚI

        // ─── EDITING OBJECTS ────────────────────────────────────────────────
        private Service _editSvc = new();
        private Barber _editBarber = new() { FullName = "" };
        private Product _editProd = new() { ProductName = "" };
        private Voucher _editVoucher = new();                    // ← MỚI

        public Service EditSvc { get => _editSvc; set => Set(ref _editSvc, value); }
        public Barber EditBarber { get => _editBarber; set => Set(ref _editBarber, value); }
        public Product EditProd { get => _editProd; set => Set(ref _editProd, value); }
        public Voucher EditVoucher { get => _editVoucher; set => Set(ref _editVoucher, value); }  // ← MỚI

        // ─── COMMANDS ───────────────────────────────────────────────────────
        public ICommand NavDashCmd { get; }
        public ICommand NavSvcCmd { get; }
        public ICommand NavBarberCmd { get; }
        public ICommand NavProdCmd { get; }
        public ICommand NavVoucherCmd { get; }   // ← MỚI
        public ICommand OpenDrawerCmd { get; }
        public ICommand CloseDrawerCmd { get; }
        public ICommand LogoutCmd { get; }

        public ICommand SaveSvcCmd { get; }
        public ICommand EditSvcCmd { get; }
        public ICommand DelSvcCmd { get; }
        public ICommand SaveBarberCmd { get; }
        public ICommand EditBarberCmd { get; }
        public ICommand DelBarberCmd { get; }
        public ICommand SaveProdCmd { get; }
        public ICommand EditProdCmd { get; }
        public ICommand DelProdCmd { get; }
        public ICommand SaveVoucherCmd { get; }  // ← MỚI
        public ICommand EditVoucherCmd { get; }  // ← MỚI
        public ICommand DelVoucherCmd { get; }  // ← MỚI
        public ICommand ToggleVoucherCmd { get; } // ← MỚI (bật/tắt nhanh)

        public AdminViewModel(Account account)
        {
            CurrentAccount = account;

            NavDashCmd = new RelayCommand(() => { Section = "Dashboard"; LoadData(); });
            NavSvcCmd = new RelayCommand(() => { Section = "Services"; LoadData(); });
            NavBarberCmd = new RelayCommand(() => { Section = "Barbers"; LoadData(); });
            NavProdCmd = new RelayCommand(() => { Section = "Products"; LoadData(); });
            NavVoucherCmd = new RelayCommand(() => { Section = "Vouchers"; LoadData(); }); // ← MỚI

            CloseDrawerCmd = new RelayCommand(_ => IsDrawerOpen = false);

            OpenDrawerCmd = new RelayCommand(p =>
            {
                DrawerContent = p?.ToString() ?? "";
                if (DrawerContent == "ServiceForm") EditSvc = new Service();
                if (DrawerContent == "BarberForm") EditBarber = new Barber { FullName = "" };
                if (DrawerContent == "ProductForm") EditProd = new Product { ProductName = "" };
                if (DrawerContent == "VoucherForm") EditVoucher = new Voucher { IsActive = true, ExpiryDate = DateTime.Now.AddMonths(1) }; // ← MỚI
                IsDrawerOpen = true;
            });

            EditSvcCmd = new RelayCommand(p => { if (p is Service s) { EditSvc = CloneSvc(s); DrawerContent = "ServiceForm"; IsDrawerOpen = true; } });
            EditBarberCmd = new RelayCommand(p => { if (p is Barber b) { EditBarber = CloneBarber(b); DrawerContent = "BarberForm"; IsDrawerOpen = true; } });
            EditProdCmd = new RelayCommand(p => { if (p is Product pr) { EditProd = CloneProd(pr); DrawerContent = "ProductForm"; IsDrawerOpen = true; } });
            EditVoucherCmd = new RelayCommand(p => { if (p is Voucher v) { EditVoucher = CloneVoucher(v); DrawerContent = "VoucherForm"; IsDrawerOpen = true; } }); // ← MỚI

            SaveSvcCmd = new RelayCommand(_ => SaveSvc());
            DelSvcCmd = new RelayCommand(p => DelSvc(p));
            SaveBarberCmd = new RelayCommand(_ => SaveBarberAction());
            DelBarberCmd = new RelayCommand(p => DelBarberAction(p));
            SaveProdCmd = new RelayCommand(_ => SaveProd());
            DelProdCmd = new RelayCommand(p => DelProd(p));
            SaveVoucherCmd = new RelayCommand(_ => SaveVoucher());   // ← MỚI
            DelVoucherCmd = new RelayCommand(p => DelVoucher(p));   // ← MỚI
            ToggleVoucherCmd = new RelayCommand(p => ToggleVoucher(p)); // ← MỚI

            LogoutCmd = new RelayCommand(_ =>
            {
                new LoginView().Show();
                Application.Current.Windows.OfType<AdminView>().FirstOrDefault()?.Close();
            });

            LoadData();
            LoadRealDashboardData();
            UpdateChartData();
        }

        // ─── LOAD DATA ──────────────────────────────────────────────────────
        private void LoadData()
        {
            try
            {
                using var db = DbContextHelper.Create();
                Services = new ObservableCollection<Service>(db.Services.AsNoTracking().OrderBy(x => x.ServiceName).ToList());
                Barbers = new ObservableCollection<Barber>(db.Barbers.AsNoTracking().OrderBy(x => x.FullName).ToList());
                Products = new ObservableCollection<Product>(db.Products.AsNoTracking().OrderBy(x => x.ProductName).ToList());
                Vouchers = new ObservableCollection<Voucher>(db.Vouchers.AsNoTracking().OrderBy(x => x.Code).ToList()); // ← MỚI
                OnPropertyChanged(nameof(Services));
                OnPropertyChanged(nameof(Barbers));
                OnPropertyChanged(nameof(Products));
                OnPropertyChanged(nameof(Vouchers));
            }
            catch { }
        }

        private void LoadRealDashboardData()
        {
            try
            {
                using var db = DbContextHelper.Create();
                var today = DateTime.Today;
                var freeB = db.Barbers.Where(b => b.IsBusy == false || b.IsBusy == null).ToList();
                FreeBarberCount = freeB.Count;
                DashboardFreeBarberList.Clear();
                foreach (var b in freeB) DashboardFreeBarberList.Add(b);

                var bills = db.Bills
                    .Include(b => b.Customer)
                    .Include(b => b.BillServices).ThenInclude(bs => bs.Service)
                    .Where(b => b.CreateDate != null && b.CreateDate.Value.Date == today)
                    .ToList();
                TodayCustomerCount = bills.Count;
                TodayRevenue = bills.Sum(b => b.TotalAmount ?? 0).ToString("N0") + " đ";

                DashboardCustomerList.Clear();
                foreach (var b in bills)
                {
                    string sn = b.BillServices.Any()
                        ? string.Join(", ", b.BillServices.Select(x => x.Service.ServiceName))
                        : "Khác";
                    DashboardCustomerList.Add(new CustomerReport
                    {
                        Name = b.Customer?.FullName ?? "Khách lẻ",
                        Phone = b.Customer?.Phone ?? "---",
                        Service = sn,
                        Time = b.CreateDate?.ToString("HH:mm") ?? "--:--",
                        Price = (b.TotalAmount ?? 0).ToString("N0") + " đ"
                    });
                }
            }
            catch { }
        }

        private void UpdateChartData()
        {
            try
            {
                using var db = DbContextHelper.Create();
                var today = DateTime.Today;
                var temp = new List<RevenueData>();

                if (SelectedChartPeriod == "1 năm qua")
                {
                    var start = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
                    var b = db.Bills.Where(x => x.CreateDate >= start).ToList();
                    for (int i = 11; i >= 0; i--)
                    {
                        var m = today.AddMonths(-i);
                        var t = b.Where(x => x.CreateDate!.Value.Month == m.Month && x.CreateDate.Value.Year == m.Year).Sum(x => x.TotalAmount ?? 0);
                        temp.Add(new RevenueData { Day = m.ToString("MM/yy"), Amount = (double)t, ShowLabel = true });
                    }
                }
                else if (SelectedChartPeriod == "1 tháng qua")
                {
                    var start = today.AddDays(-29);
                    var b = db.Bills.Where(x => x.CreateDate >= start).ToList();
                    for (int i = 29; i >= 0; i--)
                    {
                        var d = today.AddDays(-i);
                        var t = b.Where(x => x.CreateDate!.Value.Date == d).Sum(x => x.TotalAmount ?? 0);
                        temp.Add(new RevenueData { Day = d.ToString("dd/MM"), Amount = (double)t, ShowLabel = (i % 5 == 0) });
                    }
                }
                else
                {
                    var start = today.AddDays(-6);
                    var b = db.Bills.Where(x => x.CreateDate >= start).ToList();
                    for (int i = 6; i >= 0; i--)
                    {
                        var d = today.AddDays(-i);
                        var t = b.Where(x => x.CreateDate!.Value.Date == d).Sum(x => x.TotalAmount ?? 0);
                        temp.Add(new RevenueData { Day = d.ToString("dd/MM"), Amount = (double)t, ShowLabel = true });
                    }
                }

                double max = temp.Count > 0 ? temp.Max(x => x.Amount) : 1;
                if (max == 0) max = 1;
                YMax = max.ToString("N0") + " đ";
                YMid = (max / 2).ToString("N0") + " đ";
                OnPropertyChanged(nameof(YMax));
                OnPropertyChanged(nameof(YMid));

                double cw = 750, ch = 160, xs = temp.Count > 1 ? cw / (temp.Count - 1) : 0;
                string ps = "";
                RevenueChart.Clear();
                for (int i = 0; i < temp.Count; i++)
                {
                    double tx = i * xs, ty = ch - ((temp[i].Amount / max) * ch);
                    ps += $"{tx.ToString(System.Globalization.CultureInfo.InvariantCulture)},{ty.ToString(System.Globalization.CultureInfo.InvariantCulture)} ";
                    temp[i].X = tx; temp[i].Y = ty;
                    RevenueChart.Add(temp[i]);
                }
                ChartPoints = ps;
                OnPropertyChanged(nameof(ChartPoints));
            }
            catch { }
        }

        // ─── SERVICE CRUD ───────────────────────────────────────────────────
        private void SaveSvc()
        {
            using var db = DbContextHelper.Create();
            if (EditSvc.ServiceId == 0) db.Services.Add(EditSvc); else db.Services.Update(EditSvc);
            db.SaveChanges(); LoadData(); IsDrawerOpen = false;
        }
        private void DelSvc(object p)
        {
            if (p is Service s && MessageBox.Show("Xóa dịch vụ này?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using var db = DbContextHelper.Create();
                var e = db.Services.Find(s.ServiceId);
                if (e != null) { db.Services.Remove(e); db.SaveChanges(); LoadData(); }
            }
        }

        // ─── BARBER CRUD ────────────────────────────────────────────────────
        private void SaveBarberAction()
        {
            using var db = DbContextHelper.Create();
            if (EditBarber.BarberId == 0) db.Barbers.Add(EditBarber); else db.Barbers.Update(EditBarber);
            db.SaveChanges(); LoadData(); IsDrawerOpen = false;
        }
        private void DelBarberAction(object p)
        {
            if (p is Barber b && MessageBox.Show("Xóa thợ này?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using var db = DbContextHelper.Create();
                var e = db.Barbers.Find(b.BarberId);
                if (e != null) { db.Barbers.Remove(e); db.SaveChanges(); LoadData(); }
            }
        }

        // ─── PRODUCT CRUD ───────────────────────────────────────────────────
        private void SaveProd()
        {
            using var db = DbContextHelper.Create();
            if (EditProd.ProductId == 0) db.Products.Add(EditProd); else db.Products.Update(EditProd);
            db.SaveChanges(); LoadData(); IsDrawerOpen = false;
        }
        private void DelProd(object p)
        {
            if (p is Product r && MessageBox.Show("Xóa sản phẩm này?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using var db = DbContextHelper.Create();
                var e = db.Products.Find(r.ProductId);
                if (e != null) { db.Products.Remove(e); db.SaveChanges(); LoadData(); }
            }
        }

        // ─── VOUCHER CRUD (MỚI) ─────────────────────────────────────────────
        private void SaveVoucher()
        {
            if (string.IsNullOrWhiteSpace(EditVoucher.Code))
            { MessageBox.Show("Vui lòng nhập mã voucher!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (EditVoucher.DiscountAmount <= 0)
            { MessageBox.Show("Số tiền giảm phải lớn hơn 0!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            try
            {
                using var db = DbContextHelper.Create();
                // Kiểm tra trùng code (khi thêm mới)
                if (EditVoucher.VoucherId == 0)
                {
                    if (db.Vouchers.Any(v => v.Code == EditVoucher.Code))
                    { MessageBox.Show("Mã voucher đã tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); return; }
                    db.Vouchers.Add(EditVoucher);
                }
                else
                {
                    db.Vouchers.Update(EditVoucher);
                }
                db.SaveChanges();
                LoadData();
                IsDrawerOpen = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu voucher: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DelVoucher(object p)
        {
            if (p is Voucher v && MessageBox.Show($"Xóa voucher [{v.Code}]?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using var db = DbContextHelper.Create();
                var e = db.Vouchers.Find(v.VoucherId);
                if (e != null) { db.Vouchers.Remove(e); db.SaveChanges(); LoadData(); }
            }
        }

        private void ToggleVoucher(object p)
        {
            if (p is Voucher v)
            {
                using var db = DbContextHelper.Create();
                var e = db.Vouchers.Find(v.VoucherId);
                if (e != null) { e.IsActive = !e.IsActive; db.SaveChanges(); LoadData(); }
            }
        }

        // ─── CLONE HELPERS ──────────────────────────────────────────────────
        private static Service CloneSvc(Service s) => new() { ServiceId = s.ServiceId, ServiceName = s.ServiceName, Price = s.Price, DurationMinutes = s.DurationMinutes };
        private static Barber CloneBarber(Barber b) => new() { BarberId = b.BarberId, FullName = b.FullName, Phone = b.Phone, IsBusy = b.IsBusy, BasicSalary = b.BasicSalary };
        private static Product CloneProd(Product p) => new() { ProductId = p.ProductId, ProductName = p.ProductName, Price = p.Price, Stock = p.Stock };
        private static Voucher CloneVoucher(Voucher v) => new()
        {
            VoucherId = v.VoucherId,
            Code = v.Code,
            Description = v.Description,
            DiscountAmount = v.DiscountAmount,
            ExpiryDate = v.ExpiryDate,
            IsActive = v.IsActive,
            MaxUsage = v.MaxUsage,
            UsedCount = v.UsedCount
        };
    }
}