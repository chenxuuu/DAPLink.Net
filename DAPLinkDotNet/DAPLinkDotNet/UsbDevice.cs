using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DAPLinkDotNet
{
    public class UsbDevice
    {
        public string Name { get; set; } = "Unknow";
        public DeviceType Type { get; set; } = DeviceType.Unknow;
        public ushort Pid { get; set; }
        public ushort Vid { get; set; }
        public int Interface { get; set; } = -1;
        public int SendEP { get; set; } = -1;
        public int RecvEP { get; set; } = -1;
        public string SerialNumber { get; set; } = "";

        public bool IsOpen { get; set; } = false;

        public override string ToString()
        {
            return $"Device: {Name} ({Type}), {(IsOpen ? "open" : "close")}\r\n" +
                $"VID: 0x{Vid:X04}, PID: 0x{Pid:X04}\r\n" +
                $"{SerialNumber}\r\n" +
                $"send endpoint address: {SendEP}\r\n" +
                $"recv endpoint address: {RecvEP}\r\n" +
                $"interface: {Interface}\r\n";
        }
    }
}
