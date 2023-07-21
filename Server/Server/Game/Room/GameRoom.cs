using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

/*
 * 플레이어 던전 입장 시, 방 내에서의 연산 (데미지 연산, 이동 동기화) 을 처리하는
 * 게임 룸 로직 스크립트
 * 
 * 레이드 등의 멀티 플레이 컨텐츠의 경우 연산들이 처리되는 단위는 방 단위로 처리되므로
 * 해당 클래스를 거쳐서 연산이 처리된다.
 */


namespace Server.Game
{
	public partial class GameRoom :JobSerializer
	{
		public int RoomId { get; set; }
		public int RoomTemplateId { get; set; }

		bool _battleStart = false;

		Dictionary<int, Player> _players = new Dictionary<int, Player>(); // 룸 내의 오브젝트
		Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>(); // 룸 내의 오브젝트

		Monster RaidBoss = new Monster();
		int bossState = 0;
		Random random = new Random();

		S_BossStatChange bossStatPacket = new S_BossStatChange();
		S_BossStatChange bossAttackPacket = new S_BossStatChange();

		public void Init()
		{
			// 보스 몬스터의 스탯 초기화

			// 보스 몬스터의 스탯을 5레벨로 설정
			RaidBoss.Init(5);
			RoomLogicStart();
		}

		async Task BossAttackAsync()
		{
			// 공격 로직
			// 몬스터 공격 패킷 전송
			
			bossState = random.Next(0, 3);

			switch(bossState)
            {
				case 0:
					bossAttackPacket.CurHp = RaidBoss.Hp;
					bossAttackPacket.State = CreateureState.Nomalattack;
					break;
				case 1:
					bossAttackPacket.CurHp = RaidBoss.Hp;
					bossAttackPacket.State = CreateureState.Battleskill;
					break;
				case 2:
					bossAttackPacket.CurHp = RaidBoss.Hp;
					bossAttackPacket.State = CreateureState.Ultimate;
					break;

			}

			Broadcast(bossAttackPacket);

			// 비동기적으로 제어권을 반환하고 대기
			await Task.Yield();
		}

		async void RoomLogicStart()
        {
			while (RaidBoss.Stat.Hp > 0)
			{
				// 공격을 받아 배틀이 시작된 경우
				if (_battleStart)
					await BossAttackAsync();

				await Task.Delay(5000); // 5초 간격으로 공격 시도
			}

			// 보스 사망 처리
			bossStatPacket.State = CreateureState.Dead;
			bossStatPacket.CurHp = 0;

			Broadcast(bossStatPacket);
		}

		public void HandleDamage(C_BossStatChange damagePacket)
        {
			// 서버상의 보스의 데미지 처리 후 방내 모든 플레이어에게 패킷 전송

			_battleStart = true; // 보스가 데미지를 받았을 때 보스의 공격이 시작

			RaidBoss.Hp = RaidBoss.Hp - damagePacket.Damage;

			if(RaidBoss.Hp <= 0)
            {
				RaidBoss.Hp = 0; // 체력이 0이하일 때 0으로 고정
				bossStatPacket = new S_BossStatChange()
				{
					CurHp = RaidBoss.Hp,
					State = CreateureState.None,
				};
			}
			else
            {
				bossStatPacket = new S_BossStatChange()
				{
					CurHp = RaidBoss.Hp,
					State = CreateureState.None,
				};
			}

			
			foreach (Player player in _players.Values)
            {
                Console.WriteLine("[Log] 보스 스탯 변화 패킷 전송");
				player.Session.Send(bossStatPacket);
            }

        }


