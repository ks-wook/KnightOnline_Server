using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Game;
using Server.Session.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


/*
 * Db에 알리고 답변을 받을 필요는 없는 경우의 처리들을 이곳에서 처리한다
 * 
 */



namespace Server.DB
{
    public partial class DbTransaction : JobSerializer
    {
        public static void EquipItemNoti(Player player, Item item)
        {
            if (player == null || item == null)
                return;

            ItemDb itemDb = new ItemDb()
            {
                ItemDbId = item.ItemDbId,
                Equipped = item.Equipped
            };

            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Entry(itemDb).State = EntityState.Unchanged;
                    db.Entry(itemDb).Property(nameof(ItemDb.Equipped)).IsModified = true;

                    bool success = db.SaveChangesEx(); // 저장

                    if (success)
                    {
                        // TODO : 실패시 예외처리
                    }
                }
            });
        }

        public static void QuestGetNoti(Player player, int questTemplatedId)
        {
            if (player == null || questTemplatedId == 0)
                return;

            QuestDb questDb = new QuestDb()
            {
                // QuestDbId는 유니크 설정이기 때문에 자동 할당
                TemplatedId = questTemplatedId,
                IsCleared = false,
                OwnerDbId = player.PlayerDbId
            };

            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Quests.Add(questDb);

                    bool success = db.SaveChangesEx(); // 저장

                    if (success)
                    {
                        // TODO : 실패시 예외처리
                    }
                }
            });
        }

        // 스테이지 클리어 정보를 db에 갱신
        public static void StageClearNoti(Player player, string stageName)
        {
            if (player == null)
                return;

            MainStageDb mainStageDb = new MainStageDb()
            {
                // MainStageDbId는 유니크 설정이기 때문에 자동 할당
                StageName = stageName,
                OwnerDbId = player.PlayerDbId
            };


            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    bool isDuplicate = db.MainStages.Any(mainStage => mainStage.StageName == stageName && mainStage.OwnerDbId == player.PlayerDbId);

                    if (isDuplicate) // 클리어 데이터 이미 존재 시
                        return;

                    db.MainStages.Add(mainStageDb);

                    bool success = db.SaveChangesEx(); // 저장

                    if (success)
                    {
                        // TODO : 실패시 예외처리
                    }
                }
            });

        }
    }
}
