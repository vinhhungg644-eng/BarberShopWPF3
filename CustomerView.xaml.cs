using BarberShopApp.Models;
using BarberShopWPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace BarberShopWPF.Views
{
    public partial class CustomerView : Window
    {
        private CustomerViewModel VM => (CustomerViewModel)DataContext;

        public CustomerView(Account account, Customer customer)
        {
            InitializeComponent();
            DataContext = new CustomerViewModel(account, customer);
        }

        // ── Exception hợp lệ: Button trong DataGrid row dùng Tag để truyền ID ──
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var result = MessageBox.Show(
                    $"Bạn có chắc muốn hủy lịch hẹn #{id}?",
                    "Xác nhận hủy", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    VM.DoCancel(id);
            }
        }
    }
}
