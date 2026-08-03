# 08 — 신규 기체 MkII (MechaMkII) 애니메이션 배선 계획서

작성일: 2026-08-02
대상 에셋: `Assets/_Project/Art/Models/MechaMkII/`
Blender 소스: `C:\Users\iam12\Desktop\Mecha Survival\Speed\Ver1\Mecha Ver 2 [IK rig backup 0801].blend`
제작 계획서(Blender 쪽): 같은 폴더의 `Animation_Plan.md`

상태: **2026-08-03 에디터 산출물 전량 제거 — 이 계획서로 재진행 예정.** 에디터 작업(A·B·C)을
완료하고 "팔다리 꼬임"(함정 4)·흰색 모델(머티리얼 리맵)까지 해결·검증했으나, 사용자 결정으로
FBX 31개·코드 5개·생성 에셋·씬 배선·RigLab 카탈로그 등록을 전부 제거했다.

**재진행 시 반드시 읽을 것 (백업 없음 — 사용자 결정으로 전부 삭제):**
- FBX 는 Blender 에서 재익스포트한다 (§9 — .blend 와 익스포트 도구는 그대로 있다).
- §0 표의 "이미 되어 있는 것" 코드 5개는 **리포지토리에 없다** — §5 사양대로 다시 작성해야 한다.
  `RigBuilder.SpawnAsChild` 의 Generic 분기(함정 4 해결 코드)도 함께 되돌렸으므로 다시 넣어야 한다.
- 재작성 시 3가지를 빠뜨리지 말 것: ① **함정 4** — Generic 모델 루트는 FBX 회전 (-90,0,0) 유지
  (씬 배선 §7 + RigBuilder 스폰 모두), ② **머티리얼 리맵** `material`→`M_Mecha.mat` (메뉴 1번),
  ③ `bakeAxisConversion` 은 켜지 않는다 (이 파이프라인 FBX 에선 역효과 — 실측).

> 기존 기체(`05_MechaAnimation.md`)를 **대체하지 않는다.** MkII 는 별개 기체로 추가되며
> 폴더·아바타·컨트롤러가 전부 분리돼 있다. 텍스처·머티리얼만 재사용한다.

---

## 0. 인수인계 — 남은 작업 (Unity MCP 세션용)

**Blender 쪽은 전부 끝났다.** FBX 31개·코드 4개·에디터 자동화 스크립트가 이미 리포지토리에 있다.
남은 것은 **에디터 안에서만 할 수 있는 일 3가지**다.

### 이미 되어 있는 것 (다시 만들지 말 것)

| | 위치 | 검증 |
|---|---|---|
| FBX 31개 | `Assets/_Project/Art/Models/MechaMkII/` | 왕복 편차 0.00195cm |
| 순수 판정 함수 | `Gameplay/Player/MechaMkIIAnimParams.cs` | EditMode 23케이스 (**미실행**) |
| 시선 오버라이드 | `Gameplay/Player/MechaMkIIHeadAim.cs` | 컴파일만 |
| MkII 드라이버 | `Gameplay/Player/MechaMkIIAnimationDriver.cs` | 컴파일만 |
| **에디터 셋업 자동화** | `Editor/MechaMkIISetup.cs` | 컴파일만 |
| `MechaVisuals` 수정 | 순수 추가 16줄 (`VisualYaw`, `SetYawResponseOverride`) | 기존 동작 불변 |

`MechaAnimationDriver`(기존 기체용)는 **한 줄도 건드리지 않았다.** MkII 는 복사본을 쓴다.

### 할 일 A — 에셋 생성 (메뉴 한 번)

```
refresh_unity → read_console(errors)          # 새 .cs 5개 인식
Tools ▸ Mecha MkII ▸ 전체 실행 (1→4)          # execute_code 로 MechaMkIISetup.RunAll() 호출 가능
Tools ▸ Mecha MkII ▸ 검증
```
생성물: `MechaMkIIAvatar`(리그 FBX 내부), `AM_MkII_UpperBody.mask`, `AC_MechaMkII.controller`

**검증 로그에서 반드시 확인**: `path 있는 커브 0개` 가 **한 클립도 없어야 한다.**
있으면 Humanoid 로 임포트된 것이고, 그러면 Generic 으로 간 의미(스커트 흔들림)가 사라진다.

### 할 일 B — 씬 배선 (`manage_gameobject` / `manage_scene`)

대상 씬: **`Game`, `CityMap`** (`SpawnLab`·`WeaponLab` 은 랩이라 선택)

```
Player                       ← MechaController / MechaVisuals / MechaAnimationDriver (기존)
└─ Body                      ← MechaVisuals 가 회전·부유시키는 시각 루트
   ├─ MechaModel             ← 기존 기체 (그대로 둔다)
   └─ MechaMkIIModel  ★신규   ← MechaMkII.fbx 인스턴스
```

