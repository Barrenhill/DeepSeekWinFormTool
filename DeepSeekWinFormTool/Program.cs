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

            // 授权验证
            if (!LicenseManager.IsLicensed())
            {
                using (var licenseForm = new LicenseForm())
                {
                    if (licenseForm.ShowDialog() != DialogResult.OK)
                        return;
                }
            }

            Application.Run(new Form1());
        }
    }
}