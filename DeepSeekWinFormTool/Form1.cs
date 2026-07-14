using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using DeepSeekBatchTool.Core;
using DeepSeekBatchTool.License;
using DeepSeekBatchTool.Utils;

namespace DeepSeekBatchTool
{
    public partial class Form1 : Form
    {
        private TextBox txtPrompt;
        private TextBox txtSystemPrompt;
        private Button btnStart;
        private RichTextBox rtbLog;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Label lblTokenCost;
        private Label lblFileHint;

        private ApiClient _apiClient;
        private CacheHelper _cache;
        private List<string> _batchInputLines = new List<string>();
        private string _currentInputFilePath = "";
        private bool _isFileMode = false;

        private int _processedCount = 0;
        private int _totalTokens = 0;
        private int _cacheHitCount = 0;

        public Form1()
        {
            _apiClient = new ApiClient(ConfigHelper.ApiKey, ConfigHelper.ModelName, ConfigHelper.MaxConcurrent);
            _cache = new CacheHelper(ConfigHelper.CacheFilePath);

            BuildUI();
            RegisterDragDrop();

            lblStatus.Text = $"📦 缓存已加载：{_cache.Count} 条记录";
            txtSystemPrompt.Text = ConfigHelper.DefaultSystemPrompt;
        }

        private void BuildUI()
        {
            var status = LicenseManager.GetStatus();
            this.Text = $"DeepSeek 批量处理大师 v3.0 - {status.StatusText}";
            this.Size = new System.Drawing.Size(780, 760);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // 文件拖拽提示
            lblFileHint = new Label()
            {
                Text = "📂 直接把 .txt 或 .xlsx 文件拖入下方框内（自动识别）",
                Left = 20,
                Top = 20,
                Width = 700,
                Height = 25,
                ForeColor = System.Drawing.Color.Blue,
                Font = new System.Drawing.Font("微软雅黑", 9, System.Drawing.FontStyle.Bold)
            };
            this.Controls.Add(lblFileHint);

            // 输入框
            var lblInput = new Label() { Text = "📝 内容预览/手动输入（一行一个任务）：", Left = 20, Top = 50, Width = 700 };
            this.Controls.Add(lblInput);

            txtPrompt = new TextBox()
            {
                Left = 20,
                Top = 75,
                Width = 730,
                Height = 120,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = "请把以下内容翻译成专业商务英语：\n[拖入文件后这里会自动预览前几行]"
            };
            this.Controls.Add(txtPrompt);

            // 系统指令
            var lblSystem = new Label() { Text = "⚙️ 系统指令（告诉AI如何执行）：", Left = 20, Top = 210, Width = 700 };
            this.Controls.Add(lblSystem);

            txtSystemPrompt = new TextBox()
            {
                Left = 20,
                Top = 235,
                Width = 730,
                Height = 60,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = ConfigHelper.DefaultSystemPrompt
            };
            this.Controls.Add(txtSystemPrompt);

            // 开始按钮
            btnStart = new Button()
            {
                Text = "🚀 开始批量处理（自动缓存）",
                Left = 20,
                Top = 315,
                Width = 280,
                Height = 45,
                BackColor = System.Drawing.Color.LightGreen,
                Font = new System.Drawing.Font("微软雅黑", 10, System.Drawing.FontStyle.Bold)
            };
            btnStart.Click += BtnStart_ClickAsync;
            this.Controls.Add(btnStart);

            // Token成本显示
            lblTokenCost = new Label()
            {
                Text = "💰 本次预估成本：等待计算...",
                Left = btnStart.Left + btnStart.Width + 20,
                Top = 325,
                Width = 400,
                Height = 30,
                Font = new System.Drawing.Font("微软雅黑", 10, System.Drawing.FontStyle.Bold)
            };
            this.Controls.Add(lblTokenCost);

            // 进度条
            progressBar = new ProgressBar()
            {
                Left = 20,
                Top = 380,
                Width = 730,
                Height = 25,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            this.Controls.Add(progressBar);

            // 状态标签
            lblStatus = new Label()
            {
                Text = "⏳ 等待开始...",
                Left = 20,
                Top = 415,
                Width = 730,
                Height = 25
            };
            this.Controls.Add(lblStatus);

            // 日志输出
            rtbLog = new RichTextBox()
            {
                Left = 20,
                Top = 450,
                Width = 730,
                Height = 250,
                ReadOnly = true,
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.LimeGreen,
                Font = new System.Drawing.Font("Consolas", 9)
            };
            this.Controls.Add(rtbLog);
        }

        private void RegisterDragDrop()
        {
            this.AllowDrop = true;
            this.DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; else e.Effect = DragDropEffects.None; };
            this.DragDrop += Form1_DragDrop;
            txtPrompt.AllowDrop = true;
            txtPrompt.DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; else e.Effect = DragDropEffects.None; };
            txtPrompt.DragDrop += Form1_DragDrop;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;
            string filePath = files[0];
            if (!System.IO.File.Exists(filePath)) return;