1. `Body` 하위에 `MechaMkII.fbx` 인스턴스 배치 (이름 `MechaMkIIModel`)
2. Animator: Controller = `AC_MechaMkII`, Avatar = `MechaMkIIAvatar`, **Apply Root Motion 끄기**
3. `MechaMkIIModel` 에 `MechaMkIIHeadAim` 부착 (필드는 비워도 자동 탐색)
4. `Player` 에 `MechaMkIIAnimationDriver` 부착
   - `_animator` = MechaMkIIModel 의 Animator, `_visualRoot` = `Body`
   - `_visuals` = Player 의 MechaVisuals, `_headAim` = MechaMkIIModel 의 HeadAim
   - **`_dashDuration` 을 `MechaController._dashDuration`(0.35) 과 맞춘다** ← 값이 중복돼 있다
5. **두 모델을 동시에 켜지 않는다** — 어느 쪽을 쓸지 정해 하나만 활성. `MechaVisuals` 가
   `Player` 에 하나뿐이라 요·기울임·부유를 두 모델이 함께 받는다.
6. 기존 드라이버와 MkII 드라이버도 **동시에 켜지 않는다**(같은 Animator 를 두 번 몰면 안 된다).

`_hoverAmplitude` 는 **이미 0** 이다(기존 기체가 Docs/05 P6 에서 내린 결정). 바꿀 것 없음.

### 할 일 C — 검증 (CLAUDE.md §1)

```
dotnet build "Mecha Survivor.sln"   → 오류 0
read_console(types=["error"])       → 0
run_tests(EditMode)                 → MechaMkIIAnimParamsTests 23케이스 ★아직 한 번도 안 돌렸다
```
그 다음 Play 모드에서 §8.2 QA 체크리스트. **앞 5개가 이번 작업의 성패를 가른다.**

### 넘겨받는 사람이 알아야 할 함정 4개

1. **`MechaAnimationDriver`(기존)를 고치지 말 것.** MkII 판정을 원본에 넣으면 기존 기체가
   `AC_Mecha` 에 없는 `SpinTrigger` 를 세팅하려다 경고를 뱉고, 회전 판정이 `DashTrigger` 를
   가로채 **기존 기체의 대시 애니메이션이 사라진다.** 그래서 복사본을 만들어 뒀다.
2. **`AC_MechaMkII` 를 손으로 튜닝하면 4번 메뉴 재실행 시 날아간다.** 전환 시간·블렌드 좌표는
   `MechaMkIISetup.cs` 의 상수를 고치고 다시 생성하는 편이 낫다.
3. **클립을 고쳐야 하면 Blender 로 돌아간다** (§9). 클립은 전부 절차 생성이라 에디터에서
   손으로 고치지 않는다.
4. **MkII 모델 루트를 identity 로 강제하지 말 것** (2026-08-03 실측 — "팔다리 꼬임"의 원인).
   Blender FBX 는 축 변환 -90°를 루트 노드 회전에 싣는다. Humanoid(기존 기체)는 머슬 재구성
   덕에 identity 가 맞지만, **Generic 은 로컬 커브를 그대로 재생하므로 FBX 루트 회전 (-90,0,0)
   을 유지해야 똑바로 선다.** 씬 인스턴스는 localRotation (-90,0,0), RigBuilder 스폰은
   `RigProfileMath.ModelLocalRotation`(Humanoid=identity / Generic=프리팹 회전)이 처리한다.
   임포터 `bakeAxisConversion` 은 해결책이 아니다 — 이 FBX 에선 규약만 뒤집힌다(실측).

---

## 1. 기존 기체와 결정적으로 다른 점

| | 기존 `Mecha` | **신규 `MechaMkII`** |
|---|---|---|
| Rig 타입 | **Humanoid** (`MechaAvatar` 공유) | **Generic** (`MechaMkIIAvatar` 신규) |
| 디폼 본 | 24 | **27** (힙 스커트 3본 포함) |
| 세컨더리 모션 | **불가** — Humanoid 가 비휴먼 본 커브를 전량 제거 | **가능** — 스커트·안테나 커브가 살아서 들어온다 |
| 미러링 | 대각 클립을 `mirror=true` 로 절반 절약 | **불가** — 8방향 전량 실제작 |
| 리타게팅 | Mixamo 등 외부 클립 사용 가능 | 불가 (자체 제작 전량) |
| 클립 제작 방식 | 디폼 본 FK 직접 키잉 | **IK 컨트롤러 + 절차 생성 스크립트** |

