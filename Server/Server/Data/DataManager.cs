using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Data
{
    public interface ILoader<Key, Value>
    {
        Dictionary<Key, Value> MakeDict();
    }

    public class DataManager
    {
        // 제이슨 파일 로드 후 dictionary 형태로 관리
        public static Dictionary<int, StatInfo> StatDict { get; private set; } = new Dictionary<int, StatInfo>();
        public static Dictionary<int, Data.Skill> SkillDict { get; private set; } = new Dictionary<int, Data.Skill>();
        public static Dictionary<int, Data.ItemData> ItemDict { get; private set; } = new Dictionary<int, Data.ItemData>();
        public static Dictionary<string, Data.StageData> StageDict { get; private set; } = new Dictionary<string, Data.StageData>();
        public static Dictionary<int, Data.QuestRewardData> QuestRewardDict { get; private set; } = new Dictionary<int, Data.QuestRewardData>();

        public static void LoadData()
        {
            StatDict = LoadJson<Data.StatData, int, StatInfo>("StatData").MakeDict();
            SkillDict = LoadJson<Data.SkillData, int, Data.Skill>("SkillData").MakeDict();
            ItemDict = LoadJson<Data.ItemLoader, int, Data.ItemData>("ItemData").MakeDict();
            StageDict = LoadJson<Data.StageLoader, string, Data.StageData>("StageData").MakeDict();
            QuestRewardDict = LoadJson<Data.QuestRewardLoader, int, Data.QuestRewardData>("QuestRewardData").MakeDict();
        }

        static Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
        {
            // config 파일을 통해 데이터 경로 획득
            string text = File.ReadAllText($"{ConfigManager.Config.dataPath}/{path}.json");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text);
        }
    }
}
