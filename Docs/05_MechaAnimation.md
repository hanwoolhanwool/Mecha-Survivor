/resuem# 05 — 주인공 기체(Mecha.fbx) 애니메이션 제작 계획서

작성일: 2026-07-28
대상 에셋: `Assets/_Project/Art/Models/Mecha/Mecha.fbx`
상태: **전 단계 완료 (2026-07-28, P0~P6)** — 30클립 제작·배선, Game/CityMap 두 씬 일치, QA 체크리스트 통과. 이후 클립 추가·튜닝 시 이 문서의 §4-확정 설정을 그대로 따른다.

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
use_armature_deform_only=True,                   # ★ 컨트롤 본 제외 (아래 함정 참조)
bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',   # ★ 아래 함정 참조
axis_forward='-Z', axis_up='Y',
```
- **★ 본 계층 함정 (2026-08-01 실제 발생, 07 문서 P0):** FBX의 본 계층이 `Mecha.fbx`의
  MechaAvatar(**최상위=Hips, 디폼 본 24개**)와 다르면 **Humanoid + CopyFromOther가 조용히
  실패한다** — AnimationClip 서브에셋이 0개가 되는데 **콘솔에는 에러가 안 뜬다.**
  현재 리그에는 `Root`(비디폼)와 IK/Pole/Aim 컨트롤 본 9개가 있으므로 둘 다 빼야 한다:
  IK/Pole/Aim은 `use_armature_deform_only=True`가 처리하지만, **`Root`는 Hips의 부모라
  이 옵션으로도 남는다** → 내보내기 전용 복제 리그에서 Root를 삭제하고 내보낸다
  (절차·컨텍스트 오버라이드 요령은 `07_HeroPose.md` §9-3).
  진단: `ModelImporter.importedTakeInfos.Length`(정상 1) / `LoadAllAssetsAtPath`의
  Transform 개수(정상 25, Root 포함 26). Generic으로 두면 정상 임포트되므로 오판하기 쉽다.
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

## 8. 결정 사항 (2026-07-28 전부 확정)

1. **후방 클립(G5/F5, 후방 대쉬)**: ✅ **전용 제작으로 해소** — P1·P2·P5에서 직접 제작.
2. **피격 강도 구분**: 일반 피격 1종으로 시작. 강피격은 **후순위 → §10-B2**.
3. **사격 클립의 무기별 변형**: 공용 1종(`Mecha_Shoot`)으로 시작. 무기별 분기는 **후순위 → §10-B1**.
4. **상승/하강 전용 자세**: ✅ 절차 연출(MechaVisuals 피치) 유지. 클립 추가는 **후순위 → §10-B5**.
5. **대쉬 대각선 처리**: ✅ 인접 2클립 블렌드 채택 (P5). 전용 클립은 **후순위 → §10-B3**.
6. **대각선 클립의 미러링**: ✅ **미러링 채택** (P3) — FL/BL만 실제작, FR/BR은 `mirror=true`.

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

### P6 — 폴리시·통합·마감 ✅ 완료 (2026-07-28)
- CityMap 씬에 Game과 동일 배선 완료 (MechaModel+Animator+드라이버, 캡슐 OFF, 절차 부유 OFF).
  프리팹화 대신 씬 직접 배선 채택 — Player가 씬별 참조(CameraDirector 등)를 갖고 있어 침습이 작다.
- 전환 시간은 계획값 그대로 확정 (지상↔비행 0.15s / 대쉬 진입 0.05s / 사격 진입 0.06s·복귀 0.15s).
- 콘솔 워닝 0. dotnet 빌드의 MSB3277 워닝 4건은 Unity 생성 csproj(Editor)의 어셈블리 버전
  충돌 노이즈로 우리 코드와 무관 — 수정 대상 아님.
- 전환 시간·블렌드 곡선 튜닝 (관성 0 조작감 기준 — 애니메이션이 입력 반응을 가리면 깎는다).
- Player를 **프리팹화**하거나 CityMap 씬에도 동일 배선 (Game/CityMap 두 씬 일치).
- 콘솔 워닝 정리, 전체 테스트 통과 확인, 커밋.
- **완료 기준**: 두 씬 모두에서 §3 전 클립이 의도대로 재생. 아래 QA 체크리스트 전 항목 통과.

### QA 체크리스트 (P6에서 전 항목 확인) — 2026-07-28 전 항목 통과
- [x] 비행 8방 / 지상 8방 이동 애니메이션 재생 — P1~P3에서 축·대각·미러 수치/시각 검증
- [x] 착지↔이륙 전환 팝핑 없음 — P2에서 착지→Ground→이륙→Flight 왕복 확인 (0.15s 블렌드)
- [x] 이동+사격 동시 재생 (지상/비행 각각) — P4: 상체 마스크로 하체 애니 유지 확인
- [x] 피격 중 조작 끊김 없음 — P4: 피격 중 Base 레이어 진행(normTime) 유지 확인
- [x] 대쉬 4방 (지상은 순간 부양 연출) — P5: 양 모드 진입·방향 캡처·자동 복귀 확인
- [x] 발 미끄러짐 허용 범위 내 — Speed(하한 1) 재생속도 보정 배선, 원경 기준 허용
- [x] 풀링/재활성화 후 이벤트 구독 중복 없음 — 드라이버 Subscribe/Unsubscribe가
      OnEnable/OnDisable 짝 (Player는 풀링 대상 아님)

### 단계 공통 검증 (Definition of Done)
매 단계 종료 시 CLAUDE.md §1 검증 루프를 돈다:
① `dotnet build "Mecha Survivor.sln"` 오류 0 → ② `read_console` 에러 0 → ③ `run_tests` 전부 통과.
새 `.cs` 추가 시 `refresh_unity` 후 빌드. 각 단계는 독립 커밋으로 마감한다.

**EditMode 테스트 최소 목록** (`MechaAnimParamsTests`):
- 전방 이동 시 MoveZ=1·MoveX=0, 시각 yaw 90° 회전 상태의 좌표 변환 정확성
- 정지 시 (0,0) 수렴, 입력 정규화(대각선) 클램프
- 대쉬 방향 → DashX/DashZ 사분면 매핑
- Fire 유지창: 이벤트 후 0.3s 내 true, 경과 후 false

---

## 10. 후순위 백로그 — 상세 사양 (착수 시 이 절이 기준)

P0~P6 완료 시점(2026-07-28)에 의도적으로 미뤄둔 확장들. 공통 전제: **제작·임포트는
§4-확정 설정을 그대로 따른다** (FBX_SCALE_ALL, 씬 프레임 범위=액션 범위, Humanoid+Copy
Avatar, Bake Into Pose 3종). 소스 액션은 `Mecha Ver 2.blend`에 fake user로 보존되어 있고,
보행/대쉬 포즈는 방향 벡터 파라미터의 절차 생성 함수로 만들어져 재활용 가능하다.

### B1. 무기별 사격 반동 변형 ✅ 완료 (2026-07-29)

- 구현 결과: 아래 사양대로 구현. `FireType` Int(0=Light/1=Launcher/2=Heavy, 값=우선순위),
  UpperBody 레이어 3상태(ShootLight/ShootLauncher/ShootHeavy) + 승격 전환 3개,
  Heavy는 비루프 + Exit Time으로 Empty 왕복 → 연사 유지 중 킥 반복 재생.
  매핑·경합은 `MechaAnimParams.GetFireGroup/ResolveFireGroup` (테스트 4개).
  구 `Mecha_Anim_Shoot.fbx`(공용 클립)는 미사용으로 남김 — 필요 시 삭제 가능.

| 그룹 | 무기 (WeaponData.Id) | 클립 | 사양 |
|---|---|---|---|
| Light (연사·지속) | `gatling`, `beam`, `laser_cannon`, `emp_field`, `gravity_well` | `Mecha_Shoot_Light` | 현 Mecha_Shoot 계열. 0.4s 루프, 반동 펄스 약화(±3°) |
| Heavy (단발 강반동) | `railgun`, `shotgun_cannon` | `Mecha_Shoot_Heavy` | 0.35s 비루프. 강한 킥백(팔 +12°, 상체 -8° 젖힘) 후 복귀 |
| Launcher (곡사) | `missile_pod`, `cluster_bomb`, `orbital_strike` | `Mecha_Shoot_Launcher` | 0.5s 루프. 팔 대신 상체 숙임 + 백팩(어깨) 강조 자세 |

- **컨트롤러**: UpperBody 레이어의 Shoot 단일 상태를 3상태로 분할. `FireType`(Int) 파라미터
  추가, Empty→각 상태 전환 조건 `Fire && FireType Equals n`. 복귀는 현행대로 `!Fire`.
- **드라이버**: `OnWeaponFired`에서 WeaponId→그룹 매핑. **동시 발사 우선순위 규칙 필수** —
  유지창(0.3s) 내 최고 우선순위 유지: `Heavy > Launcher > Light`. 매핑·우선순위 판정은
  `MechaAnimParams`에 순수 함수로 빼서 테스트한다 (매핑 사전은 정적 캐싱 — Update 내 할당 금지).
- **테스트**: 그룹 매핑, 우선순위 경합(유지창 내 Light→Heavy 승격, 역방향 비강등), 미지 ID 폴백(Light).
- **규모**: 클립 2종 신규 + 컨트롤러/드라이버 소폭. 약 반나절.

### B2. 강피격 (대미지 크기별 변형) ✅ 완료 (2026-07-29)

- 구현 결과: **옵션 A 채택** — `PlayerDamagedEvent`에 `Damage` 필드 추가 (기본값 0).
  `Mecha_Hit_Heavy`(0.5s Additive) + `HitHeavyTrigger`, Empty 전환 우선순위 강피격 먼저.
  판정은 `MechaAnimParams.IsHeavyHit` (기본 15%, 드라이버 인스펙터 튜닝 가능,
  부동소수점 경계 보정 -1e-3 포함).
- **부수 버그 수정**: 이벤트가 `AddMaxHealth`(최대체력 업그레이드)에서도 발생하고 있어서,
  기존에는 **체력 업그레이드를 받을 때마다 피격 애니·피격음·카메라 셰이크가 재생**되고
  있었다. `Damage=0`을 체력 갱신 알림으로 규정하고 드라이버·AudioDirector·
  CameraShakeReactor에 `Damage > 0` 가드를 넣어 함께 해결 (GameHud는 갱신 알림도
  받아야 하므로 가드 없음).
- **판정 기준**: 대미지 ≥ 최대체력의 15% → 강피격 (수치는 튜닝 대상, 순수 함수로 분리).
- **클립**: `Mecha_Hit_Heavy` — 0.5s 비루프 Additive (기준 포즈=프레임 0, §4-확정의 Hit와
  동일 임포트: `hasAdditiveReferencePose=true`). 몸통 -14° 젖힘 + 머리 -18° + 팔 크게 벌어짐.
  어택 2프레임, 감쇠 나머지 — 프레임 끝은 반드시 중립 복귀.
- **컨트롤러**: Hit 레이어에 `HitHeavy` 상태 + `HitHeavyTrigger` 추가.
  Empty→HitHeavy 전환을 Empty→HitReact보다 **우선순위 앞에** 둔다 (P5 함정 §P5 참조).
- **테스트**: 임계값 판정(경계값 포함), 옵션 B 채택 시 회복 무시 검증.
- **규모**: 소. 2~3시간.

### B3. 대각 대쉬 전용 클립 (총 38클립화) ✅ 완료 (2026-07-29) → **배선 롤백 (2026-08-05)**

- **롤백 사유**: B7 대쉬 히어로화 이후 축 4방(히어로 런지)과 대각 전용 클립(구 절차 포즈)의
  스타일이 맞지 않아 사용자 지시로 배선을 되돌렸다. 두 Dash BT에서 대각 4자식(±0.71) 제거 →
  **4자식(축만)으로 복귀, 대각 입력은 §8-5 원안대로 인접 2클립 블렌드**(히어로 포즈끼리 블렌드).
  FBX 4개·클립 8개(GDashFL/BL·FDashFL/BL + 미러 FR/BR)는 **미사용으로 잔존** (B1의 구
  Mecha_Anim_Shoot와 같은 처지 — 대각 히어로 포즈 제작 시 재사용 여지). 드라이버 무변경.

- 구현 결과: 아래 사양대로 — GDashFL/BL·FDashFL/BL 실제작(절차 함수에 대각 방향 벡터),
  FR/BR은 같은 FBX의 `mirror=true` 클립. 두 Dash BT 8자식(축 4 + 대각 ±0.71).
  드라이버 무변경 (DashX/DashZ가 이미 전 방향 단위 벡터 공급).
- **제작**: 지상/비행 각 FL·BL 실제작 + FR·BR 미러 = FBX 4개, 클립 8개 (P3 방식 그대로 —
  같은 FBX에 `mirror=true` 클립 2개씩).
  포즈는 기존 절차 생성 함수 재사용: `ground_dash`/`fly_dash`에 `dir=(±0.7071, ∓0.7071)`을
  넣으면 끝 — 기울임 축이 방향 벡터에서 유도되므로 함수 수정이 필요 없다.
- **배선**: 두 Dash BT에 대각 4지점(±0.71) `AddChild`만. 드라이버 변경 없음
  (DashX/DashZ는 이미 단위 벡터 전 방향 공급).
- **규모**: 소. 1~2시간.

### B4. 지상/비행 사격·피격 분리 (원계획 G10/F10·G11/F11)

- **현재 상태**: 공용 1종씩. 사격은 상체 마스크, 피격은 Additive라 지상/비행 차이가
  구조적으로 거의 드러나지 않는다.
- **판단**: 마스크 구조를 유지하는 한 분리 실익이 없어 **사실상 보류 권장**.
  착수할 가치가 생기는 경우는 "지상 사격 시 하체가 반동을 버티는 스탠스"를 원할 때인데,
  이는 상체 마스크가 아니라 전신 오버레이(또는 하체 IK)로의 재설계라 비용이 크다.
- **경량 대안**: 마스크 유지 + UpperBody 레이어 Shoot를 `IsGrounded`로 2상태 분기
  (지상 견착만 살짝 낮고 단단하게). 클립 1종 추가로 끝나며 B1과 병행 가능.

### B5. 상승/하강 전용 자세 클립 ✅ 완료 (2026-07-29)

- 구현 결과: 아래 사양대로 — `VerticalLean` Additive 레이어(4번째) + Simple1D BT
  (`VerticalY`: Descend −1 / 중립 0 / Ascend +1). 중립 지점은 **무모션이 아니라
  전 프레임 중립 포즈의 3번째 클립**(`Mecha_Fly_VNeutral`) — 1D BT 중앙에서 양쪽이
  50/50 섞이는 것을 막는다.
- **Additive 포즈 클립 제작 요령 (확정)**: 테이크 프레임 0 = 중립(기준 포즈)로 만들고
  임포터에서 `firstFrame`을 루프 시작(6)으로 잘라 기준 프레임을 루프 밖에 둔다
  (`additiveReferencePoseFrame=0`, 클립 6~66). 기준을 루프 안에 두면 루프 경계에서
  중립으로 꺼지는 딥이 생긴다.
- 드라이버: `ComputeVerticalLean` — **접지 중엔 0 고정**(보행에 수직 자세 금지),
  비행 중 ±1 클램프. 댐핑 0.08s.
- **방법**: Flight BT를 3D로 바꾸지 말 것 (2D Freeform 8방 구조 유지가 우선).
  대신 **별도 Additive 레이어**에 1D BT(`VerticalY` 파라미터, -1~1)를 얹는다:
  `Mecha_Fly_Descend`(-1) / 중립(0, 모션 없음) / `Mecha_Fly_Ascend`(+1).
- **클립**: 포즈성 루프 2s ×2, Additive 기준 프레임 0. 상승=다리 아래로 뻗음+가슴 젖힘,
  하강=다리 접음+전방 숙임. 진폭은 절차 피치와 합산되므로 작게(±8° 급) 시작.
- **드라이버**: `VerticalY` 폴링 공급 (`MechaController.VerticalInput`, 댐핑 0.08s).
- **규모**: 중소. 반나절.

### B7. 대쉬 히어로 런지화 (07 문서 P4 = B안) ✅ 완료 (2026-08-05)

- 구현 결과: 아래 사양대로 — 축 8액션 재작성(각 fcurve 91개 = 22본 쿼터니언 + Hips location),
  `*_v1` 백업 8종 보존, FBX 8종 재익스포트. 테이크명(Scene)·클립 설정이 유지되어
  **Unity 측 변경 0** (컨트롤러·드라이버·임포트 설정·씬 모두 무변경, refresh만).
  임포트 검증: 8클립 전부 클립 1/Transform 25/0.40s/Human/비루프. Play 스모크:
  RigLab에서 Fly_Dash_L t=0.3 재생 — 히어로 L 포즈 그대로, Hips 치우침 정상
  (`Docs/Renders/HeroPose/P4_unity_dash_FlyL.png`). 검증: 빌드 0 / 콘솔 0 / EditMode 258 통과.
  F 트위스트 교차 검증: 이등분 pole로 좌 18/18/18·우 23/34/46 — §10-2-1(07) 기록값 재현.

- **목적**: 축 4방 대쉬 8클립(지상/비행 × F/B/L/R)의 몸짓을 방향별 히어로 런지 포즈
  (07 §9-2 F / §10-2-1 L·R·B)로 교체 — "총을 든 채 이동하는 역동성"을 인게임 대쉬에서 재현.
- **무변경 원칙**: 액션명(`Mecha_Ground_Dash_*`/`Mecha_Fly_Dash_*`)·FBX명·테이크명(Scene)·
  클립 설정(0~12f, 비루프, Bake Into Pose 3종)을 전부 유지하고 **내용만 교체**한다.
  → 컨트롤러·드라이버·임포트 설정 코드 전부 무변경 (재익스포트 후 refresh만).
- **클립 구조**: 0.4s(0~12f @30fps) 비루프. **f0=포즈 85% → f3=100% → f12=100% 홀드**
  (진입 스냅은 대쉬 전환 0.05s가 담당, 85% 시작은 진입 블렌드에 미세한 "밀어넣기"를 준다).
  사웨이 없음. Hips location 포함(부양 +0.20/치우침 ±0.10 — 07 포즈 클립에서 리타게팅 검증됨.
  구 대쉬 클립은 회전만 있었으나 Bake Into Pose 설정이 같아 임포트 경로 동일).
- **다리 규약**: 07 §8 aim2 + §10-2-1 pole (L/R 트레일 다리만 명시 오버라이드).
  **F는 양다리 이등분 pole 그대로** — §10-2-1의 "F에도 적용하면 좌 18/18/18·우 23/34/46"은
  이등분 pole 기준값으로 실측 재현 확인 (F 트레일 다리 이등분 pole은 (-0.1,-0.68,-0.73)로
  이미 전방-아래를 향해 내회전 문제가 없다 — 오버라이드는 옆으로 쓸린 스트레이프 전용).
  ※ 전시용 F 포즈 클립도 2026-08-05 같은 보정으로 소급 갱신 완료 (07 §9-2) — 전 클립 일치.
- **지상/비행 동일 내용**: 지상 대쉬 = 순간 부양 기동(§1)이므로 공중 런지 포즈를 그대로 수용.
  차별화(지상 저고도 등)는 플레이 확인 후 필요 시.
- **대각(FL/BL/FR/BR)**: 1차에는 기존 절차 포즈를 유지했으나 스타일 불일치로
  **2026-08-05 배선 롤백** — 대각 입력은 인접 히어로 클립 2개의 블렌드 (B3 롤백 기록 참조).
  대각 전용 히어로 포즈 제작은 07 §10-2의 대각 결정과 함께 별도 진행.
- **백업**: 교체 전 기존 8액션을 `*_v1`로 복제(fake_user) — blend는 저장소 밖이라 git 복구 불가.
- **함정 체크**: 05 P5 전환 순서(DashTrigger 우선)·스냅 파라미터는 클립 교체와 무관하게 유지.
  §4-확정 ★ 2건(FBX_SCALE_ALL·씬 범위=액션 범위) 준수.

### B8. 무기별 전신 사격 포즈 — 레이저 캐논 히어로 포즈 ✅ 완료 (2026-08-05)

- **목적**: 포즈 무기(1차: `laser_cannon`)를 **쏘는 동안 + 발사 후 유지창(기본 2s)** 동안
  전신을 방향별 히어로 포즈로 고정한다. 무기별 애니메이션 확장의 기반 구조.
- **확장 규약 (새 무기 포즈 추가 절차 — 이 3개가 전부)**:
  1. ~~`MechaAnimParams`에 포즈 상수 1개 + `FirePoses` 사전에 1줄~~
     → **B10에서 대체됨**: 포즈는 `(WeaponData.Grip, 장착 손)`이 정한다. 기존 무기에
     포즈를 붙이려면 `Grip`만 채우면 되고, ①은 *새 포즈 종류*를 만들 때만 필요하다
     (상수 1개 = `FirePoseXxx`, 값 = 우선순위).
  2. 포즈 클립 제작·임포트 (§4-확정 그대로).
  3. `AC_Mecha`의 **WeaponPose 레이어**에 상태 1개 추가 — Empty→상태 전환 조건
     `PoseType Equals n`(진입 0.12s), 상태→Empty 전환 `PoseType NotEqual n`(이탈 0.25s).
     드라이버·씬은 무변경.
- **컨트롤러**: `WeaponPose` 레이어를 **UpperBody 다음, Hit(Additive) 앞**(인덱스 2)에 삽입 —
  전신 Override(마스크 없음, weight 1)라 포즈 중 상체 반동(ShootLight)을 덮고, 피격/수직
  Additive는 그 위에 계속 얹힌다. `HeroPose` 상태 = 2D Freeform Directional BT(MoveX/MoveZ):
  중앙 `Mecha_Pose_HeroIdle`, 축 4방 `Mecha_Pose_HeroLunge{,_L,_R,_B}` — 대각 입력은 인접
  블렌드(B3 롤백과 동일 방침). 새 파라미터 `PoseType`(Int, 0=None).
  ※ FL(−0.71, 0.71)만 예외로 전용 클립 `Mecha_Pose_HeroLunge_FL` 배선 — B8-1 참조.
- **드라이버**: `OnWeaponFired` → 포즈 코드(B10부터 `evt.PoseType`)가 None이 아니면 포즈 유지창 갱신
  (`_poseHoldSeconds`, 인스펙터 기본 2s). 경합은 반동 그룹과 동일 규칙(`ResolveFirePose` —
  유지창 내 승격만). 유지창이 닫히면 `PoseType=0` 복귀. 매핑 없는 무기는 포즈에 영향 없음.
- **요 정책 (방향별 포즈의 성립 조건)**: 포즈 유지창 동안 `MechaVisuals`가 시각 요를
  **조준(카메라) 방향에 고정**한다 (`MechaAnimParams.SelectVisualYaw`,
  `MechaVisuals._animationDriver` — 비우면 컨트롤러의 드라이버 자동 참조로 씬 무변경).
  요가 이동을 따라가면 MoveX/MoveZ가 (0,1)로 수렴해 F 포즈만 보인다 (07 §10-1 원칙:
  총구·시선 고정 + 몸짓만 이동 방향). 평시 정책(이동 방향 추종)은 그대로.
- **검증 (2026-08-05)**: 빌드 0 / 콘솔 0 / EditMode **264개 전부 통과**(신규 6 — 포즈 매핑·
  경합·요 선택). Play 스모크: `laser_cannon` 발사 이벤트 중 좌이동 유지 → 요 고정 상태로
  MoveX −0.90 유지 = L 포즈 지속 재생 확인 (`Docs/Renders/HeroPose/B8_unity_pose_L.png`).
  참고 — 무기는 입력 홀드 발사라 자동 발사가 없다: Play 검증 시 `WeaponFiredEvent`를
  수동 발생시킬 것 (`EventBus<T>.Raise(ref evt)`).
- **알려진 한계 / 튜닝 여지**: ① 비행 중 정지 사격 시 중앙 클립이 지상 스탠스(HeroIdle) —
  어색하면 비행 전용 중앙 클립 분기 검토. ② 포즈 중 대쉬는 WeaponPose가 덮는다
  (대쉬 클립도 같은 히어로 런지라 실루엣 연속 — 의도 수용). ③ 유지창 동안 요가 카메라에
  고정되므로 레이저 연사 빌드에서는 사실상 상시 조준 고정(구 C안과 유사)이 된다 —
  체감이 다르면 `_poseHoldSeconds`로 조절.

### B8-1. FL 대각 하체 수정 — 전용 클립 `Mecha_Pose_HeroLunge_FL` ✅ 완료 (2026-08-06)

- **문제 (사용자 지적)**: 정면 좌측 대각(FL) 이동 중 포즈의 하체가 부자연스러움.
  원인 — F와 R은 다리 역할이 같아(오른다리 리드 턱 + 왼다리 트레일 신전) FR 인접 블렌드는
  다리끼리 맞아떨어지지만, L은 반대(왼다리 리드)라 **FL 블렌드에서 리드/트레일이 본별로
  충돌**해 하체가 무너진다. F가 양쪽 블렌드에 공유되므로 L 클립 수정만으로는 해결 불가.
- **해법 (사용자 지시 = FR 하체 미러)**: FL 지점(−0.71, 0.71)에만 전용 클립을 배선.
  Blender에서 프레임별(0~60) 합성 — **하체(Hips + 다리 8본 + Hips location) =
  slerp(F, R, 0.5)의 좌우 미러**, **상체 13본(척추·목·머리·양팔) = slerp(F, L, 0.5)**
  (기존 블렌드 결과 유지라 조준·상체는 변화 없음). 나머지 대각 3방(FR/BL/BR)은 인접 블렌드 유지.
- **이 리그의 미러 공식 (실증)**: 본 이름 Left↔Right 교차 + 쿼터니언 `(w, x, −y, −z)`,
  Hips location은 X 성분 부호 반전 — L/R 액션의 트레일 다리(스펙상 정확한 미러)에서 성분
  일치로 검증. 검증: 루프 봉합 f0=f60 / 사웨이 5.0cm(동일 진폭·기준높이) / 무릎캡 전방.
- **임포트 함정 (신규)**: `animationType=Human` + `sourceAvatar`만 설정하면
  **`avatarSetup`이 CopyFromOther로 안 잡혀 조용히 실패**한다 — 증상은 Root 함정과 동일
  (클립 0·`importedTakeInfos` 0·콘솔 무에러, Generic으로 두면 정상). Humanoid 전환 시
  `avatarSetup = ModelImporterAvatarSetup.CopyFromOther`를 반드시 명시할 것.
- **검증 (2026-08-06)**: 빌드 0 / 콘솔 0 / EditMode 271 통과. Unity 에디트 모드 샘플링으로
  FL(신규 클립) vs FR(기존 블렌드) 정면 렌더가 정확한 미러 실루엣 확인
  (`Docs/Renders/HeroPose/FL_unity_front.png`·`FR_unity_front.png`, Blender 원본
  `FL_f0_*.png`). 드라이버·씬·코드 무변경. 잔여: HeroPoseLeft(LH) BT와 대쉬 대각에는
  미적용 — 필요해지면 같은 레시피로 확장.

### B9. 왼손 무기 히어로 포즈 세트 — 미러 클립 + HeroPoseLeft ✅ 완료 (2026-08-05)

- **목적**: 미래 왼손 무기 대비 — B8 확장 규약 3단계 중 ②(클립)·③(상태)을 선반영하고,
  ①(무기 매핑)은 비워둔다.
  → **B10(2026-08-08)에서 활성화**: 매핑이 아니라 `WeaponData.Grip = OneHanded` +
  홀수 슬롯(로드아웃 둘째 무기)이면 자동으로 이 포즈가 걸린다.
- **클립 (미러 채택 — 사용자 결정)**: 오른손 5클립을 같은 FBX 임포트 설정에서
  `mirror=true`로 복제 (§8-6 대각 클립과 동일 방식, Blender 무변경). 왼손 무기는 좌우
  반전이 해부학적으로 정확한 경우라 미러가 의미상 올바르다 (07 §10-6 참조).
  `Mecha_Pose_HeroIdle_LH` / `Mecha_Pose_HeroLunge_LH`(F) / `_LH_L`(원본 `_R` 미러) /
  `_LH_R`(원본 `_L` 미러) / `_LH_B` — **L/R은 기울기가 뒤바뀌므로 기능명을 교차**했다.
- **컨트롤러**: WeaponPose 레이어에 `HeroPoseLeft` 상태 추가 — B8과 동일 구조의
  2D Freeform Directional BT(MoveX/MoveZ, 중앙 Idle_LH + 축 4방), Empty↔ 전환 조건은
  `PoseType Equals/NotEqual 2` (진입 0.12s / 이탈 0.25s).
- **코드**: `MechaAnimParams.FirePoseHeroLeft = 2` 상수만 추가 (값 = 우선순위 — 유지창 내
  오른손 히어로(1)보다 승격). 드라이버·씬 무변경.
- **주의 — 마운트는 별개**: 포즈는 팔만 움직인다. 실제 왼손 무기 추가 시
  `RigProfile_Mecha`에 LeftHand 본 마운트 엔트리도 함께 만들어야 무기 모델이 왼손에 붙는다.
- **검증 (2026-08-05)**: 빌드 0 / 콘솔 0 / EditMode 271 통과(신규 1 —
  `FirePoseHeroLeft_IsDistinctAndPromotesOverHero`). RigLab Play에서 `PoseType=2` 강제 →
  `HeroPoseLeft` 진입·미러 재생 확인. 렌더: `Docs/Renders/HeroPose/LH_unity_pose_idle_close.png`
  ·`LH_unity_pose_idle_frontleft.png`, 오른손 비교 `RH_unity_pose_idle_close_ref.png`.

### B10. 손 지정 = 로드아웃 순서, 포즈 = 파지 방식 × 손 ✅ 배선 완료 (2026-08-08)

사용자 요구: **격납고 로드아웃의 첫 무기 = 오른손, 둘째 = 왼손.** 총 종류면 그 손의 히어로
포즈, 두손 총이면 두손 포즈.

- **포즈 선택의 근거를 바꿨다 (B8 확장 규약 ① 대체)**: 무기 ID 사전
  (`MechaAnimParams.FirePoses`)은 **제거**됐다. 같은 무기라도 로드아웃 순서에 따라 손이
  바뀌므로 포즈는 무기 고유값일 수 없다 — 이제 `(파지 방식, 장착 손)`의 함수다.
  ```
  MechaAnimParams.ResolveWeaponPose(WeaponGrip grip, WeaponHand hand)
      OneHanded + Right  → FirePoseHero            (1)
      OneHanded + Left   → FirePoseHeroLeft        (2)
      TwoHanded + Right  → FirePoseHeroTwoHand     (3)
      TwoHanded + Left   → FirePoseHeroTwoHandLeft (4)
      None               → FirePoseNone            (0)
  ```
  **새 무기 포즈 확장 절차**: `WeaponData.Grip`만 채우면 끝 (코드·AC 무변경).
  새 *포즈 종류*를 늘릴 때만 상수 1개 + AC 상태 1개 (B8 규약 ②③은 그대로).
- **파지 방식 (`WeaponData.Grip`)**: 총 종류 여부와 손 개수를 한 필드로 합쳤다.
  분류 — `OneHanded`: 개틀링·레이저 캐논·레일건 / `TwoHanded`: 산탄 캐논·대출력 빔 /
  `None`: 미사일 포드·트윈 로켓·클러스터 폭탄·EMP 필드·그래비티 웰·궤도 폭격.
- **손 배정 (`WeaponSlots.HandForSlot`)**: 짝수 슬롯 = 오른손, 홀수 = 왼손. 로드아웃은
  기재 순서대로 슬롯 0·1을 채우므로 이 규칙이 곧 "먼저 쓴 무기 = 오른손"이다. 확장 슬롯
  2·3도 교대를 잇는다. 주입은 `Equip`/`ReplaceSlot` 안에서 **한 번만** — 장착 경로가
  여럿(초기 자동/업그레이드/교체)이라 호출부에 흩어 놓으면 순서와 어긋난다.
- **이벤트 전달**: `WeaponFiredEvent`에 `PoseType`(int) 추가 — 파지 방식과 손을 둘 다 아는
  건 발사 측(`Weapon.TryFire`)뿐이다. Core는 값을 나르기만 하고 의미는 Gameplay가 정의한다.
  기본값 0이라 기존 호출부·구독자 전부 무영향. 드라이버는 사전 조회 대신 `evt.PoseType`을 쓴다.
- **두손 포즈(PoseType=3·4)** ✅ — `HeroPoseTwoHand`(오른그립) + `HeroPoseTwoHandLeft`(왼그립)
  두 상태, 클립 각 5종. 제작 레시피·리그 실측 제약·함정은 **07 §10-7**이 원본.
- **★ 두손도 손을 가린다 (2026-08-08 수정)**: 처음엔 "두 손을 다 쓰니 손 구분이 없다"고 보고
  `TwoHanded`가 손을 무시했는데, **좌우 마운트가 생기면서(06 §3.4) 전제가 깨졌다** —
  두 손을 쓰더라도 **그립을 쥐는 손은 하나**고 무기 모델은 그 손에 붙는다.
  방치하면 `loadout_gatling`의 대출력 빔(슬롯 1 = 왼손)이 **"빈 오른손으로 쥐는 자세 +
  왼손에 매달린 총"** 이 되고 총구도 반대쪽(`beam@L`)에서 나간다.
- **알려진 갭 (이번 범위 밖 — 별도 결정 필요)**:
  1. ~~왼손 무기 모델 마운트 없음~~ → ✅ **해소 (2026-08-08)**: 손에 드는 무기 3종에
     미러 왼손 마운트를 추가하고 `WeaponMountVisuals`를 손 기준으로 분기했다.
     사양·미러 산출·함정은 **06 §3.4**가 원본.
  2. **오른손 마운트 겹침 (기존 결함)**: `RightHandWeapon`/`RightHandGatling`/`RightHandBeam`이
     같은 본·같은 로컬값이라 개틀링+빔 로드아웃(`loadout_gatling`)에서 두 모델이 **동시에
     오른손에 겹쳐 표시**된다. 손 지정과 무관하게 이전부터 있던 문제 — 다만 빔이 두손 무기라
     실질적으로는 "두손 무기와 한손 무기를 같은 손에 쥔 로드아웃"이라는 데이터 모순이 먼저다.
  3. **포즈 경합**: 유지창 내 승격만(B8 규칙) 유지 — 두손(3) > 왼손(2) > 오른손(1).
     개틀링(연사)+빔(두손) 조합에서 빔을 한 번 쏘면 개틀링 연사가 유지창을 계속 갱신해
     두손 포즈가 길게 남는다. 체감이 나쁘면 "최신 발사 우선 + 최소 유지시간"으로 규칙 교체 검토.
- **검증 (2026-08-08)**: 빌드 오류 0(경고 4 = 기존 MSB3277 노이즈) / 콘솔 에러 0 /
  EditMode **315개 전부 통과**(신규 7 — 파지×손 포즈 해석 3, 슬롯→손 배정 1,
  `WeaponPoseWiringTests` 3: PoseType↔AC 상태 일치·두손 BT 구성·무기 파지 분류).
  **배선 가드가 중요한 이유**: 포즈 코드(C#)와 상태 진입 조건(컨트롤러 에셋)이 다른 파일에
  있어서 한쪽만 바뀌면 콘솔 에러 없이 포즈만 안 나온다.

### B11. 무기별 발사 모션 — 산탄 캐논 반동 킥 + 펌프 재장전 ✅ 완료 (2026-08-08)

사용자 요구: **"두손 무기 애니메이션은 있지만 샷건 모션 애니메이션은 없다."**

- **문제의 정체**: `WeaponPose` 레이어는 **마스크 없는 전신 Override(weight 1)** 라서
  UpperBody 반동 상태(ShootLight/Launcher/Heavy)를 **통째로 덮는다**. 그래서 포즈 무기는
  쏘는 동안 **정지한 브레이스 루프만 재생**된다 — 산탄 캐논은 쿨다운 2.4초 단발이라
  이 정지가 특히 크게 보인다. 반동을 보이게 하려면 **같은 레이어의 단발 상태**로 넣어야 한다.
- **포즈 vs 모션 (개념 분리)**: `FirePose*`는 *쏘는 동안 유지하는 자세*(루프),
  `FireMotion*`은 *한 발마다 재생되는 단발 동작*(0.9초 비루프)이다.
  포즈는 `(파지 방식, 손)`이 정하지만 **모션은 무기가 정한다** — 같은 두손 무기라도
  대출력 빔은 지속 빔이라 펌프 동작이 어울리지 않는다. 좌우 그립 구분은 이미 PoseType(3/4)이
  하고 있어서 컨트롤러가 `ShotgunFire` / `ShotgunFireLeft` 를 알아서 고른다.
- **승인 타임라인 (30fps, 0~27프레임 = 0.9초)**:
  `0.00s` 브레이스 → `0.067s(f2)` 킥 정점(총구 +16°, 상체 후경, Hips −총축 9cm) →
  `0.20s(f6)` 반동 흡수 → `0.27s(f8)` 언더슈트(−22%) →
  `0.40s(f12)` 왼손 슬라이드 후퇴 8.5cm → `0.60s(f18)` 폐쇄 충격(총구 −4°) →
  `0.87s(f26)` 브레이스 복귀.
- **제작 (07 §10-8이 원본)**: 브레이스 f0 포즈를 기준으로 삼고 킥·펌프를 얹은 뒤
  **양팔만 2본 IK로 다시 푼다**. 방향 5종(Idle/F/L/R/B) 각각 제작 — 왼손 접점이 총 표면에
  붙어 있어야 해서 방향별 재계산이 필수다(Additive 한 장으로는 비F 방향에서 손이 총을 벗어난다).
  왼그립 5종은 `mirror=true` 임포트(L/R 기능명 교차, B9 규약 그대로).
- **컨트롤러**: WeaponPose 레이어에 `ShotgunFire` / `ShotgunFireLeft` 상태 —
  두손 포즈와 같은 2D Freeform Directional BT(MoveX/MoveZ, 중앙 + 축 4방).
  - 진입: `HeroPoseTwoHand(Left)` → `[FireMotionTrigger + FireMotion Equals 1]` 0.08s,
    **그리고 `Empty` → 같은 조건 + `PoseType Equals 3/4`** — 포즈 유지창이 닫힌 뒤의 첫 발이
    브레이스를 거치느라 0.12초 늦게 반동하는 것을 막는다.
  - 이탈: `PoseType NotEqual 3/4` → Empty (0.25s, **전환 배열 맨 앞**) /
    Exit Time 1.0 → 브레이스 복귀 (0.15s).
  - **전환 순서가 곧 동작이다**: 트리거 전환이 포즈 이탈 전환보다 뒤에 있으면 조용히 가로채인다.
    `WeaponFireMotionTests`가 `transitions[0]` 을 직접 검사한다.
- **클립이 브레이스 f0에서 시작·종료한다**: f0 = f27 = 해당 방향 브레이스의 프레임 0이라
  진입/복귀 양쪽이 봉합된다(Unity 실측 오차 4.5mm = Humanoid 리타게팅 양자화).
  대신 0.9초 동안 부유 사웨이가 멈추는데, 반동 자체가 큰 수직 운동이라 문제되지 않는다.
- **코드**: `MechaAnimParams.GetFireMotion(weaponId)` + 드라이버가
  `FireMotion`(Int) → `FireMotionTrigger`(Trigger) **순서대로** 쓴다(순서가 바뀌면 한 프레임 놓친다).
  `GetFireGroup`과 달리 **미지 무기는 폴백하지 않는다**(None) — 신무기가 남의 펌프 동작을
  물려받으면 총도 안 든 채 슬라이드를 당긴다.
- **★ 함정 — 내보내기 리그의 오브젝트 트랜스폼**: 원본 리그 `Riging_Meshy_IK` 는 Blender에서
  **x = 2.5178 에 놓여 있다.** 내보내기 복제본이 이 오프셋을 물고 나가면 Humanoid가 Hips를
  아마추어 공간 좌표로 구워서 **Unity에서 발사할 때마다 기체가 2.5m 옆으로 순간이동**한다
  (콘솔 무에러, 클립·본 수·리타게팅 전부 정상으로 보인다). 내보내기 복제본은 **반드시
  `matrix_world = Identity`** 로 만들고 어서션할 것. 진단은 `hips.position.x` 실측.
- **★ 함정 보강 — EDIT 모드 진입**: 07 §9-3의 `temp_override(active_object=…, selected_objects=…)`
  **만으로는 부족하다.** `mode_set`이 `{'FINISHED'}`를 반환하면서도 모드는 OBJECT로 남고
  `edit_bones`가 빈 컬렉션이 된다 → Root·힙아머 본이 안 지워져 디폼 27본이 나간다.
  뷰레이어의 **실제 선택·활성 상태**(`select_set(True)` + `view_layer.objects.active`)를
  만든 뒤 오버라이드해야 진입한다. 진단은 `len(edit_bones)` 어서션.
- **산출물**: 액션 `Mecha_Fire_ShotgunPump{,_L,_R,_B}` · `Mecha_Fire_ShotgunPumpIdle`
  (0~27 / 30fps / fcurve 91) → `Rigged/Mecha_Anim_ShotgunPump{,Idle,_L,_R,_B}.fbx`
  (전부 take 1 / Transform 25 / 0.90s / 비루프) + 미러 5클립 = **클립 10종**.
- **검증 (2026-08-08)**: 빌드 오류 0(경고 4 = 기존 MSB3277 노이즈) / 콘솔 에러 0 /
  EditMode **345개 전부 통과**(신규 8 — `WeaponFireMotionTests`).
  - Blender 전 방향·전 프레임 제약: 왼손 도달 ≤ 0.4545 (한계 0.4557) / 팔꿈치–총 ≥ 0.234
    (총 반경 0.085) / 머리–총 ≥ 0.232 / IK 오차 0 / f0·f27 = 브레이스 f0 (오차 0.00003°).
  - Unity 실측(에디트 모드 샘플링): 총구 최대 상승 16.4~16.8° / 손 간격 0.245 → 0.167
    (펌프 행정) / Hips 최대 이동 0.090 m / 봉합 4.5mm.
  - Play 스모크(Game 씬): `WeaponFiredEvent("shotgun_cannon", 3)` 수동 발생 →
    `PoseType=3 / FireMotion=1` → `ShotgunFire` 진입 → Exit Time 후 `HeroPoseTwoHand` 복귀 확인.
  - 렌더 `Docs/Renders/ShotgunFire/unity_F_f{00,02,06,12,18}_{rside,fl34}.png`
    (실제 산탄 캐논 모델을 오른손 마운트에 붙여 촬영).
- **알려진 한계**: ① 포즈 경합(B10 갭 3)이 그대로 적용된다 — 왼그립 두손 무기가 먼저 쏴서
  PoseType이 4로 승격된 상태에서 오른손 산탄 캐논이 쏘면 `ShotgunFireLeft`가 나온다.
  ② 대각 4방은 인접 블렌드(B8-1의 FL 전용 클립에 해당하는 보정 미적용 — 하체가 브레이스와
  동일하므로 같은 증상이 나오면 같은 레시피로 확장). ③ 쿨다운이 0.9초 밑으로 내려가면
  모션이 겹친다(현재 기본 2.4초라 여유 있음 — 겹치면 트리거가 큐잉되어 한 번 더 재생된다).

### B6. 폴리시 잔여 (알려진 한계 — 필요 시)

1. **이동속도 업그레이드와 보행 재생속도**: `ComputeSpeed`의 기준이
   `HorizontalSpeed`(배율 포함)라 속도 업그레이드가 쌓여도 Speed=1 → 실속도 18m/s에서도
   보행이 1배속이라 슬라이드가 는다. 개선: 기준을 기본속도(14) 고정으로 바꿔 배율만큼
   재생을 가속 (하한 1 유지 필수 — Idle 정지 방지 §5 표 참조).
2. **피격 연타 재트리거**: `HitReact` 재생 중 재피격은 Exit 후 지연 재생된다(트리거 잔존).
   즉시 리스타트를 원하면 HitReact self-transition(Interruption Source: Current State) 추가.
3. **대쉬 후 지상 미복귀**: 대쉬 첫 프레임에 접지가 끊겨 종료 후 Flight로 복귀하는 것은
   컨트롤러의 기존 동작(재착지는 하강 입력). 애니메이션 범위 밖 — 바꾸려면 컨트롤러의
   접지 판정(코요테 타임 등) 설계 변경이 필요하며 조작감 영향이 있으니 별도 논의.
4. **Idle 개성 모션**: 장시간 정지 시 간헐 모션(무기 점검·주변 스캔 등)은 UpperBody
   레이어에 랜덤 트리거 상태로 얹을 수 있다 — 콘텐츠성 폴리시, 우선순위 최하.
