using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using DeepSeekBatchTool.Utils;

namespace DeepSeekBatchTool.Core
{
    public class CacheHelper
    {
        private readonly ConcurrentDictionary<string, string> _cache = new ConcurrentDictionary<string, string>();
        private readonly string _cacheFilePath;

        public int Count => _cache.Count;

        public CacheHelper(string cacheFilePath = null)
        {
            _cacheFilePath = cacheFilePath ?? ConfigHelper.CacheFilePath;
            Load();
        }

        private string GetMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hash);
            }
        }

        public string Get(string input)
        {
            string key = GetMD5(input);
            if (_cache.TryGetValue(key, out string value))
                return value;
            return null;
        }

        public void Set(string input, string output)
        {
            string key = GetMD5(input);
            _cache[key] = output;
            Save();
        }

        public void Load()
        {
            if (!File.Exists(_cacheFilePath)) return;
            try
            {
                string json = File.ReadAllText(_cacheFilePath);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    foreach (var kv in dict)
                        _cache[kv.Key] = kv.Value;
                }
            }
            catch { /* 忽略损坏文件 */ }
        }

        public void Save()
        {
            try
            {
                var dict = _cache.ToDictionary(kv => kv.Key, kv => kv.Value);
                string json = JsonConvert.SerializeObject(dict, Formatting.Indented);
                File.WriteAllText(_cacheFilePath, json);
            }
            catch { /* 忽略保存失败 */ }
        }

        public void Clear()
        {
            _cache.Clear();
            if (File.Exists(_cacheFilePath)) File.Delete(_cacheFilePath);
        }
    }
}