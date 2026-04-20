using BarberShopApp.Models;
using BarberShopWPF.Helpers;
using BarberShopWPF.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BarberShopWPF.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        // ── Properties ────────────────────────────────────────────────────
        private string _username = "";
        private string _errorMsg = "";
        private bool   _isLoading;

        public string Username { get => _username; set => Set(ref _username, value); }
        public string ErrorMsg { get => _errorMsg; set => Set(ref _errorMsg, value); }
        public bool   IsLoading { get => _isLoading; set => Set(ref _isLoading, value); }

        // ── Commands ──────────────────────────────────────────────────────
        public ICommand LoginCommand    { get; }
        public ICommand RegisterCommand { get; }

        public LoginViewModel()
        {
            LoginCommand    = new RelayCommand<string?>(DoLogin);
            RegisterCommand = new RelayCommand<RegisterInfo?>(DoRegister);
        }

        // ════════════════ LOGIN (từ AccountController.Login POST) ═════════
        public void DoLogin(string? password)
        {
            ErrorMsg = "";
            var pwd = password ?? "";

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(pwd))
            { ErrorMsg = "Vui lòng nhập tên đăng nhập và mật khẩu!"; return; }

            IsLoading = true;
            try
            {
                using var db = DbContextHelper.Create();

                var account = db.Accounts
                    .Include(a => a.Role)
                    .FirstOrDefault(a => a.Username == Username && a.Password == pwd);

                if (account is null)
                { ErrorMsg = "Sai tên đăng nhập hoặc mật khẩu!"; return; }

                string role = account.Role?.RoleName?.ToUpper() ?? "";

                Window? next = role switch
                {
                    "ADMIN"        or "QUẢN LÝ"    => new AdminView(account),
                    "RECEPTIONIST" or "LỄ TÂN"     => new ReceptionistView(account),
                    "CUSTOMER"     or "KHÁCH HÀNG" => BuildCustomerView(db, account),
                    _ => null
                };

                if (next is null)
                { ErrorMsg = $"Role '{account.Role?.RoleName}' chưa được cấu hình!"; return; }

                next.Show();

                // Đóng LoginView (thay RedirectToAction)
                foreach (Window w in Application.Current.Windows)
                    if (w is LoginView) { w.Close(); break; }
            }
            catch (Exception ex) { ErrorMsg = "Lỗi DB: " + ex.Message; }
            finally { IsLoading = false; }
        }

        private static CustomerView? BuildCustomerView(BarberShopAppContext db, Account acc)
        {
            var cust = db.Customers.FirstOrDefault(c => c.AccountId == acc.AccountId);
            return cust is null ? null : new CustomerView(acc, cust);
        }

        // ════════════════ REGISTER (từ AccountController.Register POST) ════
        public void DoRegister(RegisterInfo? info)
        {
            if (info is null) return;
            ErrorMsg = "";

            if (string.IsNullOrWhiteSpace(info.Username) ||
                string.IsNullOrWhiteSpace(info.Password) ||
                string.IsNullOrWhiteSpace(info.FullName)  ||
                string.IsNullOrWhiteSpace(info.Phone))
            { ErrorMsg = "Vui lòng điền đầy đủ thông tin!"; return; }

            try
            {
                using var db = DbContextHelper.Create();

                if (db.Accounts.Any(a => a.Username == info.Username))
                { ErrorMsg = "Tên đăng nhập đã tồn tại!"; return; }

                var acc = new Account
                {
                    Username = info.Username,
                    Password = info.Password,
                    FullName = info.FullName,
                    RoleId   = 3   // Customer – giữ nguyên logic gốc
                };
                db.Accounts.Add(acc);
                db.SaveChanges();

                var cust = new Customer
                {
                    AccountId    = acc.AccountId,
                    FullName     = info.FullName,
                    Phone        = info.Phone,
                    DateOfBirth  = info.Dob.HasValue
                                    ? DateOnly.FromDateTime(info.Dob.Value) : null,
                    RewardPoints = 0,
                    CustomerTier = "Thành viên"
                };
                db.Customers.Add(cust);
                db.SaveChanges();

                ErrorMsg = "✅ Đăng ký thành công! Vui lòng đăng nhập.";
            }
            catch (Exception ex) { ErrorMsg = "Lỗi đăng ký: " + ex.Message; }
        }
    }

    public class RegisterInfo
    {
        public string    Username { get; set; } = "";
        public string    FullName { get; set; } = "";
        public string    Phone    { get; set; } = "";
        public string    Password { get; set; } = "";
        public DateTime? Dob      { get; set; }
    }
}
