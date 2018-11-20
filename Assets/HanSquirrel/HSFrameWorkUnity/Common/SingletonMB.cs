using UnityEngine;

namespace HSFrameWork.Common
{
    /// <summary>
    /// MonoBehaviour的Singleton
    /// </summary>
    public class SingletonMB<T, TAs> : MonoBehaviour where T : MonoBehaviour, TAs
    {
        private static T _instance;

        private static object _lockObj = new object();

        public static TAs Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                lock (_lockObj)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindObjectOfType(typeof(T));

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<T>();
                            singleton.name = "(singleton) " + typeof(T).ToString();

                            DontDestroyOnLoad(singleton);
                        }
                    }
                    return _instance;
                }
            }
        }

        private static bool applicationIsQuitting = false;

        public void OnDestroy()
        {
            applicationIsQuitting = true;
        }

        public static bool IsDestroy()
        {
            return applicationIsQuitting;
        }
    }
}
