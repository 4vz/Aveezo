using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public static class Decrypt
{
    public static byte[] Aes(byte[] data, byte[] key, byte[] iv)
    {
        if (data == null || data.Length <= 0) throw new ArgumentNullException(nameof(data));
        if (key == null || key.Length <= 0) throw new ArgumentNullException(nameof(key));
        if (iv == null || iv.Length <= 0) throw new ArgumentNullException(nameof(iv));

        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        byte[] re = null;

        using (var ms = new MemoryStream())
        {
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
            }

            re = ms.ToArray();
        }

        return re;
    }

    public static bool TryAes(byte[] data, byte[] key, byte[] iv, out byte[] decrypted)
    {
        decrypted = null;

        try
        {
            decrypted = Aes(data, key, iv);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}