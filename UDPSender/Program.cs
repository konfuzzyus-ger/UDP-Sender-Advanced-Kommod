using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

namespace UDPSender
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			IPEndPoint locale = new IPEndPoint (IPAddress.Any, 50000);
			TcpListener listen = new TcpListener (locale);
			listen.Start ();
			while (true) {
				Console.WriteLine ("Waiting for Connetion");
				TcpClient client = listen.AcceptTcpClient ();
				Console.WriteLine ("Connected!");
				Thread controler = new Thread (Controler.control);
				controler.Start (client);
			}
			//startTransmit ();
		}
		public static int Threads = 100;
		public static int Messages = 100;
		public static List<Message> msgs = new List<Message>();
		public static int doneThreads = 0;
		public static StreamWriter backchannel = null;
		private static int _Sleep = 8;
		public static bool isMainframe = false;
		public static List<ExecuteHost> executors = new List<ExecuteHost>();
		public static readonly int ownHostID = 0;
		public static int max_remote_HostID = 0;
		public static object max_remote_HostIDO = (object) max_remote_HostID;
		public static IPAddress Target;
		public static int Sleep
		{
			set {
				_Sleep = value;
			}
			get {
				return _Sleep * 1000;
			}
		}
		public static void startTransmit ()
		{
			Threading.ThreadMaster (MainClass.Threads);
		}
		public struct ExecuteHost  
		{  
			public int HostID;  
			public TcpClient client;    
			public StreamReader sReader;
			public StreamWriter sWriter;
		}
	}

	public class Threading 
	{
		public struct ThreadData  
		{  
			public int port;  
			public int WorkerID;  
			public int packetSize;
			public IPAddress IpAdressDest;  
		}
		public static void ThreadMaster(int ThreadCount)
		{
			Thread[] sender = new Thread[ThreadCount];
			int port = 0;
			int WorkerID = 0;
			for (int k = 1; k < 16; k++) {
				for (int i = 0; i < sender.Length; i++) {
					sender [i] = new Thread (Threading.DoWork);
				}
				foreach (Thread i in sender) {
					ThreadData d;
					//d.IpAdressDest = "141.24.212.23";
					d.IpAdressDest = MainClass.Target;
					d.packetSize = k;
					d.port = 30000 + port;
					port = port + 1;
					d.WorkerID = WorkerID;
					WorkerID = WorkerID + 1;
					i.Start (d);
				}
				Thread.Sleep (MainClass.Sleep);
				WorkerID = 0;
			}
		}
		public static void DoWork(object size_obj)
		{
			ThreadData d = (ThreadData)size_obj;
			Sender sender = new Sender (d.IpAdressDest, d.packetSize, d.port, d.WorkerID);
			List<Message> result = sender.getTest;
			string start;
			if (MainClass.isMainframe) {
				start = "HostID::" + MainClass.ownHostID + "::";
			} else {
				start = string.Empty;
			}
			foreach (Message i in result) {
				string printMsg = start + "ThreadID::" + i.WorkerID + "::MessageID::" + i.number + "::Bytes::2^" + i.size + "::Time::" + i.rtt;
				System.Console.WriteLine (printMsg);
				lock (MainClass.backchannel) {
					MainClass.backchannel.WriteLine (printMsg);
					MainClass.backchannel.Flush ();
				}
			}
			MainClass.doneThreads = MainClass.doneThreads + 1;
		}
	}

	public class Controler
	{
		public static void control(object TcpClientObject)
		{
			TcpClient client = (TcpClient)TcpClientObject;
			StreamReader sReader = new StreamReader(client.GetStream(), Encoding.ASCII);
			StreamWriter sWriter = new StreamWriter(client.GetStream(), Encoding.ASCII);
			String sData = null;
			MainClass.backchannel = sWriter;
			while (client.Connected)
			{
				sData = null;
				// reads from stream
				sData = sReader.ReadLine();
				// reads from stream
				if (sData == string.Empty) {
					break;
				}
				if (sData == null) {
					break;
				}
				// shows content on the console.
				Console.WriteLine("Client " + ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() +": " + sData);
				execute (sData, sWriter);
				// to write something back.
				//sWriter.WriteLine("Client " + ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() +": " + sData);
				//sWriter.Flush();
			}
		}
		public static void execute(string command, StreamWriter backchannel)
		{
			command = command.ToLowerInvariant ();
			if (command.StartsWith ("threads=")) {
				string data = command.Split ('=') [1];
				if (int.TryParse (data, out MainClass.Threads)) {
					backchannel.WriteLine ("Threads set to {0}", MainClass.Threads);
				} else {
					backchannel.WriteLine ("There occured an error while setting number of Threads");
				}
				backchannel.Flush ();
			} else if (command.StartsWith ("messages=")) {
				string data = command.Split ('=') [1];
				if (int.TryParse (data, out MainClass.Messages)) {
					backchannel.WriteLine ("Messages set to {0}", MainClass.Messages);
				} else {
					backchannel.WriteLine ("There occured an error while setting number of Messages");
				}
				backchannel.Flush ();
			} else if (command.StartsWith ("sleep=")) {
				string data = command.Split ('=') [1];
				int sleep = 0;
				if (int.TryParse (data, out sleep)) {
					MainClass.Sleep = sleep;
					backchannel.WriteLine ("Sleep set to {0} milliseconds", MainClass.Sleep);
				} else {
					backchannel.WriteLine ("There occured an error while setting number of Messages");
				}
				backchannel.Flush ();
			} else if (command.StartsWith("execute")) {
				string data = command.Split ('=') [1];
				if (IPAddress.TryParse (data, out MainClass.Target)) {
					try {
						Thread t = new Thread (MainClass.startTransmit);
						t.Start ();
					} catch (Exception e) {
						backchannel.WriteLine (e.ToString ());
						backchannel.Flush ();
					}
				} else {
					backchannel.WriteLine ("There occured an error while setting TargetIP");
				}
			} else if (command == "setmain") {
				MainClass.isMainframe = true;
				return;
			} else if (command.StartsWith ("connect=")) {
				string data = command.Split ('=') [1];
				IPAddress Ip;
				if (IPAddress.TryParse(data, out Ip)) {
					ConnectNewHost (Ip);
					backchannel.WriteLine ("Connect to {0} ", data);
				} else {
					backchannel.WriteLine ("There occured an error while connecting");
				}
				backchannel.Flush ();
				return;
			} else {
				backchannel.WriteLine ("Command not found.");
				backchannel.Flush ();
				return;
			}
			replicateCommand (command);
		}
		private static void replicateCommand (string command)
		{
			foreach (MainClass.ExecuteHost d in MainClass.executors) {
				d.sWriter.WriteLine (command);
				d.sWriter.Flush ();
			}
		}
		private static void ConnectNewHost(IPAddress HostIP)
		{
			IPEndPoint locale = new IPEndPoint (IPAddress.Any, 50001);
			TcpClient newHost = new TcpClient (locale);
			IPEndPoint remote = new IPEndPoint (HostIP, 50000);
			newHost.Connect (remote);
			MainClass.ExecuteHost d;
			d.client = newHost;
			lock (MainClass.max_remote_HostIDO) {
				d.HostID = MainClass.max_remote_HostID + 1;
				MainClass.max_remote_HostID = MainClass.max_remote_HostID + 1;
			}
			d.sReader= new StreamReader(newHost.GetStream(), Encoding.ASCII);
			d.sWriter = new StreamWriter(newHost.GetStream(), Encoding.ASCII);
			MainClass.executors.Add (d);
			Thread t = new Thread (listenExecutor);
			t.Start (d);
		}
		public static void listenExecutor(object executeHost)
		{
			MainClass.ExecuteHost d = (MainClass.ExecuteHost)executeHost;
			string sData;
			while (d.client.Connected)
			{
				sData = null;
				// reads from stream
				sData = d.sReader.ReadLine();
				if (sData == string.Empty) {
					break;
				}
				if (sData == null) {
					break;
				}
				lock (MainClass.backchannel) {
					MainClass.backchannel.WriteLine ("HostID::{0}::{1}", d.HostID, sData);
				}
				MainClass.backchannel.Flush();
			}
		}
	}
}
