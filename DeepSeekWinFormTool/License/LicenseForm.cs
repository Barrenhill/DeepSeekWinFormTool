using System;
using System.Windows.Forms;

namespace DeepSeekBatchTool.License
{
    public partial class LicenseForm : Form
    {
        private TextBox txtMachineCode;
        private TextBox txtLicenseKey;
        private Button btnOK;
        private Button btnExit;
        private Label lblInfo;

        public LicenseForm()
        {
            BuildUI();
            txtMachineCode.Text = LicenseManager.GetMachineCode();
        }

        private void BuildUI()
        {
            this.Text = "软件授权";
            this.Size = new System.Drawing.Size(450, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblMachine = new Label()
            {
                Text = "您的机器码（复制此码发给开发者）：",
                Left = 20,
                Top = 20,
                Width = 400
            };
            this.Controls.Add(lblMachine);

            txtMachineCode = new TextBox()
            {
                Left = 20,
                Top = 45,
                Width = 400,
                ReadOnly = true
            };
            this.Controls.Add(txtMachineCode);

            var lblKey = new Label()
            {
                Text = "请输入注册码：",
                Left = 20,
                Top = 90,
                Width = 400
            };
            this.Controls.Add(lblKey);

            txtLicenseKey = new TextBox()
            {
                Left = 20,
                Top = 115,
                Width = 400,
                PasswordChar = '*'
            };
            this.Controls.Add(txtLicenseKey);

            btnOK = new Button()
            {
                Text = "激活",
                Left = 20,
                Top = 160,
                Width = 100,
                Height = 30
            };
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);

            btnExit = new Button()
            {
                Text = "退出",
                Left = 140,
                Top = 160,
                Width = 100,
                Height = 30
            };
            btnExit.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnExit);

            lblInfo = new Label()
            {
                Text = "请向开发者提供机器码以获取注册码。",
                Left = 20,
                Top = 200,
                Width = 400,
                ForeColor = System.Drawing.Color.Gray
            };
            this.Controls.Add(lblInfo);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            string key = txtLicenseKey.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("请输入注册码！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (LicenseManager.ValidateLicense(key))
            {
                LicenseManager.SaveLicense(key);
                MessageBox.Show("激活成功！感谢使用本软件。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("注册码无效，请检查后重试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}