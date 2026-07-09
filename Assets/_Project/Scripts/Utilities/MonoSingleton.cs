using UnityEngine;

namespace MechaSurvivor.Utilities
{
    /// <summary>
    /// 씬에 하나만 존재하는 MonoBehaviour 싱글턴 베이스.
    /// 파생 클래스는 Awake/OnDestroy 오버라이드 시 반드시 base를 호출한다.
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<T>();
                }
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
