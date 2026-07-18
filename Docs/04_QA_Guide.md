# QA 가이드 (2026-07-18 기준)

Mecha Survivor 를 빠르고 재현 가능하게 검증하기 위한 안내서.
관련 문서: `01_Weapons.md`(무기 스펙) · `02_Difficulty.md`(난이도 수치) · `03_Upgrades.md`(3택 목록).

---

## 1. 테스트 씬 3종 — 목적에 맞는 씬에서 시작한다

`Assets/_Project/Scenes/`

| 씬 | 용도 | 언제 쓰나 |
|---|---|---|
| **Game.unity** | 본편 20분 런 | 통합 플레이 QA, 밸런스 체감, 결과 화면 확인 |
| **WeaponLab.unity** | 무기 실험실 — 전 무기 즉시 전환 + 정지 표적 더미 링 | 무기별 데미지/연출/레벨 성장 확인 |
| **SpawnLab.unity** | 스폰 실험실 — 본편과 동일한 WaveData/DifficultyData로 시간 스크럽 | 난이도 곡선·스폰 밀도·성능 확인 |

Play 진입 전 **콘솔 에러 0**을 반드시 확인한다.

## 2. 조작

### 본편 공통 (Game / WeaponLab / SpawnLab)

| 키 | 동작 |
|---|---|
| WASD | 이동 (카메라 기준) |
| Space / Ctrl | 상승 / 하강 |
| **Shift** | 대시 |
| 좌클릭 / 우클릭 / Q / E | 무기 슬롯 1 / 2 / 3 / 4 발사 |
| V | 시점 전환 (1인칭/3인칭) |
| Esc | 일시정지 |

### WeaponLab 전용 핫키

| 키 | 동작 |
|---|---|
| ← / → | 무기 전환 |
| 숫자 1~9, 0 | 무기 직접 선택 |
| ↑ / ↓ | 강화 레벨 조절 (1~5) |
| R | 표적 더미 재배치 (처치 시 1.5초 후 자동 보충) |

### SpawnLab 전용 핫키

| 키 | 동작 |
|---|---|
| [ / ] | 시간 −60초 / +60초 |
| ; / ' | 시간 −10초 / +10초 |
| T | 배속 순환 (1 → 2 → 4 → 0.5) |
| P | 스폰 정지 토글 (생존 적 행동은 유지) |
| K | 생존 적 전멸 (경험치 미지급 회수) |

화면 좌상단 HUD에 경과 시간·생존 수·현재 난이도 배율(스폰속도/동시수/HP)이 표시된다.

## 3. QA 체크리스트

### 3-1. 무기 QA (WeaponLab)

무기 10종 각각에 대해:
- [ ] Lv1 → Lv5 순회하며 데미지·발사 수·시각 스케일이 성장하는가 (↑키)
- [ ] Lv5 특수 연출 발동: 산탄 화염방사 / 클러스터 2차 분열 / 레일건 폭발+카메라 킥 / 궤도 폭격 융단 5연발+플래시+버섯구름 / EMP 체인 / 레이저 5갈래 / 개틀링 가속 회전
- [ ] 홀드 연사는 개틀링·레이저 캐논만 되는가
- [ ] HUD 쿨다운 게이지가 실쿨과 일치하는가
- [ ] 발사음이 무기마다 다른가 (SFX id = WeaponData.Id)
- [ ] 미사일: 수직 팝업 → 곡선 유도 4단 연출이 유지되는가

### 3-2. 난이도 QA (SpawnLab)

- [ ] `]` 로 5/10/15/19분 지점 점프 → HUD 배율이 `02_Difficulty.md` 표와 일치하는가
- [ ] 시간대별 적 구성: 12분 포탑, 16분 엘리트 상시, 19분 보스가 실제로 나오는가
- [ ] 무리(Burst) 스폰: 떼로 뭉쳐서 접근하는가 (단일 낱개 스폰이면 버그)
- [ ] 10분 지점에서 생존 100+마리일 때 FPS 확인 (기준점: 151마리 138FPS, 2026-07-18 실측)
- [ ] 상한 가득 → K로 전멸 → 즉시 스폰이 재개되는가 (간격 미루기 버그 검출)
- [ ] 적이 건물 내부·지면 아래에서 스폰되지 않는가

### 3-3. 본편 통합 QA (Game)

