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

            foreach (var fallacy in root.Fallacies ?? Enumerable.Empty<Fallacy>())
            {
                string popupText =
                    $"{fallacy.Type}\nConfidence: {Math.Round(fallacy.Confidence * 100, 1)}%\n" +
                    $"Targets: {string.Join(", ", fallacy.TargetNodes)}\n\n{fallacy.Description}";

                // Highlight fallacy text spans
                foreach (var span in fallacy.TextSpans ?? Enumerable.Empty<TextSpan>())
                {
                    int pageIndex = Math.Max(0, span.Page - 1);
                    if (pageIndex >= pdf.Pages.Count) continue;
                    var page = pdf.Pages[pageIndex];

                    PdfTextExtractor ext = new(page);
                    PdfTextExtractOptions options = new PdfTextExtractOptions();
                    string pageText = ext.ExtractText(options);
                    int start = Math.Max(0, span.Start);
                    int end = Math.Min(span.End, pageText.Length);
                    if (start >= end) continue;

                    string target = pageText.Substring(start, end - start);

                    var finder = new PdfTextFinder(page);
                    var fragments = finder.Find(target);

                    foreach (var frag in fragments)
                    {
                        foreach(var bounds in frag.Bounds){
                            page.Canvas.DrawRectangle(
                            new PdfSolidBrush(Color.FromArgb(60, Color.Yellow)), // transparent yellow
                            bounds
                        );

                        // Popup annotation near the highlight
                        var popupBounds = new RectangleF(bounds.Right + 5f, bounds.Y, 200f, 80f);
                        page.Annotations.Add(new PdfPopupAnnotation(popupBounds, popupText));
                        }
                        
                    }
                }

                // Highlight referenced premises
                foreach (var nodeId in fallacy.TargetNodes)
                {
                    var node = root.Nodes.FirstOrDefault(n => n.ID == nodeId);
                    if (node?.TextSpan == null) continue;

                    int pageIndex = Math.Max(0, node.TextSpan.Page - 1);
                    if (pageIndex >= pdf.Pages.Count) continue;
                    var page = pdf.Pages[pageIndex];

                    PdfTextExtractor ext = new(page);
                    PdfTextExtractOptions options = new PdfTextExtractOptions();
                    string pageText = ext.ExtractText(options);
                    int start = Math.Max(0, node.TextSpan.Start);
                    int end = Math.Min(node.TextSpan.End, pageText.Length);
                    if (start >= end) continue;

                    string target = pageText.Substring(start, end - start);
                    var finder = new PdfTextFinder(page);
                    var fragments = finder.Find(target);

                    foreach (var frag in fragments)
                    {
                        var bounds = frag.Bounds[0];
                        page.Canvas.DrawRectangle(
                            new PdfSolidBrush(Color.FromArgb(30, Color.Cyan)), // transparent cyan for premises
                            bounds
                        );
                    }
                }
            }

            pdf.SaveToFile(outputPath);
        }
        public static void test(string inputPdf, string outputPdf, Pdf pdfTool, Root root)
        {
            // Annotate PDF
            pdfTool.AnnotatePdf(inputPdf, root, outputPdf);
            Console.WriteLine($"Annotated PDF saved to {outputPdf}");
        }
    }
    
}