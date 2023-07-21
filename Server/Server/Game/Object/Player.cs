using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DB;
using Server.Session.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Game
{
    public class Player : GameObject
    {

        public int PlayerDbId { get; set; }
        public ClientSession Session { get; set; }
        public Inventory Inven { get; private set; } = new Inventory();

		// 기본 능력치
		public int WeaponDamage { get; private set; }
		public int ArmorDefence { get; private set; }


		// 증가한 능력치를 포함한 능력치
        public override int TotalAttack { get { return Stat.Attack + WeaponDamage; } }
        public override int TotalDefence { get { return ArmorDefence; } }


        public Player()
        {
            ObjectType = GameObjectType.Player;

            // 스탯이나 수치 관련된 부분은 모두 다른 파일로 빼서 관리
        }

        public override void OnDamaged(GameObject attacker, int damage)
        {
            base.OnDamaged(attacker, damage);
            
            
        }

        public override void OnDead(GameObject attacker)
        {
            base.OnDead(attacker);
        }

        public void HandleEquipItem(C_EquipItem equipPacket)
        {

			// 장착하려는 아이템이 플레이어의 인벤에 있는지 확인
			Item item = this.Inven.Get(equipPacket.ItemDbId);

			if (item == null)
				return;

			// 소비형 아이템은 착용 불가능
			if (item.ItemType == ItemType.Consumable)
				return;

			// 착용 요청이라면, 겹치는 부위를 해제해야함
			if (equipPacket.Equipped)
			{
				Item unequipItem = null;
				if (item.ItemType == ItemType.Weapon)
				{
					unequipItem = this.Inven.Find(
						i => i.Equipped && i.ItemType == ItemType.Weapon);
				}
				else if (item.ItemType == ItemType.Armor)
				{
					// 방어구인 경우 부위까지 같은지 봐야함.
					ArmorType armorType = ((Armor)item).ArmorType;
					unequipItem = this.Inven.Find(
						i => i.Equipped && i.ItemType == ItemType.Armor
						&& ((Armor)i).ArmorType == armorType);
				}

				if (unequipItem != null) // 이미 착용중인 아이템이 있는 경우
				{
					// 로컬 메모리(서버) 선적용
					unequipItem.Equipped = false; // 장비 해제

					// DB에 알림 -> 응답은 받을 필요 없음
					DbTransaction.EquipItemNoti(this, unequipItem);

					// 클라에 통보
					S_EquipItem eqipOkItem = new S_EquipItem();
					eqipOkItem.ItemDbId = unequipItem.ItemDbId;
					eqipOkItem.Equipped = unequipItem.Equipped;
					this.Session.Send(eqipOkItem);
				}


			}


			{
				// 로컬 메모리(서버) 선적용
				// 로컬 메모리 선적용은 언제?
				// 중요하지 않은 정보일 때
				item.Equipped = equipPacket.Equipped;

				// DB에 알림 -> 응답은 받을 필요 없음
				DbTransaction.EquipItemNoti(this, item);

				// 클라에 통보
				S_EquipItem eqipOkItem = new S_EquipItem();
				eqipOkItem.ItemDbId = equipPacket.ItemDbId;
				eqipOkItem.Equipped = equipPacket.Equipped;
				this.Session.Send(eqipOkItem);
			}

			RefreshAdditionalStat();
		}
		public void RefreshAdditionalStat()
		{
			WeaponDamage = 0;
			ArmorDefence = 0;

			foreach(Item item in Inven.items.Values)
            {
				if (item.Equipped == false)
					continue;

				switch(item.ItemType)
                {
					case ItemType.Weapon:
						WeaponDamage += ((Weapon)item).Damage;
						break;
					case ItemType.Armor:
						ArmorDefence += ((Armor)item).Defence;
						break;
				}
            }
		}
	
		public void HandleQuest(C_QuestChange questPacket, int playerDbId)
        {
			if (questPacket.IsCleared == false) // 퀘스트 추가
			{
				// DB에 퀘스트 추가 -> 응답 받을 필요는 없음
				DbTransaction.QuestGetNoti(this, questPacket.QuestTemplatedId);

				S_QuestChange questChange = new S_QuestChange();
				questChange.QuestTemplatedId = questPacket.QuestTemplatedId;
				questChange.IsCleared = false;

			}
			else if (questPacket.IsCleared && !questPacket.IsRewarded) // 퀘스트 수정(퀘스트 클리어 처리)
			{
				// DB 갱신
				S_QuestChange questChange = new S_QuestChange();
				questChange.QuestTemplatedId = questPacket.QuestTemplatedId;

				using (AppDbContext db = new AppDbContext())
                {
					QuestDb findQuest = db.Quests
						.Where(q => q.OwnerDbId == playerDbId && q.TemplatedId == questPacket.QuestTemplatedId).FirstOrDefault();
					
					if(findQuest != null)
                    {
						DbTransaction.QuestDataChange(findQuest.QuestDbId, questPacket);
						questChange.IsCleared = true;
						questChange.IsRewarded = false;
					}


				}

				this.Session.Send(questChange);

			}
			else if(questPacket.IsCleared && questPacket.IsRewarded) // 퀘스트 보상 획득 처리
			{
				// DB 갱신
				S_QuestChange questChange = new S_QuestChange();
				questChange.QuestTemplatedId = questPacket.QuestTemplatedId;

				using (AppDbContext db = new AppDbContext())
				{
					QuestDb findQuest = db.Quests
						.Where(q => q.OwnerDbId == playerDbId && q.TemplatedId == questPacket.QuestTemplatedId).FirstOrDefault();

					if (findQuest != null)
					{
						DbTransaction.QuestDataChange(findQuest.QuestDbId, questPacket);
						questChange.IsCleared = true;
						questChange.IsRewarded = true;
					}


				}

				this.Session.Send(questChange);


				// 퀘스트 보상 데이터 획득
				{
					QuestRewardData rewardData;
					DataManager.QuestRewardDict.TryGetValue(questPacket.QuestTemplatedId, out rewardData);

					if (rewardData != null) // 적합한 퀘스트 보상이 존재한다면
						DbTransaction.PlayerGetReward(this, rewardData.rewards);
				}

			}

		}
	}
}
