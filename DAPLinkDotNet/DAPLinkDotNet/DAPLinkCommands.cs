using LibUsbDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;

namespace DAPLinkDotNet
{
	/// <summary>
	/// simple commands here
	/// </summary>
	public partial class DAPLink
	{
		//https://arm-software.github.io/CMSIS_5/DAP/html/group__DAP__HostStatus.html

		private bool isOk(byte code) => code == 0;

		/// <summary>
		/// [0x01] DAP_HostStatus Command => <br/>
		/// 0x00 Connect: Status indicates that the debugger is connected to the Debug Unit.<br/>
		/// 0x01 Running: Status indicates that the target hardware is executing application code.<br/>
		/// </summary>
		public void HostStatus(byte mode, bool isOn)
		{
			this.SendRecv(new byte[] { 0x01, mode, (byte)(isOn ? 1 : 0) }, new byte[0xff]);
		}

        /// <summary>
        /// [0x02] DAP_Connect Command => <br/>
        /// Port: Selects the DAP port mode and configures the DAP I/O pins.The possible values are:<br/>
        /// 0 = Default mode: configuration of the DAP port mode is derived from DAP_DEFAULT_PORT (zero configuration).<br/>
        /// 1 = SWD mode: connect with Serial Wire Debug mode.<br/>
        /// 2 = JTAG mode: connect with 4/5-pin JTAG mode.<br/>
        /// </summary>
        /// <returns>DAP port mode</returns>
        public byte Connect(byte port)
		{
			var recvBuff = new byte[0xff];
			var len = this.SendRecv(new byte[] { 0x02, port,}, recvBuff);
			if (len < 2 || recvBuff[0] != 0x02)
				return 0;
            return recvBuff[1];
        }

        /// <summary>
        /// [0x03] DAP_Disconnect Command
        /// </summary>
        /// <returns>Status: Response Status</returns>
        public bool Disconnect()
		{
			var recvBuff = new byte[0xff];
			var len = this.SendRecv(new byte[] { 0x03}, recvBuff);
			if (len < 2 || recvBuff[0] != 0x03)
				return false;
            return isOk(recvBuff[1]);
        }

        /// <summary>
        /// [0x08] DAP_WriteABORT Command =><br/>
        /// DAP Index: Zero based device index of the selected JTAG device.For SWD mode the value is ignored.<br/>
        /// Abort: 32-bit value to write into the CoreSight ABORT register.
        /// </summary>
        /// <returns>Status: Response Status</returns>
        public bool WriteAbort(byte index, byte[] value)
        {
            var recvBuff = new byte[0xff];
			var toSend = new byte[] { 0x08, index, 0, 0, 0, 0 };
			value.CopyTo(toSend, 2);
            var len = this.SendRecv(toSend, recvBuff);
            if (len < 2 || recvBuff[0] != 0x08)
				return false;
            return isOk(recvBuff[1]);
        }
        public bool WriteAbort(byte index, UInt32 value) => WriteAbort(index, BitConverter.GetBytes(value));

        /// <summary>
        /// [0x09] DAP_Delay Command => <br/>
		/// The DAP_Delay Command waits for a time period specified in micro-seconds.<br/>
		/// Delay: wait time in µs.
        /// </summary>
        /// <returns>Status: Response Status</returns>
        public bool Delay(UInt16 us)
        {
            var recvBuff = new byte[0xff];
            var toSend = new byte[] { 0x09, 0, 0 };
            BitConverter.GetBytes(us).CopyTo(toSend, 1);
            var len = this.SendRecv(toSend, recvBuff);
            if (len < 2 || recvBuff[0] != 0x09)
                return false;
            return isOk(recvBuff[1]);
        }

        /// <summary>
        /// [0x0A] DAP_ResetTarget Command
        /// </summary>
        /// <returns>(Response Status,Execute)</returns>
        public (bool, byte) ResetTarget()
        {
            var recvBuff = new byte[0xff];
            var len = this.SendRecv(new byte[] { 0x0a}, recvBuff);
            if (len < 3 || recvBuff[0] != 0x0a)
                return (false,0);
            return (isOk(recvBuff[1]), recvBuff[2]);
        }

