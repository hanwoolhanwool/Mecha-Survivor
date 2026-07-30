# 06 — RigLab: 기체·적 로봇 장착/총구/애니메이션 조정 씬 계획서

작성일: 2026-07-30
상태: **계획** (구현 미착수)
선행 문서: `05_MechaAnimation.md` (애니메이션 확정 설정), `04_QA_Guide.md` (QA 씬 관례)

---

## 1. 목적과 범위

무기 마운트(장착 모델)·총구(투사체 생성 위치)·애니메이션을 **한 씬에서 눈으로 보며 조정하고,
결과를 데이터로 저장해 본편이 그대로 쓰게 만든다.**

동기가 된 문제:

1. 트윈 로켓 캐논/레일건 마운트 위치가 **Game·CityMap 두 씬에 수동 중복** 배치돼 있다.
   수치를 바꾸려면 씬 2곳을 똑같이 고쳐야 하고, 씬이 늘수록 더 나빠진다.
2. **투사체 생성 위치가 장착 모델과 무관하다.** 플레이어 무기는 무기 프리팹의 `_muzzle`
   (플레이어 루트의 WeaponMount 기준)에서 발사되고, 적은 `EnemyBrain.cs`에
   `transform.position + Vector3.up * 1.5f`로 하드코딩돼 있다. 레일건 포신 끝에서
   빔이 나가게 하려면 조정 도구와 데이터 통로가 둘 다 없다.
3. **캐릭터(플레이어 기체)가 추가될 예정**이고, **적 로봇도 모델로 교체·관리**해야 한다.
   지금은 기체 모델이 씬에 고정 배선돼 있어 선택/교체 구조 자체가 없다.

범위에 포함: 데이터 모델(리그 프로필), 런타임 빌더, RigLab 씬 + 조정 도구, 기존 씬 마이그레이션.
범위 제외: 새 캐릭터/적 모델 제작 자체, 인게임 캐릭터 선택 UI(격납고 연동은 후속).

## 2. 현재 구조 진단

| 항목 | 현재 상태 | 문제 |
|---|---|---|
| 마운트 위치 | Game·CityMap 씬의 본(UpperChest/RightHand) 자식으로 수동 배치, `WeaponMountVisuals`가 표시/숨김 | 씬 간 중복, 조정 시 포즈 샘플링 절차 필요 |
| 마운트 모델 표시 | `WeaponMountVisuals` (슬롯 폴링) — 동작 검증 완료 | 유지 (변경 없음) |
| 플레이어 총구 | `Weapon._muzzle` Transform (무기 프리팹 내부) | 장착 모델(포신)과 무관한 위치에서 발사 |
| 적 총구 | `EnemyBrain.cs:302` 하드코딩 `up * 1.5f` | 데이터화 안 됨, 적별 차이 표현 불가 |
| 캐릭터 선택 | 없음 (MechaModel 씬 고정 배선) | 다기체 대비 구조 부재 |
| 적 모델 | 프리미티브 프리팹 5종 (Enemy_Walker 등) | 로봇 모델 교체 예정 — 교체 시 총구/애니 조정 필요 |
| 조정 도구 | 없음 (execute_code로 수동 작업) | 반복 불가능, 눈으로 확인 어려움 |

## 3. 목표 데이터 모델 — "리그 프로필"

핵심 원칙: **씬에는 아무것도 손으로 배치하지 않는다.** 조정 결과는 전부 ScriptableObject에
저장되고, 런타임 빌더가 스폰 시 재구성한다. (§2 금지사항의 SO 밸런싱 원칙과 동일 철학)

### 3.1 `RigProfileData` (신규 SO) — 캐릭터/적 공용

```
RigProfileData
├─ ModelPrefab            기체/적 모델 (FBX 프리팹)
├─ AnimatorController     확인용 AC (적은 없을 수 있음)
├─ Mounts[]               마운트 정의 목록
│   ├─ Id                 "BackWeapon", "RightHandWeapon", ...
│   ├─ Bone               HumanBodyBones (Humanoid) 또는 본 경로 문자열 (Generic 폴백)
│   ├─ LocalPosition/Rotation/Scale   본 기준 로컬 값 — RigLab이 저장하는 대상
│   └─ VisualPrefab       마운트에 붙는 무기 모델 (TwinRocketCannons 등)
└─ Muzzles[]              총구 정의 목록
    ├─ Id                 무기/공격 식별자 ("laser_cannon", "enemy_main", ...)
    ├─ MountId            기준 마운트 (""이면 모델 루트 기준)
    └─ LocalPosition/Rotation         총구 오프셋 — RigLab이 저장하는 대상
```

- **본 지정은 HumanBodyBones 우선** (Humanoid 아바타면 이름 독립). 적 로봇이 Generic이면
  본 경로 문자열로 폴백. 두 방식 모두 순수 함수 `RigProfileMath.ResolveBone`으로 분리해
  EditMode 테스트 대상으로 만든다.
