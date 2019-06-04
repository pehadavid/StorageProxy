using System;
using System.Security.Cryptography;

namespace StorageProxy
{
    internal static class ByteHelper
    {
        private static MD5 md5 = MD5.Create();
        public static string ByteArrayToString(this byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex.Replace("-", "");
        }

        public static byte[] StringToByteArray(this String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static string GetHash(this byte[] ba)
        {            
            var hash = md5.ComputeHash(ba);
            return hash.ByteArrayToString();
        }
    }
}