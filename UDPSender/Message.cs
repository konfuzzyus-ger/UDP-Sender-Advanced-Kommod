using System;

namespace UDPSender
{
	public class Message
	{
		public Message (int _Number, int _WorkerID, int _size, byte[] _content)
		{
			number = _Number + 1;
			WorkerID = _WorkerID + 1;
			content = _content;
			size = _size;
			launch = DateTime.Now;
		}
		public readonly DateTime launch;
		private DateTime _landing;
		private bool _landingSet = false;
		public readonly int number;
		public readonly int WorkerID;
		private double _rtt;
		private bool _rttSet =false;
		public readonly int size;
		private readonly byte[] content;

		public void arrival()
		{
			_landing = DateTime.Now;
			_landingSet = true;
		}

		public double rtt
		{
			get {
				if (_landingSet == false) {
					return -1;
				}
				if (_rttSet == false) {
					TimeSpan flight = landing - launch;
					_rtt = flight.TotalMilliseconds * 1000;
				}
				return _rtt;
			}
		}
		public DateTime landing
		{
			get {
				return _landing;
			}
		}
	}
}

