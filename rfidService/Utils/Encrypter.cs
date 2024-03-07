using System;

using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace com.nem.aurawheel.Utils
{
    class Encrypter
    {
        public static string EncryptSHA1Message(string message)
        {
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] pwdbytes = Encoding.Default.GetBytes(message);
            byte[] hash = sha.ComputeHash(pwdbytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public static string EncryptMD5Message(string message)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] pwdbytes = Encoding.Default.GetBytes(message);
            byte[] hash = md5.ComputeHash(pwdbytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public static string GetSHA1HashFromFile(string fileName)
        {
            FileStream file = new FileStream(fileName, FileMode.Open);
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] hash = sha1.ComputeHash(file);
            file.Close();
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public static string GetMD5HashFromFile(string fileName)
        {
            FileStream file = new FileStream(fileName, FileMode.Open);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(file);
            file.Close();
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

    }
}
