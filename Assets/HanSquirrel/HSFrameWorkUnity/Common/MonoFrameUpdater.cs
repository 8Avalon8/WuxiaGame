using System;
using UnityEngine;

namespace HSFrameWork.Common.Inner
{
    public class MonoFrameUpdater : MonoBehaviour, IFrameUpdater
    {
        public event Action OnAppQuit;
        public event Action OnUpdate;

        private void Update()
        {
            if (OnUpdate != null)
                OnUpdate();
        }

        private void OnApplicationQuit()
        {
            if (OnAppQuit != null)
                OnAppQuit();
        }
    }
}
