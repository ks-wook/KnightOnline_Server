using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	public class GameLogic : JobSerializer
	{
		public static GameLogic Instance { get; } = new GameLogic();

		object _lock = new object();
		Dictionary<int, GameRoom> _rooms = new Dictionary<int, GameRoom>();
		int _roomId = 1;

		public void Update()
        {
			Flush();
        }

		// 방 자동 생성
		public GameRoom Add()
		{
			GameRoom gameRoom = new GameRoom();

			lock (_lock)
			{
				gameRoom.RoomId = _roomId;
				_rooms.Add(_roomId, gameRoom);
				_roomId++;
			}

			return gameRoom;
		}

		// 방의 id를 지정하여 생성
		public GameRoom Add(int roomId)
		{
			GameRoom gameRoom = new GameRoom();

			lock (_lock)
			{
				gameRoom.RoomId = roomId;
				_rooms.Add(roomId, gameRoom);
				// _roomId++;
			}

			return gameRoom;
		}

		public bool Remove(int roomId)
		{
			lock (_lock)
			{
				return _rooms.Remove(roomId);
			}
		}

		public GameRoom Find(int roomId)
		{
			lock (_lock)
			{
				GameRoom room = null;
				if (_rooms.TryGetValue(roomId, out room))
                {
					return room;
				}
				

				return null;
			}
		}
	}
}
