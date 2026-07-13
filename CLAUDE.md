# CLAUDE.md — Mecha Survivor 작업 규칙

Unity 6 (6000.5.3f1) / URP / 3D / PC 타깃. Vampire Survivors류 서바이버 게임.
**환경·폴더·어셈블리·레이어·씬 구조는 `SETUP.md`가 유일한 출처다.** 여기서 반복하지 않는다.
이 문서는 **"어떻게 작업하고, 무엇을 금지하며, 다 됐는지 어떻게 검증하는가"** 만 다룬다.

---

## 1. 검증 루프 (Definition of Done)

**아래 3단계를 모두 통과하기 전에는 절대 "완료"라고 보고하지 않는다.**
하나라도 실패하면 스스로 원인을 고치고 처음부터 다시 돌린다.

```
① 컴파일   dotnet build "Mecha Survivor.sln" -v q --nologo -clp:ErrorsOnly   → 오류 0개
② 콘솔     Unity MCP read_console (types=["error"])                          → 에러 0개
③ 테스트   Unity MCP run_tests (EditMode / 관련 시 PlayMode)                  → 전부 통과
```

### 컴파일 검증 시 반드시 지킬 것
- **`Assembly-CSharp.csproj`는 절대 빌드하지 않는다.** 삭제된 URP 템플릿 잔재(`TutorialInfo/Readme.cs`)를
  참조하는 stale 파일이라 **항상 실패**하며, 우리 코드는 asmdef로 분리돼 있어 **한 줄도 검사하지 않는다.**
- 반드시 **`Mecha Survivor.sln`** 을 빌드한다. asmdef 8개 프로젝트를 전부 포함하는 유일한 경로다.
- `.csproj` / `.sln` 은 `.gitignore` 대상이며 **Unity가 재생성**한다. 커밋하지 말고, 없으면
  Unity 에디터를 띄우거나 MCP `refresh_unity` 로 재생성시킨다.
- **새 `.cs` 파일을 추가하면 csproj가 stale해진다.** MCP `refresh_unity` 로 Unity에
  재임포트시킨 뒤 빌드해야 새 파일이 실제로 컴파일된다.

### 테스트 작성 의무
새 시스템/로직을 만들면 **EditMode 테스트를 최소 1개 함께 작성한다.**
MonoBehaviour·씬 의존이 필수인 경우에만 PlayMode 테스트를 쓴다.
순수 로직은 MonoBehaviour 밖으로 빼서 EditMode로 검증 가능하게 설계한다.

---

## 2. 절대 금지 (아키텍처 무결성)

| 금지 | 대신 사용 | 이유 |
|---|---|---|
| `Instantiate` / `Destroy` 직접 호출 | `PoolManager.Spawn(prefab, pos, rot)` / `.Despawn(instance)` | 수백 개체 전제 — GC 스파이크 방지 |
| 시스템 간 직접 참조 (`FindObjectOfType`, 인스펙터 상호 참조) | `EventBus<T>.Raise/Subscribe` 또는 `ServiceLocator.Get<T>()` | 결합도 억제 |
| 밸런싱 수치를 코드에 하드코딩 | ScriptableObject (`EnemyData`, `WeaponData`, `WaveData`) | 재빌드 없이 밸런싱 |
| `Update()` 안에서 할당(new/LINQ/람다 캡처/문자열 결합) | 사전 할당·캐싱·`for` 루프 | 프레임당 GC 방지 |
| asmdef 역방향 참조 (예: Core → Gameplay) | 단방향 유지 (`SETUP.md` 4장) | 순환 의존 차단 |
| 적끼리 물리 충돌 의존 | 코드 스티어링 (Enemy↔Enemy 충돌은 OFF) | 물리 비용 제거 |

**클래스 배치**: `Utilities`(무의존) → `Core`(계약·이벤트) → `Systems`(풀·매니저) → `Gameplay`(게임 로직) → `UI`.
새 파일은 역할에 맞는 폴더에 두고, 네임스페이스는 `MechaSurvivor.<계층>` 을 따른다.

---

## 3. 핵심 API 시그니처 (추측 금지 — 이대로 사용)

