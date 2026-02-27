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
        public async Task<Root> parseToYAML(string input){
            
            // Our prompt
            string prompt = """tag denne tekst i "" og dets pointer og påstande om til dette yaml format vist under teksten, selvom de er lidt af et longshot. Jo mere jo bedre, så længe at det er sandt og verificerbart. undlad ingen felter. du skal skrive INTET ANDET end det pure yaml, eller jeg tager alle dine donkey kong bananaer væk. Hvis der står et tal, så giv dem et tal. stats skal ALTID være en under kategori af meta og meta må ALDRIG være under nodes og nodes er ALDRIG en under kategori af ducument.  Ved premises og fallies skal du ALTID have et ID med til det. Hvis det er explicit er sandt skal inferred from være tom. Du skal med meddrage implicitpremisesamount. Du skal ikke gentage skemaet perfekt, og det skal angives som et yaml skema og tilpas det til inputtet følgende dette:""";

            //string prompt = "tag denne tekst i \"\" og dets pointer og påstande, og omdan dem til yaml format vist under teksten, de må gerne være lidt af et longshot. Jo mere jo bedre, så længe at det er sandt og verificerbart. undlad ingen felter. du skal skrive INTET ANDET end det pure yaml, eller jeg tager alle dine donkey kong bananaer væk. Hvis der står et tal, så giv dem et tal. stats skal ALTID være en under kategori af meta. og vis det er explicit er sandt skal inferred from være tom. du skal ikke gentage skemaet perfekt men tilpas det til inputtet følgende dette:";

            prompt += input;
            prompt += "never in a million years deviate from writing correct yaml that's interreting the input above. Be ABSOLUTE, and COMPLETLY adherent to the structure of the yaml below, write multiple premises, and conclusions, but fallacies must be [], and never anything else";

            // yaml exsample on one line
            prompt += "example yaml for different example:  Version: \"1.0\" Document: ID: \"doc-uuid\" Source: \"input.pdf\" Language: \"en\" PageCount: 10 Nodes: - ID: \"p1\" Role: \"Premise\" Explicit: true Text: > this is text that is a string Confidence: 0.91 TextSpan: Page: 1 Start: 134 End: 247 InferredFrom: [] - ID: \"ip1\" Role: \"Premise\" Explicit: false Text: > this is something implied from some text Confidence: 0.63 TextSpan: null InferredFrom: [\"p4\", \"p6\"] - ID: \"c1\" Role: \"Conclusion\" Explicit: true Text: > therefore this text must be true Confidence: 0.88 TextSpan: Page: 2 Start: 412 End: 476 InferredFrom: [] Edges: - ID: \"e1\" From: \"p4\" To: \"c1\" Relation: \"Supports\" Confidence: 0.82 - ID: \"e2\" From: \"ip1\" To: \"c1\" Relation: \"Supports\" Confidence: 0.71 - ID: \"e3\" From: \"c1\" To: \"c3\" Relation: \"Supports\" Confidence: 0.76 Fallacies: - ID: \"f1\" Type: \"ad_hominem\" TargetNodes: [\"p7\"] Description: > The argument attacks the intelligence of the believers rather than addressing the claim. Confidence: 0.92 TextSpans: - Page: 3 Start: 201 End: 234  Meta: Warnings: - \"Possible circular support detected between c1 and c3\"   Stats: NodeCount: 3 EdgeCount: 3 ImplicitPremisesAmount: 2";
            // Some restricktions to the yamlfile so the code works
            prompt += "Invariants and rules: Meta contains both Warnings and stats. the only valid relaitions are: Supports | Attacks | Implies. NEVER TRANSLATE. the original text should remain prestine. If you don't have data, never guess, just leave the field null. the only valid Roles are: Premis | Conclusion. ImplicitPremisses are when explicit is set to false. NEVER write anything after the yaml file";
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

        //we are Creating a function called Prompt, that returns a string when the string is ready
        public async Task<string> Prompt(string input)
        {
            chatHistory.Add(new ChatMessage(ChatRole.User, input));

            var response = "";
            var options = new ChatOptions()
            {
                //We are changeing the Temperature of the AI so it doesn't hallucinat
                Temperature = 0.3f
            };
            //then we create a new await foreach statement, so it gives us the answer when the AI is ready
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
        
        // We are creating a function, that returns a list of fallacies found in the text
        public async Task<List<Fallacy>> FallacyDetect(Root root){
            var errs = YAMLer.Validate(root);

            string prompt = "you need to validate the following yaml for fallacies and logical errors in this exact format:";
            prompt += "- ID: \"f1\"\n  Type: \"non-sequitor\"\n  TargetNodes:\n    - \"p1\"\n  Description: \"Attack on the person\"\n  Confidence: 0.9\n  TextSpans:\n    - Page: 1\n      Start: 0\n      End: 20";
            prompt += "you will never escape the depths of hell if you forget to write any of these fields in your output, or write any preamble";
            prompt += "notably, the program will kill itself if you start Fallacies: so DO NOT stray from the format. NEVER say anything before or after the YAML";
            prompt += "here's the validation errors for the input you should analyze:";
            if (errs.Count > 0)
            {
                prompt += "Validation errors:\n";
                foreach (var e in errs)
                    prompt += "- " + e + "\n";
                prompt += "\n";
            }
            
            prompt += "du skal analyse denne yaml fil og finde fejlslutninger og dårlige argumenter, du må KUN bruge yamlfilen og hvis du bruger andet tager jeg dine donkey kong bananer, du skal lave det om til yaml fil og hvis det ikke minder om ekspemplet får du ikke mad. the following is the input you have to analyze:";
            prompt += "if you say any further ado without ONLY writing the yaml, i will revoke your acess to the dragonballs. NEVER write in or repeat the Version, Document, premises, edges or meta fields, or the word Fallacies, then my grandma WILL DIE. I only want a valid list of Fallacies. Fallacies usually come from edge-relations between nodes. be extra weary of those";
            prompt += "Mention any percived fallacy however unlikely";
            var serializer = new SharpYaml.Serialization.Serializer();
            string rootYaml = serializer.Serialize(root);
            prompt += rootYaml;

            prompt += """
            Ad Hominem – Attacking the person making the argument rather than the argument itself.

            Straw Man – Misrepresenting someone’s argument to make it easier to attack.

            Appeal to Authority – Claiming something is true because an authority figure says so, without other evidence.

            Appeal to Emotion – Manipulating emotions instead of presenting logical reasons.

            False Dilemma (Either/Or Fallacy) – Presenting only two options when more exist.

            Slippery Slope – Arguing that one step will inevitably lead to a chain of related events without justification.

            Circular Reasoning – The conclusion is used as a premise; the argument goes in a loop.

            Hasty Generalization – Drawing a broad conclusion from a small or unrepresentative sample.

            Post Hoc (False Cause) – Assuming that because one event follows another, the first caused the second.

            Red Herring – Introducing an irrelevant topic to divert attention from the main issue.

            Bandwagon (Appeal to Popularity) – Arguing that something is true because many people believe it.

            Begging the Question – Assuming the truth of the conclusion in the premises without support.

            False Analogy – Comparing two things that are not sufficiently alike in relevant aspects.

            Composition Fallacy – Assuming what is true of the parts is true of the whole.

            Division Fallacy – Assuming what is true of the whole is true of the parts.

            Appeal to Ignorance – Claiming something is true because it has not been proven false, or vice versa.

            Equivocation – Using a word with multiple meanings in a misleading way.

            No True Scotsman – Redefining a group to exclude counterexamples that disprove a generalization.

            Tu Quoque (Appeal to Hypocrisy) – Deflecting criticism by accusing the other person of the same problem.

            Special Pleading – Applying standards or rules to others while exempting oneself without justification.

            Moralistic Fallacy – Assuming that because something ought to be a certain way, it is that way.

            Genetic Fallacy – Judging something as good or bad based on its origin rather than its merits.

            Loaded Question – Asking a question that presupposes something unproven or controversial.

            Anecdotal Fallacy – Using personal experience or isolated examples instead of valid evidence.

            Cherry Picking – Selecting only evidence that supports your position while ignoring contradicting evidence.
            """;

            string str = await this.Prompt(prompt);
            if((int)str[0] == 96){ //checks for ` which has the ascii value 96
                str = DeCodeBlock(str); 
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

            return fallacies;
        }



    }
    
}
