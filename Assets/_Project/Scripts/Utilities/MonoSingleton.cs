using UnityEngine;

namespace MechaSurvivor.Utilities
{
    /// <summary>
    /// 씬에 하나만 존재하는 MonoBehaviour 싱글턴 베이스.
    /// 파생 클래스는 Awake/OnDestroy 오버라이드 시 반드시 base를 호출한다.
    ///
    /// - Instance 접근 시 씬에 없으면 자동 생성한다(전역 시스템을 씬마다 수동 배치할 필요 없음).
    /// - <see cref="Persistent"/>를 true로 오버라이드하면 DontDestroyOnLoad로 씬 전환에도 생존한다.
    /// - 애플리케이션 종료 중에는 재생성하지 않는다(OnDestroy 순서 문제로 인한 유령 오브젝트 방지).
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static bool _isQuitting;

        public static T Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                if (_isQuitting)
                {
                    return null;
                }

                _instance = FindAnyObjectByType<T>();
                if (_instance == null)
                {
                    var go = new GameObject(typeof(T).Name);
                    _instance = go.AddComponent<T>();
                }

                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        /// <summary>true면 씬 전환 시에도 파괴되지 않는다(전역 시스템용).</summary>
        protected virtual bool Persistent => false;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;

            if (Persistent)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
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
