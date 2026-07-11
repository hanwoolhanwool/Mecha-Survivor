# Mecha Survivor — 프로젝트 초기 세팅 문서

이 문서는 프로젝트를 **어떤 환경과 규칙으로 시작했는지** 기록한다.
신규 참여자는 이 문서를 먼저 읽고, 아래 구조와 컨벤션을 따른다.

- 초기 세팅일: 2026-07-09
- 엔진: **Unity 6 (6000.5.3f1)**
- 원격 저장소: https://github.com/hanwoolhanwool/Mecha-Survivor

---

## 1. 개발 환경

| 항목 | 값 |
|------|-----|
| Unity | 6000.5.3f1 |
| 렌더 파이프라인 | URP 17.5 |
| 입력 | Input System (신규) |
| 물리 차원 | **3D** |
| 주 타깃 플랫폼 | **PC (StandaloneWindows64)** |
| 스크립팅 백엔드 | Mono (기본) |
| 장르 | Vampire Survivors류 — 대량 적/투사체 처리가 성능 핵심 |

> 대량 엔티티가 전제이므로 **오브젝트 풀링 필수**, `Instantiate`/`Destroy` 직접 호출 지양.

---

## 2. 버전 관리

- Git 저장소 (`main` 브랜치), **Git LFS** 사용
- `core.autocrlf = false` — 줄바꿈은 `.gitattributes`가 제어

**설정 파일**
| 파일 | 역할 |
|------|------|
| `.gitignore` | Unity 6 표준 (`Library/`, `Temp/`, `obj/`, `Logs/`, `UserSettings/` 등 제외) |
| `.gitattributes` | 바이너리(png/fbx/wav 등) → LFS, `.unity`/`.prefab`/`.asset` → YAML 머지 규칙 |
| `.editorconfig` | C# 코딩 컨벤션 (private 필드 `_camelCase`, file-scoped namespace 등) |

**규칙**
- 에셋의 `.meta` 파일은 **항상 함께 커밋**한다.
- `Library/`, `Temp/` 등 생성물은 절대 커밋하지 않는다.

---

## 3. 폴더 구조

기능(feature) 기반 구조. 우리 소스는 모두 `_Project/` 아래에 둔다.

```
Assets/
├─ _Project/
│  ├─ Art/          (Models, Textures, Materials, VFX, UI)
│  ├─ Audio/        (BGM, SFX)
│  ├─ Prefabs/      (Enemies, Player, Projectiles, Pickups)
│  ├─ ScriptableObjects/
│  ├─ Scenes/       (Boot.unity, Game.unity)
│  ├─ Settings/     (InputSystem_Actions.inputactions)
│  └─ Scripts/      (Core, Gameplay, Systems, UI, Utilities, Editor)
├─ Settings/        (URP 파이프라인 에셋 — 루트 유지)
└─ Tests/           (EditMode, PlayMode)
```

**규칙**
- 에셋스토어/서드파티는 `_Project/` 밖(`Assets/Plugins` 등)에 두어 우리 코드와 격리.
- 씬 진입은 항상 `Boot` → `Game` 순서 (아래 4·7 참고).

---

## 4. 어셈블리 정의 (asmdef)

컴파일 속도와 의존성 통제를 위해 어셈블리를 분리한다. **의존성은 단방향**이며 역참조는 금지.

```
Utilities  (기반 — 참조 없음)
   ↑
  Core      (+ Utilities)
   ↑
Systems     (+ Core, Utilities)
   ↑
Gameplay    (+ Core, Systems, Utilities, Unity.InputSystem)
   ↑
  UI         (+ Core, Gameplay, Systems, Utilities, TMP, UGUI)

Editor          (Editor 전용 — 빌드 스크립트 등)
Tests.EditMode  (전체 + TestRunner, Editor 전용)
Tests.PlayMode  (전체 + TestRunner)
```

- 새 스크립트는 역할에 맞는 어셈블리 폴더에 배치한다.
- 네임스페이스는 어셈블리 rootNamespace(`MechaSurvivor.<계층>`)를 따른다.

