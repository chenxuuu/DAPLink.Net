using DAPLinkDotNet;
using System.Text;

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
                Console.WriteLine($"recv: {BitConverter.ToString(buff)}");
                Console.WriteLine($"ascii: {Encoding.Default.GetString(buff)}");
            };
            device.ErrorReceived += (s, err) =>
            {
                Console.WriteLine($"error: {err}");
            };


            Console.WriteLine($"open: {device.Open()}");

            //https://arm-software.github.io/CMSIS_5/DAP/html/group__DAP__genCommands__gr.html
            while (true)
            {
                var toSend = Console.ReadLine()!;
                var data = Enumerable.Range(0, toSend.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(toSend.Substring(x, 2), 16))
                     .ToArray();
                device.Write(data, 0, data.Length);
                Console.WriteLine($"sent {BitConverter.ToString(data)}");
            }
        }
    }
}