        /// <summary>
        /// [0x10] DAP_SWJ_Pins Command =><br/>
        /// I/O Pin Mapping for the fields Pin Output, Pin Select, and Pin Input:<br/>
        ///  Bit 0: SWCLK/TCK<br/>
        ///  Bit 1: SWDIO/TMS<br/>
        ///  Bit 2: TDI<br/>
        ///  Bit 3: TDO<br/>
        ///  Bit 5: nTRST<br/>
        ///  Bit 7: nRESET<br/>
        /// </summary>
        /// <param name="pinOutput">Value for selected output pins</param>
        /// <param name="pinSelect">Selects which output pins will be modified</param>
        /// <param name="us">Wait timeout for the selected output to stabilize, 0..3000000 = time in µs (max 3s)</param>
        /// <returns>Pin Input: Pin state read from target Device.</returns>
        public byte? SWJPins(byte pinOutput, byte pinSelect, UInt32 us)
        {
            var recvBuff = new byte[0xff];
            var toSend = new byte[] { 0x10, pinOutput, pinSelect, 0,0,0,0 };
            BitConverter.GetBytes(us).CopyTo(toSend, 3);
            var len = this.SendRecv(toSend, recvBuff);
            if (len < 2 || recvBuff[0] != 0x10)
                return null;
            return recvBuff[1];
        }

        /// <summary>
        /// [0x11] DAP_SWJ_Clock Command
        /// </summary>
        /// <param name="Hz">Clock: Selects maximum SWD/JTAG Clock (SWCLK/TCK) value in Hz</param>
        /// <returns>Status: Response Status</returns>
        public bool SWJClock(UInt32 Hz)
        {
            var recvBuff = new byte[0xff];
            var toSend = new byte[] { 0x11, 0, 0, 0, 0 };
            BitConverter.GetBytes(Hz).CopyTo(toSend, 1);
            var len = this.SendRecv(toSend, recvBuff);
            if (len < 2 || recvBuff[0] != 0x11)
                return false;
            return isOk(recvBuff[1]);
        }

        /// <summary>
        /// [0x12] DAP_SWJ_Sequence Command
        /// </summary>
        /// <param name="data">Sequence Bit Data: Sequence generated on SWDIO/TMS (with clock @SWCLK/TCK) LSB is transmitted first</param>
        /// <returns>Status: Response Status</returns>
        public bool SWJSequence(byte[] data)
        {
            var recvBuff = new byte[0xff];
            var toSend = new List<byte> { 0x12, (byte)data.Length};
            toSend.AddRange(data);
            var len = this.SendRecv(toSend.ToArray(), recvBuff);
            if (len < 2 || recvBuff[0] != 0x12)
                return false;
            return isOk(recvBuff[1]);
        }

        /// <summary>
        /// [0x13] DAP_SWD_Configure Command =><br/>
        /// Configuration: Contains information about SWD specific features<br/>
        /// Bit 1 .. 0: Turnaround clock period of the SWD device (should be identical with the WCR [Write Control Register] value of the target): 0 = 1 clock cycle (default), 1 = 2 clock cycles, 2 = 3 clock cycles, 3 = 4 clock cycles.<br/>
        /// Bit 2: DataPhase: 0 = Do not generate Data Phase on WAIT/FAULT (default), 1 = Always generate Data Phase (also on WAIT/FAULT; Required for Sticky Overrun behavior).
        /// </summary>
        /// <param name="cfg">Configuration</param>
        /// <returns>Status: Response Status</returns>
        public bool SWDConfigure(byte cfg)
        {
            var recvBuff = new byte[0xff];
            var toSend = new byte[] { 0x13, cfg };
            var len = this.SendRecv(toSend, recvBuff);
            if (len < 2 || recvBuff[0] != 0x13)
                return false;
            return isOk(recvBuff[1]);
        }

        /// <summary>
        /// [0x1D] DAP_SWD_Sequence Command
        /// </summary>
        /// <param name="count">Sequence Count: Number of Sequences</param>
        /// <param name="info">Sequence Info: Contains number of SWCLK cycles and SWDIO mode</param>
        /// <param name="data">SWDIO Data (only for output mode): Data generated on SWDIO</param>
        /// <returns>(Response Status,SWDIO Data)</returns>
        public (bool, byte[]) SWDSequence(byte count, byte? info = null, byte[] data = null)
        {
            var recvBuff = new byte[0xff];
            var toSend = new List<byte> { 0x1d , count };
            if(info != null)
                toSend.Add((byte)info);
            if(data != null)
                toSend.AddRange(data);
            var len = this.SendRecv(toSend.ToArray(), recvBuff);
            if (len < 3 || recvBuff[0] != 0x1D)
                return (false, null);
            return (isOk(recvBuff[1]), recvBuff.Take(len).Skip(2).ToArray());
        }

