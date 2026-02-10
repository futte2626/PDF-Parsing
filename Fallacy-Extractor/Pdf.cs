// this is for handeling the pdf -> text and yaml -> pdf-comments
using System;

using System.IO;
using System.Text;
using Spire.Pdf;
using Spire.Pdf.Texts;

namespace Fallacy_Extractor;
public class Pdf {
    
    public static StringBuilder GetStringBuilderFromFile(string filename)
    {
        PdfDocument pdf = new PdfDocument();
        pdf.LoadFromFile(filename);
        StringBuilder extractedText = new StringBuilder();
        foreach (PdfPageBase page in pdf.Pages)
        {
            PdfTextExtractor extractor = new PdfTextExtractor(page);
            PdfTextExtractOptions option = new PdfTextExtractOptions {
                IsExtractAllText = true
            };
            string text = extractor.ExtractText(option);
            extractedText.AppendLine(text);
        }

        pdf.Close();
        return extractedText;
    }
}