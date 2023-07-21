using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.DB
{
    [Table("Account")]
    public class AccountDb
    {
        public int AccountDbId { get; set; }
        public string AccountName { get; set; }
        public ICollection<PlayerDb> Players { get; set; }
    }

    [Table("Player")]
    public class PlayerDb
    {
        public int PlayerDbId { get; set; } // DB Name + Id : db내에서 관리하는 primary key
        public string PlayerName { get; set; }

        [ForeignKey("Account")]
        public int AccountDbId { get; set; }
        public AccountDb Account { get; set; }


        public ICollection<ItemDb> Items { get; set; }

        public int Level { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int Attack { get; set; }
        public int Speed {get; set;}
        public int TotalExp { get; set; }
    }

    [Table("Item")]
    public class ItemDb
    {
        public int ItemDbId { get; set; }
        public int TemplateId { get; set; }
        public int Count { get; set; }
        public int Slot { get; set; }
        public bool Equipped { get; set; } = false;

        [ForeignKey("Owner")]
        public int? OwnerDbId { get; set; }
        public PlayerDb Owner { get; set; }
    }



    [Table("Quest")]
    public class QuestDb
    {
        public int QuestDbId { get; set; }
        public int TemplatedId { get; set; }
        public bool IsCleared { get; set; }
        public bool IsRewarded { get; set; }

        [ForeignKey("Owner")]
        public int? OwnerDbId { get; set; }
        public PlayerDb Owner { get; set; }
    }

    [Table("MainStage")]
    public class MainStageDb
    {
        public int MainStageDbId { get; set; }
        public string StageName { get; set; }

        [ForeignKey("Owner")]
        public int? OwnerDbId { get; set; }
        public PlayerDb Owner { get; set; }
    }
}
