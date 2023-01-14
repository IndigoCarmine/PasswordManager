using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Xml.Serialization;
using System;

namespace PasswordManager
{



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
            get
            {
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
                if (_Img == null) return ImageSourceConverter.ToImageSource();
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
                return ImageSourceConverter.ToImageSource();
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
                    return ImageSourceConverter.ToImageSource(response);
                }
                catch (HttpRequestException)
                {
                    return ImageSourceConverter.ToImageSource();
                }
                catch (InvalidOperationException)
                {
                    return ImageSourceConverter.ToImageSource();
                }
                catch (TaskCanceledException)
                {
                    return ImageSourceConverter.ToImageSource();
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

}