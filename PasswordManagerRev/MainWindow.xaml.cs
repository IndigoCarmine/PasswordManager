using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
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
                    decstr = DecryptString(v.ReadBytes((int)v.BaseStream.Length), password);
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



                    byte[] bytedata = EncryptString(memoryStream.ToArray(), password);
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

        //拝借したもの
        /// <summary>
        /// 文字列を暗号化する
        /// </summary>
        /// <param name="sourceString">暗号化する文字列</param>
        /// <param name="password">暗号化に使用するパスワード</param>
        /// <returns>暗号化された文字列</returns>
        public static byte[] EncryptString(byte[] strBytes, string password)
        {
            //RijndaelManagedオブジェクトを作成
            System.Security.Cryptography.RijndaelManaged rijndael =
                new System.Security.Cryptography.RijndaelManaged();

            //パスワードから共有キーと初期化ベクタを作成
            GenerateKeyFromPassword(
                password, rijndael.KeySize, out byte[] key, rijndael.BlockSize, out byte[] iv);
            rijndael.Key = key;
            rijndael.IV = iv;

            //対称暗号化オブジェクトの作成
            System.Security.Cryptography.ICryptoTransform encryptor =
                rijndael.CreateEncryptor();
            //バイト型配列を暗号化する
            byte[] encBytes = encryptor.TransformFinalBlock(strBytes, 0, strBytes.Length);
            //閉じる
            encryptor.Dispose();

            //バイト型配列を文字列に変換して返す
            return encBytes;
        }

        /// <summary>
        /// 暗号化された文字列を復号化する
        /// </summary>
        /// <param name="sourceString">暗号化された文字列</param>
        /// <param name="password">暗号化に使用したパスワード</param>
        /// <returns>復号化された文字列</returns>
        public static byte[] DecryptString(byte[] strBytes, string password)
        {
            //RijndaelManagedオブジェクトを作成
            System.Security.Cryptography.RijndaelManaged rijndael =
                new System.Security.Cryptography.RijndaelManaged();

            //パスワードから共有キーと初期化ベクタを作成
            GenerateKeyFromPassword(
                password, rijndael.KeySize, out byte[] key, rijndael.BlockSize, out byte[] iv);
            rijndael.Key = key;
            rijndael.IV = iv;


            //対称暗号化オブジェクトの作成
            System.Security.Cryptography.ICryptoTransform decryptor =
                rijndael.CreateDecryptor();
            //バイト型配列を復号化する
            //復号化に失敗すると例外CryptographicExceptionが発生
            byte[] decBytes = decryptor.TransformFinalBlock(strBytes, 0, strBytes.Length);
            //閉じる
            decryptor.Dispose();

            //バイト型配列を文字列に戻して返す
            return decBytes;
        }

        /// <summary>
        /// パスワードから共有キーと初期化ベクタを生成する
        /// </summary>
        /// <param name="password">基になるパスワード</param>
        /// <param name="keySize">共有キーのサイズ（ビット）</param>
        /// <param name="key">作成された共有キー</param>
        /// <param name="blockSize">初期化ベクタのサイズ（ビット）</param>
        /// <param name="iv">作成された初期化ベクタ</param>
        private static void GenerateKeyFromPassword(string password, int keySize, out byte[] key, int blockSize, out byte[] iv)
        {
            //パスワードから共有キーと初期化ベクタを作成する
            //saltを決める
            byte[] salt = System.Text.Encoding.UTF8.GetBytes("saltは必ず8バイト以上");
            //Rfc2898DeriveBytesオブジェクトを作成する
            Rfc2898DeriveBytes deriveBytes =
                new Rfc2898DeriveBytes(password, salt);
            //.NET Framework 1.1以下の時は、PasswordDeriveBytesを使用する
            //System.Security.Cryptography.PasswordDeriveBytes deriveBytes =
            //    new System.Security.Cryptography.PasswordDeriveBytes(password, salt);
            //反復処理回数を指定する デフォルトで1000回
            deriveBytes.IterationCount = 1000;

            //共有キーと初期化ベクタを生成する
            key = deriveBytes.GetBytes(keySize / 8);
            iv = deriveBytes.GetBytes(blockSize / 8);
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
        public string AccountID { get; set; }
        public string URL
        {
            get { return _URL; }
            set
            {
                _URL = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Img"));
            }
        }
        public string Password { get; set; }
        public string BindAddress { get; set; }
        public ObservableCollection<Other> Others { get; set; }

        [XmlIgnore]
        public string _URL;
        [XmlIgnore]
        public ImageSource Img
        {
            get
            {
                return ImageSourceConvert.ToImageSource(LoadfaviconFromURL(URL));
            }
        }
        [XmlIgnore]
        public ICommand ClipPassword { get; private set; }
        [XmlIgnore]
        public ICommand ShowWindow { get; private set; }
        [XmlIgnore]
        public ICommand ShowPassword { get; set; }
        [XmlIgnore]
        public System.Windows.Visibility Passwordb { get; set; }
        public Data()
        {
            Passwordb = System.Windows.Visibility.Hidden;
            Others = new ObservableCollection<Other>();
            ClipPassword = new SimpleCommand(() => Clipboard.SetText(Password));
            ShowWindow = new SimpleCommand(() => {
                InputForm window = new InputForm(this);
                window.Show();

            });
            ShowPassword = new SimpleCommand(() =>
            {
                Passwordb = System.Windows.Visibility.Visible;
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 指定されたURLの画像をImage型オブジェクトとして取得する
        /// </summary>
        /// <param name="url">画像データのURL(ex: http://example.com/foo.jpg) </param>
        /// <returns>         画像データ</returns>
        private Bitmap LoadfaviconFromURL(string url)
        {
            int buffSize = 65536; // 一度に読み込むサイズ
            MemoryStream imgStream = new MemoryStream();

            //------------------------
            // パラメータチェック
            //------------------------
            if (url == null || url.Trim().Length <= 0)
            {
                return Properties.Resources.disco;
            }
            Console.WriteLine(url);
            if (!("/" == url.Substring(url.Length)))
            {
                url += "/";
            }
            //----------------------------
            // Webサーバに要求を投げる
            //----------------------------
            try
            {
                WebRequest req = WebRequest.Create(url + "favicon.ico");
                BinaryReader reader = new BinaryReader(req.GetResponse().GetResponseStream());
                //--------------------------------------------------------
                // Webサーバからの応答データを取得し、imgStreamに保存する
                //--------------------------------------------------------
                while (true)
                {
                    byte[] buff = new byte[buffSize];

                    // 応答データの取得
                    int readBytes = reader.Read(buff, 0, buffSize);
                    if (readBytes <= 0)
                    {
                        // 最後まで取得した->ループを抜ける
                        break;
                    }

                    // バッファに追加
                    imgStream.Write(buff, 0, readBytes);
                }
            }
            catch
            {
                return Properties.Resources.disco;
            }


            Bitmap bitmap = new Bitmap(imgStream);
            if (bitmap == null)
            {
                bitmap = Properties.Resources.disco;
            }
            return bitmap;

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


