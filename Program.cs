using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WinPrint
{
    class Program
    {
        public static IEnumerable<string> GetAllPdfFiles(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"目录不存在: {folderPath}");
            }
            
            // 使用 DirectoryInfo 直接获取文件，避免编码问题
            var dir = new DirectoryInfo(folderPath);
            
            // 递归获取所有文件
            var files = new List<string>();
            GetFilesRecursive(dir, files);
            
            return files;
        }

        private static void GetFilesRecursive(DirectoryInfo dir, List<string> fileList)
        {
            try
            {
                // 获取当前目录的PDF文件
                foreach (var file in dir.GetFiles("*.pdf"))
                {
                    // 使用 FileInfo.FullName，它已经是正确的编码
                    fileList.Add(file.FullName);
                }
                
                // 递归子目录
                foreach (var subDir in dir.GetDirectories())
                {
                    GetFilesRecursive(subDir, fileList);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"访问目录 {dir.FullName} 时出错: {ex.Message}");
            }
        }

        static void Main(string[] args)
        {
            try
            {
                // 检查参数
                if (args.Length < 1)
                {
                    Console.WriteLine("使用方法: WinPrint <文件夹路径> [批次大小]");
                    Console.WriteLine("示例: WinPrint \"C:\\path\\to\\folder\" 2");
                    return;
                }

                string folderPath = args[0];
                int batchSize = 2; // 默认值

                if (args.Length >= 2)
                {
                    if (!int.TryParse(args[1], out batchSize))
                    {
                        Console.WriteLine($"警告: 无法解析批次大小参数 '{args[1]}'，使用默认值 2");
                        batchSize = 2;
                    }
                }

                Console.WriteLine($"正在处理文件夹: {folderPath}");
                Console.WriteLine($"批次大小: {batchSize}");

                var pdfPaths = GetAllPdfFiles(folderPath);
                var pdfList = pdfPaths.ToList(); // 转换为列表以便计数

                Console.WriteLine($"找到 {pdfList.Count} 个 PDF 文件");

                if (pdfList.Count == 0)
                {
                    Console.WriteLine("没有找到 PDF 文件，程序退出");
                    return;
                }

                var pdfBatchProcessor = new PDFBatchProcessor();
                pdfBatchProcessor.ProcessBatchParallel(pdfList, batchSize);

                Console.WriteLine("处理完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }
    }
}