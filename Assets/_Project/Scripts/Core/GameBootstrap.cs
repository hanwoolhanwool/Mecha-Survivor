using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaSurvivor.Core
{
    /// <summary>
    /// 부트스트랩: Boot 씬에 배치하는 진입점. 전역 서비스를 초기화한 뒤 다음 씬을 로드한다.
    /// Boot → (서비스 초기화) → Lobby → Game 흐름. 씬을 직접 열어도 안전하도록
    /// 전역 서비스는 lazy Resolve/자체 등록 구조를 유지한다.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string _gameSceneName = "Lobby";
        [SerializeField] private bool _loadGameSceneOnStart = true;

        private void Start()
        {
            InitializeServices();

            if (_loadGameSceneOnStart)
            {
                SceneManager.LoadScene(_gameSceneName);
            }
        }

        /// <summary>전역 서비스 등록 지점. (오디오/세이브/설정 등은 여기에 추가)</summary>
        private void InitializeServices()
        {
            // 예: ServiceLocator.Register<ISaveService>(new JsonSaveService());
            // PoolManager 등 MonoBehaviour 시스템은 각자 Awake에서 자체 등록한다.
        }
    }
}
