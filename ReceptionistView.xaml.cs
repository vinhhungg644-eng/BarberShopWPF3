using BarberShopApp.Models;
using BarberShopWPF.ViewModels;
using System.Windows;

namespace BarberShopWPF.Views
{
    public partial class ReceptionistView : Window
    {
        public ReceptionistView(Account account)
        {
            InitializeComponent();
            DataContext = new ReceptionistViewModel(account);
        }
    }
}