- 무기↔마운트 표시 바인딩(`WeaponData` ↔ Mount.Id)은 기존 `WeaponMountVisuals` 바인딩
  배열을 프로필로 옮긴다.

### 3.2 런타임 빌더 `RigBuilder` (신규 컴포넌트)

- 캐릭터 루트(Player)에 배치. `RigProfileData`를 받아 Awake에서 마운트 빈 오브젝트를
  본 아래 생성하고 VisualPrefab을 붙인 뒤, `WeaponMountVisuals` 바인딩을 코드로 구성.
- **Game·CityMap의 수동 마운트 오브젝트는 제거**하고 빌더 1개 + 프로필 참조로 대체.
  씬 간 불일치 문제가 원천 소멸.
- 적은 `EnemyBrain` 스폰 경로에 통합: `EnemyData`에 `RigProfile` 필드 추가(선택적),
  프로필이 있으면 모델·총구를 빌더가 구성, 없으면 기존 프리미티브 유지 (점진 이행).

### 3.3 총구 데이터의 소비자

- **플레이어**: `MechaContext.MountWeapon` 시 프로필의 Muzzle(무기 Id 매칭)을 찾아
  `Weapon._muzzle`이 가리킬 앵커 Transform을 마운트 아래 생성·주입 (`Weapon.SetMuzzle(Transform)`
  신설). 매칭 항목이 없으면 현행 동작 유지 — 기존 무기 10종은 아무것도 안 바뀐다.
- **적**: `EnemyBrain`의 하드코딩 `up * 1.5f`를 `EnemyData.MuzzleOffset`(기본값 0,1.5,0)으로
  치환. 프로필이 있으면 프로필 총구가 우선.

## 4. RigLab 씬 구성

WeaponLab/SpawnLab 관례(전용 컨트롤러 + OnGUI HUD + 핫키)를 그대로 따른다.

### 4.1 씬 요소

| 요소 | 내용 |
|---|---|
| 무대 | 평지 + 방향광 + 격자 바닥 (거리감 확인용). 원점 접지 콜라이더 필수 (지상 애니 확인) |
| 카메라 | 궤도 카메라 — 마우스 드래그 회전 / 휠 줌. 조정 부위 프레이밍이 핵심이라 본편 리그 재사용보다 전용 단순 궤도가 낫다 |
| RigLabController | 대상 로드, 핫키, HUD, 저장. `Gameplay/Debugging/` 배치 |
| 대상 카탈로그 | `RigProfileData[]` 두 목록 (캐릭터 / 적) — 인스펙터 배열 |

### 4.2 기능 블록

**① 대상 선택**
- Tab: 캐릭터 탭 ↔ 적 탭 전환. ←/→: 목록 순환. 선택 시 기존 대상 파괴 후 프로필로 재구성
  (빌더와 같은 코드 경로 — 랩에서 보는 것 = 본편에서 나오는 것 보장).
- 초기 카탈로그: 캐릭터 = Mecha 1종, 적 = 기존 프리미티브 5종 (모델 교체 대비 자리만).

**② 마운트 조정**
- [ / ] 로 마운트 순환 선택. 선택 마운트에 기즈모(축 표시) + HUD에 로컬 수치 표기.
- 조정은 **씬 뷰 핸들 병용**: Play 중 씬 뷰에서 마운트 Transform을 직접 끌면 되고,
  게임 뷰만 쓸 때를 위해 핫키 넛지(화살표+PgUp/Dn 이동, Shift=회전, Ctrl=미세)를 제공.
- R: 프로필 값으로 리셋.

**③ 총구 조정**
- 마운트와 같은 조작으로 총구 앵커 이동. 총구엔 구체+전방 화살표 기즈모.
- **F: 시험 발사** — 실제 투사체/빔을 총구에서 발사해 생성 위치·방향을 궤적으로 확인.
  플레이어 탭은 해당 무기 프리팹 발사 경로, 적 탭은 `EnemyData.ProjectilePrefab` 사용.

**④ 애니메이션 확인**
- 1~9: 상태 강제 재생 — FlyIdle / Fly8방 / GroundIdle / Walk8방 / Dash / Shoot(3그룹) / Hit / HitHeavy.
  (파라미터 주입은 05 문서의 드라이버 파라미터 규격을 그대로 사용)
- +/-: 재생 속도 0.1×~2×. 마운트·총구가 본을 따라 움직이는지 애니메이션 중에 확인하는 것이
  이 블록의 존재 이유다 (특히 손 마운트는 사격 자세에서 확인 필수).
- 적 탭: AC 없는 적은 스킵 표기.

**⑤ 저장**
- S: 현재 마운트·총구 로컬 값을 선택된 `RigProfileData`에 기록 + `EditorUtility.SetDirty`
  → `AssetDatabase.SaveAssets`. **SO는 에셋이라 Play 모드 중 저장해도 유지된다** — 이
  워크플로가 성립하는 근거. 저장 코드는 `#if UNITY_EDITOR` 가드 (RigLab은 에디터 전용 씬,
  빌드 미포함 — SpawnLab과 동일 취급).
