using System.Collections;
using System.Collections.Generic;

namespace AdInfinitum
{
    public class Coroutine : IResumable
    {
        private readonly Stack<IEnumerator> _executionStack = new Stack<IEnumerator>();

        public static Coroutine Create(IEnumerator target)
        {
            return new Coroutine(target);
        }

        public Coroutine(IEnumerator target)
        {
            _executionStack.Push(target);
        }

        public void Resume()
        {
            if (IsEnded())
            {
                return;
            }

            IEnumerator target = _executionStack.Peek();
            bool isSuccessfullyAdvanced = target.MoveNext();
            if (isSuccessfullyAdvanced)
            {
                object yieldReturnValue = target.Current;
                if (yieldReturnValue is IEnumerator)
                {
                    _executionStack.Push(yieldReturnValue as IEnumerator);
                }
            }
            else
            {
                _executionStack.Pop();
            }
        }

        public bool IsEnded()
        {
            return _executionStack.Count == 0;
        }
    }
}