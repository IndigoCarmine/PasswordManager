
using PasswordManager;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Markup;

namespace TestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            byte[] plane = new byte[100];
            RandomNumberGenerator.Fill(plane);
            string password = "dsahnfsdfgujfposdnfsdpo";

            byte[] crypto = NewCryptography.EncryptString(plane, password);
            byte[] plane2 = NewCryptography.DecryptString(crypto, password);
            check(plane, plane2);
        }
        [TestMethod]
        public void TestMethod2() 
        {
            byte[] a = new byte[] { 1, 2, 3, 4 };
            byte[] b = new byte[] { 5, 6, 7, 8 };
            byte[] data1, data2;
            using(MemoryStream stream = new MemoryStream())
            {
                stream.Write(a);
                stream.Write(b);
                data1= stream.ToArray();
            }
            data2 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }; 

            check(data1,data2);
        }
        void check(byte[] data1, byte[] data2)
        {
            for (int i = 0; i < data1.Length; i++)
            {
                Assert.AreEqual(data2[i], data1[i]);
            }
        }
        [TestMethod]
        public void BinaryTEst()
        {
            using (BinaryWriter v = new(new FileStream("C:\\Users\\taman\\test.bin", FileMode.OpenOrCreate)))
            {
                v.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

            }
        }
    }
}