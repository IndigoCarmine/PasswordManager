using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Security.Policy;
using System.Windows;
public static class Extensions
{
    public static T[] SubArray<T>(this T[] array, int offset, int length)
    {
        T[] result = new T[length];
        Array.Copy(array, offset, result, 0, length);
        return result;
    }
}

namespace PasswordManager
{
    /// <summary>
    /// データ形式
    /// 10byte: salt
    /// 16byte: initalize vector
    /// *byte: data
    /// </summary>
    public static class NewCryptography
    {
        const int salt_size = 16;
        public static byte[] EncryptString(byte[] strBytes, string password)
        {
            byte[] salt = new byte[salt_size];
            RandomNumberGenerator.Fill(salt);

            byte[] iv = new byte[16];
            RandomNumberGenerator.Fill(iv);

            using (Aes aes = Aes.Create())
            {
                aes.BlockSize = 16 * 8;
                aes.KeySize = 32 * 8;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;


                byte[] key;
                GenerateKeyFromPassword(password,salt, out key);

                using (ICryptoTransform cryptoTransform
                    = aes.CreateEncryptor(key, iv))
                {
                    byte[] crypto = cryptoTransform.TransformFinalBlock(strBytes, 0, strBytes.Length);
                    using (MemoryStream memory = new())
                    {
                        memory.Write(salt, 0, salt.Length);
                        memory.Write(iv, 0, iv.Length);
                        memory.Write(crypto,0, crypto.Length);
                        return memory.ToArray();
                    }
                }
            }
        }
        public static byte[] DecryptString(byte[] strBytes, string password)
        {
            byte[] salt = strBytes.SubArray(0, salt_size);
            byte[] iv= strBytes.SubArray(salt_size, 16);
            using(Aes aes = Aes.Create())
            {
                aes.BlockSize = 16 * 8;
                aes.KeySize = 32 * 8;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;


                byte[] key;
                GenerateKeyFromPassword(password, salt, out key);
                using (ICryptoTransform cryptoTransform
                    = aes.CreateDecryptor(key, iv))
                {
                    return cryptoTransform.TransformFinalBlock(
                        strBytes.SubArray(salt_size+16, strBytes.Length - (salt_size + 16)),0, strBytes.Length - (salt_size + 16));
                }

            }
        }
        private static void GenerateKeyFromPassword(string password, byte[] salt, out byte[] key)
        {
            //パスワードから共有キーと初期化ベクタを作成する
            //Rfc2898DeriveBytesオブジェクトを作成する
            Rfc2898DeriveBytes deriveBytes =
                new Rfc2898DeriveBytes(password, salt,1000,HashAlgorithmName.SHA512);

            //共有キーを生成する
            key = deriveBytes.GetBytes(256 / 8);
            
        }
    }

    public static class OldCryptography
    {
        //拝借したもの
        /// <summary>
        /// 文字列を暗号化する
        /// </summary>
        /// <param name="sourceString">暗号化する文字列</param>
        /// <param name="password">暗号化に使用するパスワード</param>
        /// <returns>暗号化された文字列</returns>
        public static byte[] EncryptString(byte[] strBytes, string password)
        {
            using (Aes aes = Aes.Create())
            {


                //パスワードから共有キーと初期化ベクタを作成
                GenerateKeyFromPassword(
                    password, aes.KeySize, out byte[] key, aes.BlockSize, out byte[] iv);
                aes.Key = key;
                aes.IV = iv;

                //対称暗号化オブジェクトの作成
                ICryptoTransform encryptor = aes.CreateEncryptor();
                //バイト型配列を暗号化する
                byte[] encBytes = encryptor.TransformFinalBlock(strBytes, 0, strBytes.Length);
                //閉じる
                encryptor.Dispose();

                //バイト型配列を文字列に変換して返す
                return encBytes;
            }
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

            using (Aes aes = Aes.Create())
            {
                //パスワードから共有キーと初期化ベクタを作成
                GenerateKeyFromPassword(
                    password, aes.KeySize, out byte[] key, aes.BlockSize, out byte[] iv);
                aes.Key = key;
                aes.IV = iv;


                //対称暗号化オブジェクトの作成
                ICryptoTransform decryptor = aes.CreateDecryptor();
                //バイト型配列を復号化する
                //復号化に失敗すると例外CryptographicExceptionが発生
                byte[] decBytes = decryptor.TransformFinalBlock(strBytes, 0, strBytes.Length);
                //閉じる
                decryptor.Dispose();
                return decBytes;
            }
            //バイト型配列を文字列に戻して返す

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

            //共有キーと初期化ベクタを生成する
            key = deriveBytes.GetBytes(keySize / 8);
            iv = deriveBytes.GetBytes(blockSize / 8);
        }

    }
}