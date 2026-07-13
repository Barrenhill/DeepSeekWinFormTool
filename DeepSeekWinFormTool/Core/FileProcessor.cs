using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OfficeOpenXml;

namespace DeepSeekBatchTool.Core
{
    public static class FileProcessor
    {
        static FileProcessor()
        {
            // EPPlus 非商业用途声明
            ExcelPackage.License.SetNonCommercialPersonal("DeepSeekWinFormTool");
        }

        public static List<string> ReadFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            var lines = new List<string>();

            if (ext == ".xlsx" || ext == ".xls")
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var ws = package.Workbook.Worksheets[0];
                if (ws == null) return lines;

                int row = 1;
                while (row <= ws.Dimension.Rows)
                {
                    string val = ws.Cells[row, 1].Text?.Trim();
                    if (!string.IsNullOrEmpty(val))
                        lines.Add(val);
                    row++;
                }
            }
            else if (ext == ".txt")
            {
                lines = File.ReadAllLines(filePath, Encoding.UTF8)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();
            }
            else
            {
                throw new NotSupportedException("仅支持 .xlsx 和 .txt 文件。");
            }
            return lines;
        }

        public static void SaveResults(string inputPath, List<string> inputs, List<string> results)
        {
            string dir = Path.GetDirectoryName(inputPath);
            string name = Path.GetFileNameWithoutExtension(inputPath);
            string ext = Path.GetExtension(inputPath);
            string outputPath;

            if (ext == ".xlsx" || ext == ".xls")
            {
                outputPath = Path.Combine(dir, name + "_AI处理结果.xlsx");
                // 覆盖旧文件
                if (File.Exists(outputPath)) File.Delete(outputPath);

                using var package = new ExcelPackage(new FileInfo(outputPath));
                var ws = package.Workbook.Worksheets.Add("处理结果");
                ws.Cells[1, 1].Value = "原始输入";
                ws.Cells[1, 2].Value = "AI输出";
                for (int i = 0; i < results.Count; i++)
                {
                    ws.Cells[i + 2, 1].Value = inputs[i] ?? "";
                    ws.Cells[i + 2, 2].Value = results[i] ?? "";
                }
                ws.Cells.AutoFitColumns();
                package.Save();
            }
            else
            {
                outputPath = Path.Combine(dir, name + "_AI结果.txt");
                File.WriteAllLines(outputPath, results, Encoding.UTF8);
            }
        }
    }
}