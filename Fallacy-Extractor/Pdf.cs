using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using Spire.Pdf;
using Spire.Pdf.Texts;
using Spire.Pdf.Annotations;
using Spire.Pdf.Graphics;

namespace Fallacy_Extractor
{
    public class Pdf
    {
        // Load PDF and return all text as a single string
        public string Load(string path)
        {
            PdfDocument pdf = new PdfDocument();
            pdf.LoadFromFile(path);

            StringBuilder fullText = new StringBuilder();

            for (int i = 0; i < pdf.Pages.Count; i++)
            {
                PdfPageBase page = pdf.Pages[i];

                PdfTextExtractor ext = new(page);
                PdfTextExtractOptions options = new PdfTextExtractOptions();
                string pageText = ext.ExtractText(options);

                fullText.AppendLine($"--- Page {i + 1} ---");
                fullText.AppendLine(pageText);
            }

            return fullText.ToString();
        }

        // Annotate PDF based on Root object
        public void AnnotatePdf(string pdfPath, Root root, string outputPath)
        {
            PdfDocument pdf = new PdfDocument();
            pdf.LoadFromFile(pdfPath);
            
            var annotFont = new PdfFont(PdfFontFamily.Helvetica, 10);

            foreach (var fallacy in root.Fallacies ?? Enumerable.Empty<Fallacy>())
            {
                string popupText = $"{fallacy.Type}\nConfidence: {Math.Round(fallacy.Confidence * 100, 1)}%\nTargets: {string.Join(", ", fallacy.TargetNodes)}\n\n{fallacy.Description}";

                foreach (var span in fallacy.TextSpans ?? Enumerable.Empty<TextSpan>())
                {
                    int pageIndex = Math.Max(0, span.Page - 1);
                    if (pageIndex >= pdf.Pages.Count) continue;
                    var page = pdf.Pages[pageIndex];

                    PdfTextExtractor ext = new(page);
                    PdfTextExtractOptions options = new PdfTextExtractOptions();
                    string pageText = ext.ExtractText(options) ?? "";
                    int start = Math.Max(0, span.Start);
                    int end = Math.Min(span.End, pageText.Length);
                    if (start >= pageText.Length) continue;

                    string target = pageText.Substring(start, Math.Max(1, end - start));

                    var finder = new PdfTextFinder(page) { Options = new PdfTextFindOptions() };
                    var fragments = finder.Find(target);

                    if (fragments != null && fragments.Count > 0)
                    {
                        PdfTextFragment frag = fragments.FirstOrDefault(f => f.Text == target) ?? fragments[0];

                        var rects = new List<RectangleF>();
                        if (frag.Positions != null && frag.Positions.Length > 0)
                        {
                            foreach (var p in frag.Positions)
                            {
                                SizeF size = annotFont.MeasureString(frag.Text);
                                rects.Add(new RectangleF(p.X, p.Y - size.Height, size.Width, size.Height));
                            }
                        }
                        if (rects.Count == 0)
                        {
                            rects.Add(new RectangleF(20, 20, 200, annotFont.Size + 4));
                        }

                        var firstRect = rects[0];

                        var highlight = new PdfTextMarkupAnnotation(
                            "Analysis",
                            $"Detected: {fallacy.Type}",
                            target,
                            new PointF(firstRect.X, firstRect.Y + firstRect.Height),
                            annotFont
                        );

                        highlight.TextMarkupAnnotationType = PdfTextMarkupAnnotationType.Highlight;

                        page.Annotations.Add(highlight);

                        var popupBounds = new RectangleF(firstRect.Right + 6f, Math.Max(4f, firstRect.Y - 2f), 260f, 120f);
                        var popup = new PdfPopupAnnotation(popupBounds, popupText);
                        page.Annotations.Add(popup);

                        var brush = new PdfSolidBrush(Color.FromArgb(90, Color.Yellow));
                        foreach (var r in rects)
                        {
                            var infl = RectangleF.Inflate(r, 1.0f, 2.0f);
                            page.Canvas.Save();
                            page.Canvas.DrawRectangle(brush, infl.X, infl.Y, infl.Width, infl.Height);
                            page.Canvas.Restore();
                        }
                    }
                }
            }

            pdf.SaveToFile(outputPath);
        }
        public static void test(string inputPdf, string outputPdf, Pdf pdfTool, Root root)
        {
            // Extract text (optional)
            string allText = pdfTool.Load(inputPdf);
            Console.WriteLine("--- PDF Text ---");
            Console.WriteLine(allText);

            // Annotate PDF
            pdfTool.AnnotatePdf(inputPdf, root, outputPdf);
            Console.WriteLine($"Annotated PDF saved to {outputPdf}");
        }
    }
    
}