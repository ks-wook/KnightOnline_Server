using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DB;
using Server.Game;
using Server.Session.Utils;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server
{
    public partial class ClientSession : PacketSession
    {
		public int AccountDbId { get; private set; }
		public List<LobbyPlayerInfo> LobbyPlayers { get; set; } = new List<LobbyPlayerInfo>();

        public void HandleLogin(C_Login loginPacket)
        {
			Console.WriteLine($"[Log] UniqueId({loginPacket.UniqueId}) 로그인");

			// 로그인 패킷은 플레이어의 상태가 로그인 씬 일때만 유효함
			if (ServerState != PlayerServerState.ServerStateLogin) // 타이밍이 맞지않음
				return;

			LobbyPlayers.Clear();
			
			using (AppDbContext db = new AppDbContext())
			{
				AccountDb findAccount = db.Accounts
					.Include(a => a.Players)
					.Where(a => a.AccountName == loginPacket.UniqueId).FirstOrDefault();

				
				if (findAccount != null)
				{
					// 로컬 메모리에 저장
					AccountDbId = findAccount.AccountDbId;

					S_Login loginOk = new S_Login() { LoginOk = 1 };
					foreach (PlayerDb playerDb in findAccount.Players)
                    {
						LobbyPlayerInfo lobbyPlayer = new LobbyPlayerInfo()
						{
							PlayerDbId = playerDb.PlayerDbId,
							Name = playerDb.PlayerName,
							StatInfo = new StatInfo()
							{
								Level = playerDb.Level,
								Hp = playerDb.Hp,
								MaxHp = playerDb.MaxHp,
								Attack = playerDb.Attack,
								Speed = playerDb.Speed,
								TotalExp = playerDb.TotalExp
							}
						};

						// 플레이어 계정에 소속된 캐릭터들을 탐색
						LobbyPlayers.Add(lobbyPlayer);


						// 패킷에 넣어서 전송
						loginOk.Players.Add(lobbyPlayer);
                    }

					
					Send(loginOk);

					// 로비로 이동
					ServerState = PlayerServerState.ServerStateLobby;
				}
				else // 어카운트는 이미 웹 어카운트 서버에서 있는지 검사를 완료한 상태
				{
					// 어카운트가 없다면 자동 생성 후 로그인
                    AccountDb newAccount = new AccountDb { AccountName = loginPacket.UniqueId };
                    db.Accounts.Add(newAccount);
                    db.SaveChangesEx();

                    AccountDbId = newAccount.AccountDbId;


                    S_Login loginOk = new S_Login() { LoginOk = 0 };
                    Send(loginOk);

                    // 로비로 이동
                    ServerState = PlayerServerState.ServerStateLobby;
                }
			}
		}

		public void HandleEnterLobby(C_EnterLobby enterLobbyPacket)
		{
			// 플레이어가 가지고 있는 캐릭터 중 입장하고자 하는 캐릭터가 있는지 확인
			LobbyPlayerInfo playerInfo = LobbyPlayers.Find(p => p.Name == enterLobbyPacket.Name);
			if (playerInfo == null)
				return;

			if(enterLobbyPacket.IsGameToLobby == false) // 로비에 최초 접속
            {
				// 로비에 입장하면서 현재 세션의 플레이어 정보를 초기화
				MyPlayer = ObjectManager.Instance.Add<Player>();
				{
					MyPlayer.PlayerDbId = playerInfo.PlayerDbId;
					MyPlayer.Info.Name = playerInfo.Name;
					MyPlayer.Info.PosInfo.State = CreateureState.Idle;
					MyPlayer.Info.PosInfo.PosX = 0;
					MyPlayer.Info.PosInfo.PosY = 0;
					MyPlayer.Info.PosInfo.PosZ = 0;
					MyPlayer.Stat.MergeFrom(playerInfo.StatInfo);
					MyPlayer.Session = this;



					S_ItemList itemListPacket = new S_ItemList();

					// item 로딩
					using (AppDbContext db = new AppDbContext())
					{
						// 본인의 아이템만
						List<ItemDb> items = db.Items
							.Where(i => i.OwnerDbId == playerInfo.PlayerDbId)
							.ToList();

						foreach (ItemDb itemDb in items)
						{
							Item item = Item.MakeItem(itemDb);
							if (item != null)
							{
								MyPlayer.Inven.Add(item);

								ItemInfo info = new ItemInfo();
								info.MergeFrom(item.Info);
								itemListPacket.Items.Add(info); // 클라의 아이템 목록
							}

						}

					}

					Send(itemListPacket);







					// 플레이어의 퀘스트 클리어 정보를 패킷에 담아 전송
					S_QuestList questListPacket = new S_QuestList();

					using (AppDbContext db = new AppDbContext())
					{
						// 본인의 퀘스트 정보만
						List<QuestDb> quests = db.Quests
							.Where(q => q.OwnerDbId == playerInfo.PlayerDbId)
							.ToList();

						foreach (QuestDb questDb in quests)
						{
							QuestInfo questInfo = new QuestInfo()
							{
								TemplateId = questDb.TemplatedId,
								IsCleared = questDb.IsCleared,
								IsRewarded = questDb.IsRewarded
							};
							questListPacket.Quests.Add(questInfo);

						}

					}

					Send(questListPacket);




					// 플레이어의 메인 스테이지 클리어 정보를 패킷에 담아 전송
					S_StageList stageListPacket = new S_StageList();

					using (AppDbContext db = new AppDbContext())
					{
						// 본인의 스테이지 정보만
						List<MainStageDb> mainStages = db.MainStages
							.Where(q => q.OwnerDbId == playerInfo.PlayerDbId)
							.ToList();

						foreach (MainStageDb mainStageDb in mainStages)
						{
							stageListPacket.StageNames.Add(mainStageDb.StageName);
						}

					}

					Send(stageListPacket);

				}
			}
			else // 이미 플레이어 정보가 있는 상태(게임씬)에서 다시 로비로 이동하는 경우
            {
				GameLogic.Instance.Push(() =>
				{
					GameLobby lobby = MyPlayer.Lobby;
					lobby.Push(lobby.EnterLobby, MyPlayer);
				});

				return;
			}
			

			ServerState = PlayerServerState.ServerStateLobby;

			// 개인용 로비 입장(마이룸 개념)
			GameLogic.Instance.Push(() =>
			{
				GameLobby lobby = new GameLobby();
				lobby.Push(lobby.EnterLobby, MyPlayer);
			});
			
		}

		public void HandleEnterGame(C_EnterGame enterGamePacket)
        {
			// 플레이어가 가지고 있는 캐릭터 중 입장하고자 하는 캐릭터가 있는지 확인
			LobbyPlayerInfo playerInfo = LobbyPlayers.Find(p => p.Name == enterGamePacket.Name);
			if (playerInfo == null)
				return;

			ServerState = PlayerServerState.ServerStateGame; // 현재 플레이어 상태를 게임 상태로 변경

            Console.WriteLine($"[Log] {enterGamePacket.RoomNum}번 GameRoom 게임 시작");

			// 방 입장 순서에 따른 플레이어 스폰 위치 지정
			MyPlayer.PosInfo.PosX += (float) enterGamePacket.PlayerOrder;

			// 플레이어가 들고 있던 무기 정보
			MyPlayer.Info.EquippedItemTemplatedId = enterGamePacket.EquippedItemTemplatedId;

			// 싱글 게임일 경우 0번방으로 지정하여 플레이어 로딩 처리만
			if(enterGamePacket.RoomNum == 0)
            {
				GameRoom.EnterSingleGame(MyPlayer);
            }
			else // 멀티 게임일 경우 생성된 방이 있는 지 확인하고 방으로 입장
            {
				// 패킷으로 받은 룸 번호를 통해 방 입장
				GameLogic.Instance.Push(() => // 모든 작업은 룸 단위로 이루어진다
				{
					GameRoom room = GameLogic.Instance.Find(enterGamePacket.RoomNum);
					// room.Init(enterGamePacket.RoomNum); // 몬스터 스폰
					room.Push(room.EnterMultiGame, MyPlayer);
				});
			}
        }

		public void HandleCreatePlayer(C_CreatePlayer createPacket)
        {
			if (ServerState != PlayerServerState.ServerStateLobby)
				return;

			using (AppDbContext db = new AppDbContext())
            {
				PlayerDb findPlayer = db.Players
					.Where(p => p.PlayerName == createPacket.Name).FirstOrDefault();

				if(findPlayer != null) // 해당 어카운트의 
                {
					// 이름이 겹친다
					Send(new S_CreatePlayer());
                }
				else
                {
					// 1레벨 스탯 정보 추출
					StatInfo stat = null;
					DataManager.StatDict.TryGetValue(1, out stat);

					// DB에 플레이어 추가
					PlayerDb newPlayerDb = new PlayerDb()
					{
						PlayerName = createPacket.Name,
						Level = stat.Level,
						Hp = stat.Hp,
						MaxHp = stat.MaxHp,
						Attack = stat.Attack,
						Speed = (int)stat.Speed,
						TotalExp = 0,
						AccountDbId = AccountDbId
					};

					db.Players.Add(newPlayerDb);
					db.SaveChangesEx();

					// 로컬 메모리에 추가
					LobbyPlayerInfo lobbyPlayer = new LobbyPlayerInfo()
					{
						PlayerDbId = newPlayerDb.PlayerDbId,
						Name = createPacket.Name,
						StatInfo = new StatInfo()
						{
							Level = stat.Level,
							Hp = stat.Hp,
							MaxHp = stat.MaxHp,
							Attack = stat.Attack,
							Speed = (int)stat.Speed,
							TotalExp = 0
						}
					};

					// 메모리에도 플레이어 정보를 들고있는 이유:
					// 매번 db에 접근해서 가져오는 것의 비용이 크므로
					LobbyPlayers.Add(lobbyPlayer);

					// 캐릭터 생성완료를 클라에 전달
					S_CreatePlayer newPlayer = new S_CreatePlayer() { Player = new LobbyPlayerInfo() };
					newPlayer.Player.MergeFrom(lobbyPlayer);

					Send(newPlayer);


					ItemDb itemDb = new ItemDb()
					{
						TemplateId = 1, // 기본 아이템 id
						Count = 1,
						OwnerDbId = newPlayer.Player.PlayerDbId
					};

					db.Items.Add(itemDb); // db에 추가
					bool success = db.SaveChangesEx(); // 저장

					if(!success)
                        Console.WriteLine("[Error] 기본 지급 아이템 생성 오류");
				}
			}
        }
    
		
	}
}