- [ ] 레벨업 → 3택 정지 → 선택 → 재개 흐름이 매끄러운가
- [ ] 리롤이 레벨업당 1회만 되는가 (「다시 뽑기」 버튼 소진 표시)
- [ ] 빈 슬롯이 없을 때 신규 무기가 3택에 안 나오는가
- [ ] 조합 재료(예: 개틀링 Lv3 + 스러스터 Lv2)를 채우면 조합안이 등장하고, 선택 시 재료가 소비되는가
- [ ] 슬롯 확장 2회 후 Q/E 슬롯이 실제 발사되는가
- [ ] 20분 도달 = 클리어 / 사망 = 실패, 결과 화면(통계) 표시
- [ ] **데미지 2배 버그 감시**: 풀링 재사용 오브젝트의 이벤트 중복 구독이 원인 — 같은 무기로 같은 적을 칠 때 데미지가 배로 뛰면 즉시 보고
- [ ] Esc 일시정지·재개 후 쿨다운/타이머가 정상인가 (timeScale=0 기반)

## 4. 알려진 함정 (재현 실패로 오인하기 쉬운 것들)

| 증상 | 원인/대처 |
|---|---|
| Play 중 시간이 0에 고정, 스폰 안 됨 | **에디터 비포커스 시 플레이 루프 정지.** 에디터 창에 포커스를 주거나 `Application.runInBackground = true` 설정 |
| 씬 시작 직후 첫 사운드가 안 남 | 첫 `AudioSource.Play()` 워밍업 드롭 — 루프음은 자가 복구됨. 1회성이면 재시도 후 판단 |
| `Access version should be odd…` Assert 무한 도배 | 엔진 내부 문제, 게임 코드 무관. **에디터 재시작** → 비대해진 `Logs/Editor.log` 삭제 → 재발 시 Library 재생성 |
| 로그가 안 보임 | MCP로 띄운 세션은 프로젝트 `Logs/Editor.log`, 일반 실행은 `%LOCALAPPDATA%\Unity\Editor\Editor.log` — **둘 다** 확인 |
| SpawnLab에서 시간 점프 후 스폰이 한동안 없음 | 시간 점프 시 `ResetSchedule()`이 호출돼야 정상. 핫키 사용 시 자동 처리됨 — 코드로 점프했다면 직접 호출 |

## 5. 밸런싱 수치 조정 위치 (코드 수정 금지)

| 조정 대상 | 에셋 |
|---|---|
| 무기 데미지/쿨다운/성장 | `ScriptableObjects/Weapons/WeaponData_*.asset` |
| 적 HP/이속/공격/경험치 | `ScriptableObjects/Enemies/EnemyData_*.asset` |
| 스폰 스케줄 (시간대/간격/상한/무리 수) | `ScriptableObjects/Waves/WaveData_MainRun.asset` |
| 전역 난이도 곡선 3종 | `ScriptableObjects/Waves/DifficultyData_MainRun.asset` |
| 강화 효과/가중치/최대 레벨 | `ScriptableObjects/Upgrades/*.asset` |
| 조합 재료 조건 | `ScriptableObjects/Recipes/*.asset` |

에디터에서 값을 바꾸면 Play 중에도 다음 스폰/발사부터 즉시 반영된다.

## 6. 코드 변경 후 검증 루프 (개발자용 — CLAUDE.md §1)

버그 수정이 포함된 QA라면 아래 3단계를 전부 통과해야 "완료"다.

```
① 컴파일   dotnet build "Mecha Survivor.sln" -v q --nologo -clp:ErrorsOnly   → 오류 0
② 콘솔     Unity 콘솔 (types=error)                                          → 에러 0
③ 테스트   EditMode 테스트 전체                                              → 전부 통과 (현재 140개+)
```

- `Assembly-CSharp.csproj` 단독 빌드 금지 (항상 실패하는 stale 잔재).
- run_tests가 "tests_running" 오류를 내도 실제로는 실행됨 — 결과는
  `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Mecha Survivor\TestResults.xml` 확인.

## 7. 버그 리포트 양식

```
[씬] Game / WeaponLab / SpawnLab
[시각] 런 경과 mm:ss (SpawnLab이면 HUD 시간)
[무기/강화 상태] 예: 개틀링 Lv3 + 리액터 Lv2, 슬롯 3개
[재현 절차] 1. … 2. … 3. …
[기대 / 실제] …
[빈도] 항상 / 간헐 (n회 중 m회)
[로그] 콘솔 에러 전문 + Editor.log 경로
```