        /// <summary>
        /// [0x04] DAP_TransferConfigure Command
        /// </summary>
        /// <param name="cycles">Idle Cycles: Number of extra idle cycles after each transfer.</param>
        /// <param name="waitRetry">WAIT Retry: Number of transfer retries after WAIT response.</param>
        /// <param name="matchRetry">Match Retry: Number of retries on reads with Value Match in DAP_Transfer.</param>
        /// <returns>Status: Response Status</returns>
        public bool TransferConfigure(byte cycles, UInt16 waitRetry, UInt16 matchRetry)
        {
            var recvBuff = new byte[0xff];
            var toSend = new byte[] { 0x04, cycles, 0, 0, 0, 0 };
            BitConverter.GetBytes(waitRetry).CopyTo(toSend, 2);
            BitConverter.GetBytes(matchRetry).CopyTo(toSend, 4);
            var len = this.SendRecv(toSend, recvBuff);
            if (len < 2 || recvBuff[0] != 0x04)
                return false;
            return isOk(recvBuff[1]);
        }

        /// <summary>
        /// [0x05] DAP_Transfer Command<br/>
        /// https://arm-software.github.io/CMSIS_5/DAP/html/group__DAP__Transfer.html
        /// </summary>
        /// <param name="index">DAP Index: Zero based device index of the selected JTAG device. For SWD mode the value is ignored.</param>
        /// <param name="count">Transfer Count: Number of transfers: 1 .. 255.</param>
        /// <param name="data">(Transfer Request, Transfer Data)</param>
        /// <returns>(Count,Response,TD_TimeStamp,Data)</returns>
        public (byte, byte, UInt32?, UInt32[]) Transfer(byte index, byte count, (byte, UInt32)[] data)
        {
            var recvBuff = new byte[0xff];
            var toSend = new List<byte> { 0x05, index, count };
            if(data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    toSend.Add(data[i].Item1);
                    var temp = new byte[4];
                    BitConverter.GetBytes(data[i].Item2).CopyTo(temp,0);
                    toSend.AddRange(temp);
                }
            }
            var len = this.SendRecv(toSend.ToArray(), recvBuff);
            if (len < 3 || recvBuff[0] != 0x05)
                return (0,0, null,null);
            if(len == 3)
                return (recvBuff[1], recvBuff[2], null, null);
            var r = new List<UInt32>();
            if(len > 7)
            {
                for (int i = 7; i < len; i+=4)
                {
                    r.Add(BitConverter.ToUInt32(recvBuff, i));
                }
            }
            var ts = BitConverter.ToUInt32(recvBuff, 3);
            return (recvBuff[1], recvBuff[2], ts, r.ToArray());
        }

        /// <summary>
        /// [0x06] DAP_TransferBlock Command
        /// </summary>
        /// <param name="index">DAP Index: Zero based device index of the selected JTAG device. For SWD mode the value is ignored.</param>
        /// <param name="count">Transfer Count: Number of transfers: 1 .. 65535.</param>
        /// <param name="req">Transfer Request: Contains information about requested access from host</param>
        /// <param name="data">Transfer Data: register values</param>
        /// <returns>(Count,Response,Data)</returns>
        public (UInt16, byte, UInt32[]) Transfer(byte index, UInt16 count, byte req, UInt32[] data)
        {
            var recvBuff = new byte[0xff];
            var toSend = new List<byte> { 0x06, index };
            var t = new byte[2];
            BitConverter.GetBytes(count).CopyTo(t, 0);
            toSend.AddRange(t);
            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    var temp = new byte[4];
                    BitConverter.GetBytes(data[i]).CopyTo(temp, 0);
                    toSend.AddRange(temp);
                }
            }
            var len = this.SendRecv(toSend.ToArray(), recvBuff);
            if (len < 3 || recvBuff[0] != 0x06)
                return (0, 0, null);
            var r_count = BitConverter.ToUInt16(recvBuff, 1);
            var r = new List<UInt32>();
            if (len > 4)
            {
                for (int i = 4; i < len; i += 4)
                {
                    r.Add(BitConverter.ToUInt32(recvBuff, i));
                }
            }
            return (r_count, recvBuff[3], r.ToArray());
        }

        /// <summary>
        /// [0x07] DAP_TransferAbort Command
        /// </summary>
        public void TransferAbort()
        {
            this.SendRecv(new byte[] { 0x07}, new byte[0xff]);
        }

        //atomic commands todo
        //swo commands todo
        //jtag commands todo
        //uart com commands todo
    }
}
