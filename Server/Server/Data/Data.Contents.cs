using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;


// 제이슨 파일과 변수명을 동일하게 작성해야함에 유의

/*
 * 게임 내에서 사용되는 제이슨 데이터들을 dictionary 형태로 변환해놓은 데이터 정보들
 * 
 * 1. 스탯
 * 2. 스킬
 * 3. 아이템
 * 4. 보스 몬트서
 * 5. 스테이지 보상
 * 
 * 정보들을 dictionary 화 하여 사용하며 해당 스크립트에 dictionary의 구조에 대해 작성하였다.
 */


namespace Server.Data
{
	#region Stat
	[Serializable]
	public class StatData : ILoader<int, StatInfo>
	{
		public List<StatInfo> stats = new List<StatInfo>();

		public Dictionary<int, StatInfo> MakeDict()
		{
			Dictionary<int, StatInfo> dict = new Dictionary<int, StatInfo>();
			foreach (StatInfo stat in stats)
			{
				stat.Hp = stat.MaxHp;
				dict.Add(stat.Level, stat);
			}
			return dict;
		}
	}
	#endregion



	#region Skill
	[Serializable]
	public class Skill
	{
		public int id; // 스킬 id
		public string name;
		public float cooldown;
		public float damage; // 스킬 계수
		public SkillType skillType; // 스킬 타입 1. 기본 공격 2. 전투 스킬 3. 궁극기
	}

	[Serializable]
	public class SkillData : ILoader<int, Skill>
	{
		public List<Skill> skills = new List<Skill>();

		public Dictionary<int, Skill> MakeDict()
		{
			Dictionary<int, Skill> dict = new Dictionary<int, Skill>();
			foreach (Skill skill in skills)
				dict.Add(skill.id, skill);
			return dict;
		}
	}
	#endregion



	#region Item
	[Serializable]
	public class ItemData
	{
		public int id;
		public string name;
		public ItemType itemType;
	}

	public class WeaponData : ItemData
    {
		public WeaponType weaponType;
		public int damage;

    }

	public class ArmorData : ItemData
    {
		public ArmorType armorType;
		public int defence;

    }

	public class ConsumableData : ItemData
    {
		public ConsumableType consumableType;
		public int maxCount;

    }

	[Serializable]
	public class ItemLoader : ILoader<int, ItemData>
	{
		public List<WeaponData> weapons = new List<WeaponData>();
		public List<ArmorData> armors = new List<ArmorData>();
		public List<ConsumableData> consumables = new List<ConsumableData>();

		public Dictionary<int, ItemData> MakeDict()
		{
			Dictionary<int, ItemData> dict = new Dictionary<int, ItemData>();
			foreach (ItemData item in weapons)
            {
				item.itemType = ItemType.Weapon;
				dict.Add(item.id, item);
            }
			foreach (ItemData item in armors)
			{
				item.itemType = ItemType.Armor;
				dict.Add(item.id, item);
			}
			foreach (ItemData item in consumables)
			{
				item.itemType = ItemType.Consumable;
				dict.Add(item.id, item);
			}

			return dict;
		}
	}
	#endregion



	#region Stage
	[Serializable]
	public class StageData
	{
		// 스테이지 number(id)
		public string stageName;
		public List<RewardData> rewards;
	}

	[Serializable]
	public class RewardData
	{
		// 보상 목록 1. item 도감 번호, 2. 개수
		public int id;
		public int count;
	}

	[Serializable]
	public class StageLoader : ILoader<string, StageData>
	{
		public List<StageData> stages = new List<StageData>();

		public Dictionary<string, StageData> MakeDict()
		{
			Dictionary<string, StageData> dict = new Dictionary<string, StageData>();

			foreach (StageData stage in stages)
			{
				dict.Add(stage.stageName, stage);
			}

			return dict;
		}
	}

	#endregion


	#region QuestReward
	[Serializable]
	public class QuestRewardData
	{
		// 퀘스트 ID
		public int questId;
		public List<RewardData> rewards;
	}

	[Serializable]
	public class QuestRewardLoader : ILoader<int, QuestRewardData>
	{
		public List<QuestRewardData> questRewards = new List<QuestRewardData>();

		public Dictionary<int, QuestRewardData> MakeDict()
		{
			Dictionary<int, QuestRewardData> dict = new Dictionary<int, QuestRewardData>();

			foreach (QuestRewardData questReward in questRewards)
			{
				dict.Add(questReward.questId, questReward);
			}

			return dict;
		}
	}

	#endregion
}
