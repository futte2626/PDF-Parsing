using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SharpYaml;
using SharpYaml.Serialization;

namespace Fallacy_Extractor
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.Write("hello world");
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: dotnet run <input.pdf> <output.pdf>");
                return;
            }
            string inputPdf = args[0];
            string outputPdf = args[1];
            var pdfTool = new Pdf();
            NLangParser n = new NLangParser();

            string allText = pdfTool.Load(inputPdf);
            Console.WriteLine("--- PDF Text ---");
            Console.WriteLine(allText);

            //Root yaml = await n.parseToYAML("what is love? if love exists, then point at it. love isn't physical, cuz the physical world is devoid of it.");
            var stringy =
    "Version: \"1.0\"\n" +
    "Document:\n" +
    "  ID: \"doc-uuid\"\n" +
    "  Source: \"input_text\"\n" +
    "  Language: \"da\"\n" +
    "  PageCount: 1\n" +
    "\n" +
    "Nodes:\n" +
    "  - ID: \"p1\"\n" +
    "    Role: \"Premise\"\n" +
    "    Explicit: true\n" +
    "    Text: >\n" +
    "      what is love?\n" +
    "    Confidence: 0.95\n" +
    "    TextSpan:\n" +
    "      Page: 1\n" +
    "      Start: 0\n" +
    "      End: 13\n" +
    "    InferredFrom: []\n" +
    "\n" +
    "  - ID: \"p2\"\n" +
    "    Role: \"Premise\"\n" +
    "    Explicit: true\n" +
    "    Text: >\n" +
    "      if love exists, then point at it.\n" +
    "    Confidence: 0.98\n" +
    "    TextSpan:\n" +
    "      Page: 1\n" +
    "      Start: 14\n" +
    "      End: 37\n" +
    "    InferredFrom: []\n" +
    "\n" +
    "  - ID: \"p3\"\n" +
    "    Role: \"Premise\"\n" +
    "    Explicit: true\n" +
    "    Text: >\n" +
    "      love isn't physical, cuz the physical world is devoid of it.\n" +
    "    Confidence: 0.92\n" +
    "    TextSpan:\n" +
    "      Page: 1\n" +
    "      Start: 38\n" +
    "      End: 78\n" +
    "    InferredFrom: []\n" +
    "\n" +
    "  - ID: \"ip1\"\n" +
    "    Role: \"Premise\"\n" +
    "    Explicit: false\n" +
    "    Text: >\n" +
    "      The existence of love is questioned.\n" +
    "    Confidence: 0.75\n" +
    "    TextSpan: null\n" +
    "    InferredFrom: [\"p1\", \"p2\"]\n" +
    "\n" +
    "  - ID: \"ip2\"\n" +
    "    Role: \"Premise\"\n" +
    "    Explicit: false\n" +
    "    Text: >\n" +
    "      The physical world is considered separate from love.\n" +
    "    Confidence: 0.80\n" +
    "    TextSpan: null\n" +
    "    InferredFrom: [\"p3\"]\n" +
    "\n" +
    "  - ID: \"c1\"\n" +
    "    Role: \"Conclusion\"\n" +
    "    Explicit: true\n" +
    "    Text: >\n" +
    "      love isn't physical.\n" +
    "    Confidence: 0.90\n" +
    "    TextSpan:\n" +
    "      Page: 1\n" +
    "      Start: 38\n" +
    "      End: 78\n" +
    "    InferredFrom: [\"p3\"]\n" +
    "\n" +
    "  - ID: \"ip3\"\n" +
    "    Role: \"Premise\"\n" +
    "    Explicit: false\n" +
    "    Text: >\n" +
    "      The inability to point at love suggests it is not physical.\n" +
    "    Confidence: 0.68\n" +
    "    TextSpan: null\n" +
    "    InferredFrom: [\"p2\"]\n" +
    "\n" +
    "Edges:\n" +
    "  - ID: \"e1\"\n" +
    "    From: \"p1\"\n" +
    "    To: \"ip1\"\n" +
    "    Relation: \"Supports\"\n" +
    "    Confidence: 0.85\n" +
    "\n" +
    "  - ID: \"e2\"\n" +
    "    From: \"p2\"\n" +
    "    To: \"ip3\"\n" +
    "    Relation: \"Supports\"\n" +
    "    Confidence: 0.78\n" +
    "\n" +
    "  - ID: \"e3\"\n" +
    "    From: \"p3\"\n" +
    "    To: \"c1\"\n" +
    "    Relation: \"Supports\"\n" +
    "    Confidence: 0.93\n" +
    "\n" +
    "  - ID: \"e4\"\n" +
    "    From: \"ip3\"\n" +
    "    To: \"c1\"\n" +
    "    Relation: \"Supports\"\n" +
    "    Confidence: 0.72\n" +
    "\n" +
    "Fallacies: []\n" +
    "\n" +
    "Meta:\n" +
    "  Warnings: []\n" +
    "  Stats:\n" +
    "    NodeCount: 7\n" +
    "    EdgeCount: 4\n" +
    "    ImplicitPremisesAmount: 3\n";

            if ((int)stringy[0] == 96)
            { //checks for ` which has the ascii value 96
                stringy = NLangParser.DeCodeBlock(stringy);
            }
            Console.Write(stringy);
            var serializer = new Serializer();
            Root? yaml = null;
            try
            {
                yaml = serializer.Deserialize<Root>(stringy);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid YAML syntax: " + ex.Message);
            }

            if (yaml == null)
                throw new InvalidOperationException("Failed to deserialize YAML");


            foreach (string s in YAMLer.Validate(yaml))
            {
                Console.WriteLine(s);
            }
            foreach (Fallacy f in await n.FallacyDetect(yaml))
            {
                Console.WriteLine(f.Description + f.ID + f.TargetNodes[0]);
            }
            Pdf.test(inputPdf, outputPdf, pdfTool, yaml);


        }

        
    }
}
