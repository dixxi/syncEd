﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Network
{
	// @see: http://msdn.microsoft.com/en-us/library/tst0kwb1(v=vs.110).aspx
	public class LinkEstablisher
	{
		// UDP port for sending broadcasts
		const int broadcastPort = 1337;

		// TCP port for listening after broadcasts
		const int listenPort = 1338;

		const int linkEstablishTimeoutMs = 1000;

		delegate void NewLinkHandler(Peer p);

		event NewLinkHandler NewLinkEstablished;

		/// <summary>
		/// Tries to find a peer for the given document name on the network. If no peer could be found, null is returned
		/// </summary>
		public Peer FindPeer(string documentName)
		{
			// open listening port for incoming connection
			var haveListener = new TcpListener(IPAddress.Any, listenPort);
			haveListener.Start(1); // only listen for 1 connection
			var peerTask = haveListener.AcceptTcpClientAsync();

			// send a broadcast with the document name into the network
			using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, broadcastPort);

				byte[] bytes = Encoding.ASCII.GetBytes(documentName);
				s.SendTo(bytes, ep);
			}

			// wait for an answer
			Peer peer = null;
			if (peerTask.Wait(linkEstablishTimeoutMs))
				peer = new Peer(peerTask.Result);

			// stop listening
			haveListener.Stop();

			return peer;
		}

		/// <summary>
		/// Listens on the network for new peers with the given document name.
		/// If such a peer connects, the NewLinkEstablished event is fired.
		/// TODO: make method returnable when shutting down
		/// </summary>
		/// <param name="documentName"></param>
		public void ListenForPeers(string documentName)
		{
			using (var udpClient = new UdpClient(broadcastPort))
			{
				try
				{
					while (true)
					{
						Console.WriteLine("Waiting for broadcast");
						var clientEP = new IPEndPoint(IPAddress.Any, broadcastPort);
						byte[] bytes = udpClient.Receive(ref clientEP);
						string peerDocumentName = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
						Console.WriteLine("Received broadcast from {0}:\n {1}\n", clientEP.ToString(), peerDocumentName);

						if (peerDocumentName == documentName)
						{
							// establish connection to peer
							var client = new TcpClient();
							client.Connect(clientEP.Address, listenPort);

							if (NewLinkEstablished != null)
								NewLinkEstablished(new Peer(client));
						}
					}

				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
			}
		}
	}
}