		// 싱글 던전 입장 (스폰 패킷만 전송, 데미지 연산은 클라에서)
		public static void EnterSingleGame(GameObject gameObject)
		{
			// 스태틱으로 선언한 이유는 방을 생성할 필요 없이 플레이어만 던전에 들여보내면 되므로

			if (gameObject == null)
				return;

			GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

			if (type == GameObjectType.Player) // 플레이어
			{
				Player player = gameObject as Player;

				player.RefreshAdditionalStat(); // 플레이어 스탯 초기화

				// 게임 입장 패킷 전송
				{
					S_EnterGame enterPacket = new S_EnterGame();
					enterPacket.Player = player.Info;
					player.Session.Send(enterPacket);
				}
			}
			else
            {
                Console.WriteLine("[Error] 플레이어 타입이 올바르지 않습니다.");
            }
			
		}

		// 멀티 던전 입장 (플레이어 본인 포함, 타 유저의 캐릭터, 몬스터 등 스폰 패킷 전송, 데미지 연산은 서버에서)
		public void EnterMultiGame(GameObject gameObject)
		{
			if (gameObject == null)
				return;

			GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

			if(type == GameObjectType.Player) // 플레이어
            {
				Player player = gameObject as Player;
				_players.Add(player.Id, player);

				player.Room = this;

				player.RefreshAdditionalStat(); // 플레이어 스탯 초기화

				// 본인한테 정보 전송
				{

					// 입장 패킷은 본인한테만 전송
					S_EnterGame enterPacket = new S_EnterGame();
					enterPacket.Player = player.Info;

					player.Session.Send(enterPacket);


					S_Spawn spawnPacket = new S_Spawn();
					foreach (Player p in _players.Values)
					{
						if (player != p)
							spawnPacket.Objects.Add(p.Info);
					}
					foreach (Monster m in _monsters.Values)
					{
						spawnPacket.Objects.Add(m.Info);
					}
					player.Session.Send(spawnPacket);
				}
			}

			// 타인한테 정보 전송
			{
				// 타 클라이언트에서 플레이어를 스폰시키기위해 스폰 패킷을 모두에게 전송
				S_Spawn spawnPacket = new S_Spawn();
				gameObject.Info.PosInfo.PosX = gameObject.PosInfo.PosX;
				gameObject.Info.PosInfo.PosY = 0;
				gameObject.Info.PosInfo.PosZ = 0;

				spawnPacket.Objects.Add(gameObject.Info);
				foreach (Player p in _players.Values)
				{
					if (p.Id != gameObject.Id)
						p.Session.Send(spawnPacket);		
				}
			}
		}

		// 방에서 나갈때 처리
		public void LeaveGame(int objectId)
		{

			GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

			
			if(type == GameObjectType.Player)
			{
				Player player = null;
				if (_players.TryGetValue(objectId, out player) == false)
					return;

				_players.Remove(objectId);
				player.Room = null;

				// 본인한테 정보 전송
				{
					S_LeaveGame leavePacket = new S_LeaveGame();
					player.Session.Send(leavePacket);
				}
			}

			// 타인한테 정보 전송
			{
				S_Despawn despawnPacket = new S_Despawn();
				despawnPacket.ObjectIds.Add(objectId);
				foreach (GameObject go in _players.Values)
				{
					Player p = go as Player;
					if (objectId != p.Id)
						p.Session.Send(despawnPacket);
				}
			}

		}

		// 데미지 처리
		public void HandleDemage(Player player, C_ChangeHp hpPacket)
		{
			// hp 가 변한 플레이어에 대해 찾고 그 플레이어가 있는 경우 hp 변화에 대해 다른 클라에게 통보
			Player damagedPlayer;
			if(_players.TryGetValue(player.Id, out damagedPlayer))
            {
				S_ChangeHp damagePakcet = new S_ChangeHp()
				{
					ObjectId = damagedPlayer.Id,
					Hp = hpPacket.Hp,
				};

				Broadcast(damagePakcet);
            }
		}

		public Player FindPlayer(Func<GameObject, bool> condition)
        {

			foreach (Player player in _players.Values)
            {
				if (condition.Invoke(player))
					return player;
            }

			return null;
        }

		public void Broadcast(IMessage packet)
        {
			
			foreach (Player p in _players.Values)
            {
				p.Session.Send(packet);
            }
            
        }


	}
}
