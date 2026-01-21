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

        public PDFBatchProcessor()
        {
            _printer = new PDFToPDFPrinter();
        }

        /// <summary>
        /// 处理单个PDF文件，自动生成输出路径
        /// </summary>
        /// <param name="sourcePdfPath">源PDF文件路径</param>
        /// <returns>是否成功</returns>
        public bool ProcessSinglePDF(string sourcePdfPath)
        {
            return ProcessSinglePDF(sourcePdfPath, GetOutputPath(sourcePdfPath));
        }

        /// <summary>
        /// 处理单个PDF文件，指定输出路径
        /// </summary>
        public bool ProcessSinglePDF(string sourcePdfPath, string outputPdfPath)
        {
            if (!File.Exists(sourcePdfPath))
            {
                Console.WriteLine($"文件不存在: {sourcePdfPath}");
                return false;
            }

            try
            {
                Console.WriteLine($"处理文件: {Path.GetFileName(sourcePdfPath)}");
                Console.WriteLine($"输出到: {Path.GetFileName(outputPdfPath)}");

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
        public BatchResult ProcessBatch(IEnumerable<string> pdfPaths)
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
                    if (ProcessSinglePDF(path))
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
        /// 并行处理多个PDF文件（.NET Framework 4.7 支持 Parallel）
        /// </summary>
        public BatchResult ProcessBatchParallel(IEnumerable<string> pdfPaths, int maxDegreeOfParallelism = 4)
        {
            var result = new BatchResult();
            var paths = pdfPaths.ToList();
            var results = new ConcurrentBag<bool>();
            var successFiles = new ConcurrentBag<string>();
            var failedFiles = new ConcurrentBag<string>();

            Console.WriteLine($"开始并行批量处理 {paths.Count} 个文件...");
            Console.WriteLine($"并行度: {maxDegreeOfParallelism}");
            Console.WriteLine("=======================================");

            Stopwatch totalSw = Stopwatch.StartNew();

            Parallel.ForEach(paths,
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                (path, state, index) =>
                {
                    try
                    {
                        string outputPath = GetOutputPath(path);
                        Console.WriteLine($"[线程{Task.CurrentId}] 处理: {Path.GetFileName(path)}");

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

            return result;
        }

        /// <summary>
        /// 自动生成输出路径：在文件名后加 _new
        /// 例如：a.pdf → a_new.pdf
        /// </summary>
        public static string GetOutputPath(string sourcePath)
        {
            string directory = Path.GetDirectoryName(sourcePath);
            string fileName = Path.GetFileNameWithoutExtension(sourcePath);
            string extension = Path.GetExtension(sourcePath);

            // 如果已经以 _new 结尾，避免重复添加
            if (fileName.EndsWith("_new"))
                return Path.Combine(directory, fileName + extension);

            return Path.Combine(directory, fileName + "_new" + extension);
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