using SshNet.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Aveezo
{
    public static class Hash
    {
        private static System.Security.Cryptography.MD5 md5 = null;

        private static System.Security.Cryptography.SHA256 sha256 = null;

        private static System.Security.Cryptography.SHA512 sha512 = null;

        public static string MD5(string input)
        {
            if (input == null)
                input = string.Empty;

            return MD5(Encoding.UTF8.GetBytes(input));
        }

        public static string MD5(byte[] input)
        {
            if (input == null)
                return null;

            StringBuilder sb = new StringBuilder();

            if (md5 == null)
                md5 = new MD5CryptoServiceProvider();

            byte[] result = md5.ComputeHash(input);

            foreach (byte b in result)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }

        public static byte[] SHA256(byte[] input)
        {
            if (sha256 == null)
                sha256 = System.Security.Cryptography.SHA256.Create();

            return sha256.ComputeHash(input);
        }

        public static byte[] SHA256(string input)
        {
            return SHA256(Encoding.UTF8.GetBytes(input));
        }

        public static byte[] SHA512(byte[] input)
        {
            if (sha512 == null)
                sha512 = System.Security.Cryptography.SHA512.Create();

            return sha512.ComputeHash(input);
        }

        public static byte[] SHA512(string input)
        {
            return SHA512(Encoding.UTF8.GetBytes(input));
        }
    }
}
