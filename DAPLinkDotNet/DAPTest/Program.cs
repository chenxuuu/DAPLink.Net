using DAPLinkDotNet;

namespace DAPTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var list = DAPLink.GetList();
            foreach (var item in list)
                Console.WriteLine(item.ToString());
            Console.WriteLine("Auto connect first device...");

        }
    }
}