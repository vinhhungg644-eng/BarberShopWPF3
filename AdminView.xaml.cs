using BarberShopApp.Models;
using BarberShopWPF.ViewModels;
using System.Windows;

namespace BarberShopWPF.Views
{
    public partial class AdminView : Window
    {
        public AdminView(Account account)
        {
            InitializeComponent();
            DataContext = new AdminViewModel(account);
        }
    }
}
