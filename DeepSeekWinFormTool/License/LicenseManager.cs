using System;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using DeepSeekBatchTool.Utils;

namespace DeepSeekBatchTool.License
{
    public static class LicenseManager
    {
        private static readonly string Salt = ConfigHelper.LicenseSalt;

        public static string GetMachineCode()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive"))
                {
                    foreach (ManagementObject drive in searcher.Get())
                    {
                        string serial = drive["SerialNumber"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(serial))
                        {
                            using (var md5 = MD5.Create())
                            {
                                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(serial + Salt));
                                string code = Convert.ToBase64String(hash)
                                    .Replace("/", "").Replace("+", "").Replace("=", "")
                                    .Substring(0, 16);
                                return code;
                            }
                        }
                    }
                }
            }
            catch
            {
                // 回退方案：使用计算机名+用户名
                string fallback = Environment.MachineName + Environment.UserName + Salt;
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(fallback));
                    string code = Convert.ToBase64String(hash)
                        .Replace("/", "").Replace("+", "").Replace("=", "")
                        .Substring(0, 16);
                    return code;
                }
            }
            return "DEFAULTCODE";
        }

        public static string GenerateLicenseKey(string machineCode)
        {
            // 简单算法：将机器码与盐值组合后加密（实际可用AES，这里演示）
            string combined = machineCode + Salt;
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
                string key = Convert.ToBase64String(hash)
                    .Replace("/", "").Replace("+", "").Replace("=", "")
                    .Substring(0, 20);
                return key;
            }
        }

        public static bool ValidateLicense(string inputKey)
        {
            string machine = GetMachineCode();
            string expected = GenerateLicenseKey(machine);
            return string.Equals(inputKey, expected, StringComparison.OrdinalIgnoreCase);
        }

        public static void SaveLicense(string key)
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DeepSeekBatchTool");
            Directory.CreateDirectory(appData);
            string path = Path.Combine(appData, "license.dat");
            // 简单加密保存（防止明文查看）
            byte[] data = Encoding.UTF8.GetBytes(key);
            data = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(path, data);
        }

        public static string LoadLicense()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DeepSeekBatchTool");
            string path = Path.Combine(appData, "license.dat");
            if (!File.Exists(path)) return null;
            try
            {
                byte[] data = File.ReadAllBytes(path);
                data = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                return null;
            }
        }

        public static bool IsLicensed()
        {
            string saved = LoadLicense();
            if (string.IsNullOrEmpty(saved)) return false;
            return ValidateLicense(saved);
        }
    }
}