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

namespace DAPLinkDotNet
{
    public class DAPLink : UsbDevice
    {
        /// <summary>
        /// Get DAPLink device list
        /// </summary>
        /// <returns>device</returns>
        public static List<DAPLink> GetList()
        {
            var list = new List<DAPLink>();
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
                                    var info = new DAPLink()
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
        /// 缓冲区
        /// </summary>
        private byte[] buffer { get; set; } = new byte[2048];
        private int _bytesToRead = 0;
        /// <summary>
        /// 缓冲区里实际的值
        /// </summary>
        public int BytesToRead
        {
            get => _bytesToRead;
            set { }
        }

        /// <summary>
        /// Open device usb for communicate
        /// </summary>
        /// <returns>is it open succeed</returns>
        public bool Open()
        {
            if(IsOpen)
                return true;
            switch (this.Type)
            {
                case DeviceType.Hid:
                    {
                        
                    }
                    break;
                case DeviceType.WinUsb:
                    {
                        using UsbContext context = new UsbContext();
                        using var allDevices = context.List();
                        var matched = false;
                        foreach (var device in allDevices)
                        {
                            if (matched)
                                continue;
                            //pid vid不匹配
                            if (device.ProductId != this.Pid || device.VendorId != this.Vid)
                                continue;
                            //序列号不匹配
                            if (device.Info.SerialNumber != this.SerialNumber)
                                continue;
                            try
                            {
                                device.Open();
                                device.ResetDevice();
                                UsbConfigInfo configInfo = device.Configs[0];
                                ReadOnlyCollection<UsbInterfaceInfo> interfaceList = configInfo.Interfaces;
                                if (interfaceList.Count < this.Interface - 1)
                                    continue;
                                //匹配上了
                                matched = true;
                                device.SetConfiguration(1);
                                device.ClaimInterface(this.Interface);
                                device.SetAltInterface(this.Interface);
                            }
                            catch
                            {
                                return false;
                            }
                            this.IsOpen = true;//开了
                            UsbConfigInfo configInfo1 = device.Configs[0];
                            ReadOnlyCollection<UsbInterfaceInfo> interfaceList1 = configInfo1.Interfaces;
                            new Thread(() =>
                            {
                                //read bulk
                            });
                        }
                    }
                    break;
                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// received data
        /// </summary>
        public event EventHandler<byte[]> DataReceived;

        /// <summary>
        /// received error
        /// </summary>
        public event EventHandler<DAPError> ErrorReceived;

        /// <summary>
        /// 接收buff
        /// </summary>
        private List<byte[]> recvBuffer { get; set; } = new List<byte[]>();
        /// <summary>
        /// Send data to dap
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool Write(byte[] data, int offset, int length)
        {
            if (!IsOpen)
                return false;
            lock(recvBuffer)
                recvBuffer.Add(data.Skip(offset).Take(length).ToArray());
            //todo 通知发送
            return true;
        }

        /// <summary>
        /// read data from buffer
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public int Read(byte[] data, int offset, int length)
        {
            if (!IsOpen || length == 0 || offset >= data.Length)
                return 0;
            lock(buffer)
            {
                if (length > _bytesToRead)
                    length = _bytesToRead;
                if(offset+length > data.Length)
                    length = data.Length - offset;
                Array.Copy(buffer, 0, data, offset, length);
                _bytesToRead -= length;
                return length;
            }
        }
    }
}
