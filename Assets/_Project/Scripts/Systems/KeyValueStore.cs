using System.Collections.Generic;
using UnityEngine;

namespace MechaSurvivor.Systems
{
    /// <summary>
    /// 영구 저장 키-값 추상화. GameSettings/PlayerRecords가 공유한다.
    /// 실기기는 PlayerPrefs, 테스트는 InMemory — 순수 로직을 EditMode로 검증하기 위한 분리.
    /// </summary>
    public interface IKeyValueStore
    {
        float GetFloat(string key, float defaultValue);
        void SetFloat(string key, float value);
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        string GetString(string key, string defaultValue);
        void SetString(string key, string value);
        void Save();
    }

    /// <summary>PlayerPrefs 기반 실저장소.</summary>
    public sealed class PlayerPrefsKeyValueStore : IKeyValueStore
    {
        public float GetFloat(string key, float defaultValue) => PlayerPrefs.GetFloat(key, defaultValue);
        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
        public int GetInt(string key, int defaultValue) => PlayerPrefs.GetInt(key, defaultValue);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public string GetString(string key, string defaultValue) => PlayerPrefs.GetString(key, defaultValue);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public void Save() => PlayerPrefs.Save();
    }

    /// <summary>테스트용 인메모리 저장소. Save는 아무것도 하지 않는다.</summary>
    public sealed class InMemoryKeyValueStore : IKeyValueStore
    {
        private readonly Dictionary<string, float> _floats = new();
        private readonly Dictionary<string, int> _ints = new();
        private readonly Dictionary<string, string> _strings = new();

        public float GetFloat(string key, float defaultValue) =>
            _floats.TryGetValue(key, out float value) ? value : defaultValue;

        public void SetFloat(string key, float value) => _floats[key] = value;

        public int GetInt(string key, int defaultValue) =>
            _ints.TryGetValue(key, out int value) ? value : defaultValue;

        public void SetInt(string key, int value) => _ints[key] = value;

        public string GetString(string key, string defaultValue) =>
            _strings.TryGetValue(key, out string value) ? value : defaultValue;

        public void SetString(string key, string value) => _strings[key] = value;

        public void Save() { }
    }
}
