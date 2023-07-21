using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class JobSerializer
    {
		Queue<IJob> _jobQueue = new Queue<IJob>();
		object _lock = new object();
		bool _flush = false;

		// Job을 내부에서 만들 수 있다
		public void Push(Action action) { Push(new Job(action)); }
		public void Push<T1>(Action<T1> action, T1 t1) { Push(new Job<T1>(action, t1)); }
		public void Push<T1, T2>(Action<T1, T2> action, T1 t1, T2 t2) { Push(new Job<T1, T2>(action, t1, t2)); }
		public void Push<T1, T2, T3>(Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3) { Push(new Job<T1, T2, T3>(action, t1, t2, t3)); }


		public void Push(IJob job)
		{
			bool flush = false;

			lock (_lock)
			{
				_jobQueue.Enqueue(job);
				if (_flush == false)
					flush = _flush = true;
			}

			if (flush)
				Flush();
		}

		public void Flush()
		{
			while (true)
			{
				IJob job = Pop();
				if (job == null)
					return;

				job.Excute();
			}
		}

		IJob Pop()
		{
			lock (_lock)
			{
				if (_jobQueue.Count == 0)
				{
					_flush = false;
					return null;
				}
				return _jobQueue.Dequeue();
			}
		}
	}
}
