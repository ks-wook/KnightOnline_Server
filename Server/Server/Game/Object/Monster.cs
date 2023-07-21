using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using System.Collections.Generic;

namespace Server.Game
{
    
    class Monster : GameObject
    {
        public override GameObjectType ObjectType { get; protected set; } = GameObjectType.Monster;

        public void Init(int level)
        {
            StatInfo monsterStat;
            DataManager.StatDict.TryGetValue(level, out monsterStat);
            Stat.MergeFrom(monsterStat);

            Stat.Hp = Stat.MaxHp;
            State = CreateureState.Idle;

            // TEMP
            this.PosInfo.PosX = 0;
            this.PosInfo.PosY = 0;
            this.PosInfo.PosZ = 0;
        }

        public override void OnDamaged(GameObject attacker, int damage)
        {
            base.OnDamaged(attacker, damage);

            
        }

        public override void OnDead(GameObject attacker)
        {
            base.OnDead(attacker);

            // TEMP : 보상은 스테이지 클리어를 통해 획득, 임시로 몬스터 클래스에 선언

            // TEMP 보스 몬스터를 잡으면 스테이지 클리어
            // TEMP 보상 패킷 전송
            /*StageData stageData = null;
            DataManager.StageDict.TryGetValue(1, out stageData);

            Player player = attacker as Player;
            // DbTransaction.PlayerGetReward(player, stageData.rewards, player.Room);*/
        }

    }
}
