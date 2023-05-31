using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Linq;
using System.Threading;
using HidLibrary;

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
        /// 收到数据的回调
        /// </summary>
        public event EventHandler<int> DataReceived;

        /// <summary>
        /// get an error
        /// </summary>
        public event EventHandler<DAPError> ErrorReceived;

        /// <summary>
        /// 发送buff
        /// </summary>
        private List<byte[]> toSendBuffer { get; set; } = new List<byte[]>();

        /// <summary>
        /// 向dap发数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool Write(byte[] data, int offset, int length)
        {
            if (!IsOpen)
                return false;
            lock (toSendBuffer)
                toSendBuffer.Add(data.Skip(offset).Take(length).ToArray());
            return true;
        }

        /// <summary>
        /// 从缓冲区读数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public int Read(byte[] data, int offset, int length)
        {
            if (!IsOpen || length == 0 || offset >= data.Length)
                return 0;
            lock (buffer)
            {
                if (length > _bytesToRead)
                    length = _bytesToRead;
                if (offset + length > data.Length)//确保长度没超
                    length = data.Length - offset;
                Array.Copy(buffer, 0, data, offset, length);
                if (length < _bytesToRead)//剩下的数据往前挪
                    Array.Copy(buffer, length, buffer, 0, _bytesToRead - length);
                _bytesToRead -= length;
                return length;
            }
        }


        /// <summary>
        /// received data
        /// </summary>
        public event EventHandler<bool> ConnectionClosed;
        private void Closed()
        {
            ConnectionClosed?.Invoke(this, true);
            lock (toSendBuffer)
                toSendBuffer.Clear();
            _bytesToRead = 0;
            IsOpen = false;
            needClose = false;
        }

        private bool needClose = false;
        /// <summary>
        /// 主动关闭usb
        /// </summary>
        public void Close()
        {
            needClose = true;
        }

        /// <summary>
        /// Open device usb for communicate
        /// </summary>
        /// <returns>is it open succeed</returns>
        public bool Open()
        {
            if (IsOpen)
                return true;
            switch (this.Type)
            {
                case DeviceType.Hid:
                    {
                        //todo
                        return false;
                        //var HidDeviceList = HidDevices.Enumerate(this.Vid, this.Pid).ToList();
                        //var matched = false;
                        //foreach (var device in HidDeviceList)
                        //{
                        //    if (matched)
                        //        break;
                        //    var buff = new byte[1024];
                        //    device.ReadSerialNumber(out buff);
                        //    for(int i=0;i<buff.Length;i+=2)
                        //        buff[i/2] = buff[i];
                        //    var SerialNumber = Encoding.Default.GetString(buff);
                        //    SerialNumber = SerialNumber.Substring(0,SerialNumber.IndexOf('\0'));
                        //    //序列号不匹配
                        //    if (SerialNumber != this.SerialNumber)
                        //        continue;
                        //    //匹配上了
                        //    matched = true;
                        //    IsOpen = true;
                        //    new Thread(() =>
                        //    {
                        //        needClose = false;
                        //        var timeout = 1;
                        //        while (true)
                        //        {
                        //            try
                        //            {
                        //                //读数据
                                        
                        //                var err = device.Read(0);
                        //                if (err.Status == HidDeviceData.ReadStatus.NotConnected)
                        //                {
                        //                    //断了，退出吧
                        //                    Closed();
                        //                    return;
                        //                }
                        //                if (err.Data.Length > 0)//有数据了，放缓冲区里
                        //                {
                        //                    var readLength = err.Data.Length;
                        //                    Console.WriteLine($"recv len {readLength}");
                        //                    lock (buffer)
                        //                    {
                        //                        if (BytesToRead + readLength > buffer.Length)
                        //                            readLength = buffer.Length - _bytesToRead;
                        //                        Array.Copy(err.Data, 0, buffer, _bytesToRead, readLength);
                        //                        _bytesToRead += readLength;
                        //                    }
                        //                    DataReceived?.Invoke(this, _bytesToRead);
                        //                }
                        //                else
                        //                {
                        //                    Thread.Sleep(timeout);
                        //                }
                        //                lock (toSendBuffer)//发数据
                        //                {
                        //                    while (toSendBuffer.Count > 0)
                        //                    {
                        //                        var data = toSendBuffer[0];
                        //                        toSendBuffer.RemoveAt(0);
                        //                        var serr = device.Write(data, 1000);
                        //                        if(!serr)
                        //                            ErrorReceived.Invoke(this, DAPError.Send);
                        //                    }
                        //                }
                        //                if (needClose) //主动关闭
                        //                {
                        //                    Closed();
                        //                    ErrorReceived?.Invoke(this, DAPError.Closed);
                        //                    return;
                        //                }
                        //            }
                        //            catch (Exception e)
                        //            {
                        //                Closed();
                        //                ErrorReceived?.Invoke(this, DAPError.Closed);
                        //                Console.WriteLine(e);
                        //                break;
                        //            }
                        //        }
                        //    }).Start();
                        //}
                        //if (!matched)//没匹配上或打开失败了
                        //{
                        //    return false;
                        //}
                    }
                    break;
                case DeviceType.WinUsb:
                    {
                        UsbContext context = new UsbContext();
                        var allDevices = context.List();
                        var matched = false;
                        foreach (var device in allDevices)
                        {
                            if (matched)
                                break;
                            //pid vid不匹配
                            if (device.ProductId != this.Pid || device.VendorId != this.Vid)
                                continue;
                            //序列号不匹配
                            if (!device.TryOpen() || device.Info.SerialNumber != this.SerialNumber)
                                continue;
                            //匹配上了
                            matched = true;
                            try
                            {
                                device.ResetDevice();
                                if(!device.TryOpen())
                                {
                                    matched = false;
                                    break;//没打开
                                }
                                UsbConfigInfo configInfo = device.Configs[0];
                                ReadOnlyCollection<UsbInterfaceInfo> interfaceList = configInfo.Interfaces;
                                if (interfaceList.Count < this.Interface - 1)
                                    continue;
                                device.SetConfiguration(1);
                                device.ClaimInterface(this.Interface);
                                device.SetAltInterface(0);
                            }
                            catch
                            {
                                matched = false;
                                break;//没打开
                            }
                            this.IsOpen = true;//开了
                            UsbConfigInfo configInfo1 = device.Configs[0];
                            ReadOnlyCollection<UsbInterfaceInfo> interfaceList1 = configInfo1.Interfaces;
                            new Thread(() =>
                            {
                                needClose = false;
                                var timeout = 50;
                                var temp = new byte[1024];
                                var readLength = 0;
                                var reader =
                                    device.OpenEndpointReader(
                                        (LibUsbDotNet.Main.ReadEndpointID)this.RecvEP,
                                        1024);
                                var writer =
                                    device.OpenEndpointWriter(
                                        (LibUsbDotNet.Main.WriteEndpointID)this.SendEP);
                                while (true)
                                {
                                    try
                                    {
                                        //读数据
                                        var err = reader.Read(temp, timeout, out readLength);
                                        switch(err)
                                        {
                                            case Error.Success:
                                            case Error.Timeout:
                                            case Error.Busy:
                                            case Error.Pipe:
                                                break;
                                            default:
                                                //断了，退出吧
                                                try { device.Close(); } catch { }
                                                ErrorReceived?.Invoke(this, DAPError.Closed);
                                                allDevices.Dispose();
                                                context.Dispose();
                                                Closed();
                                                return;
                                        }
                                        if(readLength > 0)//有数据了，放缓冲区里
                                        {
                                            Console.WriteLine($"recv len {readLength}");
                                            lock(buffer)
                                            {
                                                if (BytesToRead + readLength > buffer.Length)
                                                    readLength = buffer.Length - _bytesToRead;
                                                Array.Copy(temp, 0, buffer, _bytesToRead, readLength);
                                                _bytesToRead += readLength;
                                            }
                                            DataReceived?.Invoke(this, _bytesToRead);
                                        }
                                        lock (toSendBuffer)//发数据
                                        {
                                            while(toSendBuffer.Count > 0)
                                            {
                                                var data = toSendBuffer[0];
                                                toSendBuffer.RemoveAt(0);
                                                try
                                                {
                                                    //var writeLength = 0;
                                                    var serr = writer.Write(data, 1000, out _);
                                                    //Console.WriteLine($"write len {writeLength} {serr}");
                                                }
                                                catch
                                                {
                                                    ErrorReceived?.Invoke(this, DAPError.Send);
                                                }
                                            }
                                        }
                                        if(needClose) //主动关闭
                                        {
                                            try { device.Close(); } catch { }
                                            allDevices.Dispose();
                                            context.Dispose();
                                            Closed();
                                            return;
                                        }
                                    }
                                    catch(Exception e)
                                    {
                                        try { device.Close(); } catch { }
                                        allDevices.Dispose();
                                        context.Dispose();
                                        Closed();
                                        ErrorReceived?.Invoke(this, DAPError.Closed);
                                        Console.WriteLine(e);
                                        break;
                                    }
                                }
                            }).Start();
                        }
                        if (!matched)//没匹配上或打开失败了
                        {
                            allDevices.Dispose();
                            context.Dispose();
                            return false;
                        }
                    }
                    break;
                default:
                    return false;
            }
            return true;
        }


    }
}
