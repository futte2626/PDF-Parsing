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
            Root str = await n.parseToYAML("what is love? if love exists, then point at it. love isn't physical, cuz the physical world is devoid of it. trust");
            

            foreach(string s in YAMLer.Validate(str)){
                Console.WriteLine(s);
            }
            
        }
    }
}
