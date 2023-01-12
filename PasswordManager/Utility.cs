using System.IO;
using System.Windows.Media.Imaging;
using System;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows.Forms.Design;
using System.IO.Packaging;
using System.Security.Cryptography;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Security.Policy;

namespace PasswordManager
{

    /// <summary>
    /// ImageからImageSourceへ変更するためのクラス
    /// </summary>
    static class ImageSourceConverter
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

    class PathGetter
    {
        string filename_filter;
        public PathGetter(string filename_filter)
        {
            this.filename_filter = filename_filter;
        }
        public string? OpenFile()
        {
            OpenFileDialog dialog = new()
            {
                Multiselect = false,
                Title = "作成済みのデータファイルを選択してください。",
                AddExtension = true,
                CheckFileExists = true,
                Filter = filename_filter
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string? MakeNewFile()
        {
            SaveFileDialog dialog = new()
            {
                Title = "保存先のファイルを選択してください。",
                AddExtension = true,
                CheckFileExists = false,
                Filter = filename_filter
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
    }

    class PasswordFile
    {
        public string path;
        public string password;

        public PasswordFile(string path,string password) 
        { 
            this.path = path;
            this.password = password;
        }

        public ObservableCollection<Data>? Read(string path,string password)
        {
            this.path = path;
            this.password = password;
            return Read();
        }

        /// <summary>
        /// if file is not exist, return null.
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<Data>? Read() 
        {
            FileStream filestream;
            XmlSerializer serializer = new(typeof(ObservableCollection<Data>));
            try
            {
                filestream = new FileStream(path, FileMode.Open);
                using (BinaryReader v = new(filestream))
                {
                    if (v.BaseStream.Length == 0) return null;
                    byte[] decstr;

                    decstr = NewCryptography.DecryptString(v.ReadBytes((int)v.BaseStream.Length), password);
                    using (MemoryStream memoryStream = new(decstr))
                    {
                        return (ObservableCollection<Data>)serializer.Deserialize(memoryStream)!;       
                    }
                }
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public bool Write(ObservableCollection<Data> data,string path)
        {
            this.path = path;
            return Write(data);
        }

        public bool Write(ObservableCollection<Data> data)
        {
            if (data == null) return false;
            XmlSerializer serializer = new(typeof(ObservableCollection<Data>));
            using (BinaryWriter v = new(new FileStream(path, FileMode.OpenOrCreate)))
            {
                using (MemoryStream memoryStream = new())
                {
                    serializer.Serialize(memoryStream, data);
                    byte[] salt;
                    byte[] bytedata = NewCryptography.EncryptString(memoryStream.ToArray(), password,out salt);
                    v.Write(salt, 0, salt.Length);
                    v.Write(bytedata,0, bytedata.Length);
                }

            }
            return true;
        }
    }


}