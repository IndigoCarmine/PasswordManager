using System.Windows;

namespace PasswordManager
{
    /// <summary>
    /// InputForm.xaml の相互作用ロジック
    /// </summary>
    public partial class InputForm : Window
    {
        private readonly Data SelectedData;
        public InputForm(Data data)
        {
            InitializeComponent();
            SelectedData = data;
            this.DataContext = data;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var NForm = new NormalForm("追加したい要素名を入力してください。");
            NForm.ShowDialog();
            if (NForm.Value != null)
            {
                string key = NForm.Value;
                SelectedData.Others.Add(new Other() { Key = key });
            }
            else { this.Close(); }

        }
    }
}
