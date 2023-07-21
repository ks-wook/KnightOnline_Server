using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	public partial class GameRoom :JobSerializer
	{
		
		// 방에서의 이동 처리
		public void HandleMove(Player player, C_Move movePacket)
        {
			if (player == null)
				return;
			
			// 서버에서의 플레이어 이동 처리
			ObjectInfo info = player.Info;
			info.PosInfo = movePacket.PosInfo;

			// 다른 클라이언트에게 같은 정보를 전달
			S_Move resMovePacket = new S_Move();
			resMovePacket.ObjectId = player.Info.ObjectId;
			resMovePacket.PosInfo = movePacket.PosInfo;
			resMovePacket.PosInfo.DirInfo = movePacket.PosInfo.DirInfo;

			Broadcast(resMovePacket);
		}

		// 스킬 사용 처리
		public void HandleSkill(Player player, C_Skill skillPacket)
        {
			if (player == null)
				return;

			ObjectInfo info = player.Info;

			// 스킬 사용 정보 브로드캐스트
			info.PosInfo.State = CreateureState.Nomalattack;
			S_Skill skill = new S_Skill() { Info = new SkillInfo() };
			skill.ObjectId = info.ObjectId;
			skill.Info.SkillId = skillPacket.Info.SkillId;
			Broadcast(skill);
		}

	}
}
