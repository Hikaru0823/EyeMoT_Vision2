using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Heatmaps
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> _executionQueue = new();

        public void Update()
        {
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                    _executionQueue.Dequeue().Invoke();
            }
        }

        public static void Enqueue(Action action)
        {
            lock (_executionQueue)
                _executionQueue.Enqueue(action);
        }
    }
}
