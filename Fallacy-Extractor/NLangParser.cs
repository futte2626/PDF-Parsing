/// we're using AI to make a string -> YAML containing the premisis, conclusions etc
///
// yaml format:
// 
// version: "1.0"

// document:
//   id: "doc-uuid"
//   source: "input.pdf"
//   language: "en"
//   page_count: 10

// nodes:
//   - id: "p1"
//     role: "Premise"            # premise | conclusion
//     explicit: true             # false => implicit
//     text: >
//       this is text that is a string
//     confidence: 0.91
//     text_span:
//       page: 1
//       start: 134
//       end: 247
//     inferred_from: []          # non-empty only if implicit

//   - id: "ip1"
//     role: "Premise"
//     explicit: false
//     text: >
//       this is someting implied from some text
//     confidence: 0.63
//     text_span: null
//     inferred_from: ["p4", "p6"]

//   - id: "c1"
//     role: "Conclusion"
//     explicit: true
//     text: >
//       therefor this text must be true
//     confidence: 0.88
//     text_span:
//       page: 2
//       start: 412
//       end: 476
//     inferred_from: []

// edges:
//   - id: "e1"
//     from: "p4"
//     to: "c1"
//     relation: "Supports"       # supports | attacks
//     confidence: 0.82

//   - id: "e2"
//     from: "ip1"
//     to: "c1"
//     relation: "Supports"
//     confidence: 0.71

//   - id: "e3"
//     from: "c1"
//     to: "c3"
//     relation: "Supports"
//     confidence: 0.76

// fallacies: # should be empty, as it'll get appended by fallacy-detector
//   - id: "f1"
//     type: "ad_hominem"
//     target_nodes: ["p7"]
//     description: >
//       The argument attacks the intelligence of the believers rather than
//       addressing the claim.
//     confidence: 0.92
//     text_spans:
//       - page: 3
//         start: 201
//         end: 234

// meta:
//   warnings:
//     - "Possible circular support detected between c1 and c3"  #or other relational stuff that might be a fallacy
//   stats:
//     node_count: 14
//     edge_count: 17
//     implicit_premises: 4


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OllamaSharp.Models;
using SharpYaml.Serialization;

namespace Fallacy_Extractor
{
    public class NLangParser
    {
        private readonly IChatClient chatClient;
        private readonly List<ChatMessage> chatHistory;

