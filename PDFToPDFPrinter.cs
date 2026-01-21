using System;
using System.Drawing.Printing;
using System.IO;
using PdfiumViewer;

namespace WinPrint
{
    public class PDFToPDFPrinter
    {
        /// <summary>
        /// 将PDF文件直接打印到新的PDF文件（静默）
        /// </summary>
        /// <param name="sourcePdfPath">源PDF文件路径</param>
        /// <param name="outputPdfPath">输出的PDF文件路径</param>
        /// <returns>是否成功</returns>
        public bool PrintPDFToPDF(string sourcePdfPath, string outputPdfPath)
        {
            try
            {
                Console.WriteLine($"开始转换: {sourcePdfPath} -> {outputPdfPath}");

                string pdfPrinterName = GetPDFPrinterName();
                if (string.IsNullOrEmpty(pdfPrinterName))
                {
                    Console.WriteLine("错误: 未找到Microsoft Print to PDF打印机");
                    Console.WriteLine("请确保Windows已安装此打印机功能");
                    return false;
                }
                Console.WriteLine($"找到PDF打印机: {pdfPrinterName}");

                if (!File.Exists(sourcePdfPath))
                {
                    Console.WriteLine($"错误: 源文件不存在 - {sourcePdfPath}");
                    return false;
                }

                using (var pdfDocument = PdfDocument.Load(sourcePdfPath))
                {
                    Console.WriteLine($"PDF加载成功，共{pdfDocument.PageCount}页");

                    using (var printDocument = pdfDocument.CreatePrintDocument())
                    {
                        printDocument.PrintController = new StandardPrintController();

                        printDocument.DocumentName = Path.GetFileName(sourcePdfPath);
                        printDocument.PrinterSettings.PrinterName = pdfPrinterName;
                        printDocument.PrinterSettings.PrintToFile = true;
                        printDocument.PrinterSettings.PrintFileName = outputPdfPath;
                        printDocument.PrinterSettings.Copies = 1;

                        // 设置打印范围
                        printDocument.PrinterSettings.FromPage = 1;
                        printDocument.PrinterSettings.ToPage = pdfDocument.PageCount;

                        // SetPaperSizeToA4(printDocument);

                        // // 4. 设置颜色模式（彩色）
                        // printDocument.DefaultPageSettings.Color = true; // true = 彩色，false = 黑白

                        // // 5. 设置缩放（适合纸张大小）
                        // printDocument.OriginAtMargins = false; // 重要：确保从纸张边缘开始

                        // // 6. 设置打印质量
                        // printDocument.DefaultPageSettings.PrinterResolution = new PrinterResolution
                        // {
                        //     Kind = PrinterResolutionKind.High // 高质量
                        // };

                        // // 7. 设置方向（自动适应）
                        // printDocument.DefaultPageSettings.Landscape = false; // 默认纵向

                        // // 8. 添加打印事件处理以控制缩放
                        // printDocument.PrintPage += (sender, e) =>
                        // {
                        //     // 如果需要在代码层面控制缩放，可以在这里实现
                        //     // 但PdfiumViewer通常会自动处理缩放

                        //     e.HasMorePages = false;
                        // };

                        // // 9. 额外的缩放设置（通过打印机设置）
                        // SetScalingOptions(printDocument);

                        // 4. 静默打印（不显示对话框）
                        Console.WriteLine("开始静默打印...");
                        printDocument.Print();

                        Console.WriteLine($"PDF文件已生成: {outputPdfPath}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转换失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取系统PDF打印机名称
        /// </summary>
        private string GetPDFPrinterName()
        {
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                if (printer.Contains("Microsoft Print to PDF"))
                {
                    return printer;
                }
            }
            return null;
        }
        /// <summary>
        /// 设置纸张为A4（统一版本）
        /// </summary>
        private void SetPaperSizeToA4(PrintDocument printDocument)
        {
            SetPaperSizeToA4(printDocument.DefaultPageSettings);
        }

        /// <summary>
        /// 设置纸张为A4（重载版本，直接设置PageSettings）
        /// </summary>
        private void SetPaperSizeToA4(PageSettings pageSettings)
        {
            bool foundA4 = false;

            // 查找打印机支持的A4纸张
            foreach (PaperSize paperSize in pageSettings.PrinterSettings.PaperSizes)
            {
                if (IsA4Paper(paperSize))
                {
                    pageSettings.PaperSize = paperSize;
                    foundA4 = true;
                    Console.WriteLine($"找到并设置A4纸张: {paperSize.PaperName} ({paperSize.Width}x{paperSize.Height})");
                    break;
                }
            }

            // 如果没找到，创建A4尺寸
            if (!foundA4)
            {
                // A4尺寸：210mm × 297mm = 827 × 1169 1/100英寸
                PaperSize a4Paper = new PaperSize("A4", 827, 1169);
                pageSettings.PaperSize = a4Paper;
                Console.WriteLine($"创建自定义A4尺寸: {a4Paper.Width}x{a4Paper.Height}");
            }
        }

        /// <summary>
        /// 判断是否为A4纸张
        /// </summary>
        private bool IsA4Paper(PaperSize paperSize)
        {
            // 方法1：检查PaperKind
            if (paperSize.Kind == PaperKind.A4)
                return true;

            // 方法2：检查名称
            string name = paperSize.PaperName.ToLower();
            if (name.Contains("a4") || name.Contains("a4纸"))
                return true;

            // 方法3：检查尺寸（A4: 827×1169，允许±5的误差）
            const int a4Width = 827;
            const int a4Height = 1169;
            const int tolerance = 5;

            bool isWidthMatch = Math.Abs(paperSize.Width - a4Width) <= tolerance;
            bool isHeightMatch = Math.Abs(paperSize.Height - a4Height) <= tolerance;

            return isWidthMatch && isHeightMatch;
        }
        private void SetScalingOptions(PrintDocument printDocument)
        {
            try
            {
                // 方法1：通过PrinterSettings设置（如果支持）
                // 注意：不是所有打印机都支持这些设置

                // 设置缩放比例100%（无缩放）
                printDocument.DefaultPageSettings.PrinterSettings.DefaultPageSettings.PrinterResolution.X = 600;
                printDocument.DefaultPageSettings.PrinterSettings.DefaultPageSettings.PrinterResolution.Y = 600;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"缩放设置失败: {ex.Message}");
            }
        }
    }
}