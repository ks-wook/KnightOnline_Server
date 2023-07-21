using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    // 게임 서버에서 들고있는 가상의 inventory
    public class Inventory
    {
        public Dictionary<int, Item> items = new Dictionary<int, Item>();

        private int _slotCount = 0;
        public void Add(Item item)
        {
            item.Slot = _slotCount++;
            items.Add(item.ItemDbId, item);
        }

        public Item Get(int itemDbId)
        {
            Item item = null;
            items.TryGetValue(itemDbId, out item);
            return item;
        }

        public Item Find(Func<Item, bool> condition)
        {
            foreach(Item item in items.Values)
            {
                if (condition.Invoke(item))
                    return item;
            }

            return null;
        }

        
    }
}
