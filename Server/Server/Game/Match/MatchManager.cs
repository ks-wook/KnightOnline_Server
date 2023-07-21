using Google.Protobuf.Protocol;
using Server;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

class MatchManager
{
	#region Singleton
	static MatchManager _instance = new MatchManager();
	public static MatchManager Instance { get { return _instance; } }
	#endregion

	MatchQueue _matchQueue = new MatchQueue();

	MatchManager() { }

	// 매칭 큐 요청 -> req = true 요청, req = false = 요청 취소, 취소라면 매치 큐에서 빼야함
	public void MatchReq(Player player, bool req)
    {
		if(req)
        {
			Console.WriteLine("[Log] 플레이어 매칭 요청");
			_matchQueue.Add(player);
        }
		else
        {
			Console.WriteLine("[Log] 플레이어 매칭 취소");
			_matchQueue.Remove(player);
        }
    }

	public void MatchQueueFlush()
    {
		// 매치 큐를 열어서 플레이어가 게임에 입장할만큼 모였는지 확인
		// TEMP : 플레이어가 2명 모였다면 방 생성 후 입장
		List<Player> matchedPlayers = _matchQueue.MatchedPlayers();
		if(matchedPlayers != null)
        {			
			// 방생성 후 입장
			// 방의 id는 방에 들어갈 첫번째 플레이어의 session id
			GameRoom room = GameLogic.Instance.Add(matchedPlayers[0].Session.SessionId);
			room.Init();

            Console.WriteLine($"[Log] {room.RoomId} 번 방 생성");

			int playerOrder = 0;

			foreach(Player player in matchedPlayers)
            {
				// 매칭 완료 패킷에 룸 ID를 전송하여 플레이어가 entergame 패킷을 전송해서
				// 게임에 직접 접속하게끔 유도
				S_RaidMatch matchOkPacket = new S_RaidMatch();
				matchOkPacket.Matched = true;
				matchOkPacket.RoomNum = room.RoomId;
				matchOkPacket.Player = player.Info;
				matchOkPacket.Order = playerOrder++;
				player.Session.Send(matchOkPacket);
			}

		}
	}
	
}


