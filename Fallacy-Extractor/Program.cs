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
            string str = await n.parseToYAML("what is love? if love exists, then point at it. love isn't physical, cuz the physical world is devoid of it. trust");
             var serializer = new Serializer();
             Root root;
             try
             {
                 root = serializer.Deserialize<Root>(str);
             }
             catch (Exception ex)
             {
                 Console.WriteLine("Invalid YAML syntax: " + ex.Message);
                 return;
             }


            Console.WriteLine(YAMLer.Validate(root));
        }
    }
}
