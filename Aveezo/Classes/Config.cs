using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Aveezo;

public class Config : IDictionary<string, string>
{
    #region Fields

    private static readonly byte[] header = { (byte)'A', (byte)'F', (byte)'I', (byte)'S', (byte)'C', (byte)'O', (byte)'F', (byte)'G' };

    public string Path { get; }

    private readonly byte[] iv = new byte[16];

    private readonly byte[] key = new byte[32] {
            0xFF, 0x23, 0x23, 0x12, 0x52, 0x12, 0xAD, 0xCC,
            0xFF, 0x23, 0x23, 0x12, 0x52, 0x12, 0xAD, 0xCC,
            0xFF, 0x23, 0x23, 0x12, 0x52, 0x12, 0xAD, 0xCC,
            0xFF, 0x23, 0x23, 0x12, 0x52, 0x12, 0xAD, 0xCC
        };

    private Dictionary<string, string> configs;

    public ICollection<string> Keys => configs.Keys;

    public ICollection<string> Values => configs.Values;

    public int Count => configs.Count;

    public bool IsReadOnly => false;

    public string this[string key]
    {
        get => configs[key];
        set
        {
            if (configs.ContainsKey(key))
            {
                if (configs[key] != value)
                {
                    configs[key] = value;
                    Save();
                }
            }
            else
            {
                Add(key, value);
            }
        }
    }

    #endregion

    #region Constructors

    private Config(string path)
    {
        try
        {
            Path = path;

            var fileInfo = new FileInfo(Path);
            var encrypted = false;

            byte[] configBytes = null;

            if (fileInfo.Exists)
            {
                var data = File.ReadAllBytes(Path);

                if (data.Length > 0)
                {
                    if (data.Length > 24)
                    {
                        if (data.StartsWith(header))
                        {
                            var encryptedData = new byte[data.Length - 24];

                            Buffer.BlockCopy(data, 8, iv, 0, 16);
                            Buffer.BlockCopy(data, 24, encryptedData, 0, data.Length - 24);

                            // decrypt
                            using var aes = Aes.Create();
                            aes.Key = key;
                            aes.IV = iv;

                            using (var ms = new MemoryStream())
                            {
                                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                                {
                                    cs.Write(encryptedData, 0, encryptedData.Length);
                                }

                                configBytes = ms.ToArray();
                            }
                            
                            encrypted = true;
                        }
                        else
                            configBytes = data;
                    }
                    else
                        configBytes = data;
                }
                else
                    configBytes = new byte[] { };
            }
            else
            {
                configBytes = new byte[] { };
            }

            configs = new Dictionary<string, string>();

            var lines = Encoding.UTF8.GetString(configBytes).Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                var pair = line.Split(new[] { ':' }, 2);

                if (pair.Length == 2)
                {
                    var pkey = pair[0];

                    if (!configs.ContainsKey(pkey))
                        configs.Add(pkey, pair[1]);
                }
            }

            if (!encrypted)
            {
                Rnd.Bytes(iv);
                Save();
            }
        }
        catch (Exception exception)
        {
            throw exception;
        }
    }

    #endregion

    #region Operators

    public static implicit operator bool(Config config) => config != null;

    #endregion

    #region Methods

    private void Save()
    {
        var sb = new StringBuilder();

        foreach (var (key, value) in configs)
        {
            if (sb.Length > 0)
                sb.Append("\r\n");

            sb.Append($"{key}:{value}");
        }

        var configBytes = Encoding.UTF8.GetBytes(sb.ToString());

        // encrypt
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using (var fs = new FileStream(Path, FileMode.Create))
        {
            fs.Write(header);
            fs.Write(iv);

            using (var cs = new CryptoStream(fs, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(configBytes, 0, configBytes.Length);
            }
        }
    }

    public void Add(string key, string value)
    {
        if (!ContainsKey(key))
        {
            configs.Add(key, value);
            Save();
        }
    }

    public bool ContainsKey(string key) => configs.ContainsKey(key);

    public bool Remove(string key)
    {
        if (ContainsKey(key))
        {
            configs.Remove(key);
            Save();
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) => configs.TryGetValue(key, out value);

    public void Add(KeyValuePair<string, string> item)
    {
        Add(item.Key, item.Value);
    }

    public void Clear()
    {
        configs.Clear();
        Save();
    }

    public bool Contains(KeyValuePair<string, string> item)
    {
        if (ContainsKey(item.Key))
        {
            if (configs[item.Key] == item.Value)
                return true;
            else
                return false;
        }
        else
            return false;
    }

    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotImplementedException();

    public bool Remove(KeyValuePair<string, string> item)
    {
        if (Contains(item))
        {
            return Remove(item.Key);
        }
        else
            return false;
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => configs.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => configs.GetEnumerator();

    #endregion

    #region Statics

    public static Config Load(string directory, string file) => Load(System.IO.Path.Combine(directory, file));

    public static Config Load(string path)
    {
        Config config = null;


        try
        {
            config = new Config(path);
        }
        catch
        {
        }

        return config;
    }

    #endregion
}
