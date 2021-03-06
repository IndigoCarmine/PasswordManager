using System;
using System.Security.Cryptography;

namespace PasswordManager
{
    public static class Cryptography
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
            deriveBytes.IterationCount = 1000;

            //共有キーと初期化ベクタを生成する
            key = deriveBytes.GetBytes(keySize / 8);
            Console.WriteLine(key.ToString());
            iv = deriveBytes.GetBytes(blockSize / 8);
            Console.WriteLine(iv.ToString());
        }

    }
}