            try
            {
                _batchInputLines = FileProcessor.ReadFile(filePath);
                _currentInputFilePath = filePath;
                _isFileMode = true;

                // 预览前几行
                string preview = string.Join("\n", _batchInputLines.Count > 5 ? _batchInputLines.GetRange(0, 5) : _batchInputLines);
                txtPrompt.Text = $"【已加载：{System.IO.Path.GetFileName(filePath)}，共 {_batchInputLines.Count} 条】\n预览前5行：\n{preview}";
                lblFileHint.Text = $"✅ 已识别文件，共 {_batchInputLines.Count} 条数据待处理";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取文件失败：{ex.Message}");
            }
        }

        private async void BtnStart_ClickAsync(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            rtbLog.Clear();
            _processedCount = 0;
            _totalTokens = 0;
            _cacheHitCount = 0;
            progressBar.Value = 0;

            List<string> tasks;
            if (_isFileMode && _batchInputLines.Count > 0)
            {
                tasks = _batchInputLines;
                lblStatus.Text = $"📂 文件模式：{System.IO.Path.GetFileName(_currentInputFilePath)}，共 {tasks.Count} 条";
            }
            else
            {
                tasks = new List<string>(txtPrompt.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                _isFileMode = false;
                lblStatus.Text = $"📝 文本框模式，共 {tasks.Count} 条";
            }

            if (tasks.Count == 0)
            {
                MessageBox.Show("没有发现任何待处理的任务！");
                btnStart.Enabled = true;
                return;
            }

            string systemPrompt = txtSystemPrompt.Text.Trim();
            if (string.IsNullOrEmpty(systemPrompt))
                systemPrompt = ConfigHelper.DefaultSystemPrompt;

            progressBar.Maximum = tasks.Count;
            var results = new string[tasks.Count];
            var taskList = new List<Task>();

            for (int i = 0; i < tasks.Count; i++)
            {
                int index = i;
                string input = tasks[i];
                taskList.Add(Task.Run(async () =>
                {
                    // 先查缓存
                    string cached = _cache.Get(input);
                    if (cached != null)
                    {
                        Interlocked.Increment(ref _cacheHitCount);
                        results[index] = cached;
                    }
                    else
                    {
                        string result = await _apiClient.SendRequestAsync(input, systemPrompt);
                        results[index] = result;
                        // 非错误结果才缓存
                        if (!result.StartsWith("❌"))
                        {
                            _cache.Set(input, result);
                            // 粗略估算token（不计精确，仅用于显示）
                            Interlocked.Add(ref _totalTokens, input.Length / 2 + result.Length / 2);
                        }
                    }

                    int current = Interlocked.Increment(ref _processedCount);
                    progressBar.Invoke((MethodInvoker)delegate { progressBar.Value = current; });
                    if (current % 5 == 0)
                    {
                        UpdateUI($"⚡ 进度：{current}/{tasks.Count}，缓存命中 {_cacheHitCount} 次", true);
                    }
                }));
            }

            await Task.WhenAll(taskList);

            // 保存结果
            if (_isFileMode && !string.IsNullOrEmpty(_currentInputFilePath))
            {
                try
                {
                    FileProcessor.SaveResults(_currentInputFilePath, tasks, new List<string>(results));
                    MessageBox.Show($"✅ 结果已保存至原文件同目录。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存结果失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // 日志显示前几条
            rtbLog.Clear();
            for (int i = 0; i < Math.Min(5, results.Length); i++)
            {
                rtbLog.AppendText($"【样例 {i + 1}】\n{results[i]}\n\n");
            }
            if (results.Length > 5)
                rtbLog.AppendText($"... 共 {results.Length} 条。");

            // 显示成本估算（deepseek 约 2元/百万token）
            double cost = _totalTokens * 0.000002;
            lblStatus.Text = $"🎉 全部完成！总任务 {tasks.Count}，缓存命中 {_cacheHitCount} 次，预估成本 {cost:F4} 元";
            lblTokenCost.Text = $"💰 实际成本：{cost:F4} 元（建议报价 {cost * 30:F2} 元）";
            btnStart.Enabled = true;
        }

        private void UpdateUI(string message, bool appendToLog = false)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke((MethodInvoker)delegate { UpdateUI(message, appendToLog); });
                return;
            }
            if (appendToLog)
            {
                rtbLog.AppendText(message + "\n");
                rtbLog.ScrollToCaret();
            }
            lblStatus.Text = message;
        }
    }
}