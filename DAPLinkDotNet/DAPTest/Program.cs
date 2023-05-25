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
            var device = list[0];

            device.DataReceived += (s, len) =>
            {
                var buff = new byte[len];
                device.Read(buff, 0, len);
                //Console.WriteLine($"recv: {BitConverter.ToString(buff)}");
            };


            Console.WriteLine($"open: {device.Open()}");
            device.Write(new byte[] { 0x00, 0x04 }, 0, 2);

            Console.ReadLine();
        }
    }
}