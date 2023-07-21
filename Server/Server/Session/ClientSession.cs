using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;
using System.Net;
using Google.Protobuf.Protocol;
using Google.Protobuf;
using Server.Game;
using Server.Data;

namespace Server
{
	public partial class ClientSession : PacketSession
	{
		public PlayerServerState ServerState { get; private set; } = PlayerServerState.ServerStateLogin;


		public Player MyPlayer { get; set; } // 해당 세션의 플레이어
		public int SessionId { get; set; }

		object _lock = new object();
		List<ArraySegment<byte>> _reserveQueue = new List<ArraySegment<byte>>();

        #region Network
		// Send 예약 함수
        public void Send(IMessage packet)
		{
			string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
			MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), msgName);
			ushort size = (ushort)packet.CalculateSize();
			byte[] sendBuffer = new byte[size + 4];
			Array.Copy(BitConverter.GetBytes(size + 4), 0, sendBuffer, 0, sizeof(ushort));
			Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));
			Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);

			lock (_lock)
            {
				// 실제 보내는 것은 다른 쓰레드에게 떠넘긴다.
				_reserveQueue.Add(sendBuffer);
			}
		}

		// 실제 네트워크 IO 보내는 함수
		public void FlushSend()
        {
			List<ArraySegment<byte>> sendList = null;

			lock (_lock)
            {
				if (_reserveQueue.Count == 0)
					return;

				sendList = _reserveQueue;
				_reserveQueue = new List<ArraySegment<byte>>();
            }

			Send(sendList);
        }

		public override void OnConnected(EndPoint endPoint)
		{
			Console.WriteLine($"[Log] OnConnected : {endPoint}");

            {
				S_Connected connectedPacket = new S_Connected();
				Send(connectedPacket);
            }			
         
        }

		public override void OnRecvPacket(ArraySegment<byte> buffer)
		{
			PacketManager.Instance.OnRecvPacket(this, buffer);
		}

		public override void OnDisconnected(EndPoint endPoint)
		{
			SessionManager.Instance.Remove(this);

			Console.WriteLine($"[Log] OnDisconnected : {endPoint}");
		}

		public override void OnSend(int numOfBytes)
		{
			// Console.WriteLine($"Transferred bytes: {numOfBytes}");
		}
        #endregion


    }
}
