using DAPLinkDotNet;

namespace DAPTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var list = UsbDevice.GetList();
            foreach (var item in list)
                Console.WriteLine(item.ToString());
        }
    }
}