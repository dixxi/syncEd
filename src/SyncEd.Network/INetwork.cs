﻿using SyncEd.Network.Packets;

namespace SyncEd.Network
{
	public delegate void SendBackFunc(object packet);
	public delegate void PacketHandler(object packet, Peer peer, SendBackFunc sendBack);

	public interface INetwork
	{
		event PacketHandler PacketArrived;

		Peer Self { get; }

		/// <summary>
		/// Starts the network subsystem which is responsible for managing links and packets
		/// </summary>
		/// <returns>Returns true if a peer could be found for the given document name</returns>
		bool Start(string documentName);

		/// <summary>
		/// Stops the network subsystem
		/// </summary>
		void Stop();

		/// <summary>
		/// Broadcasts a packet into the network
		/// </summary>
		/// <param name="packet">Packet to send, can be any object</param>
		void SendPacket(object packet);
	}
}