```csharp
// 스폰/소멸  (MechaSurvivor.Systems)
Component inst = PoolManager.Instance.Spawn(prefab, position, rotation);
PoolManager.Instance.Despawn(inst);
PoolManager.Instance.Prewarm(prefab, count);

// 이벤트  (MechaSurvivor.Core) — 이벤트는 readonly struct : IEvent 로 정의
EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);    // OnEnable
EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);  // OnDisable — 누락 시 누수
EventBus<EnemyKilledEvent>.Raise(new EnemyKilledEvent(...));

// 전역 서비스  (MechaSurvivor.Core)
ServiceLocator.Register(this);              // Awake
ServiceLocator.Get<PoolManager>();          // 미등록 시 throw
ServiceLocator.TryGet<PoolManager>(out var pm);  // 실패 허용 시

// 피격  (MechaSurvivor.Core)
IDamageable.TakeDamage(float amount, in DamageInfo info = default);
IPoolable.OnSpawnedFromPool() / OnReturnedToPool();   // 풀 재사용 시 상태 초기화
```

**구독 해제 규칙**: `Subscribe`는 `OnEnable`, `Unsubscribe`는 `OnDisable`에서. 풀링된 오브젝트는
재사용되므로 이 짝이 깨지면 **핸들러가 중복 등록되어 데미지가 2배로 들어가는 류의 버그**가 난다.

---

## 4. Unity MCP 사용 규칙

- 스크립트 편집은 **일반 파일 도구(Read/Edit/Write)** 로 한다. MCP 스크립트 도구보다 안정적이다.
- 파일 추가/변경 후에는 **`refresh_unity`** → **`read_console`** 순으로 확인한다. 컴파일 중이면
  `editor_state` 의 `isCompiling` 이 내려갈 때까지 기다린다.
- 씬/프리팹/ScriptableObject 에셋 **생성·배치는 MCP로** 한다 (`manage_scene`, `manage_prefabs`,
  `manage_scriptable_object`, `manage_gameobject`). `.unity`/`.prefab` YAML을 손으로 편집하지 않는다.
- Play 모드 진입 전 반드시 콘솔 에러 0을 확인한다.

---

## 5. 작업 워크플로

1. **착수 전**: 관련 기존 코드를 읽는다. 유사 시스템이 이미 있으면 새로 만들지 말고 확장한다.
2. **구현**: 위 §2 금지사항과 §3 시그니처를 지킨다.
3. **검증**: §1 의 3단계를 전부 통과시킨다. 실패하면 스스로 고친다.
4. **보고**: 무엇을 왜 그렇게 했는지 + 검증 결과(오류 0 / 테스트 N개 통과)를 사실대로 적는다.
   실패한 게 있으면 숨기지 말고 그대로 말한다.

---

## 6. 게임 정체성

<!-- [참고] 정답 예시:
Vampire Survivors식 자동 전투 로그라이크. 플레이어는 이동만 조작하고 무기는 자동 발사.
30분 생존이 1회차 목표. 경험치 → 레벨업 → 무기/패시브 선택으로 빌드를 완성하는 것이 핵심 재미.
-->
<!-- ↓↓↓ 여기에 직접 작성하세요 ↓↓↓ -->


<!-- ↑↑↑ 여기까지 ↑↑↑ -->

---

## 7. Git 규칙

<!-- [참고] 정답 예시:
- 커밋 메시지: `<type>: <한글 요약>` (type = feat / fix / refactor / chore / test / docs)
- 커밋은 논리 단위로 쪼갠다. "이것저것 수정" 금지.
- `main`에 직접 커밋해도 되지만, 커밋 전 반드시 §1 검증 루프를 통과시킨다.
- 커밋·푸시는 사용자가 명시적으로 요청할 때만 한다.
-->
<!-- ↓↓↓ 여기에 직접 작성하세요 ↓↓↓ -->


<!-- ↑↑↑ 여기까지 ↑↑↑ -->

---

## 8. 자율 진행 범위

물어보지 않고 진행해도 되는 것 / 반드시 확인받아야 하는 것.

<!-- [참고] 정답 예시:
**물어보지 말고 진행**: 코드 작성·리팩터링, 테스트 추가, 검증 루프 실행, 컴파일 에러 자체 수정,
                    ScriptableObject 에셋 생성, 프리팹 구성.
**반드시 확인**: 게임 밸런스 수치 결정, 새 외부 패키지 추가, 기존 아키텍처 규칙 변경,
              파일 대량 삭제, git push, 유료 에셋 도입.
-->
<!-- ↓↓↓ 여기에 직접 작성하세요 ↓↓↓ -->


<!-- ↑↑↑ 여기까지 ↑↑↑ -->
