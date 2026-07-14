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
        private Label lblStatus;
        private Label lblInfo;
        private Label lblTrial;

        public LicenseForm()
        {
            BuildUI();
            RefreshStatus();
            txtMachineCode.Text = LicenseManager.GetMachineCode();
        }

        private void BuildUI()
        {
            this.Text = "软件授权";
            this.Size = new System.Drawing.Size(500, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 状态信息
            lblStatus = new Label()
            {
                Left = 20,
                Top = 20,
                Width = 450,
                Height = 30,
                Font = new System.Drawing.Font("微软雅黑", 10, System.Drawing.FontStyle.Bold)
            };
            this.Controls.Add(lblStatus);

            // 体验期信息
            lblTrial = new Label()
            {
                Left = 20,
                Top = 55,
                Width = 450,
                Height = 20,
                ForeColor = System.Drawing.Color.Gray
            };
            this.Controls.Add(lblTrial);

            var lblMachine = new Label()
            {
                Text = "您的机器码（复制此码发给开发者）：",
                Left = 20,
                Top = 90,
                Width = 400
            };
            this.Controls.Add(lblMachine);

            txtMachineCode = new TextBox()
            {
                Left = 20,
                Top = 115,
                Width = 400,
                ReadOnly = true
            };
            this.Controls.Add(txtMachineCode);

            var lblKey = new Label()
            {
                Text = "请输入注册码：",
                Left = 20,
                Top = 150,
                Width = 400
            };
            this.Controls.Add(lblKey);

            txtLicenseKey = new TextBox()
            {
                Left = 20,
                Top = 175,
                Width = 400,
                PasswordChar = '*'
            };
            this.Controls.Add(txtLicenseKey);

            btnOK = new Button()
            {
                Text = "激活",
                Left = 20,
                Top = 210,
                Width = 100,
                Height = 30
            };
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);

            btnExit = new Button()
            {
                Text = "退出",
                Left = 140,
                Top = 210,
                Width = 100,
                Height = 30
            };
            btnExit.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnExit);

            lblInfo = new Label()
            {
                Text = "请向开发者提供机器码以获取注册码。",
                Left = 20,
                Top = 250,
                Width = 400,
                ForeColor = System.Drawing.Color.Gray
            };
            this.Controls.Add(lblInfo);
        }

        private void RefreshStatus()
        {
            var status = LicenseManager.GetStatus();
            if (status.IsActivated)
            {
                lblStatus.Text = status.StatusText;
                lblStatus.ForeColor = System.Drawing.Color.Green;
                if (status.IsPermanent)
                    lblTrial.Text = "✅ 已永久激活，感谢支持！";
                else if (status.ExpireDate.HasValue)
                    lblTrial.Text = $"⏳ 授权将在 {status.ExpireDate.Value.ToShortDateString()} 到期，请及时续期。";
                else
                    lblTrial.Text = "";
                btnOK.Enabled = false;
                txtLicenseKey.Enabled = false;
            }
            else
            {
                lblStatus.Text = status.StatusText;
                lblStatus.ForeColor = status.TrialDaysLeft > 3 ? System.Drawing.Color.Blue : System.Drawing.Color.Orange;
                lblTrial.Text = status.TrialDaysLeft > 0 ? $"体验期剩余 {status.TrialDaysLeft} 天" : "体验期已结束，请激活！";
                btnOK.Enabled = true;
                txtLicenseKey.Enabled = true;
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            string key = txtLicenseKey.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                MessageBox.Show("请输入注册码！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                LicenseManager.Activate(key);
                MessageBox.Show("激活成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"激活失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}