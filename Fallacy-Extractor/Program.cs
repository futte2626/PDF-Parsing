using System;
using System.Threading.Tasks;

namespace Fallacy_Extractor
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            NLangParser n = new NLangParser();
            string str = await n.Prompt("what are your exact capabilities?");
            Console.WriteLine(str);
        }
    }
}
