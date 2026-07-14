using System;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;  // 用于注册表操作
using DeepSeekBatchTool.Utils;

namespace DeepSeekBatchTool.License
{
    public class LicenseInfo
    {
        public bool IsActivated { get; set; }
        public bool IsPermanent { get; set; }
        public DateTime? ExpireDate { get; set; }
        public int TrialDaysTotal => LicenseManager.TRIAL_DAYS; // 引用内部常量
        public DateTime TrialStart { get; set; }
        public int TrialDaysLeft => Math.Max(0, (TrialStart.AddDays(TrialDaysTotal) - DateTime.Now).Days);
        public bool IsTrialValid => TrialDaysLeft > 0 && !IsActivated;
        public bool IsLicenseValid => IsActivated && (IsPermanent || ExpireDate > DateTime.Now);
        public bool CanUse => IsLicenseValid || IsTrialValid;
        public string StatusText
        {
            get
            {
                if (IsActivated)
                {
                    if (IsPermanent) return "已激活（永久）";
                    else if (ExpireDate.HasValue) return $"已激活（至 {ExpireDate.Value.ToShortDateString()}）";
                }
                if (IsTrialValid) return $"体验期剩余 {TrialDaysLeft} 天";
                return "已过期，请激活";
            }
        }
    }

    public static class LicenseManager
    {
        public const int TRIAL_DAYS = 7;// 默认体验天数
        private static readonly string Salt = ConfigHelper.LicenseSalt;
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DeepSeekBatchTool");
        private static readonly string LicenseFile = Path.Combine(AppDataPath, "license.dat");
        private static readonly string TrialRegistryKey = @"HKEY_CURRENT_USER\Software\DeepSeekBatchTool";
        private static readonly string TrialRegistryValue = "TrialStartTicks";

        private static byte[] GetAesKey()
        {
            using (var sha = SHA256.Create())
                return sha.ComputeHash(Encoding.UTF8.GetBytes(Salt));
        }
        // 获取体验期起始时间（从注册表读取，若不存在则创建）
        private static DateTime GetTrialStart()
        {
            try
            {
                // 尝试从注册表读取
                object value = Registry.GetValue(TrialRegistryKey, TrialRegistryValue, null);
                if (value != null && long.TryParse(value.ToString(), out long ticks))
                {
                    var startTime = new DateTime(ticks);
                    // ★ 防时间回退检测：如果记录的时间比当前时间还晚，说明用户篡改了系统时间 ★
                    if (startTime > DateTime.Now)
                    {
                        // 可采取策略：视为作弊，立即结束体验期
                        // return DateTime.Now; // 或者直接返回一个过期时间
                        return DateTime.Now.AddDays(-TRIAL_DAYS - 1); // 使体验期立即失效
                    }
                    return startTime;
                }
            }
            catch { /* 忽略异常，继续执行 */ }

            // 首次运行：创建记录
            var now = DateTime.Now;
            try
            {
                Registry.SetValue(TrialRegistryKey, TrialRegistryValue, now.Ticks, RegistryValueKind.QWord);
            }
            catch { /* 如果注册表写入失败，回退到文件 */ }
            return now;
        }
        // AES加密（用于生成注册码）
        public static string Encrypt(string plainText)
        {
            byte[] key = GetAesKey();
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = new byte[16]; // 固定IV（实际可随机，但为保持简单固定）
                var encryptor = aes.CreateEncryptor();
                byte[] plain = Encoding.UTF8.GetBytes(plainText);
                byte[] cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
                return Convert.ToBase64String(cipher);
            }
        }

        // AES解密（用于验证注册码）
        public static string Decrypt(string cipherText)
        {
            try
            {
                byte[] key = GetAesKey();
                byte[] cipher = Convert.FromBase64String(cipherText);
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = new byte[16];
                    var decryptor = aes.CreateDecryptor();
                    byte[] plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                    return Encoding.UTF8.GetString(plain);
                }
            }
            catch { return null; }
        }

        // 生成机器码（不变）
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
                                return Convert.ToBase64String(hash).Replace("/", "").Replace("+", "").Replace("=", "").Substring(0, 16);
                            }
                        }
                    }
                }
            }
            catch
            {
                string fallback = Environment.MachineName + Environment.UserName + Salt;
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(fallback));
                    return Convert.ToBase64String(hash).Replace("/", "").Replace("+", "").Replace("=", "").Substring(0, 16);
                }
            }
            return "DEFAULTCODE";
        }

        // 生成注册码（授权端用）  expireDays: 0=永久, >0 表示天数
        public static string GenerateLicense(string machineCode, int expireDays)
        {
            long expireTicks;
            if (expireDays <= 0)
                expireTicks = long.MaxValue; // 永久
            else
                expireTicks = DateTime.Now.AddDays(expireDays).Ticks;
            string plain = $"{machineCode}|{expireTicks}";
            return Encrypt(plain);
        }

        // 验证注册码
        public static (bool valid, bool permanent, DateTime expireDate) ValidateLicense(string licenseKey)
        {
            string plain = Decrypt(licenseKey);
            if (string.IsNullOrEmpty(plain)) return (false, false, DateTime.MinValue);
            var parts = plain.Split('|');
            if (parts.Length != 2) return (false, false, DateTime.MinValue);
            string machine = parts[0];
            string ticksStr = parts[1];
            if (machine != GetMachineCode()) return (false, false, DateTime.MinValue);
            if (!long.TryParse(ticksStr, out long ticks)) return (false, false, DateTime.MinValue);
            if (ticks == long.MaxValue) return (true, true, DateTime.MaxValue); // 永久
            var expire = new DateTime(ticks);
            if (expire <= DateTime.Now) return (false, false, expire); // 已过期
            return (true, false, expire);
        }

        // 保存激活信息（加密存储）
        public static void Activate(string licenseKey)
        {
            var (valid, permanent, expireDate) = ValidateLicense(licenseKey);
            if (!valid) throw new Exception("注册码无效或已过期");

            Directory.CreateDirectory(AppDataPath);
            var info = new
            {
                Activated = true,
                Permanent = permanent,
                ExpireTicks = permanent ? long.MaxValue : expireDate.Ticks
            };
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(info);
            // 简单加密存储（使用DPAPI保护）
            byte[] data = Encoding.UTF8.GetBytes(json);
            data = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(LicenseFile, data);
        }

        // 加载激活信息
        private static dynamic LoadActivation()
        {
            if (!File.Exists(LicenseFile)) return null;
            try
            {
                byte[] data = File.ReadAllBytes(LicenseFile);
                data = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                string json = Encoding.UTF8.GetString(data);
                return Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            }
            catch { return null; }
        }

        // 获取当前完整授权状态（使用上述方法）
        public static LicenseInfo GetStatus()
        {
            var info = new LicenseInfo();
            info.TrialStart = GetTrialStart(); // 从注册表读取

            // 加载激活信息（与之前相同）
            var act = LoadActivation();
            if (act != null && act.Activated == true)
            {
                info.IsActivated = true;
                if (act.Permanent == true || act.ExpireTicks == long.MaxValue)
                {
                    info.IsPermanent = true;
                    info.ExpireDate = null;
                }
                else
                {
                    info.IsPermanent = false;
                    info.ExpireDate = new DateTime((long)act.ExpireTicks);
                }
            }
            else
            {
                info.IsActivated = false;
            }
            return info;
        }

        // 判断是否授权（供主程序启动时调用）
        public static bool IsAuthorized()
        {
            var status = GetStatus();
            return status.CanUse;
        }
    }
}