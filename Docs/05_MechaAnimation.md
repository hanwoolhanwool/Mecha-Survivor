# 05 — 주인공 기체(Mecha.fbx) 애니메이션 제작 계획서

작성일: 2026-07-28
대상 에셋: `Assets/_Project/Art/Models/Mecha/Mecha.fbx`
상태: **P5 완료 (2026-07-28)** — 30클립 전량 제작·배선 (이동 20 + 사격/피격 2 + 대쉬 8). 남은 것은 P6 폴리시·CityMap 동기화·QA. 이 문서가 제작·임포트·배선·검증의 단일 기준이다.

---

## 1. 목적과 범위

주인공 기체의 지상/비행 전 상태 애니메이션을 제작하고, Animator + 코드 드라이버로
기존 이동 시스템(`MechaController`)에 배선한다.

**전제가 되는 게임 규칙 (GDD 2.2~2.3):**
- 기본 상태는 **비행**. 하강해 접지하면 **보행(Grounded)** 으로 전환.
- 이동은 **관성 0, 지연 0** — 입력이 곧 속도. 따라서 애니메이션도 전환 블렌드를 짧게 잡아야 한다.
- 대쉬는 수평 임펄스, **지속 0.35초 / 쿨다운 1.6초** (`MechaController._dashDuration`).
- 지상 대쉬는 연출상 "순간적으로 떠서 미끄러지는" 스러스터 기동이다 (다리로 뛰는 회피가 아님).

## 2. 소스 에셋 현황

| 항목 | 내용 |
|---|---|
| 모델 | Mecha.fbx — 20파츠 SkinnedMesh, 50k tris, 전고 2m (1:1 스케일) |
| 리그 | 24본, **Mixamo 규격** / Unity **Humanoid** 설정 완료 (아바타 isValid/isHuman 확인됨) |
| 애니메이션 | **없음** — 이번 작업으로 전량 신규 제작 |
| 제작 소스 | Blender `Mecha Ver 2.blend` (GameReady 컬렉션) |
| 재질 | `M_Mecha.mat` (URP Lit) — 애니메이션 작업과 무관, 건드리지 않음 |

Humanoid 리그이므로 Blender 직접 키프레임 제작과 Mixamo 리타게팅 **둘 다 가능**하다.
기계적인 움직임(정지·경직·스냅)이 핵심 개성이므로 **직접 제작을 기본**으로 하고,
보행류만 Mixamo 베이스에서 다듬는 방식을 허용한다.

## 3. 클립 목록 (제작 사양)

명명 규칙: `Mecha_<상태>_<동작>[_방향]`. 모든 클립은 **제자리(in-place), 루트 모션 없음**
— 이동은 코드(`MechaController`)가 전담하므로 애니메이션이 위치를 옮기면 안 된다.

### 3.1 지상 (Grounded)

| # | 클립 | 루프 | 길이(권장) | 내용 |
|---|---|---|---|---|
| G1 | `Mecha_Ground_Idle` | ○ | 2~4s | 정지 대기. 무게감 있는 미세 자세 보정 |
| G2 | `Mecha_Ground_Walk_F` | ○ | 0.8~1.2s | 전방 보행. 이동속도 14m/s에 맞춰 재생속도 파라미터로 보정 |
| G3 | `Mecha_Ground_Walk_L` | ○ | 0.8~1.2s | 좌측 스트레이프 보행 |
| G4 | `Mecha_Ground_Walk_R` | ○ | 0.8~1.2s | 우측 스트레이프 보행 |
| G5 | `Mecha_Ground_Walk_B` | ○ | 0.8~1.2s | **후방 보행 — 요청 목록엔 없지만 블렌드 트리 완결에 필수** (§8-1) |
| G6 | `Mecha_Ground_Walk_FL` | ○ | 0.8~1.2s | 전방-좌 대각선 보행 |
| G7 | `Mecha_Ground_Walk_FR` | ○ | 0.8~1.2s | 전방-우 대각선 보행 |
| G8 | `Mecha_Ground_Walk_BL` | ○ | 0.8~1.2s | 후방-좌 대각선 보행 |
| G9 | `Mecha_Ground_Walk_BR` | ○ | 0.8~1.2s | 후방-우 대각선 보행 |
| G10 | `Mecha_Ground_Shoot` | ○ | 0.3~0.5s | 사격 자세 + 반동. **상체 전용**(§5 레이어) — 보행과 동시 재생 |
| G11 | `Mecha_Ground_Hit` | × | 0.3s | 피격 경직. 짧게 — 조작감을 해치면 안 됨 |
| G12 | `Mecha_Ground_Dash_F` | × | 0.4s | 전방 대쉬. **순간 부양** — 다리 접고 스러스터 분사 |
| G13 | `Mecha_Ground_Dash_L` | × | 0.4s | 좌측 대쉬 (동일 컨셉) |
| G14 | `Mecha_Ground_Dash_R` | × | 0.4s | 우측 대쉬 |
| G15 | `Mecha_Ground_Dash_B` | × | 0.4s | 후방 대쉬 |