**Generic 을 고른 이유는 세컨더리 모션 하나다.** Humanoid 임포트는 아바타에 매핑되지 않은
본의 커브를 전량 제거한다 — 소스 FBX 에 `headfront`·`head_end` 커브가 분명히 있었는데
임포트된 44 클립에는 `path` 있는 커브가 0개였다(실측). 스커트·백팩·안테나가 전부 비휴먼
본이므로 Humanoid 를 쓰는 한 세컨더리는 Unity 에 전달되지 않는다.

**Generic 도 Avatar 는 필요하다.** 리타게팅만 안 될 뿐이고, Avatar Mask 는 Transform 단위로
그대로 쓸 수 있어 상체 사격 레이어를 구현할 수 있다.

---

## 2. 에셋 목록 (배치 완료)

```
Assets/_Project/Art/Models/MechaMkII/
├─ MechaMkII.fbx                 리그 + 메시 20파츠 / 49,986 tris / 디폼 27본
└─ Clips/                        애니메이션 전용 FBX 30개 (아마추어만, 각 ~200KB)
   ├─ MechaMkII@Hover_Idle.fbx        MechaMkII@Hover_Settle.fbx
   ├─ MechaMkII@Fly_{F,B,L,R,FL,FR,BL,BR}.fbx
   ├─ MechaMkII@Fly_{Ascend,Descend}.fbx
   ├─ MechaMkII@Ground_Idle.fbx       MechaMkII@Ground_Walk_{8방향}.fbx
   ├─ MechaMkII@Dash_{F,B,L,R}.fbx    MechaMkII@DashSpin_{CW,CCW}.fbx
   └─ MechaMkII@Shoot.fbx             MechaMkII@Hit.fbx
```

**텍스처·머티리얼은 기존 `Mecha/` 것을 참조한다.** 메시가 동일하므로 UV·텍스처가 같다.
중복 임포트하면 메모리가 두 배가 된다. 색 변형이 필요해지면 그때 `M_MechaMkII.mat` 로 분기한다.

### 클립 사양

| 클립 | 프레임(FBX) | 루프 | 내용 |
|---|---|---|---|
| `Hover_Idle` | 1~61 | ○ | 호버링. **상하 부유 ±8.5cm 를 클립이 전담** (§6 참고) |
| `Fly_*` (8방향) | 1~31 | ○ | 진행 방향으로 기울이고 팔다리가 반대로 끌린다 |
| `Fly_Ascend/Descend` | 1~31 | ○ | 다리를 펴 내리거나 접어 올린다 |
| `Ground_Idle` | 1~61 | ○ | 접지 대기 |
| `Ground_Walk_*` (8방향) | 1~31 | ○ | 인플레이스 보행. 스탠스 발이 진행 반대로 미끄러진다 |
| `Dash_*` (4방향) | 1~13 | × | 스러스터 분사. 시작·끝이 `Hover_Idle` 첫 프레임과 **정확히 일치** |
| `DashSpin_CW/CCW` | 1~15 | × | 회전 반응. **절대 각도 없음** (§4) |
| `Hover_Settle` | 1~19 | × | 이동→정지 반동. 시작=`Fly_F` f0, 끝=`Hover_Idle` f0 |
| `Hover_Launch` | 1~15 | × | 정지→이동 반동. 시작=`Hover_Idle` f0, 끝=`Fly_F` f0 |
| `Shoot` | 1~13 | ○ | **상체 전용 — 발 완전 고정** |
| `Hit` | 1~10 | × | 피격. 시작·끝이 `Hover_Idle` 첫 프레임 |

> FBX 는 프레임이 1부터다(Blender 0~60 → FBX 1~61). 애니메이션 이벤트 프레임은 +1.

---

## 3. 임포트 설정 — **자동화돼 있다**

`Assets/_Project/Scripts/Editor/MechaMkIISetup.cs` 가 3~4장의 작업을 전부 수행한다.
`.controller`·`.mask` 를 손으로 쓰지 않는 이유는 하나다 — **FBX 서브에셋의 fileID 를 사람이 알 수 없다.**
Unity 의 `AnimatorController` API 로 만들면 참조가 항상 정확하고, 클립을 다시 구워도 재생성만 하면 된다.

```
Tools ▸ Mecha MkII ▸ 전체 실행 (1→4)
```

| 메뉴 | 하는 일 |
|---|---|
| 1. 리그 FBX 임포트 설정 | Generic + Create From This Model + **Root node = Hips** + Normals=Import + 머티리얼 `material`→`M_Mecha.mat` 리맵 → 아바타 생성·검증 |
| 2. 클립 FBX 30개 임포트 설정 | Copy From Other Avatar + 루프 플래그 + Bake Into Pose 3종 |
| 3. 상체 아바타 마스크 생성 | `AM_MkII_UpperBody.mask` — 27본 중 상체 13본 ON |
| 4. Animator Controller 생성 | `AC_MechaMkII.controller` — 레이어 3장·파라미터 14개·상태 7개 |
| 검증 | 클립별 서브에셋 유무, **path 있는 커브 생존**, 스커트 커브, 루프 설정 대조 |

