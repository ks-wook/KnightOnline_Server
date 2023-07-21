using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Data
{
    [Serializable]
    public class ServerConfig
    {
        public string dataPath; // json 파일과 이름을 맞춰줘야 함
        public string connectionString;
    }


    // config 파일 로드
    public class ConfigManager
    {
        public static ServerConfig Config { get; private set; }

        public static void LoadConfig()
        {
            string text = File.ReadAllText("config.json");
            Config =  Newtonsoft.Json.JsonConvert.DeserializeObject<ServerConfig>(text);
        }

    }
}
