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
	public class CustomerViewModel : BaseViewModel
	{
		private string _section = "Home";
		public string Section { get => _section; set { Set(ref _section, value); OnPropertyChanged(nameof(ShowHome)); OnPropertyChanged(nameof(ShowBook)); OnPropertyChanged(nameof(ShowHist)); } }
		public bool ShowHome => Section == "Home";
		public bool ShowBook => Section == "Booking";
		public bool ShowHist => Section == "History";

		public Account CurrentAccount { get; }
		public Customer CurrentCustomer { get; }

		public ObservableCollection<Service> Services { get; } = new();
		public ObservableCollection<Barber> Barbers { get; } = new();
		public ObservableCollection<Appointment> Upcoming { get; } = new();
		public ObservableCollection<Appointment> AllAppts { get; } = new();

		private Service? _selSvc; public Service? SelService { get => _selSvc; set => Set(ref _selSvc, value); }
		private Barber? _selBarber; public Barber? SelBarber { get => _selBarber; set => Set(ref _selBarber, value); }
		private DateTime _apptDate = DateTime.Now; public DateTime ApptDate { get => _apptDate; set => Set(ref _apptDate, value); }
		private string _notes = ""; public string Notes { get => _notes; set => Set(ref _notes, value); }

		public List<int> Quantities { get; } = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		private int _quantity = 1;
		public int Quantity { get => _quantity; set { Set(ref _quantity, value); OnPropertyChanged(nameof(IsSingleBooking)); OnPropertyChanged(nameof(IsMultiBooking)); if (value > 1) SelBarber = null; } }
		public bool IsSingleBooking => Quantity == 1;
		public bool IsMultiBooking => Quantity > 1;

		private int _points; public int Points { get => _points; set => Set(ref _points, value); }
		private string _tier = ""; public string Tier { get => _tier; set => Set(ref _tier, value); }
		private string _status = ""; public string Status { get => _status; set => Set(ref _status, value); }
		public int UpcomingCount => Upcoming.Count;

		public ICommand NavHomeCmd { get; }
		public ICommand NavBookCmd { get; }
		public ICommand NavHistCmd { get; }
		public ICommand BookCmd { get; }
		public ICommand CancelCmd { get; }
		public ICommand LogoutCmd { get; }

		public CustomerViewModel(Account account, Customer customer)
		{
			CurrentAccount = account; CurrentCustomer = customer;
			Points = customer.RewardPoints ?? 0; Tier = customer.CustomerTier ?? "Thành viên";
			NavHomeCmd = new RelayCommand(() => { Section = "Home"; LoadHome(); });
			NavBookCmd = new RelayCommand(() => { Section = "Booking"; LoadBookData(); });
			NavHistCmd = new RelayCommand(() => { Section = "History"; LoadAllAppts(); });
			BookCmd = new RelayCommand(DoBook); CancelCmd = new RelayCommand<int?>(DoCancel);
			LogoutCmd = new RelayCommand(Logout);
			LoadHome();
		}

		public void LoadHome()
		{
			try
			{
				using var db = DbContextHelper.Create();
				Upcoming.Clear();
				var list = db.Appointments.Include(a => a.Barber).Include(a => a.Service).Where(a => a.CustomerId == CurrentCustomer.CustomerId && a.AppointmentTime >= DateTime.Now && a.Status != "Đã hủy" && a.Status != "Hoàn thành").OrderBy(a => a.AppointmentTime).ToList();
				foreach (var a in list) Upcoming.Add(a);
				OnPropertyChanged(nameof(UpcomingCount));
				var fresh = db.Customers.Find(CurrentCustomer.CustomerId);
				Points = fresh?.RewardPoints ?? 0; Tier = fresh?.CustomerTier ?? "Thành viên";
			}
			catch (Exception ex) { Status = "❌ " + ex.Message; }
		}

		public void LoadBookData()
		{
			try
			{
				using var db = DbContextHelper.Create();
				Services.Clear(); foreach (var s in db.Services.AsNoTracking().OrderBy(x => x.ServiceName)) Services.Add(s);
				Barbers.Clear();
				// ĐÃ SỬA: Lấy toàn bộ thợ, không lọc IsBusy nữa để khách có thể đặt cho tương lai
				foreach (var b in db.Barbers.AsNoTracking().OrderBy(x => x.FullName)) Barbers.Add(b);
			}
			catch (Exception ex) { Status = "❌ " + ex.Message; }
		}

		public List<string> TimeSlots { get; } = GenerateTimeSlots();
		private static List<string> GenerateTimeSlots() { var slots = new List<string>(); for (int h = 8; h <= 19; h++) { for (int m = 0; m < 60; m += 10) slots.Add($"{h:D2}:{m:D2}"); } return slots; }
		private string _selectedTime = "08:00"; public string SelectedTime { get => _selectedTime; set => Set(ref _selectedTime, value); }

		private void DoBook()
		{
			Status = "";
			if (SelService is null) { Status = "⚠ Vui lòng chọn dịch vụ!"; return; }

			if (IsSingleBooking && SelBarber is null) { Status = "⚠ Vui lòng chọn thợ cắt!"; return; }

			try
			{
				var timeParts = SelectedTime.Split(':');
				int hour = int.Parse(timeParts[0]);
				int minute = int.Parse(timeParts[1]);

				DateTime fullTime = new DateTime(
					ApptDate.Year, ApptDate.Month, ApptDate.Day,
					hour, minute, 0);

				if (fullTime <= DateTime.Now)
				{
					Status = "⚠ Thời gian đặt lịch không thể ở quá khứ!";
					return;
				}

				using var db = DbContextHelper.Create();

				// ─── BỘ LỌC CHỐNG XUNG ĐỘT (CONFLICT) SIÊU CẤP ───
				if (SelBarber != null)
				{
					// 1. TRƯỜNG HỢP ONLINE CHẠM TRÁN OFFLINE (Khách vãng lai)
					var dbBarber = db.Barbers.Find(SelBarber.BarberId);
					if (dbBarber != null && dbBarber.IsBusy == true)
					{
						// Giả sử 1 khách vãng lai cắt mất tầm 45 phút. 
						// Nếu khách Online cố tình book giờ TỪ BÂY GIỜ đến 45 PHÚT TỚI -> Chặn!
						if (fullTime < DateTime.Now.AddMinutes(45))
						{
							Status = $"⚠ Thợ {SelBarber.FullName} đang kẹt cắt cho khách tại quán. Vui lòng chọn giờ sau {DateTime.Now.AddMinutes(45):HH:mm}!";
							return;
						}
					}

					// 2. TRƯỜNG HỢP ONLINE CHẠM TRÁN ONLINE (Đè khung giờ)
					// Lấy thời gian bắt đầu và kết thúc của LỊCH ĐANG BẤM ĐẶT
					DateTime requestedStart = fullTime;
					int requestedDuration = SelService.DurationMinutes; // Lấy thời gian của dịch vụ
					DateTime requestedEnd = requestedStart.AddMinutes(requestedDuration);

					// Kéo toàn bộ lịch của Thợ đó trong "CÙNG NGÀY" về RAM để soi
					var dailyAppts = db.Appointments
						.Include(a => a.Service)
						.Where(a => a.BarberId == SelBarber.BarberId
								 && a.AppointmentTime.Date == fullTime.Date
								 && a.Status != "Đã hủy"
								 && a.Status != "Hoàn thành")
						.ToList();

					// Thuật toán kiểm tra 2 khoảng thời gian bị đè nhau: (Start A < End B) VÀ (Start B < End A)
					bool isConflict = dailyAppts.Any(a =>
					{
						DateTime existingStart = a.AppointmentTime;
						int existingDuration = a.Service?.DurationMinutes ?? 30; // Mặc định 30p nếu DB lỗi null
						DateTime existingEnd = existingStart.AddMinutes(existingDuration);

						return existingStart < requestedEnd && requestedStart < existingEnd;
					});

					if (isConflict)
					{
						Status = $"⚠ Thợ {SelBarber.FullName} đã có lịch kẹt trong khung giờ này. Vui lòng chọn thời gian khác!";
						return;
					}
				}
				// ─────────────────────────────────────────────────

				// Gắn thêm số lượng người vào Ghi chú nếu > 1
				string finalNotes = (Notes ?? "").Trim();
				if (Quantity > 1)
				{
					finalNotes = $"[Đặt cho {Quantity} người] " + finalNotes;
				}

				db.Appointments.Add(new Appointment
				{
					CustomerId = CurrentCustomer.CustomerId,
					BarberId = SelBarber?.BarberId,
					ServiceId = SelService.ServiceId,
					AppointmentTime = fullTime,
					Notes = finalNotes,
					Status = "Chờ xác nhận"
				});
				db.SaveChanges();

				Status = $"✅ Đặt thành công lúc {SelectedTime} ngày {fullTime:dd/MM}!";
				SelService = null;
				SelBarber = null;
				Quantity = 1;
				Notes = "";
				Section = "History";
				LoadAllAppts();
			}
			catch (Exception ex) { Status = "❌ Lỗi: " + ex.Message; }
		}

		public void LoadAllAppts()
		{
			try
			{
				using var db = DbContextHelper.Create(); AllAppts.Clear();
				foreach (var a in db.Appointments.Include(a => a.Barber).Include(a => a.Service).Where(a => a.CustomerId == CurrentCustomer.CustomerId).OrderByDescending(a => a.AppointmentTime)) AllAppts.Add(a);
			}
			catch (Exception ex) { Status = "❌ " + ex.Message; }
		}

		public void DoCancel(int? apptId)
		{
			if (apptId is null) return;
			try
			{
				using var db = DbContextHelper.Create();
				var a = db.Appointments.FirstOrDefault(x => x.AppointmentId == apptId && x.CustomerId == CurrentCustomer.CustomerId);
				if (a is null) { Status = "⚠ Không tìm thấy lịch hẹn!"; return; }
				if (a.Status != "Chờ xác nhận") { Status = "⚠ Chỉ hủy được lịch đang chờ!"; return; }
				a.Status = "Đã hủy"; db.SaveChanges(); Status = "✅ Đã hủy lịch hẹn!"; LoadAllAppts(); LoadHome();
			}
			catch (Exception ex) { Status = "❌ " + ex.Message; }
		}

		private void Logout() { new LoginView().Show(); foreach (Window w in Application.Current.Windows) if (w is CustomerView) { w.Close(); break; } }
	}
}