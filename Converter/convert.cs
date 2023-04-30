using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Security.Cryptography;

namespace Converter
{
    public class convert
    {
        public static void Unzip(string file, string output)
        {
            if (!Directory.Exists(output))
            {
                Directory.CreateDirectory(output);
            }
            System.IO.Compression.ZipFile.ExtractToDirectory(file, output);
        }
        public static bool Decrypt(string outputFolder, string HexKey)
        {
            XmlDocument manifest = new XmlDocument();
            manifest.Load(outputFolder + "\\manifest.xml");
            manifest.GetElementsByTagName("uuid");

            XmlNode? uuidXmlElement = manifest.GetElementsByTagName("uuid")[0];
            if (uuidXmlElement == null)
                return false;

            XmlNode? ivXmlElement = manifest.GetElementsByTagName("iv")[0];
            if (ivXmlElement == null)
                return false;

            string uuid = uuidXmlElement.InnerText;
            string iv = ivXmlElement.InnerText;
            byte[] uuidhex = StringToByteArray(uuid.Replace("-", ""));
            byte[] numArray = StringToByteArray(HexKey);

            byte[] key;
            try
            {
                key = exclusiveOR(uuidhex, numArray);
            }
            catch
            {
                return false;
            }

            Aes aes = Aes.Create();
            aes.BlockSize = 128;
            aes.Key = key;
            aes.IV = StringToByteArray(iv);
            ICryptoTransform decryptor = aes.CreateDecryptor();
            
            byte[] encFile = File.ReadAllBytes(outputFolder + "\\" + uuid + ".xml.gz.enc");

            try
            {
                using (MemoryStream ms = new MemoryStream(encFile))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        var decompressor = new System.IO.Compression.GZipStream(cs, System.IO.Compression.CompressionMode.Decompress);
                        FileStream outputFileStream = File.Create(outputFolder + "\\" + uuid + ".xml");
                        decompressor.CopyTo(outputFileStream);
                        outputFileStream.Close();
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }


        public static bool TryStringToByteArray(string hex)
        {
            try
            {
                StringToByteArray(hex);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static byte[] exclusiveOR(byte[] key, byte[] PAN)
        {
            if (key.Length == PAN.Length)
            {
                byte[] result = new byte[key.Length];
                for (int i = 0; i < key.Length; i++)
                {
                    result[i] = (byte)(key[i] ^ PAN[i]);
                }
                return result;
            }
            else
            {
                throw new ArgumentException();
            }
        }
    }
}
