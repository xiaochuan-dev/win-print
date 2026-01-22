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
            
            var dir = new DirectoryInfo(folderPath);
            var files = new List<string>();
            GetFilesRecursive(dir, files);
            
            return files;
        }

        private static void GetFilesRecursive(DirectoryInfo dir, List<string> fileList)
        {
            try
            {
                foreach (var file in dir.GetFiles("*.pdf"))
                {
                    fileList.Add(file.FullName);
                }
                
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
                if (args.Length < 1)
                {
                    Console.WriteLine("使用方法: WinPrint <文件夹路径> [批次大小] [输出文件夹路径]");
                    Console.WriteLine("示例: WinPrint \"C:\\path\\to\\folder\" 2 \"C:\\output\"");
                    Console.WriteLine("注意: 如果不指定输出文件夹，默认会在输入文件夹下创建 'pdf_output' 文件夹");
                    return;
                }

                string folderPath = args[0];
                int batchSize = 2;
                string outputRootPath = "";

                if (args.Length >= 2)
                {
                    if (!int.TryParse(args[1], out batchSize))
                    {
                        Console.WriteLine($"警告: 无法解析批次大小参数 '{args[1]}'，使用默认值 2");
                        batchSize = 2;
                    }
                }

                if (args.Length >= 3)
                {
                    outputRootPath = args[2];
                }
                else
                {
                    // 默认在输入文件夹下创建 pdf_output 文件夹
                    outputRootPath = Path.Combine(folderPath, "pdf_output");
                }

                Console.WriteLine($"正在处理文件夹: {folderPath}");
                Console.WriteLine($"批次大小: {batchSize}");
                Console.WriteLine($"输出文件夹: {outputRootPath}");

                var pdfPaths = GetAllPdfFiles(folderPath);
                var pdfList = pdfPaths.ToList();

                Console.WriteLine($"找到 {pdfList.Count} 个 PDF 文件");

                if (pdfList.Count == 0)
                {
                    Console.WriteLine("没有找到 PDF 文件，程序退出");
                    return;
                }

                var pdfBatchProcessor = new PDFBatchProcessor(outputRootPath);
                pdfBatchProcessor.ProcessBatchParallel(pdfList, batchSize, folderPath);

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