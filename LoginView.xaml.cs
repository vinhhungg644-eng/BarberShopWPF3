using BarberShopWPF.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace BarberShopWPF.Views
{
    public partial class LoginView : Window
    {
        private LoginViewModel VM => (LoginViewModel)DataContext;

        public LoginView()
        {
            InitializeComponent();

            // SỬA LỖI: Chỉ cho phép DragMove khi nhấn vào vùng trống, 
            // không để nó chiếm quyền của các nút bấm/link.
            this.MouseLeftButtonDown += (s, e) =>
            {
                // Kiểm tra nếu người dùng nhấn vào nền (Window) chứ không phải vào Control con
                if (e.OriginalSource == this || e.OriginalSource is System.Windows.Controls.Grid || e.OriginalSource is System.Windows.Controls.Border)
                {
                    if (e.ButtonState == MouseButtonState.Pressed) DragMove();
                }
            };
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
            => VM.DoLogin(PbPwd.Password);

        private void PbPwd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) VM.DoLogin(PbPwd.Password);
        }
		private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ButtonState == MouseButtonState.Pressed)
				this.DragMove();
		}
		private void CloseBtn_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        // SỬA LỖI: Gộp lại thành 1 lần gọi duy nhất và ưu tiên dùng Preview
        private void RegisterLink_Click(object sender, MouseButtonEventArgs e)
        {
            // Ngăn chặn sự kiện trôi lên các lớp cha (để không bị dính DragMove)
            e.Handled = true;

            // Chỉ khởi tạo và show 1 lần duy nhất
            var registerWin = new RegisterView(VM)
            {
                Owner = this
            };

            registerWin.ShowDialog();
        }
    }
}