using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Data;
using Server.DB;
using Server.Game;
using ServerCore;
using System;

class PacketHandler
{
	// 멀티 플레이 시 플레이어 이동 동기화 패킷 처리
	public static void C_MoveHandler(PacketSession session, IMessage packet)
	{
		C_Move movePacket = packet as C_Move;
		ClientSession clientSession = session as ClientSession;


        // Console.WriteLine($"C_Move Pos : ({movePacket.PosInfo.PosX}, {movePacket.PosInfo.PosY}, {movePacket.PosInfo.PosZ})");

		Player player = clientSession.MyPlayer;
		if (player == null)
			return;

		GameRoom room = player.Room;
		if (room == null)
			return;

		room.Push(room.HandleMove, player, movePacket);
	}

	// 멀티 플레이 시 플레이어 스킬 사용 패킷 처리
	public static void C_SkillHandler(PacketSession session, IMessage packet)
    {
		C_Skill skillPacket = packet as C_Skill;
		ClientSession clientSession = session as ClientSession;


		// null 인경우를 대비해 값을 변수에 대입 후 사용
		Player player = clientSession.MyPlayer;
		if (player == null)
			return;

		GameRoom room = player.Room;
		if (room == null)
			return;


		room.Push(room.HandleSkill, player, skillPacket);
	}

	// 플레이어 hp 변화 패킷 처리
	public static void C_ChangeHpHandler(PacketSession session, IMessage packet)
	{
		C_ChangeHp changePacket = packet as C_ChangeHp;
		ClientSession clientSession = session as ClientSession;


		// null 인경우를 대비해 값을 변수에 대입 후 사용
		Player player = clientSession.MyPlayer;
		if (player == null)
			return;

		GameRoom room = player.Room;
		if (room == null)
			return;

		room.Push(room.HandleDemage, clientSession.MyPlayer, changePacket);
	}

	// 플레이어 로그인 요청 패킷 처리
	public static void C_LoginHandler(PacketSession session, IMessage packet)
	{
		C_Login loginPacket = packet as C_Login;
		ClientSession clientSession = session as ClientSession;
		clientSession.HandleLogin(loginPacket);
	}

	// 플레이어 게임 입장 요청 패킷 처리
	public static void C_EnterGameHandler (PacketSession session, IMessage packet)
	{
        Console.WriteLine("[Log] 게임 입장 패킷 수신");
		C_EnterGame enterGamePacket = (C_EnterGame)packet;
		ClientSession clientSession = (ClientSession)session;
		clientSession.HandleEnterGame(enterGamePacket);
	}

	// 게임에서 떠날 때 패킷 처리
	public static void C_LeaveGameHandler(PacketSession session, IMessage packet)
	{
		C_LeaveGame leaveGamePacket = packet as C_LeaveGame;
		ClientSession clientSession = (ClientSession)session;

		GameRoom room = clientSession.MyPlayer.Room;
		if (room != null)
			room.Push(room.LeaveGame, clientSession.MyPlayer.Id);

	}

	// 플레이어 로비 입장 요청 패킷 처리
	public static void C_EnterLobbyHandler(PacketSession session, IMessage packet)
	{
		C_EnterLobby enterLobbyPacket = (C_EnterLobby)packet;
		ClientSession clientSession = (ClientSession)session;
		clientSession.HandleEnterLobby(enterLobbyPacket);
	}

	// 플레이어 캐릭터 생성 요청 패킷 처리
	public static void C_CreatePlayerHandler(PacketSession session, IMessage packet)
	{
		C_CreatePlayer createPlayerPacket = (C_CreatePlayer)packet;
		ClientSession clientSession = (ClientSession)session;
		clientSession.HandleCreatePlayer(createPlayerPacket);
	}

	// 플레이어 메인 스테이지 클리어 요청 패킷 처리
	public static void C_StageClearHandler(PacketSession session, IMessage packet)
    {
        C_StageClear stageClearPacket = (C_StageClear)packet;
		ClientSession clientSession = (ClientSession)session;

		Player player = clientSession.MyPlayer;

		if (player == null) // 플레이어가 존재하지 않는 경우
			return;


		// 스테이지 클리어 관련 데이터 테이블 갱신
		DbTransaction.StageClearNoti(player, stageClearPacket.StageName);



		// 플레이어가 클리어한 던전 정보에 맞는 보상을 준다
		StageData stageData = null;
		DataManager.StageDict.TryGetValue(stageClearPacket.StageName, out stageData);

		if (stageData == null) // 스테이지에 대한 보상 정보가 없는 경우
			return;

		DbTransaction.PlayerGetReward(player, stageData.rewards);
    }

	// 플레이어 장비 착용 요청 패킷 처리
    public static void C_EquipItemHandler(PacketSession session, IMessage packet)
	{
        C_EquipItem equipPacket = (C_EquipItem)packet;
		ClientSession clientSession = (ClientSession)session;

		Player player = clientSession.MyPlayer;
		if (player == null)
			return;

		player.HandleEquipItem(equipPacket);
    }

	// 플레이어 레이드 매칭 요청 패킷 처리
	public static void C_RaidMatchHandler(PacketSession session, IMessage packet)
	{
		C_RaidMatch matchPacket = (C_RaidMatch)packet;
		ClientSession clientSession = (ClientSession)session;


		Player player = clientSession.MyPlayer;
		if (player == null)
			return;


		MatchManager.Instance.MatchReq(player, matchPacket.Req);
	}

	// 플레이어 퀘스트 관련 요청 패킷 처리
	public static void C_QuestChangeHandler(PacketSession session, IMessage packet)
	{
		C_QuestChange questPacket = (C_QuestChange)packet;
		ClientSession clientSession = (ClientSession)session;

		Console.WriteLine("[Log] 퀘스트 정보 변경 요청");

		Player player = clientSession.MyPlayer;
		
		if (player == null)
			return;

		player.HandleQuest(questPacket, clientSession.AccountDbId);

	}

	// 플레이어 경험치 획득 요청 처리
	public static void C_GetExpHandler(PacketSession session, IMessage packet)
	{
		C_GetExp expPacket = (C_GetExp) packet;
		ClientSession clientSession = (ClientSession)session;

		Console.WriteLine("[Log] Player id : " + clientSession.MyPlayer.PlayerDbId + 
			" 경험치 : " + expPacket.TotalExp);

		int preLevel = clientSession.MyPlayer.Stat.Level;
		int newLevel = DbTransaction.PlayerGetExp(clientSession.MyPlayer, expPacket.TotalExp);

		if (newLevel - preLevel > 0) // 레벨업이 이루어진 경우
        {
			S_LevelUp levelPacket = new S_LevelUp()
			{
				NewLevel = newLevel,
				TotalExp = expPacket.TotalExp
			};

			clientSession.Send(levelPacket);
		}
		else if(newLevel == preLevel) // 레벨업이 이루어지지 않은 경우
        {
			S_GetExp getExp = new S_GetExp()
			{
				TotalExp = expPacket.TotalExp
			};

			clientSession.Send(getExp);
        }
		else
        {
            Console.WriteLine("[Error] 플레이어 경험치 처리 예외 발생");
        }
	}

	// 플레이어가 레이드 보스에게 변화를 주었을 때 요청 처리
	public static void C_BossStatChangeHandler(PacketSession session, IMessage packet)
	{
		C_BossStatChange bossStatChange = (C_BossStatChange)packet;
		ClientSession clientSession = (ClientSession)session;

		Player player = clientSession.MyPlayer;

		if (player == null)
			return;

		player.Room.HandleDamage(bossStatChange);
	}
}
