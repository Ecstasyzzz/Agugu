using System.Collections.Generic;

namespace AdInfinitum
{
    public class ParallelCoroutineExecutor
    {
        private readonly List<IResumable> _resumables = new List<IResumable>();

        public void Add(IResumable resumable)
        {
            _resumables.Add(resumable);
        }

        public void Resume()
        {
            foreach (IResumable r in _resumables)
            {
                r.Resume();
            }

            _resumables.RemoveAll(r => r.IsEnded());
        }
    }
}