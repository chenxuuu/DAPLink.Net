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

        public override string ToString()
        {
            return $"Device: {Name} ({Type})\r\n" +
                $"VID: 0x{Vid:X04}, PID: 0x{Pid:X04}\r\n" +
                $"{SerialNumber}\r\n" +
                $"send endpoint address: {SendEP}\r\n" +
                $"recv endpoint address: {RecvEP}\r\n" +
                $"interface: {Interface}\r\n";
        }

        /// <summary>
        /// Get DAPLink device list
        /// </summary>
        /// <returns>device</returns>
        public static List<UsbDevice> GetList()
        {
            var list = new List<UsbDevice>();
            using (UsbContext context = new UsbContext())
            {
                using var allDevices = context.List();
                foreach (var usbRegistry in allDevices)
                {
                    if (usbRegistry.TryOpen())
                    {
                        try
                        {
                            UsbConfigInfo configInfo = usbRegistry.Configs[0];
                            ReadOnlyCollection<UsbInterfaceInfo> interfaceList = configInfo.Interfaces;
                            for (int iInterface = 0; iInterface < interfaceList.Count; iInterface++)
                            {
                                UsbInterfaceInfo interfaceInfo = interfaceList[iInterface];
                                var name = "";
                                if (!String.IsNullOrEmpty(interfaceInfo.Interface))
                                    name = interfaceInfo.Interface;
                                if (name.ToUpper().Contains("CMSIS-DAP"))//名字包含这个才是dap调试器
                                {
                                    var info = new UsbDevice()
                                    {
                                        Vid = usbRegistry.VendorId,
                                        Pid = usbRegistry.ProductId,
                                        SerialNumber = usbRegistry.Info.SerialNumber,
                                    };
                                    info.Name = name;
                                    ReadOnlyCollection<UsbEndpointInfo> endpointList = interfaceInfo.Endpoints;
                                    for (int iEndpoint = 0; iEndpoint < endpointList.Count; iEndpoint++)
                                    {
                                        var addr = endpointList[iEndpoint].EndpointAddress;
                                        //小的ep算输出，大的算输入
                                        if (addr < 0x80 && info.SendEP == -1)
                                            info.SendEP = addr;
                                        else if (addr >= 0x80 && info.RecvEP == -1)
                                            info.RecvEP = addr;
                                    }
                                    //收发都得有才算合法
                                    if (info.SendEP != -1 || info.RecvEP != -1)
                                    {
                                        info.Type = interfaceInfo.Class switch
                                        {
                                            ClassCode.Hid => DeviceType.Hid,
                                            _ => DeviceType.WinUsb,
                                        };
                                        info.Interface = iInterface;
                                        list.Add(info);
                                    }
                                }
                            }
                        }
                        catch { }
                        usbRegistry.Close();
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Open device usb for communicate
        /// </summary>
        /// <returns>is it open succeed</returns>
        public bool Open()
        {
            if (this.Type == DeviceType.Unknow) return false;

            return true;
        }
    }
}
