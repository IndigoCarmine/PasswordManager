﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;

namespace PasswordManager
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<Data> BindingDataList = new ObservableCollection<Data>();

        Settings settings;
        string password;
        
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

            if (!File.Exists("settings.xml"))
            {
                //初回起動
                settings = new Settings();
            }
            else
            {
                //二回目以降
                ImportSetting();
                if (settings.UseTheSameFile)
                {
                    var PassForm = new PasswordForm("パスワードを入力してください。");
                    PassForm.ShowDialog();
                    password = PassForm.Value;
                    ImportData();
                }
            }
            this.DataContext = BindingDataList;

        }
        /// <summary>
        /// Passwordの取得と配置
        /// </summary>
        private void ImportData()
        {
            if (settings == null)
            {
                Select_File();
            }
            else if (!File.Exists(settings.DefaultFilePath))
            {
                MessageBox.Show("ファイルが存在しません。消去または、移動された可能性があります。");
                return;
            }
            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<Data>));
            using (BinaryReader v = new BinaryReader(new FileStream(settings.DefaultFilePath, FileMode.Open)))
            {
                if (v.BaseStream.Length == 0) return;
                byte[] decstr;
                try
                {
                    decstr = Cryptography.DecryptString(v.ReadBytes((int)v.BaseStream.Length), password);
                    using (MemoryStream memoryStream = new MemoryStream(decstr))
                    {

                        BindingDataList.Clear();
                        foreach (Data data in (ObservableCollection<Data>)serializer.Deserialize(memoryStream))
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
                    return;
                }
            }

        }
        private bool SaveData()
        {
            //初回用
            if (password == null || password == "")
            {
                var PassForm = new PasswordForm("パスワードを作成してください。");
                PassForm.ShowDialog();
                password = PassForm.Value;
            }


            if (settings == null || settings.DefaultFilePath == null)
            {
                while (!Select_File())
                {
                    MessageBoxResult messageboxResult = MessageBox.Show("ファイルを選択しないと保存できません。 \n 選択しますか。", "選択", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    switch (messageboxResult)
                    {
                        case MessageBoxResult.Yes:
                            break;
                        case MessageBoxResult.No:
                            return false;
                        default:
                            break;
                    }

                }

            }

            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<Data>));
            using (BinaryWriter v = new BinaryWriter(new FileStream(settings.DefaultFilePath, FileMode.OpenOrCreate)))
            {

                using (MemoryStream memoryStream = new MemoryStream())
                {

                    serializer.Serialize(memoryStream, BindingDataList);



                    byte[] bytedata = Cryptography.EncryptString(memoryStream.ToArray(), password);
                    v.Write(bytedata, 0, bytedata.Length);
                }

            }
            return true;
        }

        private void ImportSetting()
        {
            
            //設定ファイルの読み込み
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            using (var reader = new StreamReader("settings.xml", Encoding.UTF8))
            {
                settings = (Settings)serializer.Deserialize(reader);
            }

            return;
        }
        private void SaveSettings()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            using (StreamWriter writer = new StreamWriter("settings.xml"))
            {
                serializer.Serialize(writer, settings);
            }
        }
        public void AddData(object sender, RoutedEventArgs e)
        {
            Data data = new Data();
            data.ShowWindow.Execute(null);
            BindingDataList.Add(data);
        }
        public void Open_Click(object sender, RoutedEventArgs e)
        {
            Select_File();
            var PassForm = new PasswordForm("パスワードを入力してください。");
            PassForm.ShowDialog();
            password = PassForm.Value;
            ImportData();

        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveData();
            SaveSettings();
        }

        /// <summary>
        /// 成功したら真を返す
        /// </summary>
        /// <returns></returns>
        public bool Select_File()
        {
            settings = new Settings();
            var dialog = new OpenFileDialog
            {
                Multiselect = false,
                Title = "作成済みのデータファイルを選択してください。",
                AddExtension = true,
                CheckFileExists = false,
                Filter = "PWM Files (*.pwm)|*.pwm"
            };
            if ((bool)dialog.ShowDialog())
            {
                settings.DefaultFilePath = dialog.FileName;
                return true;
            }
            else
            {
                return false;
            }
        }


        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (settings != null) settings.UseTheSameFile = true;
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (settings != null) settings.UseTheSameFile = true;
        }
    }

    public class Data : INotifyPropertyChanged
    {
        #region 記録される内容
        public string AccountID
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

        public string URL
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
        public string Password
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
        public string BindAddress
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
        private string _AccountID;
        [XmlIgnore]
        private string _Password;
        [XmlIgnore]
        private string _BindAddress;
        [XmlIgnore]
        private string _URL;
        [XmlIgnore]
        public ImageSource Img
        {
            set
            {
                _Img = value;
                NotifyPropertyChanged();

            }
            get
            {
                if (_Img == null) return ImageSourceConvert.ToImageSource(Properties.Resources.disco);
                return _Img;
            }
        }
        [XmlIgnore]
        private ImageSource _Img;
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
                InputForm window = new InputForm(this);
                window.Show();

            });
            ShowPassword = new SimpleCommand(() =>
            {
                Passwordb = Visibility.Visible;
            });
            AddPropertyChangedHandler("URL", async () => Img = await LoadfaviconFromURL(_URL));
        }
        public event PropertyChangedEventHandler PropertyChanged = (o, e) => { };

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AddPropertyChangedHandler(string propertyName, Action action)
        {
            if (action == null) throw new ArgumentNullException("action");

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
        private async Task<ImageSource> LoadfaviconFromURL(string url)
        {

 
            //------------------------
            // パラメータチェック
            //------------------------
            if (url == null || url.Trim().Length <= 0)
            {
                return ImageSourceConvert.ToImageSource(Properties.Resources.disco);
            }
            if (!("/" == url.Substring(url.Length)))
            {
                url += "/";
            }
            url += "favicon.ico";
            //----------------------------
            // Webサーバに要求を投げる
            //----------------------------           
            using (var client = new HttpClient())
            {
                client.Timeout = new TimeSpan(0, 0, 10);
                try
                {
                    byte[] response = await client.GetByteArrayAsync(url);
                    using (MemoryStream imgStream = new MemoryStream(response))
                    {
                        var bitmap = new Bitmap(imgStream);
                        if (bitmap == null)
                        {
                            bitmap = Properties.Resources.disco;
                        }
                    
                        return ImageSourceConvert.ToImageSource(bitmap);
                    }
                }
                catch(HttpRequestException) 
                {
                    return ImageSourceConvert.ToImageSource(Properties.Resources.disco);
                }
            }
                

        }
    }

    /// <summary>
    /// コマンド
    /// </summary>
    class SimpleCommand : ICommand
    {
        Action Comclick_event;
        public SimpleCommand(Action _click_event)
        {
            Comclick_event = _click_event;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            Comclick_event();
        }
    }

    /// <summary>
    /// ImageからImageSourceへ変更するためのクラス
    /// </summary>
    static class ImageSourceConvert
    {

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public static ImageSource ToImageSource(this Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }
    }


    public class Settings
    {
        public string DefaultFilePath;
        public bool UseTheSameFile =true;
    }




    public class Other
    {
        public string Key { get; set; }
        public string Value { get; set; }

    }

    /// <summary>
    /// 拝借したもの
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
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
                        this.Add(item.Key, item.Value);
                    }
                }
            }
            finally
            {
                reader.Read();
            }
        }

        public void WriteXml(System.Xml.XmlWriter writer)
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
            public TKey Key { get; set; }
            public TValue Value { get; set; }

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


