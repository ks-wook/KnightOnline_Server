using Google.Protobuf.Protocol;
using System.Collections.Generic;

namespace Server.Game 
{
    public class GameLobby : JobSerializer
    {
        Dictionary<int, Player> _players = new Dictionary<int, Player>(); // 로비 내의 플레이어

        public void EnterLobby(Player player)
        {
			if (player == null)
				return;

			player.Lobby = this;

			{
				Player searchedPlayer;
				if(_players.TryGetValue(player.Id, out searchedPlayer) == false)
                {
					_players.Add(player.Id, player);
				}

				// player.Room = this;

				player.RefreshAdditionalStat(); // 플레이어 스탯 초기화

				// 본인한테 정보 전송
				{

					// 입장 패킷은 본인한테만 전송
					S_EnterLobby enterPacket = new S_EnterLobby();
					enterPacket.Player = player.Info;
					player.Session.Send(enterPacket);


					S_Spawn spawnPacket = new S_Spawn();
					foreach (Player p in _players.Values)
					{
						if (player != p)
							spawnPacket.Objects.Add(p.Info);
					}

					player.Session.Send(spawnPacket);
				}
			}
		}




    }



}
