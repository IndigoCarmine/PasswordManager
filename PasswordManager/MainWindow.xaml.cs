using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;



namespace PasswordManager
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<Data> BindingDataList = new();

        PathGetter pathGetter = new("PWM Files (*.pwm)|*.pwm");
        PasswordFile? passwordFile;
        bool UseTheSameFile = false;

        public MainWindow()
        {
            InitializeComponent();

            this.Closing += new CancelEventHandler((sender, e) => {
                MessageBoxResult messageboxResult = MessageBox.Show("保存して終了しますか。\n（はい=保存終了, いいえ=終了）", "選択", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);
                switch (messageboxResult)
                {
                    case MessageBoxResult.Yes:
                        SaveData();
                        SaveSettings();
                        e.Cancel = false;
                        break;
                    case MessageBoxResult.No:
                        e.Cancel = false;
                        SaveSettings();
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                    default:
                        e.Cancel = true;
                        break;
                }
            });

            Initalize();
        }


        private void FirstNavigation()
        {

        }


       /// <summary>
       /// settings.xmlが存在していれば読み込み、
       /// </summary>
        private void Initalize()
        {
            if (!File.Exists("settings.xml"))
            {
                ShowIntroduction();
                return;
            }

            Settings? settings = LoadSetting();
            if (settings == null || settings.DefaultFilePath == null) return;

            string? password = ShowPasswordForm();
            if (password == null) return;

            passwordFile = new(settings.DefaultFilePath, password);


            BindingDataList = passwordFile.Read() ?? new();
            this.DataContext = BindingDataList;
            return;
        }
        /// <summary>
        /// 紹介のなにかを表示する。
        /// </summary>
        static private void ShowIntroduction()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="count">Passwordの再入力回数</param>
        private void ImportData(int count = 3)
        {
            if (count < 0) return;
            if (passwordFile == null) return;

            if (!File.Exists(passwordFile.path))
            {
                MessageBox.Show("ファイルが存在しません。消去または、移動された可能性があります。");
                return;
            }

            try
            {
                ObservableCollection<Data>? data = passwordFile.Read();
                if(data !=null) 
                { 
                    BindingDataList= data;
                    return;
                }
            }
            catch (SystemException e)
            {
                switch (e)
                {
                    case CryptographicException:
                        var PassForm = new PasswordForm("パスワードが間違っています。再入力してください。");
                        PassForm.ShowDialog();
                        ShowPasswordForm();
                        if(PassForm.Value != null) passwordFile.password = PassForm.Value;
                        break;
                    case IOException:
                        MessageBox.Show("ファイルが開けません。他のアプリがファイルを開いています。");
                        break;
                    default:
                        MessageBox.Show(e.ToString()); 
                        break;
                }

                return;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="count">Passwordの再設定回数</param>
        /// <returns></returns>
        private bool SaveData(int count=3)
        {
            if (count < 0) return false;

            
            if (passwordFile == null) {
                MessageBoxResult messageboxResult = MessageBox.Show("ファイルを新規作成しますか。 \n No=既存ファイルに上書き。", "選択", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
                string? path = null;
                switch (messageboxResult)
                {
                    case MessageBoxResult.Yes:
                        path = pathGetter.MakeNewFile();
                        break;
                    case MessageBoxResult.No:
                        path = pathGetter.OpenFile();
                        break;
                    case MessageBoxResult.Cancel:
                        return false;
                    default:
                        break;
                }
                //設定されずに試行回数も超えたら終わる。
                if (path == null) return SaveData(count - 1);

                string? password = ShowPasswordForm();
                if (password == null) return SaveData(count - 1);

                passwordFile = new(path,password);
            }

            try
            {
                return passwordFile.Write(BindingDataList);
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case IOException:
                        MessageBox.Show("ファイルが開けません。他のアプリがファイルを開いています。");
                        break;
                    default:
                        MessageBox.Show("[In SaveData()] " + e.ToString());
                        break;
                }
            }
            return false;

        }

        /// <summary>
        /// 設定を設定ファイルから取得する。
        /// </summary>
        /// <returns>失敗したときnullを返す。</returns>
        private Settings? LoadSetting()
        {
            //設定ファイルの読み込み

            if (!File.Exists("settings.xml")) return null;
            XmlSerializer serializer = new(typeof(Settings));

            using (StreamReader reader = new("settings.xml", Encoding.UTF8))
            {
                try
                {
                    return (Settings?)serializer.Deserialize(reader);
              
                }
                catch (InvalidOperationException)
                {
                   return null;
                }
            }
        }

        /// <summary>
        /// 内部情報をもとにsettingsファイルを生成する。
        /// </summary>
        /// <returns>成功したら真</returns>
        private bool SaveSettings()
        {
            if (passwordFile == null) return false;
            
            Settings settings = new();
            settings.DefaultFilePath = passwordFile.path;

            XmlSerializer serializer = new(typeof(Settings));
            try
            {
                using (StreamWriter writer = new("settings.xml"))
                {
                    serializer.Serialize(writer, settings);
                }
            } catch (Exception ex)
            {
                switch (ex)
                {
                    case IOException: //file is
                        return false;
                    default:
                        MessageBox.Show("[In SabeSattings()] "+ ex.ToString());
                        break;
                }
            }
            return true;

            
        }

        private string? ShowPasswordForm()
        {
            PasswordForm PassForm = new("パスワードを入力してください。");
            PassForm.ShowDialog();
            return PassForm.Value;
        }

        public void AddData(object sender, RoutedEventArgs e)
        {
            Data data = new();
            data.ShowWindow.Execute(null);
            BindingDataList.Add(data);
        }
        public void Open_Click(object sender, RoutedEventArgs e)
        {
            string? path = pathGetter.OpenFile();
            if(path == null) return;

            string? password = ShowPasswordForm();
            if (password == null) return;

            passwordFile = new(path, password);
            SaveSettings();
            ImportData();
            this.DataContext = BindingDataList;
        }
        public void New_Click(object sender, RoutedEventArgs e)
        {
            BindingDataList = new ObservableCollection<Data>();
            this.DataContext = BindingDataList;
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveData();
            SaveSettings();
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            string? path = pathGetter.MakeNewFile();
            if(path == null)
            {
                MessageBox.Show("保存されませんでした。");
                return;
            }
            
            string? password = ShowPasswordForm();
            if (password == null) return;

            passwordFile = new(path, password);

            try
            {
                passwordFile.Write(this.BindingDataList);
            }
            catch(Exception ex)
            {
                switch (ex)
                {
                    case IOException:
                        MessageBox.Show("ファイルが開けません");
                        break;
                    default: 
                        MessageBox.Show("[In Save_As_Click()] "+ ex.ToString());
                        break;
                }
                return;
            }

            SaveSettings();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UseTheSameFile = true;
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
           UseTheSameFile = true;
        }

        private void XmlView(object sender, RoutedEventArgs e)
        {
            XmlSerializer serializer = new(typeof(ObservableCollection<Data>));

            using (MemoryStream memoryStream = new())
            {
                serializer.Serialize(memoryStream, BindingDataList);
                string context = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                MessageBox.Show(context);
                ClipboardService.SetText(context);
            }


        }

        private void Import_Old_Click(object sender, RoutedEventArgs e)
        {
            string? path = pathGetter.OpenFile();
            if (path == null) return;

            string? password = ShowPasswordForm();
            if (password == null) return;

            FileStream filestream;
            XmlSerializer serializer = new(typeof(ObservableCollection<Data>));
            try
            {
                filestream = new FileStream(path, FileMode.Open);
                using (BinaryReader v = new(filestream))
                {
                    if (v.BaseStream.Length == 0) return;
                    byte[] decstr;

                    decstr = OldCryptography.DecryptString(v.ReadBytes((int)v.BaseStream.Length), password);
                    using (MemoryStream memoryStream = new(decstr))
                    {
                        BindingDataList = (ObservableCollection<Data>)serializer.Deserialize(memoryStream)!;
                        this.DataContext= BindingDataList;
                    }
                }
            }
            catch (FileNotFoundException)
            {
                return;
            }

        }
    }

 }