### 3.2 비행 (Flight)

| # | 클립 | 루프 | 길이(권장) | 내용 |
|---|---|---|---|---|
| F1 | `Mecha_Fly_Idle` | ○ | 2~4s | 호버링. 상하 부유 + 자세 보정 스러스터 (GDD 2.4 "떠 있는 기계") |
| F2 | `Mecha_Fly_F` | ○ | 1~2s | 전방 비행. 전경(前傾) 자세 |
| F3 | `Mecha_Fly_L` | ○ | 1~2s | 좌측 비행. 좌측 뱅킹 |
| F4 | `Mecha_Fly_R` | ○ | 1~2s | 우측 비행. 우측 뱅킹 |
| F5 | `Mecha_Fly_B` | ○ | 1~2s | **후방 비행 — 블렌드 완결용 추가** (§8-1) |
| F6 | `Mecha_Fly_FL` | ○ | 1~2s | 전방-좌 대각선 비행. 전경 + 좌 뱅킹 복합 |
| F7 | `Mecha_Fly_FR` | ○ | 1~2s | 전방-우 대각선 비행 |
| F8 | `Mecha_Fly_BL` | ○ | 1~2s | 후방-좌 대각선 비행 |
| F9 | `Mecha_Fly_BR` | ○ | 1~2s | 후방-우 대각선 비행 |
| F10 | `Mecha_Fly_Shoot` | ○ | 0.3~0.5s | 비행 사격. 상체 전용 — 모든 비행 이동과 동시 재생 |
| F11 | `Mecha_Fly_Hit` | × | 0.3s | 비행 피격. 기체 흔들림 |
| F12 | `Mecha_Fly_Dash_F` | × | 0.4s | 전방 부스트 |
| F13 | `Mecha_Fly_Dash_L` | × | 0.4s | 좌측 부스트 |
| F14 | `Mecha_Fly_Dash_R` | × | 0.4s | 우측 부스트 |
| F15 | `Mecha_Fly_Dash_B` | × | 0.4s | 후방 부스트 |

**총 30클립.** 보행/비행 이동은 **8방향 전용 클립**(전·후·좌·우·대각 4종)으로 구성한다 —
대각선을 4방향 클립의 블렌드 합성에 맡기지 않고 전용 클립으로 품질을 보장한다.
대쉬는 지속 0.35초의 순간 연출이라 4방향으로 충분하며, 대각 입력은 가장 가까운 축으로
스냅하거나 인접 2클립 블렌드로 처리한다 (§8-5).
상승/하강 전용 클립은 만들지 않는다 — `VerticalInput` 기반 피치 연출은
기존 `MechaVisuals`(뱅킹/피치 절차 연출)가 이미 담당하므로 중복 제작하지 않는다.

**진폭 기준 (2026-07-28 확정):** 서바이버 시점 카메라(원경)에서 체감되도록 클립 진폭은
근경 기준보다 크게 잡는다 — Idle 부유 ±10cm, 이동 자세 각도(트레일/뱅킹) 15~35° 급.
절차 부유(`MechaVisuals._hoverAmplitude`)는 **0으로 끄고 부유는 애니메이션이 전담**한다
(이중 부유 제거). 이동 기울임(lean)은 절차 연출을 유지하고 클립 자세와 합산된다.

## 4. 제작 파이프라인 (Blender → Unity)

1. **Blender에서 클립별 Action으로 제작.** 30fps 기준.
2. **루트 모션 금지** — Hips 이하만 움직이고 루트 본은 원점 고정.
3. 내보내기: 기존 `Mecha.fbx`는 그대로 두고, **애니메이션 전용 FBX를 별도 파일로** 내보낸다
   (예: `Rigged/Mecha_Anim_GroundWalk.fbx`). 메시 없이 아마추어+Action만 포함 → 파일 경량화.
   - 여러 Action을 한 FBX에 담을 경우 NLA 트랙(Stash)으로 정리해 멀티 테이크로 내보낸다.