**4번은 재실행 가능하다** — 기존 컨트롤러를 지우고 통째로 다시 만든다. 손으로 튜닝한 값이 있으면 날아가므로,
튜닝 후에는 스크립트의 상수(전환 시간·블렌드 좌표)를 고치고 다시 돌리는 편이 낫다.

아래는 스크립트가 실제로 설정하는 값이다(수동으로 할 때의 참고).

### 수동 설정 시 참고

### 3.1 리그 FBX (`MechaMkII.fbx`)

```
Rig     Animation Type = Generic
        Avatar Definition = Create From This Model
        Root node = Hips                    ← ★ 반드시 지정
Model   Scale Factor = 1  (Blender 가 FBX_SCALE_ALL 로 내보내 fileScale=1)
        Normals = Import                    ← Calculate 로 두면 하드 엣지가 뭉개진다
        Tangents = Calculate Mikktspace
```
→ 생성되는 아바타를 `MechaMkIIAvatar` 로 확인한다.

### 3.2 클립 FBX (30개 공통)

```
Rig     Animation Type = Generic
        Avatar Definition = Copy From Other Avatar
        Source = MechaMkIIAvatar
Model   Import Meshes = OFF (아마추어만 들어 있다)
Animation
        Loop Time = §2 표의 루프 여부대로
        Loop Pose = 루프 클립만 ON
        Root Transform Rotation / Position(Y) / Position(XZ) = 전부 Bake Into Pose
```

**Apply Root Motion 은 끈다** (Animator 컴포넌트). 전 클립 인플레이스이고 이동은
`MechaController` 가 전담한다.

> **함정 (기존 기체에서 실제로 발생):** Blender 익스포트를 `FBX_SCALE_NONE` 으로 하면
> fileScale=0.01 이 되어 Hips 이동값이 ×100 으로 튄다. 이번 FBX 는 `FBX_SCALE_ALL` 로
> 내보냈으므로 fileScale=1 이다. **재익스포트할 때 이 설정을 유지할 것.**

### 3.3 Avatar Mask (상체 사격 레이어용)

`AM_MkII_UpperBody.mask` 를 새로 만든다. **Generic 은 Transform 섹션에서 본 단위로 마스킹한다**
(Humanoid 의 신체 부위 토글이 아니다). 아래 경로만 ON:

```
Hips/Spine02/Spine01/Spine
Hips/Spine02/Spine01/Spine/neck
Hips/Spine02/Spine01/Spine/neck/Head
Hips/Spine02/Spine01/Spine/neck/Head/head_end
Hips/Spine02/Spine01/Spine/neck/Head/headfront
Hips/Spine02/Spine01/Spine/LeftShoulder
Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm
Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm
Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand
Hips/Spine02/Spine01/Spine/RightShoulder
Hips/Spine02/Spine01/Spine/RightShoulder/RightArm
Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm
Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand
```

OFF 로 둘 것: `Hips`, `Hips/Spine02`, `Hips/Spine02/Spine01`, 다리 8본, 스커트 3본.

> **척추 계층이 이름과 반대다.** `Hips → Spine02 → Spine01 → Spine` 순이고 `Spine` 이 가슴이다.
> 마스크 기준점은 `Spine` 이지 `Spine02` 가 아니다.

---

## 4. Animator Controller — `AC_MechaMkII.controller`

기존 `AC_Mecha` 와 같은 3레이어 구조를 쓰되, **전환 반동 상태 2개**와 **회전 대시 2개**가 추가된다.

| 레이어 | 역할 | 마스크 | 블렌딩 |
|---|---|---|---|
| 0 Base | 이동 전부 + 대시 + 전환 반동 | 없음 | Override |
| 1 UpperBody | 사격 | `AM_MkII_UpperBody` | Override, weight 1 고정 |
| 2 Hit | 피격 | 없음 | Additive (기준 포즈 = 프레임 0) |

### 4.1 Base 레이어

상태 7개. **대시·회전·전환은 전부 `AnyState` 에서 건다.**

```
   [Flight BT] ←──IsGrounded──→ [Ground BT]      (기본 상태 = Flight)
                                                  speedParameter = Speed (Ground)

   AnyState ─SpinTrigger & SpinSign>0→ [DashSpin_CW ]─┐
            ─SpinTrigger & SpinSign<0→ [DashSpin_CCW]─┤ Exit Time
            ─DashTrigger─────────────→ [Dash BT     ]─┤ → IsGrounded 로 Flight/Ground 복귀
            ─SettleTrigger───────────→ [Hover_Settle]─┤
            ─LaunchTrigger───────────→ [Hover_Launch]─┘ → Flight
```

