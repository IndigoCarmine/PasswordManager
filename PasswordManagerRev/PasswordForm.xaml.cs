using System.Windows;

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
    }
}
