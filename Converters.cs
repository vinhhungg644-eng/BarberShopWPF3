using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BarberShopWPF.Helpers
{
    // ── Ẩn TextBlock khi chuỗi rỗng ──────────────────────────────────────
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => string.IsNullOrWhiteSpace(v as string)
               ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    // ── Ẩn placeholder khi collection có phần tử ─────────────────────────
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => v is int n && n == 0 ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    // ── Trả về Style NavButton hoặc NavButtonActive ───────────────────────
    public class BoolToNavStyleConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
        {
            bool active = v is bool b && b;
            return active
                ? Application.Current.FindResource("NavButtonActive")
                : Application.Current.FindResource("NavButton");
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    // ── Màu chữ cho trạng thái lịch hẹn ─────────────────────────────────
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => (v as string) switch
            {
                "Đã xác nhận" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                "Hoàn thành"  => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                "Đã hủy"      => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                _             => new SolidColorBrush(Color.FromRgb(255, 152, 0))
            };
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    // ── Màu nền cho badge trạng thái ─────────────────────────────────────
    public class StatusToBgConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => (v as string) switch
            {
                "Đã xác nhận" => new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                "Hoàn thành"  => new SolidColorBrush(Color.FromRgb(227, 242, 253)),
                "Đã hủy"      => new SolidColorBrush(Color.FromRgb(255, 235, 238)),
                _             => new SolidColorBrush(Color.FromRgb(255, 243, 224))
            };
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }
}