- **Flight BT** — 2D Freeform Directional, `MoveX/MoveZ`. 중앙 `Hover_Idle`,
  축 4방 `Fly_F/B/L/R`, 대각 4방 `Fly_FL/FR/BL/BR`.
- **Ground BT** — 동일 구조. 중앙 `Ground_Idle`, 8방 `Ground_Walk_*`. `Speed` 로 재생속도 보정.
- **Dash BT** — 2D, `DashX/DashZ` 로 `Dash_F/B/L/R` 선택.
- **DashSpin** — 별도 상태 2개. `SpinSign` 부호로 CW/CCW 가 갈린다.
- **Hover_Settle / Hover_Launch** — 별도 Override 상태. **Additive 가 아니다** (2026-08-02 결정).
  클립이 완성된 포즈이므로 그냥 재생하면 된다.

> ★ **AnyState 를 쓰는 이유** — 기존 기체에서 실제로 터진 함정이다. 지상 대시 첫 프레임에
> 접지가 끊겨 `IsGrounded=false` 와 `DashTrigger` 가 **같은 프레임**에 들어온다. 상태 전환으로
> 걸면 `Ground → Flight` 가 먼저 평가돼 대시를 놓친다. `AnyState` 전환은 상태 전환보다 먼저
> 평가되므로 순서 문제가 원천적으로 사라진다. (`canTransitionToSelf = false` 필수)

#### 블렌드 트리 좌표 (기존 `AC_Mecha` 와 동일한 규약)

| 클립 | (MoveX, MoveZ) |
|---|---|
| `Hover_Idle` / `Ground_Idle` | (0, 0) |
| `*_F` | (0, 1) |
| `*_B` | (0, −1) |
| `*_L` | **(−1, 0)** |
| `*_R` | (1, 0) |
| `*_FL` | (−0.7071, 0.7071) |
| `*_FR` | (0.7071, 0.7071) |
| `*_BL` | (−0.7071, −0.7071) |
| `*_BR` | (0.7071, −0.7071) |

> `_L` 이 MoveX **−1** 인 것은 기존 `AC_Mecha` 가 이미 그렇게 배치돼 있어서다.
> Blender 쪽에서는 기체의 왼쪽이 아마추어 **+X** 이고, FBX 축 변환을 거치면 Unity 의
> 캐릭터 왼쪽(−X)이 된다. 두 규약이 일치한다.

### 4.2 전환 시간

| 전환 | 시간 | 근거 |
|---|---|---|
| 지상 ↔ 비행 | 0.15s | 기존 기체 확정값 |
| 대시 진입 | 0.05s | 관성 0 원칙 — 길면 조작감이 죽는다 |
| **Flight → Hover_Settle** | **0.05s** | 클립 첫 프레임이 `Fly_F` f0 와 일치하므로 짧아도 팝이 없다 |
| **Hover_Settle → Flight BT** | **0.02s** | 끝 포즈가 `Hover_Idle` f0 와 **0.000cm** 일치 (실측) |
| Hover_Launch 도 동일 | | 시작 `Hover_Idle` f0 / 끝 `Fly_F` f0 |
| DashSpin 진입/복귀 | 0.05s / 0.05s | 시작·끝 모두 `Hover_Idle` f0 와 일치 |
| 사격 진입/복귀 | 0.06s / 0.15s | 기존 기체 확정값 |

**전환 클립의 접합은 Blender 쪽에서 이미 보장돼 있다** — 30클립 전부 접합 편차 0.000cm 로
실측 확인했다(`mecha_verify_mkii.py`). 그래서 전환 시간을 짧게 잡아도 팝이 없다.

### 4.3 Animator 파라미터

기존 기체의 파라미터를 그대로 쓰고 **회전 대시용 2개만 추가**한다.

**MkII 는 파라미터 구성이 기존 기체와 다르다.** 아래 14개를 만든다 —
`FireType`·`HitHeavyTrigger` 는 **없다**(MkII 는 사격·피격 클립이 각각 하나뿐이다).

| 파라미터 | 타입 | 공급원 |
|---|---|---|
| `IsGrounded` | Bool | `MechaController.IsGrounded` |
| `MoveX` `MoveZ` `Speed` `VerticalY` | Float | `MechaAnimParams` (기존과 동일) |
| `DashTrigger` `DashX` `DashZ` | Trigger/Float | 일반 대시 (급선회가 아닐 때만) |
| **`SpinTrigger`** | Trigger | `MechaMkIIAnimParams.ShouldSpin` 이 참일 때 |
| **`SpinSign`** | Float | `MechaMkIIAnimParams.SpinSign` (+1=CW, −1=CCW) |
| **`SettleTrigger`** | Trigger | 이동 → 정지 전환 (히스테리시스 판정) |
| **`LaunchTrigger`** | Trigger | 정지 → 이동 전환 |
| `Fire` | Bool | 사격 유지창 |
| `HitTrigger` | Trigger | `PlayerDamagedEvent` |

