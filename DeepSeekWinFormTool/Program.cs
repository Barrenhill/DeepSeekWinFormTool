using System;
using System.Windows.Forms;
using DeepSeekBatchTool.License;

namespace DeepSeekBatchTool
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 检查授权状态
            if (!LicenseManager.IsAuthorized())
            {
                using (var licenseForm = new LicenseForm())
                {
                    // 如果用户取消或关闭，退出程序
                    if (licenseForm.ShowDialog() != DialogResult.OK)
                        return;
                }
                // 再次检查（如果激活成功，则继续）
                if (!LicenseManager.IsAuthorized())
                    return;
            }

            Application.Run(new Form1());
        }
    }
}