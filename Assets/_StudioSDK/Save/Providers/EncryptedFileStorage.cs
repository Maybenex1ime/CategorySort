using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace LogosSDK.Save.Providers
{
    public class EncryptedFileStorage : JsonFileStorage
    {
        // Simple default key/IV for AES (In production, derive from DeviceID or a secure backend key)
        private readonly byte[] _key = Encoding.UTF8.GetBytes("AQPK_LglGame12345678901234567890"); // 32 bytes
        private readonly byte[] _iv = Encoding.UTF8.GetBytes("AQPK_IV123456789"); // 16 bytes

        public EncryptedFileStorage(SafeFileWriter writer) : base(writer)
        {
        }

        public override void Save<T>(string key, T data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                var encrypted = EncryptString(json);
                _writer.Write(GetFilePath(key), encrypted);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save {typeof(T).Name} to EncryptedFileStorage key {key}: {ex.Message}");
            }
        }

        public override T Load<T>(string key, T defaultValue)
        {
            var encrypted = _writer.Read(GetFilePath(key));
            if (!string.IsNullOrEmpty(encrypted))
            {
                try
                {
                    var json = DecryptString(encrypted);
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to decrypt/deserialize {typeof(T).Name} from EncryptedFileStorage key {key}: {ex.Message}");
                }
            }
            return defaultValue;
        }

        private string EncryptString(string plainText)
        {
            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using MemoryStream ms = new MemoryStream();
            using CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (StreamWriter sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
            // Return base64 encoded string
            return Convert.ToBase64String(ms.ToArray());
        }

        private string DecryptString(string cipherText)
        {
            var buffer = Convert.FromBase64String(cipherText);

            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using MemoryStream ms = new MemoryStream(buffer);
            using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
    }
}
