using Server;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

/*
 * 멀티플레이시 플레이어 매칭을 위한 매칭 큐 클래스
 */


class MatchQueue
{
	#region Singleton
	static MatchQueue _instance = new MatchQueue();
	public static MatchQueue Instance { get { return _instance; } }
	#endregion

	List<Player> _matchQueue = new List<Player>();

	private static int MatchPlayerNum = 2;

	object _lock = new object();

	public void Add(Player player)
	{
		
		lock(_lock)
        {
			_matchQueue.Add(player);
        }
	}

	public void Remove(Player player)
    {
		lock (_lock)
		{
			if (_matchQueue.Contains(player))
				_matchQueue.Remove(player);
		}

	}

	public List<Player> MatchedPlayers()
    {
		if(_matchQueue.Count >= MatchPlayerNum) // TEMP 플레이어 2명 이상
        {
			List<Player> matchedPlayers = new List<Player>();
			List<Player> _matchQueue_cpy = new List<Player>(_matchQueue);
			foreach(Player player in _matchQueue)
            {
				matchedPlayers.Add(player);
				_matchQueue_cpy.Remove(player);

				// 매칭 큐에서 방에 입장할 플레이어들을 추출
				if (matchedPlayers.Count == MatchPlayerNum)
					break;
            }

			_matchQueue = _matchQueue_cpy;
            Console.WriteLine($"[Log] 대기중인 플에이어 {_matchQueue.Count} 명");
			return matchedPlayers;
        }

		return null;
    }
}