4. Unity 임포트 설정 (클립 FBX 전부 동일):
   - Rig = **Humanoid**, Avatar = **Mecha.fbx의 아바타를 Copy From Other Avatar로 지정**
   - Loop Time은 §3 표의 루프 여부대로. 루프 클립은 Loop Pose + Cycle Offset 검토
   - Root Transform Position(Y/XZ)·Rotation 전부 **Bake Into Pose** (제자리 강제)
5. **주의 (기존 사고 이력):** FBX Rig 타입을 코드로 바꿀 땐 `manage_asset` modify가 아니라
   `execute_code`로 `ModelImporter.animationType`을 직접 설정한다. 임포트 잡 중 Unity 수동 조작 금지.

### §4-확정 — P0에서 검증된 설정값 (2026-07-28, 이대로 사용)

**Blender 내보내기** (`bpy.ops.export_scene.fbx`):
```python
use_selection=True, object_types={'ARMATURE'},   # Riging_Meshy 아마추어만 선택
add_leaf_bones=False,
bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',   # ★ 아래 함정 참조
axis_forward='-Z', axis_up='Y',
```
- **★ 스케일 함정 (P0에서 실제 발생):** `apply_scale_options='FBX_SCALE_NONE'`(기본값)으로
  내보내면 fileScale=0.01이 되는데, `Mecha.fbx`는 fileScale=1이다. 이 불일치 상태로 Humanoid
  리타게팅하면 **Hips 이동값이 ×100으로 튀어 기체가 5m 위로 떠오른다.**
  반드시 `FBX_SCALE_ALL`로 내보내 fileScale=1을 맞춘다.
- **★ 프레임 범위 함정 (P1 후 실제 발생):** 내보내기는 액션 범위가 아니라 **씬 프레임 범위로
  베이크**한다. 씬 범위(0~120)가 액션(0~60)보다 길면 "2초 동작 + 2초 정지 홀드"인 4초짜리
  클립이 나온다. **내보내기 직전 `scene.frame_start/end`를 액션 `frame_range`에 맞출 것.**
- Blender 5.1은 레이어드 액션이라 `action.fcurves`가 없다 —
  `action.layers[*].strips[*].channelbags[*].fcurves`로 접근.
- 소스 액션은 `Mecha Ver 2.blend`에 `use_fake_user=True`로 보존 (`Mecha_Fly_Idle` 등).

