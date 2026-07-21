using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EyeMoTMouseModule
{
    public class DontDestroySingleObject : MonoBehaviour
    {
        public static DontDestroySingleObject Instance
        {
            get; private set;
        }

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            this.transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }
}
