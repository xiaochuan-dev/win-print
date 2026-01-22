using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WinPrint
{
    public class PDFBatchProcessor
    {
        private readonly PDFToPDFPrinter _printer;
        private readonly string _outputRootPath;

        public PDFBatchProcessor(string outputRootPath)
        {
            _printer = new PDFToPDFPrinter();
            _outputRootPath = outputRootPath;
            
            // 确保输出根目录存在
            if (!Directory.Exists(_outputRootPath))
            {
                Directory.CreateDirectory(_outputRootPath);
            }
        }

        /// <summary>
        /// 获取相对于源文件夹的相对路径
        /// </summary>
        private string GetRelativePath(string fullPath, string basePath)
        {
            Uri fullUri = new Uri(fullPath, UriKind.Absolute);
            Uri baseUri = new Uri(basePath + Path.DirectorySeparatorChar, UriKind.Absolute);
            
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString()
                .Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// 获取输出文件路径，保持相同的目录结构
        /// </summary>
        public string GetOutputPath(string sourcePdfPath, string sourceRootPath)
        {
            // 获取相对路径
            string relativePath = GetRelativePath(sourcePdfPath, sourceRootPath);
            
            // 组合输出路径
            string outputPath = Path.Combine(_outputRootPath, relativePath);
            
            // 获取目录名
            string outputDirectory = Path.GetDirectoryName(outputPath);
            
            // 如果目录不存在则创建
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            
            // 处理文件名：在文件名后加 _new
            string fileName = Path.GetFileNameWithoutExtension(relativePath);
            string extension = Path.GetExtension(relativePath);

            // 如果已经以 _new 结尾，避免重复添加
            if (fileName.EndsWith("_new"))
                return Path.Combine(outputDirectory, fileName + extension);

            return Path.Combine(outputDirectory, fileName + "_new" + extension);
        }

        /// <summary>
        /// 处理单个PDF文件
        /// </summary>
        public bool ProcessSinglePDF(string sourcePdfPath, string sourceRootPath)
        {
            string outputPath = GetOutputPath(sourcePdfPath, sourceRootPath);
            return ProcessSinglePDF(sourcePdfPath, outputPath, sourceRootPath);
        }

        /// <summary>
        /// 处理单个PDF文件，指定输出路径
        /// </summary>
        public bool ProcessSinglePDF(string sourcePdfPath, string outputPdfPath, string sourceRootPath)
        {
            if (!File.Exists(sourcePdfPath))
            {
                Console.WriteLine($"文件不存在: {sourcePdfPath}");
                return false;
            }

            try
            {
                string relativePath = GetRelativePath(sourcePdfPath, sourceRootPath);
                Console.WriteLine($"处理文件: {relativePath}");
                Console.WriteLine($"输出到: {GetRelativePath(outputPdfPath, _outputRootPath)}");

                Stopwatch sw = Stopwatch.StartNew();
                bool success = _printer.PrintPDFToPDF(sourcePdfPath, outputPdfPath);
                sw.Stop();

                if (success && File.Exists(outputPdfPath))
                {
                    FileInfo info = new FileInfo(outputPdfPath);
                    Console.WriteLine($"✓ 成功！耗时: {sw.ElapsedMilliseconds}ms, 大小: {info.Length:N0} 字节");
                    return true;
                }
                else
                {
                    Console.WriteLine($"✗ 失败！耗时: {sw.ElapsedMilliseconds}ms");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 批量处理多个PDF文件（顺序执行）
        /// </summary>
        public BatchResult ProcessBatch(IEnumerable<string> pdfPaths, string sourceRootPath)
        {
            var result = new BatchResult();
            var paths = pdfPaths.ToList();

            Console.WriteLine($"开始批量处理 {paths.Count} 个文件...");
            Console.WriteLine("=======================================");

            Stopwatch totalSw = Stopwatch.StartNew();

            foreach (var path in paths)
            {
                try
                {
                    if (ProcessSinglePDF(path, sourceRootPath))
                    {
                        result.SuccessCount++;
                        result.SuccessFiles.Add(path);
                    }
                    else
                    {
                        result.FailedCount++;
                        result.FailedFiles.Add(path);
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedFiles.Add(path);
                    Console.WriteLine($"✗ 处理失败 {path}: {ex.Message}");
                }
            }

            totalSw.Stop();

            Console.WriteLine("=======================================");
            Console.WriteLine($"批量处理完成！");
            Console.WriteLine($"总计: {paths.Count} 文件");
            Console.WriteLine($"成功: {result.SuccessCount} 文件");
            Console.WriteLine($"失败: {result.FailedCount} 文件");
            Console.WriteLine($"总耗时: {totalSw.ElapsedMilliseconds}ms");
            Console.WriteLine($"平均每个文件: {totalSw.ElapsedMilliseconds / Math.Max(1, paths.Count)}ms");

            return result;
        }

        /// <summary>
        /// 并行处理多个PDF文件
        /// </summary>
        public BatchResult ProcessBatchParallel(IEnumerable<string> pdfPaths, int maxDegreeOfParallelism, string sourceRootPath)
        {
            var result = new BatchResult();
            var paths = pdfPaths.ToList();
            var results = new ConcurrentBag<bool>();
            var successFiles = new ConcurrentBag<string>();
            var failedFiles = new ConcurrentBag<string>();

            Console.WriteLine($"开始并行批量处理 {paths.Count} 个文件...");
            Console.WriteLine($"并行度: {maxDegreeOfParallelism}");
            Console.WriteLine($"输出根目录: {_outputRootPath}");
            Console.WriteLine("=======================================");

            Stopwatch totalSw = Stopwatch.StartNew();

            Parallel.ForEach(paths,
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                (path, state, index) =>
                {
                    try
                    {
                        string outputPath = GetOutputPath(path, sourceRootPath);
                        string relativePath = GetRelativePath(path, sourceRootPath);
                        
                        Console.WriteLine($"[线程{Task.CurrentId}] 处理: {relativePath}");

                        // 确保输出目录存在
                        string outputDirectory = Path.GetDirectoryName(outputPath);
                        if (!Directory.Exists(outputDirectory))
                        {
                            Directory.CreateDirectory(outputDirectory);
                        }

                        Stopwatch sw = Stopwatch.StartNew();
                        bool success = _printer.PrintPDFToPDF(path, outputPath);
                        sw.Stop();

                        if (success && File.Exists(outputPath))
                        {
                            results.Add(true);
                            successFiles.Add(path);
                            Console.WriteLine($"[线程{Task.CurrentId}] ✓ 成功！耗时: {sw.ElapsedMilliseconds}ms");
                        }
                        else
                        {
                            results.Add(false);
                            failedFiles.Add(path);
                            Console.WriteLine($"[线程{Task.CurrentId}] ✗ 失败！耗时: {sw.ElapsedMilliseconds}ms");
                        }
                    }
                    catch (Exception ex)
                    {
                        results.Add(false);
                        failedFiles.Add(path);
                        Console.WriteLine($"[线程{Task.CurrentId}] ✗ 错误: {ex.Message}");
                    }
                });

            totalSw.Stop();

            // 统计结果
            result.SuccessCount = results.Count(r => r);
            result.FailedCount = results.Count(r => !r);
            result.SuccessFiles = successFiles.ToList();
            result.FailedFiles = failedFiles.ToList();

            Console.WriteLine("=======================================");
            Console.WriteLine($"并行批量处理完成！");
            Console.WriteLine($"总计: {paths.Count} 文件");
            Console.WriteLine($"成功: {result.SuccessCount} 文件");
            Console.WriteLine($"失败: {result.FailedCount} 文件");
            Console.WriteLine($"总耗时: {totalSw.ElapsedMilliseconds}ms");
            Console.WriteLine($"平均每个文件: {totalSw.ElapsedMilliseconds / Math.Max(1, paths.Count)}ms");
            Console.WriteLine($"输出文件夹: {_outputRootPath}");

            return result;
        }

        /// <summary>
        /// 批量处理结果
        /// </summary>
        public class BatchResult
        {
            public int SuccessCount { get; set; }
            public int FailedCount { get; set; }
            public List<string> SuccessFiles { get; set; } = new List<string>();
            public List<string> FailedFiles { get; set; } = new List<string>();
        }
    }
}