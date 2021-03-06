﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncEd.Network.Tcp
{
	public class UdpBroadcastNetwork
	{
		private Thread udpListenThread;
		private UdpClient udp;
		private readonly int broadcastPort = 1337; // UDP port for sending broadcasts
		private readonly Action<object, IPEndPoint> objectReceived;

		public UdpBroadcastNetwork(Action<object, IPEndPoint> objectReceived)
		{
			this.objectReceived = objectReceived;

			Start();
		}

		private void Start()
		{
			udp = new UdpClient();
			udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			udp.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));
			udp.EnableBroadcast = true;

			udpListenThread = new Thread(() =>
			{
				while (true)
				{
					try
					{
						//Log.WriteLine("Waiting for broadcast");
						var ep = new IPEndPoint(IPAddress.Any, broadcastPort);
						byte[] bytes;
						try
						{
							bytes = udp.Receive(ref ep);
						}
						catch (Exception)
						{
							// asume the socket has been closed for shutting down
							return;
						}
						if (bytes != null && bytes.Length != 0)
						{
							var packet = Utils.Deserialize(bytes);
							//Log.WriteLine("Received broadcast from {0}: {1}", ep.Address, packet.DocumentName);
							objectReceived(packet, ep);
						}
					}
					catch (Exception e)
					{
						Log.WriteLine("Exception in UDP broadcast listening: " + e.ToString());
					}
				}
			});

			udpListenThread.Start();
		}

		public void Stop()
		{
			udp.Close();
			udpListenThread.Join();
		}

		public void BroadcastObject(object o)
		{
			Log.WriteLine("UDP out: " + o);
			udp.Client.SendTo(Utils.Serialize(o), new IPEndPoint(IPAddress.Broadcast, broadcastPort));
		}
	}
}