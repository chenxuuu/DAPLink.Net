using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace DAPLinkDotNet
{
	// https://arm-software.github.io/CMSIS_5/DAP/html/group__DAP__genCommands__gr.html

	/// <summary>
	/// [0x00] 0x00 DAP_Info
	/// </summary>
	public partial class DAPLink
	{
		private string Get0x01String(byte code)
		{
			var recvBuff = new byte[0xff];
			var len = this.SendRecv(new byte[] { 0x00, code }, recvBuff);
			if (len > 0)
				len = recvBuff[1];
			return Encoding.Default.GetString(recvBuff, 2, len);
		}



		/// <summary>
		/// [0x00] 0x01 = Get the Vendor Name (string).
		/// </summary>
		/// <returns></returns>
		public string GetVendorName() => Get0x01String(0x01);

		/// <summary>
		/// [0x00] 0x02 = Get the Product Name (string).
		/// </summary>
		/// <returns></returns>
		public string GetProductName() => Get0x01String(0x02);

		/// <summary>
		/// [0x00] 0x03 = Get the Serial Number (string).
		/// </summary>
		/// <returns></returns>
		public string GetSerialNumber() => Get0x01String(0x03);

		/// <summary>
		/// [0x00] 0x04 = Get the CMSIS-DAP Protocol Version (string).
		/// </summary>
		/// <returns></returns>
		public string GetCMSIS_DAPProtocolVersion() => Get0x01String(0x04);

		/// <summary>
		/// [0x00] 0x05 = Get the Target Device Vendor (string).
		/// </summary>
		/// <returns></returns>
		public string GetTargetDeviceVendor() => Get0x01String(0x05);

		/// <summary>
		/// [0x00] 0x06 = Get the Target Device Name (string).
		/// </summary>
		/// <returns></returns>
		public string GetTargetDeviceName() => Get0x01String(0x06);

		/// <summary>
		/// [0x00] 0x07 = Get the Target Board Vendor (string).
		/// </summary>
		/// <returns></returns>
		public string GetTargetBoardVendor() => Get0x01String(0x07);

		/// <summary>
		/// [0x00] 0x08 = Get the Target Board Name (string).
		/// </summary>
		/// <returns></returns>
		public string GetTargetBoardName() => Get0x01String(0x08);

		/// <summary>
		/// [0x00] 0x09 = Get the Product Firmware Version (string, vendor-specific format).
		/// </summary>
		/// <returns></returns>
		public string GetProductFirmwareVersion() => Get0x01String(0x09);


		/// <summary>
		/// [0x00] 0xF0 = Get information about the Capabilities (BYTE) of the Debug Unit
		/// </summary>
		/// <returns></returns>
		public DAPLinkCapabilities GetCapabilities()
		{
			var r = new DAPLinkCapabilities();
			var recvBuff = new byte[0xff];
			var len = this.SendRecv(new byte[] { 0x00, 0xf0 }, recvBuff);
			if (len > 0)
				len = recvBuff[1];
			else
				return r;

			r.SWDCommands = (recvBuff[2] & 1) == 1;
			r.JTAGCommands = ((recvBuff[2] >> 1) & 1) == 1;
			r.SWOUART = ((recvBuff[2] >> 2) & 1) == 1;
			r.SWOManchester = ((recvBuff[2] >> 3) & 1) == 1;
			r.AtomicCommands = ((recvBuff[2] >> 4) & 1) == 1;
			r.TestDomainTimer = ((recvBuff[2] >> 5) & 1) == 1;
			r.SWOStreamingTrace = ((recvBuff[2] >> 6) & 1) == 1;
			r.UARTCommunicationPort = ((recvBuff[2] >> 7) & 1) == 1;
			if(len >= 2)
				r.USBCOMPort = (recvBuff[3] & 1) == 1;
			return r;
		}


		private UInt64 Get0x01Word(byte code)
		{
			var r = new DAPLinkCapabilities();
			var recvBuff = new byte[0xff];
			var len = this.SendRecv(new byte[] { 0x00, code }, recvBuff);
			if (len < 2)
				return 0;
			len = recvBuff[1];
			return len switch
			{
				1 => recvBuff[2],
				2 => BitConverter.ToUInt16(recvBuff, 2),
				4 => BitConverter.ToUInt32(recvBuff, 2),
				8 => BitConverter.ToUInt64(recvBuff, 2),
				_ => 0,
			};
		}

		/// <summary>
		/// [0x00] 0xF1 = Get the Test Domain Timer parameter information (WORD).
		/// </summary>
		/// <returns></returns>
		public UInt64 GetTestDomainTimer() => Get0x01Word(0xf1);

		/// <summary>
		/// [0x00] 0xFB = Get the UART Receive Buffer Size (WORD).
		/// </summary>
		/// <returns></returns>
		public UInt64 GetUARTReceiveBufferSize() => Get0x01Word(0xfb);

		/// <summary>
		/// [0x00] 0xFC = Get the UART Transmit Buffer Size (WORD).
		/// </summary>
		/// <returns></returns>
		public UInt64 GetUARTTransmitBufferSize() => Get0x01Word(0xfc);

		/// <summary>
		/// [0x00] 0xFD = Get the SWO Trace Buffer Size (WORD).
		/// </summary>
		/// <returns></returns>
		public UInt64 GetSWOTraceBufferSize() => Get0x01Word(0xfd);

        /// <summary>
        /// [0x00] 0xFE = Get the maximum Packet Count (BYTE).
        /// </summary>
        /// <returns></returns>
		public byte GetMaximumPacketCount() => (byte)Get0x01Word(0xfe);

		/// <summary>
		/// [0x00] 0xFF = Get the maximum Packet Size (SHORT).
		/// </summary>
		/// <returns></returns>
		public UInt16 GetMaximumPacketSize() => (UInt16)Get0x01Word(0xff);
	}

	public class DAPLinkCapabilities
	{
		public bool SWDCommands = false;
		public bool JTAGCommands = false;
		public bool SWOUART = false;
		public bool SWOManchester = false;
		public bool AtomicCommands = false;
		public bool TestDomainTimer = false;
		public bool SWOStreamingTrace = false;
		public bool UARTCommunicationPort = false;
		public bool USBCOMPort = false;

		public override string ToString()
        {
            return $"SWD Commands: {(this.SWDCommands ? "yes" : "no")}\r\n" +
					$"JTAG Commands: {(this.JTAGCommands ? "yes" : "no")}\r\n" +
					$"SWO UART: {(this.SWOUART ? "yes" : "no")}\r\n" +
					$"SWO Manchester: {(this.SWOManchester ? "yes" : "no")}\r\n" +
					$"Atomic Commands: {(this.AtomicCommands ? "yes" : "no")}\r\n" +
					$"Test Domain Timer: {(this.TestDomainTimer ? "yes" : "no")}\r\n" +
					$"SWO Streaming Trace: {(this.SWOStreamingTrace ? "yes" : "no")}\r\n" +
					$"UART Communication Port: {(this.UARTCommunicationPort ? "yes" : "no")}\r\n" +
					$"USB COM Port: {(this.USBCOMPort ? "yes" : "no")}";
		}
    }
}
