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

        private static SHA256Managed sha256Managed = null;

        private static SHA512Managed sha512Managed = null;

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
            if (sha256Managed == null)
                sha256Managed = new SHA256Managed();

            return sha256Managed.ComputeHash(input);
        }

        public static byte[] SHA256(string input)
        {
            return SHA256(Encoding.UTF8.GetBytes(input));
        }

        public static byte[] SHA512(byte[] input)
        {
            if (sha512Managed == null)
                sha512Managed = new SHA512Managed();

            return sha512Managed.ComputeHash(input);
        }

        public static byte[] SHA512(string input)
        {
            return SHA512(Encoding.UTF8.GetBytes(input));
        }
    }
}