---

## 5. ProjectSettings (3D / PC)

### 레이어
| slot | 이름 | slot | 이름 |
|:----:|------|:----:|------|
| 8 | Player | 12 | Pickup |
| 9 | Enemy | 13 | Wall |
| 10 | PlayerProjectile | 14 | Ground |
| 11 | EnemyProjectile | | |

### 태그
`Enemy`, `Pickup`, `Projectile` (`Player`는 빌트인)

### 3D 충돌 매트릭스 (성능 최적화)
- **Enemy ↔ Enemy = OFF** — 수백 마리의 물리 충돌 제거. 밀집·겹침은 **코드 스티어링**으로 처리.
- 투사체는 대상 레이어하고만 충돌:
  - PlayerProjectile ↔ Enemy / Wall
  - EnemyProjectile ↔ Player / Wall
- Pickup ↔ Player 만 충돌 (그 외 전부 해제)
- 총 17개 불필요 페어를 비활성화.

---

## 6. 코드 아키텍처

| 어셈블리 | 구성 요소 | 역할 |
|----------|-----------|------|
| Utilities | `MonoSingleton<T>` | 싱글턴 베이스 |
| Core | `IDamageable`, `IPoolable`, `DamageInfo` | 시스템 간 계약 |
| Core | `EventBus<T>` + 이벤트 struct | 타입 기반 발행/구독 (결합도↓) |
| Core | `ServiceLocator` | 경량 전역 서비스 등록/조회 |
| Core | `GameBootstrap` | Boot→초기화→Game 진입점 |
| Systems | `ComponentPool<T>`, `PoolManager` | 오브젝트 풀링 (인스턴스 키 Despawn) |
| Gameplay | `EnemyData`, `WeaponData`, `WaveData` (SO) | 재빌드 없는 밸런싱 데이터 |
| Gameplay | `Health` (IDamageable) | 체력/피격/사망 |

**원칙**
- 스폰/소멸은 `PoolManager.Spawn/Despawn` 경유 (풀링 강제).
- 이벤트는 `readonly struct : IEvent` 로 정의해 힙 할당 최소화.
- 밸런싱 수치는 ScriptableObject 에셋으로 분리 (코드 재빌드 불필요).

---

## 7. 씬 흐름

```
Boot.unity  (GameBootstrap + Main Camera)
   │  전역 서비스 초기화
   ▼
Game.unity  (실제 게임플레이)
```

- 빌드 순서: **Boot(index 0) → Game(index 1)**
- 게임 씬을 직접 열어도 초기화가 누락되지 않도록 Boot 진입을 강제한다.

---

## 8. CI/CD

GameCI 기반 파이프라인 (`.github/workflows/ci.yml`).

```
push / PR (main, develop)
  └─ test  : unity-test-runner (EditMode + PlayMode)
      └─ build : unity-builder → StandaloneWindows64 → 아티팩트 업로드
```

- Library 캐시, LFS 체크아웃, 동시 실행 취소 포함.
- **동작 조건**: 저장소 Secrets에 Unity 라이선스 등록 필요
  (`UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD`). 상세는 `.github/workflows/README.md`.
- 로컬 CLI 빌드: `Assets/_Project/Scripts/Editor/BuildScript.cs`
  (`-executeMethod MechaSurvivor.Editor.BuildScript.PerformWindowsBuild`).

---

## 9. 신규 참여자 온보딩 체크리스트

1. **Unity 6000.5.3f1** 설치 (Unity Hub)
2. 저장소 클론 (**Git LFS 설치 필수**: `git lfs install`)
3. Unity Hub에서 프로젝트 열기 → 최초 임포트 대기
4. `Assets/_Project/Scenes/Boot.unity` 로 진입해 실행 확인
5. 코드는 역할별 어셈블리 폴더(3·4장)에, 컨벤션은 `.editorconfig` 준수
6. 스폰/소멸은 `PoolManager`, 시스템 간 통신은 `EventBus<T>` 사용
```
