using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	public class ObjectManager
	{
		public static ObjectManager Instance { get; } = new ObjectManager();

		static object _lock = new object();
		static Dictionary<int, Player> _players = new Dictionary<int, Player>();
		static Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();

		// [ ........ | ........ | ........ | ........ ] 32 bits
		// [ objType  | count(24) ] 32 bits
		int _counter = 1; // TODO

		

		public T Add<T>() where T : GameObject, new()
        {
			T gameObject = new T();
			lock(_lock)
            {
				gameObject.Id = GenerateId(gameObject.ObjectType); // id 발급
				if(gameObject.ObjectType == GameObjectType.Player)
                {
					_players.Add(gameObject.Id, gameObject as Player);
                    Console.WriteLine($"[Log] Player id : {gameObject.Id}");
                }
				else if (gameObject.ObjectType == GameObjectType.Monster)
				{
					_monsters.Add(gameObject.Id, gameObject as Monster);
					Console.WriteLine($"[Log] Monster id : {gameObject.Id}");

				}
			}

			return gameObject;
		}

		int GenerateId(GameObjectType type)
        {
			lock(_lock)
            {
				return ((int)type << 24) | (_counter++);
            }
        }

		public static GameObjectType GetObjectTypeById(int id)
        {
			int type = (id >> 24) & 0x7F;// 0111 1111
			return (GameObjectType)type;
        }

		public bool Remove(int objectId)
		{
			GameObjectType objectType = GetObjectTypeById(objectId);

			lock (_lock)
			{
				if (objectType == GameObjectType.Player)
					return _players.Remove(objectId);
			}

			return false;
		}

		public static GameObject Find(int objectId)
		{
			GameObjectType objectType = GetObjectTypeById(objectId);

			lock (_lock)
			{
				if(objectType == GameObjectType.Player)
                {
					Player player = null;
					if (_players.TryGetValue(objectId, out player))
						return player;
				}
				if (objectType == GameObjectType.Monster)
				{
					Monster monster = null;
					if (_monsters.TryGetValue(objectId, out monster))
						return monster;
				}

				return null;
			}
		}
	}
}
