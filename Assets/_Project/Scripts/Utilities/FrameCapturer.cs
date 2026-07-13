using System.IO;
using UnityEngine;

namespace MechaSurvivor.Utilities
{
    /// <summary>
    /// 데모/검증 영상용 연속 프레임 캡처. 지정 간격으로 게임 화면(PNG)을 떨어뜨린다.
    /// ffmpeg 등 외부 도구로 조립하는 것을 전제로 하며, 씬에 두지 않고 필요 시 런타임에 붙인다.
    /// </summary>
    public sealed class FrameCapturer : MonoBehaviour
    {
        public string OutputDirectory;
        public float Interval = 0.1f;
        public int MaxFrames = 300;

        public int CapturedFrames { get; private set; }
        public bool IsDone => CapturedFrames >= MaxFrames;

        private float _nextCaptureTime;
        private bool _directoryEnsured;

        private void OnEnable()
        {
            _nextCaptureTime = Time.unscaledTime;
        }

        private void Update()
        {
            if (IsDone || string.IsNullOrEmpty(OutputDirectory) ||
                Time.unscaledTime < _nextCaptureTime)
            {
                return;
            }

            // AddComponent 직후 OnEnable이 필드 주입보다 먼저 도는 경우가 있어
            // 폴더 보장은 첫 캡처 직전에 한다.
            if (!_directoryEnsured)
            {
                Directory.CreateDirectory(OutputDirectory);
                _directoryEnsured = true;
            }

            _nextCaptureTime = Time.unscaledTime + Interval;
            string path = Path.Combine(OutputDirectory, $"frame_{CapturedFrames:D4}.png");
            ScreenCapture.CaptureScreenshot(path);
            CapturedFrames++;
        }
    }
}
