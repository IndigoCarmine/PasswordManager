using System.Windows;
using System.Windows.Input;
namespace PasswordManager
{
    /// <summary>
    /// PasswordForm.xaml の相互作用ロジック
    /// </summary>
    public partial class PasswordForm : Window
    {
        public string Value { get; set; }

        public PasswordForm(string Message)
        {
            this.DataContext = Message;
            InitializeComponent();
            Password.Focus();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Value = Password.Password;
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Value = null;
            this.Close();
        }

        private void Password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Button_Click(null, null);
        }
    }
}
