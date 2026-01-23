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
//     role: "premise"            # premise | conclusion
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
//     role: "premise"
//     explicit: false
//     text: >
//       this is someting implied from some text
//     confidence: 0.63
//     text_span: null
//     inferred_from: ["p4", "p6"]

//   - id: "c1"
//     role: "conclusion"
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
//     relation: "supports"       # supports | attacks
//     confidence: 0.82

//   - id: "e2"
//     from: "ip1"
//     to: "c1"
//     relation: "supports"
//     confidence: 0.71

//   - id: "e3"
//     from: "c1"
//     to: "c3"
//     relation: "supports"
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

namespace Fallacy_Extractor
{
    public class NLangParser
    {
        private readonly IChatClient chatClient;
        private readonly List<ChatMessage> chatHistory;

        public NLangParser()
        {
            chatClient = new OllamaApiClient(new Uri("http://localhost:11434/"), "gemma3:12b");
            chatHistory = new List<ChatMessage>();
        }
        public async Task<string> parseToYAML(string input){
            string prompt = "ud fra dette stykke yaml, find nogle fejlslutninger (fallacies) gemt i teksten, hvis der er nogen. skriv dem i yaml, eller jeg tager dine chaos orbs. skriv intet andet end det YAML felt. de skal følge dette format (der kan være flere end 1 fejlslutning) fallacies: # should be empty, as it'll get appended by fallacy-detector - id: \"f1\" type: \"ad_hominem\" target_nodes: [\"p7\"] description: > The argument attacks the intelligence of the believers rather than addressing the claim. confidence: 0.92 #float between 0 and 1 text_spans: - page: 3 start: 201 end: 234 her starter YAML inputtet";
            prompt += input;
            prompt += "";
            return await this.Prompt(prompt);
        }

        public async Task<string> Prompt(string input)
        {
            chatHistory.Add(new ChatMessage(ChatRole.User, input));

            var response = "";
            await foreach (ChatResponseUpdate item in chatClient.GetStreamingResponseAsync(chatHistory))
            {
                Console.Write(item.Text); // display as it streams
                response += item.Text;
            }

            chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
            Console.WriteLine();
            return response;
        }
    }
}
