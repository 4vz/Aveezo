using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo;

public static class Encrypt
{
    public static byte[] Aes(byte[] data, byte[] key, byte[] iv)
    {
        if (data == null || data.Length <= 0) throw new ArgumentNullException(nameof(data));
        if (key == null || key.Length <= 0) throw new ArgumentNullException(nameof(key));
        if (iv == null || iv.Length <= 0) throw new ArgumentNullException(nameof(iv));

        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        byte[] encrypted = null;

        using (var ms = new MemoryStream())
        {
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write)) 
            { 
                cs.Write(data, 0, data.Length);
            }

            encrypted = ms.ToArray();
        }

        return encrypted;
    }

    public static byte[] Aes(string data, byte[] key, byte[] iv) => Aes(Encoding.UTF8.GetBytes(data), key, iv);
}
