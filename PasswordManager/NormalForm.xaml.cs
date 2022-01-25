using System.Windows;
using System.Windows.Input;
namespace PasswordManager
{
    /// <summary>
    /// PasswordForm.xaml の相互作用ロジック
    /// </summary>
    public partial class NormalForm : Window
    {
        public string? Value { get; set; }

        public NormalForm(string Message)
        {
            this.DataContext = Message;
            InitializeComponent();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Value = Text.Text;
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Value = null;
            this.Close();
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {

            //引数は適当
            if (e.Key == Key.Enter) Button_Click(0, new RoutedEventArgs());
        }

    }
}
