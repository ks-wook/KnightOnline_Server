using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Game;
using Server.Session.Utils;
using System;
using System.Collections.Generic;

/*
 * DB 갱신과 관련된 처리를 하는 스크립트로 JobSerializer를 통해 모든 작업들이 순서대로
 * 누락되지 않도록 구조되어 있다.
 * 
 * 플레이어 정보 갱신, 아이템, 스테이지, 퀘스트 등 테이블을 수정하는 모든 작업들을 해당
 * 클래스를 통해 처리한다.
 */


namespace Server.DB
{
    public partial class DbTransaction : JobSerializer
    {
        public static DbTransaction Instance { get; } = new DbTransaction();

        public static void SavePlayerStatus_AllInOne(Player player, GameRoom room)
        {
            if (player == null || room == null)
                return;
            
            PlayerDb playerDb = new PlayerDb();
            playerDb.PlayerDbId = player.PlayerDbId;
            playerDb.Hp = player.Stat.Hp;

            Instance.Push(() =>
            {
                using(AppDbContext db = new AppDbContext())
                {
                    db.Entry(playerDb).State = EntityState.Unchanged;
                    db.Entry(playerDb).Property("Hp").IsModified = true;
                    bool success = db.SaveChangesEx();
                    if(success)
                    {
                        room.Push(() => Console.WriteLine($"[Log] Hp Saved({playerDb.Hp})"));
                    }
                }
            });
        }

        public static void SavePlayerStatus_Step1(Player player, GameRoom room)
        {
            if (player == null || room == null)
                return;

            PlayerDb playerDb = new PlayerDb();
            playerDb.PlayerDbId = player.PlayerDbId;
            playerDb.Hp = player.Stat.Hp;
            Instance.Push<PlayerDb, GameRoom>(SavePlayerStatus_Step2, playerDb, room);
        }
        public static void SavePlayerStatus_Step2(PlayerDb playerDb, GameRoom room)
        {
            using (AppDbContext db = new AppDbContext())
            {
                db.Entry(playerDb).State = EntityState.Unchanged;
                db.Entry(playerDb).Property("Hp").IsModified = true;
                bool success = db.SaveChangesEx();
                if (success)
                {
                    room.Push(SavePlayerStatus_Step3, playerDb.Hp);
                }
            }
        }

        // 완료 통보
        public static void SavePlayerStatus_Step3(int hp)
        {
            Console.WriteLine($"[Log] Hp Saved {hp}");
        }

        // 플레이어 보상 획득시 DB 처리(아이템)
        public static void PlayerGetReward(Player player, List<RewardData> rewards)
        {
            if (rewards == null)
                return;

            List<ItemDb> itemDbs = new List<ItemDb>();

            for(int i = 0; i < rewards.Count; i++)
            {
                ItemDb itemDb = new ItemDb()
                {
                    TemplateId = rewards[i].id,
                    Count = rewards[i].count,
                    OwnerDbId = player.PlayerDbId
                };

                
                itemDbs.Add(itemDb); // db에 저장될 아이템
            }

            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    for(int i = 0; i < itemDbs.Count; i++)
                        db.Items.Add(itemDbs[i]); // db에 추가

                    bool success = db.SaveChangesEx(); // 저장
                    if (success)
                    {
                        // 로컬 인벤토리에 저장
                        S_AddItem itemPacket = new S_AddItem();

                        for (int j = 0; j < itemDbs.Count; j++)
                        {
                            Item newItem = Item.MakeItem(itemDbs[j]);

                            ItemInfo itemInfo = new ItemInfo();
                            itemInfo.MergeFrom(newItem.Info);

                            player.Inven.Add(newItem);
                            itemPacket.Items.Add(itemInfo);
                            Console.WriteLine($"[Log] {newItem.TemplateId} 생성");
                        }

                        // 클라에게 보상이 지급됨을 알림
                        // 클라에서 해당 패킷을 받고 보상획득 UI를 출력한다.
                        player.Session.Send(itemPacket);

                    }
                }
            });
        }

        

        // 퀘스트 클리어시 DB 처리
        public static void QuestDataChange(int questDbId, C_QuestChange questPacket)
        {
            QuestDb questDb = new QuestDb()
            {
                QuestDbId = questDbId,
                IsCleared = questPacket.IsCleared,
                IsRewarded = questPacket.IsRewarded,
            };

            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Entry(questDb).State = EntityState.Unchanged;
                    db.Entry(questDb).Property(nameof(QuestDb.IsCleared)).IsModified = true;
                    db.Entry(questDb).Property(nameof(QuestDb.IsRewarded)).IsModified = true;

                    bool success = db.SaveChangesEx(); // 저장

                    if (!success)
                    {
                        Console.WriteLine("[Error] 퀘스트 데이터 갱신 실패");
                    }
                }
            });
        }

        // 플레이어 경험치 획득시 DB 처리 (반환 값으로 현재 플레이어 레벨)
        public static int PlayerGetExp(Player player, int totalExp)
        {
            if (player == null)
                return 0;

            player.Stat.TotalExp = totalExp;

            PlayerDb playerDb = new PlayerDb()
            {
                PlayerDbId = player.PlayerDbId,
                TotalExp = totalExp
            };

            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Entry(playerDb).State = EntityState.Unchanged;
                    db.Entry(playerDb).Property(nameof(PlayerDb.TotalExp)).IsModified = true;

                    bool success = db.SaveChangesEx(); // 저장

                    if (!success)
                    {
                        Console.WriteLine("[Error] 플레이어 경험치 데이터 갱신 실패");
                    }
                }
            });

            StatInfo nextStatInfo = null;
            DataManager.StatDict.TryGetValue(player.Stat.Level + 1, out nextStatInfo);

            if (totalExp >= nextStatInfo.TotalExp)  // 레벨업 여부 확인
            {
                PlayerLevelUp(player, ++player.Stat.Level);
            }

            return player.Stat.Level;
        }

        // 플레이어 레벨업시 DB 처리
        public static void PlayerLevelUp(Player player, int newLevel)
        {
            // 데이터 매니저로부터 다음 레벨의 스탯을 가져온 후 DB의 플에이어 스탯 데이터 갱신
            StatInfo newStat = null;
            DataManager.StatDict.TryGetValue(newLevel, out newStat);

            PlayerDb playerDb = new PlayerDb()
            {
                PlayerDbId = player.PlayerDbId,
                Level = newLevel,
                Attack = newStat.Attack,
                MaxHp = newStat.MaxHp,
                Hp = newStat.MaxHp,
            };

            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Entry(playerDb).State = EntityState.Unchanged;
                    db.Entry(playerDb).Property(nameof(PlayerDb.Level)).IsModified = true;
                    db.Entry(playerDb).Property(nameof(PlayerDb.Attack)).IsModified = true;
                    db.Entry(playerDb).Property(nameof(PlayerDb.MaxHp)).IsModified = true;
                    db.Entry(playerDb).Property(nameof(PlayerDb.Hp)).IsModified = true;

                    bool success = db.SaveChangesEx(); // 저장

                    if (!success)
                    {
                        Console.WriteLine("[Error] 플레이어 스탯 데이터 갱신 실패");
                    }
                }
            });

        }

    }
}
