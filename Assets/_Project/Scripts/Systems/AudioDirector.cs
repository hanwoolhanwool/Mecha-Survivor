using System.Collections.Generic;
using UnityEngine;
using MechaSurvivor.Core;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// 전역 SFX 재생기. 이벤트만 구독한다 — 전투 코드는 오디오의 존재를 모른다
    /// (StatisticsRecorder와 같은 원칙, GDD 8.3 ②).
    /// 보이스는 Awake에서 고정 개수로 만들어 돌려쓴다 (런타임 Instantiate 없음).
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        // 예약 이벤트음 id (SfxLibrary 키)
        public const string EnemyDeathSfx = "enemy_death";
        public const string PlayerHitSfx = "player_hit";
        public const string PlayerDeathSfx = "player_death";
        public const string XpPickupSfx = "xp_pickup";
        public const string LevelUpSfx = "level_up";
        public const string RunClearSfx = "run_clear";
        public const string RunFailSfx = "run_fail";

        [SerializeField] private SfxLibrary _library;

        [Tooltip("동시 재생 보이스 수. 모두 사용 중이면 가장 오래된 것을 뺏는다")]
        [SerializeField] private int _voiceCount = 20;

        [Tooltip("윈도우(초)당 전체 발음 상한 — 대량 사망 시 소리 도배 방지")]
        [SerializeField] private float _throttleWindow = 0.05f;
        [SerializeField] private int _throttleBudget = 8;

        [Tooltip("DamageDealt의 SourceId가 여기 있으면 명중 지점에서 발음 (드론처럼 WeaponFired를 안 거치는 발사원)")]
        [SerializeField] private string[] _damageDealtSfxIds = { "support_drone" };

        [Header("3D 사운드 감쇠")]
        [SerializeField] private float _spatialMinDistance = 12f;
        [SerializeField] private float _spatialMaxDistance = 160f;

        private AudioSource[] _voices;
        private int _nextVoice;
        private SfxThrottle _throttle;
        private HashSet<string> _damageDealtSet;
        private GameSettings _settings;

        private void Awake()
        {
            // 볼륨은 사용자 설정이 단일 출처다 — 환경설정 패널에서 바꾸면 즉시 반영된다.
            _settings = GameSettings.Resolve();
            _throttle = new SfxThrottle(_throttleWindow, _throttleBudget);
            _damageDealtSet = new HashSet<string>(_damageDealtSfxIds);
            CreateVoices();
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet(out AudioDirector current) && current == this)
            {
                ServiceLocator.Unregister<AudioDirector>();
            }
        }

        private void OnEnable()
        {
            EventBus<WeaponFiredEvent>.Subscribe(OnWeaponFired);
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
            EventBus<PlayerDamagedEvent>.Subscribe(OnPlayerDamaged);
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
            EventBus<PlayerLeveledUpEvent>.Subscribe(OnPlayerLeveledUp);
            EventBus<ExperienceGainedEvent>.Subscribe(OnExperienceGained);
            EventBus<RunEndedEvent>.Subscribe(OnRunEnded);
            EventBus<DamageDealtEvent>.Subscribe(OnDamageDealt);
        }

        private void OnDisable()
        {
            EventBus<WeaponFiredEvent>.Unsubscribe(OnWeaponFired);
            EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
            EventBus<PlayerDamagedEvent>.Unsubscribe(OnPlayerDamaged);
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
            EventBus<PlayerLeveledUpEvent>.Unsubscribe(OnPlayerLeveledUp);
            EventBus<ExperienceGainedEvent>.Unsubscribe(OnExperienceGained);
            EventBus<RunEndedEvent>.Unsubscribe(OnRunEnded);
            EventBus<DamageDealtEvent>.Unsubscribe(OnDamageDealt);
        }

        // ── 공개 API (게임플레이가 직접 요청할 때 — ServiceLocator 경유) ──

        /// <summary>2D(비공간) 재생. 플레이어 자신의 소리·UI 소리.</summary>
        public void Play(string id) => PlayInternal(id, default, false);

        /// <summary>월드 위치 재생. 라이브러리 엔트리가 Spatial일 때만 3D로 들린다.</summary>
        public void PlayAt(string id, Vector3 position) => PlayInternal(id, position, true);

        // ── 이벤트 핸들러 ─────────────────────────────────────────────

        private void OnWeaponFired(WeaponFiredEvent evt) => Play(evt.WeaponId);
        private void OnEnemyKilled(EnemyKilledEvent evt) => PlayAt(EnemyDeathSfx, evt.Position);
        private void OnPlayerDamaged(PlayerDamagedEvent evt) => Play(PlayerHitSfx);
        private void OnPlayerDied(PlayerDiedEvent evt) => PlayAt(PlayerDeathSfx, evt.Position);
        private void OnPlayerLeveledUp(PlayerLeveledUpEvent evt) => Play(LevelUpSfx);
        private void OnExperienceGained(ExperienceGainedEvent evt) => Play(XpPickupSfx);
        private void OnRunEnded(RunEndedEvent evt) => Play(evt.Victory ? RunClearSfx : RunFailSfx);

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            // 발사음은 WeaponFired가 담당한다. 여기서는 WeaponFired를 거치지 않는
            // 발사원(드론)만 명중 지점에서 울린다 — 같은 id를 이중 재생하지 않기 위한 분리.
            if (_damageDealtSet.Contains(evt.SourceId))
            {
                PlayAt(evt.SourceId, evt.Position);
            }
        }

        // ── 내부 ─────────────────────────────────────────────────────

        private void PlayInternal(string id, Vector3 position, bool hasPosition)
        {
            if (_library == null || !_library.TryGet(id, out var entry) || entry.Clip == null)
            {
                return;
            }

            // unscaledTime — 레벨업 일시정지(timeScale 0) 중에도 UI 소리는 나야 한다.
            if (!_throttle.TryAcquire(id, entry.MinInterval, Time.unscaledTime))
            {
                return;
            }

            var voice = NextVoice();
            bool spatial = entry.Spatial && hasPosition;
            voice.transform.position = hasPosition ? position : transform.position;
            voice.spatialBlend = spatial ? 1f : 0f;
            voice.clip = entry.Clip;
            voice.volume = entry.Volume * _settings.MasterVolume;
            voice.pitch = Random.Range(entry.PitchMin, entry.PitchMax);
            voice.Play();
        }

        private AudioSource NextVoice()
        {
            // 빈 보이스 우선, 전부 사용 중이면 라운드로빈 순서상 가장 오래된 것을 뺏는다.
            for (int i = 0; i < _voices.Length; i++)
            {
                int index = (_nextVoice + i) % _voices.Length;
                if (!_voices[index].isPlaying)
                {
                    _nextVoice = (index + 1) % _voices.Length;
                    return _voices[index];
                }
            }

            var stolen = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            return stolen;
        }

        private void CreateVoices()
        {
            _voices = new AudioSource[Mathf.Max(1, _voiceCount)];
            for (int i = 0; i < _voices.Length; i++)
            {
                var child = new GameObject($"Voice{i}");
                child.transform.SetParent(transform, false);
                var source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = _spatialMinDistance;
                source.maxDistance = _spatialMaxDistance;
                source.dopplerLevel = 0f;
                _voices[i] = source;
            }
        }
    }
}
