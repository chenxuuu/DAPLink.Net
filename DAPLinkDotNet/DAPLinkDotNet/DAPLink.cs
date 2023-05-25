using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace DAPLinkDotNet
{
    public class DAPLink
    {
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

        private UsbDevice device;
        /// <summary>
        /// 新建一个daplink设备
        /// </summary>
        /// <param name="usbDevice">usb设备</param>
        DAPLink(UsbDevice usbDevice) => device = usbDevice;

        public override string ToString() => device.ToString();
    }
}