- HUD에 미저장 변경(dirty) 표시.

### 4.3 조작표 (초안)

| 키 | 기능 |
|---|---|
| Tab / ←→ | 캐릭터·적 탭 / 대상 순환 |
| [ ] | 마운트/총구 순환 (마운트 → 총구 순) |
| 화살표+PgUp/Dn | 선택 항목 이동 (Shift=회전, Ctrl=미세 0.005) |
| R | 선택 항목 프로필 값으로 리셋 |
| F | 시험 발사 |
| 1~9, +/- | 애니메이션 상태 / 재생 속도 |
| S | 프로필에 저장 |

## 5. 개발 단계

각 단계는 CLAUDE.md §1 검증 루프(컴파일 0 / 콘솔 0 / 테스트 통과)를 통과해야 완료다.

| 단계 | 내용 | 완료 기준 |
|---|---|---|
| **P0** | `RigProfileData` + `RigProfileMath`(본 해석·오프셋 수학) + `RigBuilder`. Mecha 프로필 1개를 현재 Game 씬 수치로 작성 | EditMode 테스트(본 해석/빌드 결과), 빌더가 만든 마운트가 기존 수동 배치와 월드 포즈 일치 |
| **P1** | Game·CityMap 마이그레이션 — 수동 마운트 제거, 빌더로 대체. `Weapon.SetMuzzle` + 플레이어 총구 주입, `EnemyData.MuzzleOffset` 치환 | 두 씬 Play에서 장착 표시·발사 위치가 이전과 동일(스크린샷 대조), 기존 테스트 전부 통과 |
| **P2** | RigLab 씬 뼈대 — 무대/궤도 카메라/대상 선택/애니메이션 강제 재생 | 캐릭터·적 전환과 애니 재생이 HUD 안내대로 동작 |
| **P3** | 마운트·총구 조정 + 시험 발사 + 저장 | 랩에서 수치 수정→저장→Game 씬 Play에 반영되는 왕복 확인 |
| **P4** | QA — 04 문서에 체크리스트 추가, 함정 기록 | 체크리스트 통과, 문서 갱신 |

예상 신규 파일: `RigProfileData.cs`, `RigProfileMath.cs`, `RigBuilder.cs`,
`RigLabController.cs`, `RigLab.unity`, `RigProfile_Mecha.asset` (+적 프로필은 모델 도입 시).

## 6. 테스트 계획

- **EditMode**: `RigProfileMath` — Humanoid 본 해석/경로 폴백/미존재 본 처리, 총구 오프셋
  로컬→월드 환산, 프로필→빌드 결과(마운트 수·부모·로컬 값) 검증. `WeaponMountVisualsTests`는
  바인딩 소스가 프로필로 바뀌는 부분만 보강.
- **PlayMode**(필요 시): 빌더+애니메이션 결합(본 추종)은 씬 의존이라 PlayMode 1개 허용.
- 조정 UI(핫키·기즈모)는 수동 QA 영역 — P4 체크리스트로 커버.

## 7. 알려진 함정 (설계에 선반영)

- **FBX 루트 스케일을 덮으면 안 된다** (레일건 ×100 보정) — 크기 조절은 마운트 오브젝트 담당.
  빌더도 VisualPrefab 인스턴스의 루트 트랜스폼을 건드리지 않는다.
- **에디트 모드는 FBX 눕는 포즈** — RigLab은 Play 모드 조정이 기본이므로 회피됨. 에디트 모드
  지원은 하지 않는다 (포즈 샘플링 복잡도가 이득보다 크다).
- Play 중 **씬 뷰 핸들로 조정한 값은 Stop 시 소실** — 그래서 저장(S)이 SO에 기록하는 구조.
  Stop 전 저장을 HUD dirty 표시로 상기시킨다.
- 비포커스 Play 루프 정지 — RigLab 진입 시 `Application.runInBackground = true`.
- 새 .cs 추가 후 csproj stale — `refresh_unity` 후 빌드 (CLAUDE.md §1).

## 8. 미결정 사항 (착수 전 확인 필요)

1. **총구 정책**: 무기 총구를 마운트 모델 포신에 붙이면, 손 애니메이션에 따라 발사 위치가
   흔들린다. 조준 정확도(에임 방향)는 `MechaAimer`가 별도 계산하므로 게임플레이 영향은 없지만
   연출 취향 문제 — 포신 추종(제안) vs 현행 고정 유지 중 선택.
2. **캐릭터 카탈로그와 격납고 연동 시점**: RigLab 카탈로그는 독립 배열로 시작하되, 캐릭터
   선택이 본편 기능이 되는 시점에 `LoadoutData`/격납고와 단일 소스로 합칠지.
3. **적 로봇 모델 도입 순서**: 프로필 구조는 P0에서 적 대응으로 만들지만, 실제 적 모델
   교체는 별도 작업 — 어떤 적부터 모델화할지.

---

*변경 이력*
- 2026-07-30: 초안 작성
