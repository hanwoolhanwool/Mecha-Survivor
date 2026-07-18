using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// 사용자 환경설정 (로비 환경설정 패널이 쓰고, AudioDirector/CameraShakeReactor가 읽는다).
    /// 순수 로직 + IKeyValueStore 저장 — EditMode 테스트 대상. Unity API는 ApplyDisplay에만 격리.
    /// 값은 세터에서 즉시 저장한다 — 설정 변경은 드물어 저장 비용이 문제되지 않는다.
    /// </summary>
    public sealed class GameSettings
    {
        public const float DefaultMasterVolume = 0.8f;
        public const float DefaultScreenShake = 1f;

        private const string MasterVolumeKey = "settings.master_volume";
        private const string ScreenShakeKey = "settings.screen_shake";
        private const string FullscreenKey = "settings.fullscreen";
        private const string VSyncKey = "settings.vsync";

        private readonly IKeyValueStore _store;
        private float _masterVolume;
        private float _screenShake;
        private bool _fullscreen;
        private bool _vsync;

        public GameSettings(IKeyValueStore store)
        {
            _store = store;
            _masterVolume = Mathf.Clamp01(store.GetFloat(MasterVolumeKey, DefaultMasterVolume));
            _screenShake = Mathf.Clamp01(store.GetFloat(ScreenShakeKey, DefaultScreenShake));
            _fullscreen = store.GetInt(FullscreenKey, 1) != 0;
            _vsync = store.GetInt(VSyncKey, 1) != 0;
        }

        /// <summary>전체 SFX 볼륨 (0~1).</summary>
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                _store.SetFloat(MasterVolumeKey, _masterVolume);
                _store.Save();
            }
        }

        /// <summary>카메라 셰이크/킥 배율 (0 = 정적인 카메라).</summary>
        public float ScreenShake
        {
            get => _screenShake;
            set
            {
                _screenShake = Mathf.Clamp01(value);
                _store.SetFloat(ScreenShakeKey, _screenShake);
                _store.Save();
            }
        }

        public bool Fullscreen
        {
            get => _fullscreen;
            set
            {
                _fullscreen = value;
                _store.SetInt(FullscreenKey, value ? 1 : 0);
                _store.Save();
            }
        }

        public bool VSync
        {
            get => _vsync;
            set
            {
                _vsync = value;
                _store.SetInt(VSyncKey, value ? 1 : 0);
                _store.Save();
            }
        }

        /// <summary>
        /// 전역 인스턴스 조회. Boot을 거치지 않고 씬을 직접 열어도(랩 씬 등)
        /// 처음 필요해진 시점에 만들어 등록한다 — 초기화 누락이 불가능한 구조.
        /// </summary>
        public static GameSettings Resolve()
        {
            if (!ServiceLocator.TryGet(out GameSettings settings))
            {
                settings = new GameSettings(new PlayerPrefsKeyValueStore());
                ServiceLocator.Register(settings);
            }

            return settings;
        }

        /// <summary>화면 관련 설정을 실제 디스플레이에 반영한다. (Unity API — 테스트에서 호출 금지)</summary>
        public void ApplyDisplay()
        {
            Screen.fullScreenMode = _fullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
            QualitySettings.vSyncCount = _vsync ? 1 : 0;
        }
    }
}