`DashSpin_CW`/`_CCW` 두 상태는 `SpinTrigger` 로 진입하고 `SpinSign` 조건(`> 0` / `< 0`)으로 갈린다.
`Hover_Settle`/`Hover_Launch` 는 각각 `SettleTrigger`/`LaunchTrigger` 로 진입한다.

---

## 5. 코드 — 이미 작성된 것

### 5.1 `MechaMkIIAnimParams` (순수 함수, EditMode 테스트 완비)

`Assets/_Project/Scripts/Gameplay/Player/MechaMkIIAnimParams.cs`

| 함수 | 하는 일 |
|---|---|
| `DashYaw(dir, fallback)` | 월드 대시 방향 → 요 각도. 수평 성분이 없으면 폴백 |
| `YawDelta(visualYaw, dir)` | 부호 있는 각도 차 (−180~180). **±180° 랩어라운드 처리** |
| `ShouldSpin(visualYaw, dir, θ=90)` | 급선회 판정. **경계값 90°는 발동에 포함** |
| `SpinSign(...)` | +1=CW / −1=CCW / 0=회전없음. 정확히 180°는 CW 로 고정(결정적) |
| `YawFollowResponse(spinning, base, spin=28)` | 대시 창 동안 요 추종 응답 상향 |
| `ClampedAim(head, target, body, ±60°, ±25°)` | 각도 제한을 건 머리 회전 |
| `AimWeight(context, progress)` | 상황별 시선 오버라이드 강도 |

**트리거 규칙은 계획서 5-B 의 1번만 채택했다** — `|Δyaw| ≥ 90°`.
후방 대시(180°)와 측면 대시(90°)가 자동으로 포함되고 전방·소각도는 제외된다.
판정이 입력과 현재 시각 요만으로 결정되어 예측 가능하고, 게임 상태(적 수·체력)를
애니메이션이 참조하지 않아 배선이 단순하다. 적 밀집·피격 후 조건은 콘텐츠성 추가로 남긴다.

`ResolveMoving(wasMoving, velocity, start, stop)` — 전환 반동 트리거의 이동 판정.
**히스테리시스**를 둔다(출발 2.5m/s, 정지 1.2m/s). 임계값 하나로 판정하면 그 근처에서
정지/이동이 매 프레임 뒤집혀 전환 클립이 연달아 발사된다.

테스트: `Assets/Tests/EditMode/MechaMkIIAnimParamsTests.cs` (23케이스)
— 경계값 90°, ±180° 랩어라운드 양방향, 180° 결정성, 요 클램프, 피치 클램프, 몸통 상대성,
weight 표 전 항목, 히스테리시스 경계 왕복(전환 2회만 일어나는지), 수직속도 무시.

### 5.2 `MechaMkIIHeadAim` (런타임 시선 오버라이드)

`Assets/_Project/Scripts/Gameplay/Player/MechaMkIIHeadAim.cs`

**왜 필요한가** — Blender 의 Damped Track 조준 제약은 FBX 에 포함되지 않는다. 익스포트되는
것은 베이크된 `Head` 회전뿐이라, 클립에 구워진 시선은 런타임에 적을 따라가지 않는다.
`LateUpdate`(Animator 평가 이후)에서 `Head` 본 회전을 직접 덮어쓴다.

- 목표: `EnemyBrain.ActiveEnemies` 중 최근접(0.2초 주기 재탐색). 없으면 이동 방향
- 제한: 몸통 기준 요 ±60° / 피치 ±25°
- 강도: `Context` 프로퍼티로 드라이버가 상황을 알려 준다

| 상황 | weight | 이유 |
|---|---|---|
| 호버·이동·사격 (`Normal`) | 1.0 | 적을 주시 |
| 회전 대시 (`SpinDash`) | 0 → 1 (진행도) | 클립의 "머리 선행" 연출을 살린다 |
| 피격 (`Hit`) | 0 | 충격으로 고개가 젖혀지는 연출을 그대로 |
| 전환 반동 (`Recoil`) | 0.4 | 반동을 남기되 대략 적 방향은 유지 |

### 5.3 `MechaMkIIAnimationDriver` (기존 드라이버의 복사본)

`Assets/_Project/Scripts/Gameplay/Player/MechaMkIIAnimationDriver.cs`

**상속·공유가 아니라 복사본이다.** 원본 `MechaAnimationDriver` 를 고쳐 분기시키면,
기존 기체가 `AC_Mecha` 에 없는 `SpinTrigger` 를 세팅하려다 매 대시마다 경고를 뱉고,
회전 판정이 `DashTrigger` 를 가로채 **기존 기체의 대시 애니메이션이 사라진다.**
복사본이면 회귀 위험이 0이다.

