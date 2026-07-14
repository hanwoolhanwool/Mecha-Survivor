using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaSurvivor.Gameplay
{
    /// <summary>한 프레임의 조작 입력 스냅숏. 소비자는 장치를 직접 읽지 않는다.</summary>
    public readonly struct MechaInputFrame
    {
        public readonly Vector2 Move;          // WASD (카메라 기준 수평)
        public readonly float Vertical;        // Space(+1) / Shift(-1)
        public readonly Vector2 Look;          // 마우스 델타
        public readonly bool Fire1Held;        // 좌클릭 — 무기 슬롯 1
        public readonly bool Fire2Held;        // 우클릭 — 무기 슬롯 2
        public readonly bool Fire3Held;        // Q — 무기 슬롯 3
        public readonly bool Fire4Held;        // E — 무기 슬롯 4
        public readonly bool CameraTogglePressed; // V — 시점 전환
        public readonly bool PausePressed;     // Esc
        public readonly bool DashPressed;      // F — 대시 (누른 순간)

        public MechaInputFrame(
            Vector2 move, float vertical, Vector2 look,
            bool fire1Held, bool fire2Held, bool fire3Held, bool fire4Held,
            bool cameraTogglePressed, bool pausePressed, bool dashPressed = false)
        {
            Move = move;
            Vertical = vertical;
            Look = look;
            Fire1Held = fire1Held;
            Fire2Held = fire2Held;
            Fire3Held = fire3Held;
            Fire4Held = fire4Held;
            CameraTogglePressed = cameraTogglePressed;
            PausePressed = pausePressed;
            DashPressed = dashPressed;
        }

        public bool IsFireHeld(int slotIndex) => slotIndex switch
        {
            0 => Fire1Held,
            1 => Fire2Held,
            2 => Fire3Held,
            3 => Fire4Held,
            _ => false,
        };
    }

    /// <summary>
    /// 입력 장치를 매 프레임 폴링해 <see cref="MechaInputFrame"/>으로 변환한다.
    /// 액션 에셋 대신 직접 폴링해 입력 지연 0을 보장한다 (GDD 2.3 절대 원칙).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MechaInput : MonoBehaviour
    {
        public MechaInputFrame Frame { get; private set; }

        private bool _hasInjected;
        private MechaInputFrame _injected;

        /// <summary>
        /// 데모·자동화 테스트용 입력 주입. 다음 Update 1프레임에서 실제 장치 대신 소비된다.
        /// 매 프레임 다시 주입하지 않으면 자동으로 장치 폴링으로 복귀한다.
        /// </summary>
        public void InjectFrame(in MechaInputFrame frame)
        {
            _injected = frame;
            _hasInjected = true;
        }

        private void Update()
        {
            if (_hasInjected)
            {
                Frame = _injected;
                _hasInjected = false;
                return;
            }

            Frame = Poll();
        }

        private static MechaInputFrame Poll()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (kb == null)
            {
                return default;
            }

            Vector2 move = Vector2.zero;
            if (kb.wKey.isPressed) move.y += 1f;
            if (kb.sKey.isPressed) move.y -= 1f;
            if (kb.dKey.isPressed) move.x += 1f;
            if (kb.aKey.isPressed) move.x -= 1f;

            float vertical = 0f;
            if (kb.spaceKey.isPressed) vertical += 1f;
            if (kb.leftShiftKey.isPressed) vertical -= 1f;

            Vector2 look = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;

            return new MechaInputFrame(
                move,
                vertical,
                look,
                fire1Held: mouse != null && mouse.leftButton.isPressed,
                fire2Held: mouse != null && mouse.rightButton.isPressed,
                fire3Held: kb.qKey.isPressed,
                fire4Held: kb.eKey.isPressed,
                cameraTogglePressed: kb.vKey.wasPressedThisFrame,
                pausePressed: kb.escapeKey.wasPressedThisFrame,
                dashPressed: kb.fKey.wasPressedThisFrame);
        }
    }
}
