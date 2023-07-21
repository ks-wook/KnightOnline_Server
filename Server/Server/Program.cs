using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Data;
using Server.DB;
using Server.Game;
using ServerCore;

// 1. Recv (N개) tjqld
// 2. GameLogic (1) 요리사 - 실제 방에서 게임 로직 (AI등) 처리
// 3. Send (1개) 서빙
// 4. DB (1) 결제/장부 - DB 처리


namespace Server
{
	class Program
	{
		static Listener _listener = new Listener();

		static void GameLogicTask()
        {
			while (true)
			{
				GameLogic.Instance.Update();
				Thread.Sleep(0);
			}
		}

		static void DbTask()
        {
			while(true)
            {
				DbTransaction.Instance.Flush();
				Thread.Sleep(0);
            }
        }

		static void MatchTask()
        {
			while(true)
            {
				MatchManager.Instance.MatchQueueFlush();
				Thread.Sleep(0);
            }
        }

		static void NetworkTask()
        {
			while(true)
            {
				List<ClientSession> sessions = SessionManager.Instance.GetSessions();

				foreach (ClientSession session in sessions)
                {
					session.FlushSend();
                }

				Thread.Sleep(0);
            }
        }

        static void FlushRoom()
        {
            JobTimer.Instance.Push(FlushRoom, 250);
        }

		public static int Port { get; } = 7777;
		public static string IpAddress { get; set; }




        static void Main(string[] args)
		{
			ConfigManager.LoadConfig();
			DataManager.LoadData();
			


			// DNS (Domain Name System)
			string host = Dns.GetHostName();
			IPHostEntry ipHost = Dns.GetHostEntry(host);
			IPAddress ipAddr = ipHost.AddressList[1];
			IPEndPoint endPoint = new IPEndPoint(ipAddr, Port);

			IpAddress = ipAddr.ToString();


			_listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
			Console.WriteLine("Listening...");

            // GameLogicTask
            {
				Task gameLogicTask = new Task(GameLogicTask, TaskCreationOptions.LongRunning);
				gameLogicTask.Start();
			}

			// NetworkTask
			{
				Task networkTask = new Task(NetworkTask, TaskCreationOptions.LongRunning);
				networkTask.Start();
			}

			// NetworkTask
			{
				Task matchTask = new Task(MatchTask, TaskCreationOptions.LongRunning);
				matchTask.Start();
			}

			// Db
			DbTask();
		}
	}
}
