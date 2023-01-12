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
using Microsoft.Win32;

namespace PasswordManager
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<Data> BindingDataList = new();

        Settings? settings;
        string? password;
        
        public MainWindow()
        {
            InitializeComponent();
            Initalize();

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

        }

       
        private void Initalize()
        {
            bool SettingFlileExist = File.Exists("settings.xml");
            if (!SettingFlileExist)
            {
                var result = MessageBox.Show("初回起動ですか。", "選択", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (result == MessageBoxResult.Yes)
                {
                    //初回
                    ShowIntroduction();
                    this.DataContext = BindingDataList;
                    return;
                }
                else { 

                }
            }
            bool ReadSuccess = ImportSetting();
            while (!(SettingFlileExist || ReadSuccess))
            {
                //設定ファイル紛失の２回目以降
                var path = OpenFile();
                if (path == null)
                {
                    MessageBox.Show("ファイルが選択されていません。終了します。");
                    this.Close();
                }
                else
                {
                    settings = new() { DefaultFilePath = path };
                    SaveSettings();

                }
                ReadSuccess = ImportSetting();
            }

            //二回目以降
            if (settings!.UseTheSameFile)
            {
                var PassForm = new PasswordForm("パスワードを入力してください。");
                PassForm.ShowDialog();
                password = PassForm.Value;
                ImportData();
            }
            this.DataContext = BindingDataList;

            return;


        }
        /// <summary>
        /// 紹介のなにかを表示する。
        /// </summary>
        static private void ShowIntroduction()
        {

        }
        static private string? OpenFile()
        {
            OpenFileDialog dialog = new()
            {
                Multiselect = false,
                Title = "作成済みのデータファイルを選択してください。",
                AddExtension = true,
                CheckFileExists = true,
                Filter = "PWM Files (*.pwm)|*.pwm"
            };
            if ((bool)dialog.ShowDialog()!)
            { 
                return dialog.FileName;
            }
            else
            {
                return null;
            }
        }
        static private string? MakeNewFile()
        {
            SaveFileDialog dialog = new()
            {
                Title = "保存先のファイルを選択してください。",
                AddExtension = true,
                CheckFileExists = false,
                Filter = "PWM Files (*.pwm)|*.pwm"
            };
            if ((bool)dialog.ShowDialog()!)
            {
                return dialog.FileName;
            }
            else
            {
                return null;
            }
        }
        private void ImportData()
        {
            if (!File.Exists(settings!.DefaultFilePath))
            {
                MessageBox.Show("ファイルが存在しません。消去または、移動された可能性があります。");
                return;
            }
            else
            {
                FileStream filestream;
                XmlSerializer serializer = new(typeof(ObservableCollection<Data>));
                try
                {
                    filestream = new FileStream(settings!.DefaultFilePath!, FileMode.Open);
                    using (BinaryReader v = new(filestream))
                    {
                        if (v.BaseStream.Length == 0) return;
                        byte[] decstr;
                        try
                        {
                            decstr = Cryptography.DecryptString(v.ReadBytes((int)v.BaseStream.Length), password!);
                            using (MemoryStream memoryStream = new(decstr))
                            {

                                BindingDataList.Clear();
                                foreach (Data data in (ObservableCollection<Data>)(serializer.Deserialize(memoryStream)!))
                                {
                                    BindingDataList.Add(data);
                                }
                            }
                        }
                        catch (CryptographicException)
                        {
                            var PassForm = new PasswordForm("パスワードが間違っています。再入力してください。");
                            PassForm.ShowDialog();
                            password = PassForm.Value;
                            ImportData();
                            return;
                        }
                    }
                }
                catch (FileNotFoundException)
                {

                }
                

            }

        }
        private bool SaveData()
        {
            int count = 3;
            //初回用
            while(password == null || password == "")
            {
                var PassForm = new PasswordForm("パスワードを作成してください。");
                PassForm.ShowDialog();
                if (PassForm.Value != null) password = PassForm.Value;
                count--;
                if(count == 0)
                {
                    MessageBox.Show("保存できませんでした。");
                    return false;
                }
            }
            while (settings == null || settings.DefaultFilePath == null)
            {
                MessageBoxResult messageboxResult = MessageBox.Show("ファイルを新規作成しますか。 \n No=既存ファイルに上書き。", "選択", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
                string? path = null;
                switch (messageboxResult)
                {
                    case MessageBoxResult.Yes:
                        path = MakeNewFile();
                        break;
                    case MessageBoxResult.No:
                        path = OpenFile();
                        break;
                    case MessageBoxResult.Cancel:
                        return false;
                        break;
                    default:
                        break;
                }
                if (path ==null) break;
                settings = new() { DefaultFilePath = path };
            }

            XmlSerializer serializer = new(typeof(ObservableCollection<Data>));
            using (BinaryWriter v = new(new FileStream(settings!.DefaultFilePath!, FileMode.OpenOrCreate)))
            {
                using (MemoryStream memoryStream = new())
                {
                    serializer.Serialize(memoryStream, BindingDataList);
                    byte[] bytedata = Cryptography.EncryptString(memoryStream.ToArray(), password);
                    v.Write(bytedata, 0, bytedata.Length);
                }

            }
            return true;
        }

        private bool ImportSetting()
        {
            //設定ファイルの読み込み
            XmlSerializer serializer = new(typeof(Settings));
            if (!File.Exists("settings.xml")) return false;
            using (StreamReader reader = new("settings.xml", Encoding.UTF8))
            {
                try
                {
                    settings = serializer.Deserialize(reader) as Settings;
                }catch (InvalidOperationException)
                {
                   return false;
                }
            }
            return true;
        }

        /// <summary>
        /// settingsがnullでなければ、保存されてtrueを返す。
        /// </summary>
        /// <returns></returns>
        private bool SaveSettings()
        {
            if(settings ==null)
            {
                return false;
            }
            else
            {
                XmlSerializer serializer = new(typeof(Settings));
                using (StreamWriter writer = new("settings.xml"))
                {
                    serializer.Serialize(writer, settings);
                }
                return true;
            }

            
        }

        public void AddData(object sender, RoutedEventArgs e)
        {
            Data data = new();
            data.ShowWindow.Execute(null);
            BindingDataList.Add(data);
        }
        public void Open_Click(object sender, RoutedEventArgs e)
        {
            var path = OpenFile();
            if (path == null) return;
            settings = new() { DefaultFilePath = path };
            SaveSettings();
            PasswordForm PassForm = new("パスワードを入力してください。");
            PassForm.ShowDialog();
            password = PassForm.Value;
            ImportData();
        }
        public void New_Click(object sender, RoutedEventArgs e)
        {
            settings = new();
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
            settings = new();
            settings.DefaultFilePath = MakeNewFile();
            if(settings.DefaultFilePath == null)
            {
                MessageBox.Show("保存されませんでした。");
                return;
            }
            SaveData();
            SaveSettings();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (settings != null) settings.UseTheSameFile = true;
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (settings != null) settings.UseTheSameFile = true;
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
    }

    public class Data : INotifyPropertyChanged
    {
        #region 記録される内容
        public string? AccountID
        {
            get
            {
                return _AccountID;
            }
            set
            {
                _AccountID = value;
                NotifyPropertyChanged();
            }
        }

        public string? URL
        {
            get { 
                return _URL;
            }
            set
            {
                _URL = value;
                NotifyPropertyChanged();
            }
        }
        public string? Password
        {
            get
            {
                return _Password;
            }
            set
            {
                _Password = value;
                NotifyPropertyChanged();
            }
        }
        public string? BindAddress
        {
            get
            {
                return _BindAddress;
            }
            set
            {
                _BindAddress = value;
                NotifyPropertyChanged();
            }
        }
        public ObservableCollection<Other> Others { get; set; }

        #endregion

        [XmlIgnore]
        private string? _AccountID;
        [XmlIgnore]
        private string? _Password;
        [XmlIgnore]
        private string? _BindAddress;
        [XmlIgnore]
        private string? _URL;
        [XmlIgnore]
        public ImageSource? Img
        {
            set
            {
                _Img = value;
                NotifyPropertyChanged();

            }
            get
            {
                if (_Img == null) return ImageSourceConvert.ToImageSource();
                return _Img;
            }
        }
        [XmlIgnore]
        private ImageSource? _Img;
        [XmlIgnore]
        public ICommand ClipPassword { get; private set; }
        [XmlIgnore]
        public ICommand ShowWindow { get; private set; }
        [XmlIgnore]
        public ICommand ShowPassword { get; set; }
        [XmlIgnore]
        public Visibility Passwordb { get; set; }
        public Data()
        {
            Passwordb = Visibility.Hidden;
            Others = new ObservableCollection<Other>();
            ClipPassword = new SimpleCommand(() => ClipboardService.SetText(Password));
            ShowWindow = new SimpleCommand(() => {
                InputForm window = new(this);
                window.Show();

            });
            ShowPassword = new SimpleCommand(() =>
            {
                Passwordb = Visibility.Visible;
            });
            AddPropertyChangedHandler(nameof(URL), async () => Img = await LoadfaviconFromURL(_URL));
        }
        public event PropertyChangedEventHandler? PropertyChanged = (o, e) => { };

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged!(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AddPropertyChangedHandler(string propertyName, Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == propertyName)
                {
                    action();
                }
            };
        }

        /// <summary>
        /// 指定されたURLの画像をImage型オブジェクトとして取得する
        /// </summary>
        /// <param name="url">画像データのURL(ex: http://example.com/foo.jpg) </param>
        /// <returns>         画像データ</returns>
        static private async Task<ImageSource?> LoadfaviconFromURL(string? url)
        {

 
            // パラメータチェック
            if (url == null || url.Trim().Length <= 0)
            {
                return ImageSourceConvert.ToImageSource();
            }
            if (!("/" == url[url.Length..]))
            {
                url += "/";
            }
            url += "favicon.ico";

            // Webサーバに要求を投げる         
            using (HttpClient client = new())
            {
                client.Timeout = new(0, 0, 10);
                try
                {
                    byte[] response = await client.GetByteArrayAsync(url);
                    return ImageSourceConvert.ToImageSource(response);
                }
                catch(HttpRequestException) 
                {
                    return ImageSourceConvert.ToImageSource();
                }
                catch(InvalidOperationException) {
                    return ImageSourceConvert.ToImageSource(); 
                }
            }
                

        }
    }

    /// <summary>
    /// コマンド
    /// </summary>
    class SimpleCommand : ICommand
    {
        readonly Action Comclick_event;
        public SimpleCommand(Action _click_event)
        {
            Comclick_event = _click_event;
        }

        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            Comclick_event();
        }
    }

    /// <summary>
    /// ImageからImageSourceへ変更するためのクラス
    /// </summary>
    static class ImageSourceConvert
    {


        public static BitmapImage? ToImageSource(byte[] bmp)
        {
            BitmapImage imageSource = new();

            try
            {
                using (MemoryStream stream = new(bmp))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    imageSource.BeginInit();
                    imageSource.StreamSource = stream;
                    imageSource.CacheOption = BitmapCacheOption.OnLoad;
                    imageSource.EndInit();
                }
                return imageSource;
            }
            catch (System.NotSupportedException)
            {
                return null;
            }
        }
        public static BitmapImage? ToImageSource()
        {
            return new BitmapImage(new Uri("img_229205.png", UriKind.Relative));
        }
    }


    public class Settings
    {
        public string? DefaultFilePath;
        public bool UseTheSameFile = true;
    }




    public class Other
    {
        public string? Key { get; set; }
        public string? Value { get; set; }

    }

    /// <summary>
    /// 拝借したもの
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable where TKey: notnull
    {
        public XmlSchema? GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            var serializer = new XmlSerializer(typeof(KeyValueItem));

            reader.Read();
            if (reader.IsEmptyElement)
                return;

            try
            {
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    // これがないと下でぬるりが出る時がある
                    if (!serializer.CanDeserialize(reader))
                        return;
                    else
                    {
                        var item = serializer.Deserialize(reader) as KeyValueItem; // 従来はここでnullになるときがあった
                        this.Add(item!.Key!, item.Value!);
                    }
                }
            }
            finally
            {
                reader.Read();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);

            var serializer = new XmlSerializer(typeof(KeyValueItem));

            foreach (var item in this.Keys.Select(key => new KeyValueItem(key, this[key])))
            {
                serializer.Serialize(writer, item, ns);
            }
        }

        public class KeyValueItem
        {
            public TKey? Key { get; set; }
            public TValue? Value { get; set; }

            public KeyValueItem(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }

            public KeyValueItem()
            {
            }
        }
    }
}