MkII 가 추가로 하는 일:

| 기능 | 동작 |
|---|---|
| **회전 대시** | 대시 상승 엣지에서 `ShouldSpin` → 참이면 `DashTrigger` 대신 `SpinTrigger`+`SpinSign`, 그리고 `MechaVisuals` 요 응답을 대시 창(0.35s) 동안 28로 상향 후 원복 |
| **전환 반동** | `ResolveMoving` 히스테리시스로 이동↔정지 전환을 잡아 `LaunchTrigger`/`SettleTrigger`. 접지 중이거나 대시 중이면 발사하지 않는다 |
| **시선 상황 전달** | `MechaMkIIHeadAim.Context` 갱신. 우선순위 **피격 > 회전 대시 > 전환 반동 > 평상시** |

안전장치: `OnDisable` 에서 요 응답 오버라이드를 반드시 원복한다(안 하면 기체가 계속 빠르게 돈다).
회전 창은 시간으로도 닫히므로 대시가 도중에 끊겨도 남지 않는다.

### 5.4 `MechaVisuals` — 유일하게 수정한 기존 파일 (순수 추가 16줄)

```csharp
public float VisualYaw => _visualYaw;                  // 회전 대시 판정 기준
public void SetYawResponseOverride(float value)        // 음수 = 인스펙터 값 사용
float yawResponse = _yawResponseOverride > 0f ? _yawResponseOverride : _yawFollowResponse;
```

기본값이 `-1` 이라 **아무도 호출하지 않으면 기존과 완전히 같은 코드 경로**다.
기존 기체는 이 메서드를 호출하지 않으므로 동작 변화가 없다.

> `MechaVisuals` 는 `Player` 에 **하나뿐이고 두 기체가 공유한다**(`_visualRoot` = `Body`).
> 그래서 두 기체 모델을 동시에 켜면 안 된다 — 요·기울임·부유를 함께 받는다.
> `_hoverAmplitude` 는 이미 0 으로 돼 있다(기존 기체가 Docs/05 P6 에서 내린 결정).

---

## 6. ★ 호버 부유 중복 — 반드시 지킬 것

**MkII 프리팹의 `MechaVisuals._hoverAmplitude` 를 0 으로 둔다.**

`Hover_Idle` 클립이 상하 부유 ±8.5cm 를 이미 담고 있다. 절차 부유를 켜 두면 **부유가 두 번
적용된다.** 기존 기체도 P6 에서 같은 결정(절차 부유 OFF, 애니메이션 전담)을 내렸다.

이동 기울임(`_maxLeanAngle` 12°, `_leanResponse` 8)과 요 추종(`_yawFollowResponse` 10)은
**유지한다.** 클립에는 절대 각도가 없고 상대적인 랙·오버슈트만 들어 있어 코드와 싸우지 않는다.

---

## 7. 씬 배선

- Game / CityMap 두 씬의 `Player` 하위에 `MechaMkII.fbx` 인스턴스를 둔다.
  기존 `MechaModel` 과 **동시에 켜 두지 않는다** (기체 선택이 생기면 그때 토글).
- 모델 루트 localRotation = **(-90, 0, 0) — FBX 루트 회전 유지** (함정 4번. identity 로 두면 눕는다).
- Animator: `AC_MechaMkII.controller` + `MechaMkIIAvatar`, **Apply Root Motion OFF**.
- `MechaMkIIHeadAim` 은 모델 인스턴스에 붙인다. `_head` 를 비워 두면 이름으로 `Head` 를 찾는다.
- 배치·프리팹·컨트롤러 에셋 작업은 전부 MCP 로 한다. 씬 작업 전 `set_active_instance` 필수.

---

## 8. 검증

### 8.1 Blender 쪽 — 완료 (자동화돼 있다)

`Blender_Tools/mecha_verify_mkii.py` 가 계획서 §7 항목을 전부 잰다. **189항목 전부 통과.**

| 항목 | 기준 | 결과 |
|---|---|---|
| 루프 닫힘 (루프 21클립) | 1mm | 최대 0.0000cm |
| 인플레이스 (루프 클립 Hips XY) | 1mm | 최대 0.0000cm |
| 전환 접합 (시작/끝 ↔ 인접 상태 f0) | 1cm | **0.000cm** |
| 리치 한계 (팔 45.6cm / 다리 106.1cm) | 100% | 팔 최대 92% / 다리 최대 97% |
| 접지 (평가된 발 메시 최저 정점) | ±2cm | 통과 |
| 스케일 커브 | 0개 | 0개 |
| 스위치 키 (f0 · CONSTANT) | 필수 | 통과 |
| 회전 대시 절대회전 | 시작·끝 0, 중간 ±10° | 최대 8.5° |
| 머리 선행 | 머리 피크 ≤ 몸통 피크 | 통과 |
| 5-A 부호 반대 | 정지=앞 / 출발=뒤 | **정지 +1.72cm / 출발 −0.66cm** |
| 5-A 지연 순서 | 상체 ≤ 손 | 통과 |
| 오버랩 멱등 (복원→재적용) | 0 | 0.00000cm |
| **FBX 왕복** (30클립, 디폼 27본 전 쌍 거리) | 0.02cm | **최대 0.00195cm** |
| 세컨더리 커브 생존 | 필수 | `HipArmor_L/R`·`HipDetail_L`·`headfront`·`head_end` 전부 생존 |

