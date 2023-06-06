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
            var device = new DAPLink(list[0]);

            device.Open();

            Console.WriteLine(device.SWJPins(0,0,0));
   //         Console.WriteLine(device.GetSerialNumber());
   //         Console.WriteLine(device.GetCMSIS_DAPProtocolVersion());
   //         Console.WriteLine(device.GetTargetDeviceVendor());
   //         Console.WriteLine(device.GetTargetDeviceName());
   //         Console.WriteLine(device.GetTargetBoardVendor());
   //         Console.WriteLine(device.GetTargetBoardName());
   //         Console.WriteLine(device.GetProductFirmwareVersion());
   //         Console.WriteLine(device.GetCapabilities());
   //         Console.WriteLine(device.GetMaximumPacketCount());
   //         Console.WriteLine(device.GetMaximumPacketSize());

   //         while (true)
   //         {
   //             device.SetHostStatus(0, false);
   //             Thread.Sleep(10);
   //             device.SetHostStatus(0, true);
   //             Thread.Sleep(10);
			//}
            //var recvBuff = new byte[1024];
            //while (true)
            //{
            //    var len = device.SendRecv(ReadHex(), recvBuff);
            //    Console.WriteLine($"recv {BitConverter.ToString(recvBuff, 0, len)}");
            //    Console.WriteLine($"{Encoding.Default.GetString(recvBuff, 0, len)}");

            //}
        }


        private static byte[] ReadHex()
        {
            var toSend = Console.ReadLine()!;
            var data = Enumerable.Range(0, toSend.Length)
                 .Where(x => x % 2 == 0)
                 .Select(x => Convert.ToByte(toSend.Substring(x, 2), 16))
                 .ToArray();
            return data;
        }
    }
}