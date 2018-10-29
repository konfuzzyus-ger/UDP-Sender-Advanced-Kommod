using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

namespace UDPSender
{
	public class Sender
	{
		public Sender (IPAddress IPv4Adress, int size, int portnumber, int WorkerID)
		{
			locale = new IPEndPoint (IPAddress.Any, portnumber);
			remote = new IPEndPoint (IPv4Adress, 12345);
			_WorkerID = WorkerID;
			if (size == 0) {
				ByteBuffer = 1;
			} else {
				ByteBuffer = (int)Math.Pow (2, size);
			}
			_size = size;
		}
		private IPEndPoint locale = null;
		private IPEndPoint remote = null;
		private UdpClient Antenna = null;
		private Random rnd = new Random();
		private readonly int ByteBuffer;
		private readonly int _WorkerID;
		private readonly int _size;

		public List<Message> getTest
		{
			get{
				Antenna = new UdpClient (locale);
				List<Message> chunk = shoot();
				Antenna = null;
				return chunk;
			}
		}

		private List<Message> shoot ()
		{
			List<Message> msgs = new List<Message> ();
			for (int i = 0; i < MainClass.Messages; i++) {
				byte[] content = new byte[ByteBuffer];
				rnd.NextBytes (content);
				content[0] = (byte) i;
				Message msg = new Message (i, _WorkerID, _size, content);
				Antenna.Send (content, _size, remote);
				byte[] recv = Antenna.Receive (ref locale); //(ref locale);
				if (recv.Length > 0 && (int) recv[0] == i )
				{
					msg.arrival();
				}
				msgs.Add(msg);
			}
			return msgs;
		}
	}
}