### 8.2 Unity 쪽 — 남은 QA 체크리스트

- [ ] 임포트 후 **`AnimationClip` 서브에셋이 클립당 1개**씩 생겼는가
      (0개면 아바타·본 계층 불일치. Humanoid 에서 조용히 실패하던 함정의 Generic 판)
- [ ] 클립에 **`path` 있는 커브가 남아 있는가** — Generic 이므로 남아야 정상.
      0개면 어딘가에서 Humanoid 로 임포트된 것이다
- [ ] 비행 8방 / 지상 8방 이동 재생, 축↔대각 경계에서 떨림 없음
- [ ] 착지 ↔ 이륙 전환 팝핑 없음
- [ ] **이동 → 정지 전환에서 팔다리가 앞으로 쏠렸다 돌아오는가** (이번 연출의 핵심)
- [ ] **정지 → 이동 전환에서는 반대로 뒤로 끌리는가**
- [ ] 90° 이상 방향 전환 대시에서만 `DashSpin` 이 재생되는가 (전방 대시는 일반 대시)
- [ ] 회전 대시 중 **머리가 몸보다 먼저 도는가**
- [ ] 이동 + 사격 동시 재생 (상체 마스크로 하체 유지)
- [ ] 피격 중 조작 끊김 없음
- [ ] **호버 부유가 두 번 적용되지 않는가** (§6 — `_hoverAmplitude` = 0 확인)
- [ ] 시선 오버라이드가 각도 제한을 지키는가, weight 전환 시 머리가 튀지 않는가
- [ ] 스커트 장갑판이 다리 스윙을 따라 흔들리는가 (Generic 의 성과 확인)

### 8.3 단계 공통 (CLAUDE.md §1)

`dotnet build "Mecha Survivor.sln"` 오류 0 → `read_console` 에러 0 → `run_tests` 전부 통과.
**새 `.cs` 를 추가했으므로 `refresh_unity` 후 빌드해야 실제로 컴파일된다.**

---

## 9. 재생성 방법 (클립을 고쳐야 할 때)

클립은 전부 **절차 생성**이라 손으로 고치지 않는다. 진폭·타이밍을 바꾸려면 스크립트를 고치고
다시 굽는다.

```
Blender 에서 "Mecha Ver 2 [IK rig backup 0801].blend" 열기
  1) 텍스트 에디터 → mecha_clips_mkii.py 수정 → Run Script → build_all()
  2) 텍스트 에디터 → mecha_verify_mkii.py → run()        ← 189항목 전부 PASS 여야 한다
  3) N 패널 → Mecha → Unity 익스포트 → "신규 기체 MkII (Generic)" → 전 클립 일괄
  4) Unity_Export/MechaMkII/ → Assets/_Project/Art/Models/MechaMkII/ 로 복사
```

| 파일 | 역할 |
|---|---|
| `Blender_Tools/mecha_clips_mkii.py` | 클립 30종 절차 생성 (포즈 함수 + 빌더) |
| `Blender_Tools/mecha_verify_mkii.py` | 계획서 §7 검증 189항목 |
| `Blender_Tools/mecha_overlap.py` | 스프링·댐퍼 관성 반동 베이크 (N 패널 도구) |
| `Blender_Tools/mecha_export.py` | Unity FBX 익스포트 (기체 선택 지원) |

> **익스포트 도구에서 2026-08-02 에 잡은 잠복 버그 2건** — 둘 다 IK 컨트롤러로 만든 클립에서만
> 드러난다(기존 기체는 디폼 본 FK 클립이라 무증상이었다). 재익스포트할 때 이 수정이 들어간
> 버전인지 확인할 것.
> 1. 일괄 모드가 **첫 클립 베이크 후 제약을 뮤트한 채** 다음 클립을 베이크했다 → 2번째부터
>    IK·풋롤·세컨더리가 죽은 FBX
> 2. 클립마다 **포즈를 초기화하지 않아** 직전 베이크 값이 남고, 척추 Copy Rotation 이 `ADD`
>    믹스라 그 위에 또 더해졌다 → 2번째부터 상체가 접힌 FBX (실측 편차 38cm)
