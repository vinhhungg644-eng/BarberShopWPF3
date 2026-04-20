using BarberShopWPF.ViewModels;
using System;
using System.Windows;
using System.Windows.Media;

namespace BarberShopWPF.Views
{
    public partial class RegisterView : Window
    {
        private readonly LoginViewModel _vm;

        public RegisterView(LoginViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            MouseLeftButtonDown += (_, e) => { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };
        }

        private void RegisterBtn_Click(object sender, RoutedEventArgs e)
        {
            _vm.DoRegister(new RegisterInfo
            {
                FullName = TxtName.Text.Trim(),
                Phone    = TxtPhone.Text.Trim(),
                Username = TxtUser.Text.Trim(),
                Password = PbPwd.Password,
                Dob      = DpDob.SelectedDate
            });

            bool success = _vm.ErrorMsg.StartsWith("✅");
            MsgBorder.Background = success
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 40, 20))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 10, 15));
            TxtMsg.Foreground = success
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 187, 106))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 83, 112));
            TxtMsg.Text       = _vm.ErrorMsg;
            MsgBorder.Visibility = Visibility.Visible;

            if (success)
            {
                var t = new System.Windows.Threading.DispatcherTimer
                        { Interval = TimeSpan.FromSeconds(1.5) };
                t.Tick += (_, _) => { t.Stop(); Close(); };
                t.Start();
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