**Unity 임포트** (`execute_code`, codedom/C#6):
```csharp
importer.animationType = ModelImporterAnimationType.Human;
importer.avatarSetup   = ModelImporterAvatarSetup.CopyFromOther;
importer.sourceAvatar  = /* Mecha.fbx의 MechaAvatar */;
clip.loopTime = true;  clip.loopPose = true;
clip.lockRootRotation = true;   clip.keepOriginalOrientation = true;
clip.lockRootHeightY  = true;   clip.keepOriginalPositionY   = true;
clip.lockRootPositionXZ = true; clip.keepOriginalPositionXZ  = true;
importer.clipAnimations = new[]{ clip };  importer.SaveAndReimport();
```
- 에디트 모드에서 모델이 누워 보이는 것은 정상이다(FBX 기본 포즈 문제, 스킨드 메시는
  본이 구동). **판정은 반드시 Play 모드에서** 한다.
- `execute_code`의 safety check가 `AssetDatabase.DeleteAsset`을 차단한다 — 에셋은 삭제 대신 재사용.

## 5. Animator Controller 설계

`Assets/_Project/Art/Models/Mecha/AC_Mecha.controller` — 레이어 3장 구조.

### 레이어

| 레이어 | 역할 | 마스크 | 블렌딩 |
|---|---|---|---|
| 0 Base | 이동 전부 (지상/비행 블렌드 트리, 대쉬) | 없음 (전신) | Override |
| 1 UpperBody | 사격 (G10/F10) | **상체 아바타 마스크** (척추 상단·팔·머리) | Override, weight는 사격 중 1 |
| 2 Hit | 피격 (G11/F11) | 없음 (전신) | **Additive** — 이동을 끊지 않고 경직만 얹는다 |

사격을 상체 레이어로 분리하는 것이 "걸으면서/날면서 총쏘기" 요구의 구현 방식이다.
사격 전용 이동 클립을 만들지 않고 30클립으로 전 조합을 커버한다.

### Base 레이어 상태 구조

```
[Ground BT] ←(IsGrounded)→ [Flight BT]
     │                          │
     └──(DashTrigger)→ [Dash BT (2D, DashX/DashZ)] ─(0.4s 후 자동 복귀)
```

- **Ground BT**: 2D Freeform Directional 블렌드 트리. 파라미터 `MoveX/MoveZ`
  (기체 시각 전방 기준 로컬 속도, -1~1 정규화). 중앙 = G1 Idle,
  **8방 배치** = G2~G5(축) + G6~G9(대각, 좌표 ±0.71 지점).
- **Flight BT**: 동일 구조. 중앙 = F1 Hover, 8방 = F2~F5(축) + F6~F9(대각).
- **Dash BT**: 지상/비행 각각 4방향 클립을 `DashX/DashZ`로 선택. Exit Time으로 복귀.
- 전환 시간: 지상↔비행 0.15s, 대쉬 진입 0.05s (관성 0 원칙 — 길면 조작감이 죽는다).

### Animator 파라미터

| 파라미터 | 타입 | 공급원 |
|---|---|---|
| `IsGrounded` | Bool | `MechaController.IsGrounded` |
| `MoveX`, `MoveZ` | Float | 월드 속도를 시각 요(yaw) 로컬로 변환·정규화 |
| `Speed` | Float | 수평 속력 / 기준속도, **하한 1** — Ground 상태 speedParameter로 배선. 하한이 없으면 정지 시 Idle이 멈춘다 (P2 확정) |
| `DashTrigger` | Trigger | `MechaController.IsDashing` 상승 엣지 |
| `DashX`, `DashZ` | Float | 대쉬 방향의 로컬 성분 |
| `Fire` | Bool | `WeaponFiredEvent` 수신 후 짧은 유지창(0.3s) |
| `HitTrigger` | Trigger | `PlayerDamagedEvent` 수신 |

## 6. 코드 배선 — `MechaAnimationDriver` (신규 재작성)

구 `MechaAnimationDriver.cs`는 07-28 모델 교체 때 삭제됐다. 새로 작성한다.

- 위치: `Assets/_Project/Scripts/Gameplay/Player/MechaAnimationDriver.cs`
  (네임스페이스 `MechaSurvivor.Gameplay`)
- 입력: 같은 오브젝트의 `MechaController`(폴링) + `EventBus<WeaponFiredEvent>` /
  `EventBus<PlayerDamagedEvent>`(구독). **Subscribe는 OnEnable, Unsubscribe는 OnDisable** (CLAUDE.md §3).
- 파라미터 해시는 `Animator.StringToHash`로 **정적 사전 캐싱** — Update 내 할당 금지 (CLAUDE.md §2).
- **순수 로직 분리**: "월드 속도+시각 yaw → MoveX/MoveZ", "대쉬 방향 → DashX/DashZ",
  "Fire 유지창 갱신" 계산은 정적 클래스 `MechaAnimParams`로 빼서 EditMode 테스트 대상으로 삼는다.
- 시각 yaw는 `MechaVisuals`가 계산하는 회전(이동 방향 추종)을 기준으로 삼는다 —
  Animator를 붙일 모델 인스턴스가 `MechaVisuals`의 visual root 하위에 들어가므로
  transform 자체의 yaw를 읽으면 된다.

## 7. 씬 배선

- Game / CityMap 두 씬의 `Player` 하위에 Mecha.fbx 인스턴스 배치 (`Player/Body/MechaModel`),
  Body 캡슐 MeshRenderer는 다시 끈다 (현재 캡슐 표시는 임시 상태).
- Animator 컴포넌트에 `AC_Mecha.controller` + Mecha 아바타 연결, Apply Root Motion **끔**.
- 배치·프리팹·컨트롤러 에셋 작업은 전부 **MCP**로 한다 (`manage_gameobject`, `manage_animation` 등).
  씬 작업 전 `set_active_instance` 필수 (멀티 인스턴스 핀 풀림 이력 있음).

## 8. 결정 필요 사항 (제작 착수 전 확인)

1. **후방 클립(G5/F5, 후방 대쉬)**: 요청 목록에 "왼쪽|오른쪽"만 있었으나, 블렌드 트리와
   4방향 대쉬가 성립하려면 후방이 필요해 계획에 포함했다. 미러링으로 때울지 전용 제작할지만 결정.
2. **피격 강도 구분**: 일반 피격 1종으로 시작. 대미지 크기별 변형(강피격)은 후순위.
3. **사격 클립의 무기별 변형**: 무기 6종이지만 우선 공용 1종(G6/F6)으로 시작.
   무기별 반동 차이는 추후 `WeaponFiredEvent`의 무기 정보로 분기 확장 가능.
4. **상승/하강 전용 자세**: §3 결정대로 절차 연출(MechaVisuals 피치)로 대체. 이견 있으면 클립 추가.
5. **대쉬 대각선 처리**: 대쉬 클립은 4방향만 제작하고 대각 입력은 인접 2클립 블렌드로 처리
   (0.35초 순간 연출이라 전용 클립의 체감 이득이 작다). 대각 대쉬도 전용 클립을 원하면
   지상/비행 각 4클립(총 8클립) 추가 — 이 경우 총 38클립.
6. **대각선 클립의 미러링 활용**: FL↔FR, BL↔BR은 Unity 임포터의 Mirror 옵션으로 한쪽만
   제작해 뒤집을 수 있다 (제작량 절반). 비대칭 디테일(무기 든 팔 등)이 중요하면 전용 제작.

## 9. 개발 단계 (P0~P6)

"30클립 전부 만든 뒤 한 번에 배선"이 아니라, **매 단계가 Play 가능한 수직 슬라이스로 끝나는**
구조다. 파이프라인 문제(리타게팅·스케일·루트모션)는 클립 1개일 때 잡아야 싸다.

### P0 — 파이프라인 스파이크 (클립 1개로 전 구간 관통) ✅ 완료 (2026-07-28)
- 제작: **F1 호버 Idle 단 1개.**
- Blender 내보내기(§4) → 임포트 설정 → 상태 1개짜리 임시 컨트롤러 → Game 씬 Player에
  모델+Animator 배선(§7, 캡슐 OFF)까지 **전체 경로를 한 번에 뚫는다.**
- **완료 기준**: Play 시 기체가 호버 애니메이션으로 떠 있음. 리타게팅 왜곡·스케일·
  발 미끄러짐 없음. 여기서 발견된 임포트 설정값을 §4에 확정 기록.

### P1 — 비행 코어 (기본 상태부터) ✅ 완료 (2026-07-28)
- 제작: F2~F5 (축 4방향 비행).
- `AC_Mecha.controller` 정식 생성 — Base 레이어 + **Flight BT** (중앙 F1, 축 4방).
- `MechaAnimationDriver` + `MechaAnimParams` 신규 작성(§6) — 이 단계에선
  `MoveX/MoveZ/Speed`만 공급. **EditMode 테스트 동시 작성** (좌표 변환·정규화).
- **완료 기준**: 비행 8방향 입력(대각은 블렌드 합성으로 임시 커버)이 자연스럽게 재생.

### P2 — 지상 코어 + 상태 전환 ✅ 완료 (2026-07-28)
- 제작: G1~G5 (지상 Idle + 축 4방 보행).
- **Ground BT** 추가, `IsGrounded`로 Flight↔Ground 전환 배선 (전환 0.15s).
- 보행 발 미끄러짐은 `Speed` 파라미터로 재생속도 보정.
- **완료 기준**: 착지→보행→이륙 전환이 끊김 없이 재생. 접지 순간 팝핑 없음.

### P3 — 대각선 전용 클립 확장 ✅ 완료 (2026-07-28)
- **§8-6 결정: 미러링 채택** — FL/BL만 실제작, FR/BR은 같은 FBX 안에 `mirror=true` 클립으로 생성.
- 제작: G6~G9, F6~F9 (대각 8클립 — 미러링 채택 시 실제작 4클립).
- 두 BT를 8방 배치로 개편 (대각 지점 ±0.71).
- **완료 기준**: 대각 이동 시 합성 블렌드가 아닌 전용 클립 재생, 축↔대각 경계에서 떨림 없음.

### P4 — 전투 반응 (사격·피격) ✅ 완료 (2026-07-28)
- 사격은 지상/비행 공용 1클립(`Mecha_Shoot`)으로 시작 (§8-3 취지 연장 — 상체 마스크라 구분 체감 없음).
  피격도 공용 1클립(`Mecha_Hit`, Additive 기준 포즈=프레임 0). G10/F10·G11/F11 분리는 후순위.
- UpperBody 레이어는 weight 고정 1 + `Fire` Bool로 Empty↔Shoot 전환 방식 (스크립트 weight 제어 없음).
- 제작: G10/F10 사격, G11/F11 피격.
- 상체 아바타 마스크 생성 → **UpperBody 레이어**(사격), **Additive Hit 레이어**(피격) 구성.
- 드라이버에 `EventBus<WeaponFiredEvent>`/`<PlayerDamagedEvent>` 구독 추가
  (OnEnable/OnDisable 짝 엄수) + Fire 유지창 로직 EditMode 테스트.
- **완료 기준**: 보행/비행 이동 중 사격이 하체를 끊지 않음. 피격이 이동을 멈추지 않음.

### P5 — 대쉬 ✅ 완료 (2026-07-28)
- **전환 우선순위 함정 (실제 발생):** 지상 대쉬 첫 프레임에 CharacterController 접지가 끊겨
  `IsGrounded=false`와 `DashTrigger`가 같은 프레임에 들어온다. Ground 상태의 전환 순서가
  `→Flight`가 먼저면 GroundDash 대신 Flight로 새므로 **DashTrigger 전환을 우선순위 맨 앞에** 둔다.
- 대쉬 종료 후 접지가 회복되지 않아 지상에서도 Flight로 복귀하는 것은 컨트롤러의 기존 동작
  (대쉬 = 순간 부양 기동, 재착지는 하강 입력) — 연출상 의도와 일치.
- 제작: G12~G15, F12~F15 (대쉬 8클립).
- **Dash BT** + `DashTrigger/DashX/DashZ` 배선 — `IsDashing` 상승 엣지 검출,
  0.4s 후 이전 상태 자동 복귀. 대각 입력 처리는 §8-5 결정대로.
- **완료 기준**: 4방향 대쉬 + 지상 대쉬의 "순간 부양" 연출 확인. 대쉬 종료 시 착지/비행
  상태 복귀가 올바름.

### P6 — 폴리시·통합·마감
- 전환 시간·블렌드 곡선 튜닝 (관성 0 조작감 기준 — 애니메이션이 입력 반응을 가리면 깎는다).
- Player를 **프리팹화**하거나 CityMap 씬에도 동일 배선 (Game/CityMap 두 씬 일치).
- 콘솔 워닝 정리, 전체 테스트 통과 확인, 커밋.
- **완료 기준**: 두 씬 모두에서 §3 전 클립이 의도대로 재생. 아래 QA 체크리스트 전 항목 통과.

### QA 체크리스트 (P6에서 전 항목 확인)
- [ ] 비행 8방 / 지상 8방 이동 애니메이션 재생
- [ ] 착지↔이륙 전환 팝핑 없음
- [ ] 이동+사격 동시 재생 (지상/비행 각각)
- [ ] 피격 중 조작 끊김 없음
- [ ] 대쉬 4방 (지상은 순간 부양 연출)
- [ ] 발 미끄러짐 허용 범위 내 (완전 제거는 목표 아님 — 서바이버 시점상 원경)
- [ ] 풀링/재활성화 후 이벤트 구독 중복 없음 (피격 애니 2회 재생 버그 체크)

### 단계 공통 검증 (Definition of Done)
매 단계 종료 시 CLAUDE.md §1 검증 루프를 돈다:
① `dotnet build "Mecha Survivor.sln"` 오류 0 → ② `read_console` 에러 0 → ③ `run_tests` 전부 통과.
새 `.cs` 추가 시 `refresh_unity` 후 빌드. 각 단계는 독립 커밋으로 마감한다.

**EditMode 테스트 최소 목록** (`MechaAnimParamsTests`):
- 전방 이동 시 MoveZ=1·MoveX=0, 시각 yaw 90° 회전 상태의 좌표 변환 정확성
- 정지 시 (0,0) 수렴, 입력 정규화(대각선) 클램프
- 대쉬 방향 → DashX/DashZ 사분면 매핑
- Fire 유지창: 이벤트 후 0.3s 내 true, 경과 후 false
