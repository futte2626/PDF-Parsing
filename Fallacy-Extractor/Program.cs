using System;
using System.Threading.Tasks;
using SharpYaml;
using SharpYaml.Serialization;

namespace Fallacy_Extractor
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            NLangParser n = new NLangParser();
            //Root yaml = await n.parseToYAML("what is love? if love exists, then point at it. love isn't physical, cuz the physical world is devoid of it.");
            string stringy = """
            ```yaml
            Version: "1.0"
            Document:
            ID: "doc-uuid"
            Source: "input_text"
            Language: "da"
            PageCount: 1
            Nodes:
            - ID: "p1"
                Role: "Premise"
                Explicit: true
                Text: >
                what is love?
                Confidence: 0.95
                TextSpan:
                Page: 1
                Start: 0
                End: 13
                InferredFrom: []
            - ID: "p2"
                Role: "Premise"
                Explicit: true
                Text: >
                if love exists, then point at it.
                Confidence: 0.98
                TextSpan:
                Page: 1
                Start: 14
                End: 36
                InferredFrom: []
            - ID: "p3"
                Role: "Premise"
                Explicit: true
                Text: >
                love isn't physical, cuz the physical world is devoid of it.
                Confidence: 0.92
                TextSpan:
                Page: 1
                Start: 37
                End: 75
                InferredFrom: []
            - ID: "ip1"
                Role: "Premise"
                Explicit: false
                Text: >
                The existence of love is questioned.
                Confidence: 0.75
                TextSpan: null
                InferredFrom: ["p1", "p2"]
            - ID: "ip2"
                Role: "Premise"
                Explicit: false
                Text: >
                The physical world is defined as lacking love.
                Confidence: 0.80
                TextSpan: null
                InferredFrom: ["p3"]
            - ID: "c1"
                Role: "Conclusion"
                Explicit: true
                Text: >
                Therefore, love is not a physical phenomenon.
                Confidence: 0.85
                TextSpan:
                Page: 1
                Start: 76
                End: 116
                InferredFrom: ["p3"]
            - ID: "ip3"
                Role: "Premise"
                Explicit: false
                Text: >
                The inability to point at love implies it's not physical.
                Confidence: 0.68
                TextSpan: null
                InferredFrom: ["p2"]
            Edges:
            - ID: "e1"
                From: "p1"
                To: "ip1"
                Relation: "Supports"
                Confidence: 0.78
            - ID: "e2"
                From: "p2"
                To: "ip3"
                Relation: "Supports"
                Confidence: 0.83
            - ID: "e3"
                From: "p3"
                To: "c1"
                Relation: "Supports"
                Confidence: 0.90
            - ID: "e4"
                From: "ip1"
                To: "c1"
                Relation: "Supports"
                Confidence: 0.65
            - ID: "e5"
                From: "ip3"
                To: "c1"
                Relation: "Supports"
                Confidence: 0.72
            Fallacies: []
            Meta:
            Warnings: []
            Stats:
                NodeCount: 6
                EdgeCount: 5
                ImplicitPremisesAmount: 3
            ```
            """;
            if((int)stringy[0] == 96){ //checks for ` which has the ascii value 96
                stringy = NLangParser.DeCodeBlock(stringy); 
            }
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
            

            foreach(string s in YAMLer.Validate(yaml)){
                Console.WriteLine(s);
            }
            // foreach(Fallacy f in await n.FallacyDetect(yaml)){
            //     Console.WriteLine(f.Description + f.ID + f.TargetNodes);
            // }
            
            
        }
    }
}
