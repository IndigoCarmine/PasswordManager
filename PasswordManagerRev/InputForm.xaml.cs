using System.Windows;

namespace PasswordManager
{
    /// <summary>
    /// InputForm.xaml の相互作用ロジック
    /// </summary>
    public partial class InputForm : Window
    {
        private Data SelectedData;
        public InputForm(Data data)
        {
            InitializeComponent();
            SelectedData = data;
            this.DataContext = data;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var PassForm = new PasswordForm("追加したい要素名を入力してください。");
            PassForm.ShowDialog();
            string key = PassForm.Value;
            SelectedData.Others.Add(new Other() { Key = key });
        }
    }
}