        public NLangParser()
{
            chatClient = new OllamaApiClient(new Uri("http://localhost:11434/"), "gemma3:12b");
            chatHistory = [];
        }
        public async Task<Root> ParseToYAML(string input){
            // my prompt
            string prompt = "master prompt: tag denne tekst i \"\" og dets pointer og påstande om til dette yaml format vist under teksten, selvom de er lidt af et longshot. Jo mere jo bedre, så længe at det er sandt og verificerbart. Et implicit præmisse er et præmisse hvor explicit er false. undlad ingen felter. du skal skrive INTET ANDET end det pure yaml, eller jeg tager alle dine donkey kong bananaer væk. hvis der står et tal, så giv dem et tal. du skal ikke gentage skemaet perfekt men tilpas det til inputtet følgende dette:\n";

            //string prompt = "tag denne tekst i \"\" og dets pointer og påstande, og omdan dem til yaml format vist under teksten, de må gerne være lidt af et longshot. Jo mere jo bedre, så længe at det er sandt og verificerbart. undlad ingen felter. du skal skrive INTET ANDET end det pure yaml, eller jeg tager alle dine donkey kong bananaer væk. Hvis der står et tal, så giv dem et tal. stats skal ALTID være en under kategori af meta. og vis det er explicit er sandt skal inferred from være tom. du skal ikke gentage skemaet perfekt men tilpas det til inputtet følgende dette:";

            prompt += input;
            prompt += "example yaml for different example:  Version: \"1.0\" Document: ID: \"doc-uuid\" Source: \"input.pdf\" Language: \"en\" PageCount: 10 Nodes: - ID: \"p1\" Role: \"Premise\" Explicit: true Text: > this is text that is a string Confidence: 0.91 TextSpan: Page: 1 Start: 134 End: 247 InferredFrom: [] - ID: \"ip1\" Role: \"Premise\" Explicit: false Text: > this is something implied from some text Confidence: 0.63 TextSpan: null InferredFrom: [\"p4\", \"p6\"] - ID: \"c1\" Role: \"Conclusion\" Explicit: true Text: > therefore this text must be true Confidence: 0.88 TextSpan: Page: 2 Start: 412 End: 476 InferredFrom: [] Edges: - ID: \"e1\" From: \"p4\" To: \"c1\" Relation: \"Supports\" Confidence: 0.82 - ID: \"e2\" From: \"ip1\" To: \"c1\" Relation: \"Supports\" Confidence: 0.71 - ID: \"e3\" From: \"c1\" To: \"c3\" Relation: \"Supports\" Confidence: 0.76 Fallacies: - ID: \"f1\" Type: \"ad_hominem\" TargetNodes: [\"p7\"] Description: > The argument attacks the intelligence of the believers rather than addressing the claim. Confidence: 0.92 TextSpans: - Page: 3 Start: 201 End: 234  Meta: Warnings: - \"Possible circular support detected between c1 and c3\"   Stats: NodeCount: 3 EdgeCount: 3 ImplicitPremisesAmount: 2";
            prompt += "Invariants and rules: Meta contains both Warnings and stats. the only valid relaitions are: Supports | Attacks | Implies. NEVER TRANSLATE. the original text should remain prestine. If you don't have data, never guess, just leave the field null. the only valid Roles are: Premis | Conclusion. ImplicitPremisses are when explicit is set to false";
            string msg = await this.Prompt(prompt);

            if((int)msg[0] == 96){ //checks for ` which has the ascii value 96
                msg = DeCodeBlock(msg); 
            }
            //Console.WriteLine((int)msg[0]);


            var serializer = new Serializer();
            Root? root = null;
            try
            {
                root = serializer.Deserialize<Root>(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid YAML syntax: " + ex.Message);
            }

            if (root == null)
                throw new InvalidOperationException("Failed to deserialize YAML");

            return root;
        }

        public async Task<string> Prompt(string input)
        {
            chatHistory.Add(new ChatMessage(ChatRole.User, input));

            var response = "";
            var options = new ChatOptions()
            {
                Temperature = 0.3f
            };
            await foreach (ChatResponseUpdate item in chatClient.GetStreamingResponseAsync(chatHistory, options))
            {
                Console.Write(item.Text); // display as it streams
                response += item.Text;
            }

            chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
            chatHistory.Clear();
            Console.WriteLine();
            return response;
        }
        public static string DeCodeBlock(string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            if (s.Length < 10)
                throw new ArgumentException("String must be at least 10 characters long", nameof(s));
            int start = 7;// ```yaml
            return s.Substring(start, s.Length - start - 3);
        }
        public async Task<List<Fallacy>> FallacyDetect(Root root){
            var errs = YAMLer.Validate(root);

            string prompt = "you need to validate the following yaml for fallacies and logical errors in this exact format:";
            prompt += "- [{ID: \"f1\", type: \"ad_hominem\", target_nodes: [\"p7\"], description: \"The argument attacks the intelligence of the believers rather than addressing the claim.\", confidence: 0.92, text_spans: [{page: 3, start: 201, end: 234}]}]";
            prompt += "write as many fallacies as there are from the graph. DO NOT START THE FALLACIES WITH Fallacies: start with the simple -";
            prompt += "here's the validation errors for the input you should analyze:";
            if (errs.Count > 0)
            {
                prompt += "Validation errors:\n";
                foreach (var e in errs)
                    prompt += "- " + e + "\n";
                prompt += "\n";
            }

            prompt += "you do NOT follow the schema that the input YAML file is in. you're writing distinct YAML following the schema above.";
            var serializer = new SharpYaml.Serialization.Serializer();
            string rootYaml = serializer.Serialize(root);
            prompt += rootYaml;


            string str = await this.Prompt(prompt);
            if((int)str[0] == 96){ //checks for ` which has the ascii value 96
                str = NLangParser.DeCodeBlock(str); 
            }

            List<Fallacy> fallacies = new List<Fallacy>();
            try
            {
                fallacies = serializer.Deserialize<List<Fallacy>>(str);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse AI output as Fallacy list: " + ex.Message);
            }
            chatHistory.Clear();
            return fallacies;
        }



    }
    
}
