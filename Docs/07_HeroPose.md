# 07 — 히어로 포즈 구현 계획서 (공중 런지 포즈)

작성: 2026-08-01. 레퍼런스: 사용자 제공 이미지
(`C:\Users\iam12\Downloads\ChatGPT Image 2026년 8월 1일 오전 03_48_01.png`).
제작 파이프라인·확정 설정은 **`05_MechaAnimation.md` §4-확정을 그대로 따른다** (여기서 반복하지 않음).

**상태: A안 P0~P1 완료 (2026-08-01)** — 클립 제작·임포트·검증까지. §9에 결과와 잔여 결정 사항.

---

## 1. 목적

레퍼런스 이미지의 **공중 런지(도약 사격) 포즈**를 주인공 기체(Mecha.fbx)로 재현한다.
1차 산출물은 **포즈 클립 1개**이고, 어디에 쓸지(전시용 vs 인게임)는 §3에서 결정한다.

## 2. 포즈 분석 (레퍼런스 이미지 분해)

전신이 공중에 떠 있고, 상체를 앞으로 숙이며 좌측(총구 방향)으로 비튼 도약 자세.

| 부위 | 자세 | Blender 키잉 포인트 (전방 -Y 기준) |
|---|---|---|
| Hips (전신) | 공중 부양 + 전경(前傾) 약 20~25° + 좌측 비틀림 | Hips 이동 +Z(상승) / +X축 회전(전경) / Z축 요 비틀림. 루트 본은 원점 고정 |
| 척추/가슴 | 전경을 이어받아 추가 굽힘 + 좌회전(총구 쪽으로) | Spine~Chest에 트위스트 분산 |
| 머리 | 총구 방향(좌하단)을 응시 | Neck+Head 좌회전, 약간 숙임 |
| 오른팔 | 팔꿈치 굽혀 몸통 앞을 가로질러 소총 파지, 총구는 좌하단 | 오른손이 몸 중앙~좌측으로. **손의 위치·회전만 잡으면 레일건은 RigProfile로 따라옴** (§4 참고) |
| 왼팔 | 뒤-바깥쪽으로 크게 뻗어 균형, 손바닥 펼침 | 어깨 신전 + 외전. ※ 리그 24본 — 손가락 본 없음, "펼친 손"은 표현 불가(한계로 수용) |
| 오른다리 | 무릎을 강하게 접어 발이 뒤로 들림 (접은 다리) | 고관절 굴곡 + 무릎 최대 굴곡 |
| 왼다리 | 아래-뒤로 곧게 뻗음 (진행 반대 방향) | 고관절 신전, 무릎 거의 폄, 발끝 포인트 |

핵심 실루엣: **"오른다리 접고 왼다리 뻗은 비대칭 X자"** + **총구와 시선이 같은 방향(좌하단)**.
이 두 가지가 살면 나머지는 미세 조정.

## 3. 활용처 — 결정 사항

| 안 | 내용 | 추가 작업량 |
|---|---|---|
| **A. 전시용 히어로 포즈 (권장 1차)** | 미세한 부유 사웨이(±5cm, 2~4s 루프)를 얹은 정적 포즈 클립. RigLab/격납고/타이틀 연출·스크린샷용 | 클립 1개 + 확인용 배선만. 게임 코드 무변경 |
| B. 인게임 대쉬/점프 액션 | 기존 대쉬 8클립(0.4s)의 절정 프레임을 이 포즈로 교체하거나, 점프 전용 상태 신설 | AC_Mecha 전환·드라이버 수정 + EditMode 테스트 필요 |

**권장 진행**: A를 먼저 완성(P0~P1)해 포즈 품질을 확정하고, B는 결과를 보고 별도 결정.
포즈 액션은 공용이므로 A→B 확장 시 재제작 없음.

## 4. 제작 방식 — 핵심 결정

- **소스**: `Mecha Ver 2.blend`의 `Riging_Meshy` 아마추어에 새 Action
  **`Mecha_Pose_HeroLunge`** (30fps, 2s=60프레임 루프, `use_fake_user=True` 보존).
  - 프레임 0 = 완성 포즈. 루프 구간 전체가 포즈 유지 + 미세 사웨이(Hips ±5cm, 팔다리 ±2° 남짓).
- **소총 문제**: 모델에 소총 메시가 없다. 오른손 장착 레일건(`Mount_RightHandWeapon`,
  RigProfile_Mecha)이 소총 역할 — **포즈에서 오른손 트랜스폼만 정확히 잡으면 무기는
  본을 따라온다.** 총구 각도가 어색하면 RigLab에서 마운트 로컬값 조정(씬 편집 금지, [[riglab-status]]).
- **루트 모션 금지 유지**: 공중 부양감은 Hips +Z 이동으로만. 루트 본 원점 고정.
- **내보내기/임포트**: `Rigged/Mecha_Anim_HeroLunge.fbx` (아마추어만),
  05 §4-확정 설정 그대로 (`FBX_SCALE_ALL` ★, 씬 프레임 범위=액션 범위 ★,
  Humanoid + CopyFromOther + Bake Into Pose 전부).

## 5. 단계

### P0 — Blender 포즈 제작 + 내보내기
1. §2 표대로 키잉 → 뷰포트 4방향 캡처로 레퍼런스와 실루엣 비교(특히 X자 다리·총구/시선 일치).
2. 미세 사웨이 키 추가(0/30/60프레임, 60=0 복사로 루프 봉합).
3. `scene.frame_start/end`를 액션 범위에 맞춘 뒤 FBX 내보내기.
   **잡 실행 중 Unity 수동 조작 금지** ([[meshy-model-pipeline]]).

### P1 — Unity 임포트 + 검증 배선 (A안)
1. `refresh_unity` → 임포트 설정(execute_code, 05 §4-확정 코드) → 콘솔 에러 0 확인.
2. 확인 배선은 **RigLab.unity 재사용**: RigLab의 프리뷰 Animator에서 클립 재생
   (Animator `AlwaysAnimate` 유지 — 게임 뷰 미렌더 시 포즈 멈춤 함정).
   에디트 모드에서 눕는 건 정상, **판정은 Play에서**.
3. 레퍼런스 이미지와 같은 앵글로 스크린샷 → 나란히 비교 → 어긋난 부위는 P0로 돌아가 수정
   (반복 1~3회 예상).
4. 필요 시 격납고/타이틀 노출 배선 — 사용자 확인 후.

### P2 — (보류) 인게임 액션화 (B안)
사용자가 A 결과를 보고 결정. 착수 시 05 §10 백로그 형식으로 사양을 먼저 쓴다
(대쉬 교체라면 스냅 파라미터·전환 순서 함정 — 05 P5 절 참조).

## 6. 검증 (Definition of Done)

- CLAUDE.md §1 3단계: `dotnet build` 오류 0 / `read_console` 에러 0 / 기존 테스트 전부 통과.
  (A안은 신규 코드 없음 — 기존 테스트 회귀 확인만. B안 착수 시 드라이버 로직 EditMode 테스트 필수.)
- 시각 판정: Play 모드 스크린샷 vs 레퍼런스 이미지 — 실루엣 3요소
  (①비대칭 X자 다리 ②전경+좌비틀림 상체 ③총구·시선 방향 일치) 체크.

## 7. 리스크 / 알려진 한계

| 항목 | 내용 | 대응 |
|---|---|---|
| 손가락 표현 불가 | 24본 리그, 손가락 본 없음 → 왼손 "펼침" 재현 불가 | 손목 각도로 뉘앙스만. 수용 |
| Humanoid 리타게팅 뒤틀림 | 극단 포즈(무릎 최대 굴곡)에서 근육 공간 클램프 가능 | ✅ 실측 결과 왜곡 없음 (무릎 굴곡 ~105° 그대로 재현) |
| 오른손-총 정렬 | 손 위치는 애니, 총 각도는 RigProfile — 두 소스 | ✅ 현 RigProfile 값 그대로 총구가 좌하단(기체 오른쪽-전방-하향) — 조정 불필요 |
| 스케일/프레임 함정 | 05 §4-확정의 ★ 2건 (재발 이력 있음) | ✅ 내보내기 스크립트에 명시 포함, 재발 없음 |
| **Root 본 함정 (신규 발견)** | 아마추어에 `Root` 본이 있으면 Humanoid CopyFromOther가 **조용히 실패**(클립 0개, 콘솔 에러 없음) | §9-3 절차 — 내보내기 전용 복제 리그에서 Root 제거 + `use_armature_deform_only=True`. 05 §4-확정에도 ★로 기록 |

---

## 8. 제작 규약 — 포즈 지정 방식 (재작업 시 그대로 사용)

본 로컬 축을 추측하지 않고 **본별 절대 월드 방향**으로 지정했다. 헬퍼는 아래 한 줄이 핵심이다.

```python
# 본의 head→tail 축을 월드 방향 d로 정렬 (twist = d축 기준 추가 회전)
q = rest_dir.rotation_difference(d) @ R_rest          # R_rest = bone.matrix_local.to_quaternion()
if twist: q = Quaternion(d, radians(twist)) @ q
pose_bone.matrix = Matrix.Translation(head) @ q.to_matrix().to_4x4()   # 부모 먼저, 매 본마다 view_layer.update()
```

- 좌표 규약(Blender 아마추어 공간): **+X=기체 왼쪽 / −Y=전방 / +Z=위**.
  `limbdir(az, el)` — 팔다리 (az=0 전방, +90 기체 왼쪽, el=−90 아래).
  `updir(pitch, roll)` — 척추/골반/머리 (pitch=+ 앞으로 숙임, roll=+ 기체 왼쪽으로 기울임).
- 처음엔 "본별 월드축 오일러 델타"로 시도했으나 **체인 누적(Δ_bone = Δ_parent @ δ)** 때문에
  팔이 의도와 다른 방향으로 올라가 제어가 불가능했다. 절대 방향 지정으로 바꾸고 나서야 수렴.
- 포즈 각도는 **레퍼런스의 관절 픽셀 좌표를 재고 단축(foreshortening) 보정으로 역산**해서 얻었다
  (화면상 길이 ÷ 실제 본 길이 = 코사인 → 카메라 축 성분). 눈대중 반복보다 훨씬 빨랐다.
- 포즈 스펙 원본: `scratchpad/heropose.py`(헬퍼) + `scratchpad/hero_action.py`(POSE_SPEC/SWAY).
  본 문서 §9-2에 확정 수치를 그대로 옮겨 두었다.

## 9. 결과 (2026-08-01)

### 9-1. 산출물

| 항목 | 값 |
|---|---|
| Blender 액션 | `Mecha_Pose_HeroLunge` (0~60프레임 / 30fps / 2s 루프, `use_fake_user`, `Mecha Ver 2.blend`) |
| FBX | `Assets/_Project/Art/Models/Mecha/Rigged/Mecha_Anim_HeroLunge.fbx` (아마추어만, 24본) |
| Unity 클립 | `Mecha_Pose_HeroLunge` — len 2.0s / 30fps / Loop / Human, Humanoid+CopyFromOther(MechaAvatar), Bake Into Pose 3종 |
| 사웨이 | Hips 월드 Y 0.382 → 0.333 → 0.382 (약 5cm 부유, 프레임 0=60 봉합 확인) |
| 루트 모션 | 없음 (root Y = 0 고정 확인) |

### 9-2. 확정 포즈 수치 (kind / a / b / twist)

`L`=limbdir(az, el), `U`=updir(pitch, roll). 프레임 30에서 사웨이 델타(±1~3°)만 더해진다.

```
Hips  U 20 -14  10   | Spine02 U 24 -15   4 | Spine01 U 30 -16  -3 | Spine U 36 -17 -9
neck  U 26 -14 -16   | Head    U 20 -12 -26
RightShoulder L -85   6 | RightArm L -55 -62 | RightForeArm L   5 -22 | RightHand L -12 -32
LeftShoulder  L  95   8 | LeftArm  L 100 -10 | LeftForeArm  L 118 -20 | LeftHand  L 124 -30
RightUpLeg L -48 -45 | RightLeg L 158  -8 | RightFoot L 168 -40 | RightToeBase L 168 -45
LeftUpLeg  L 155 -48 | LeftLeg  L 158 -36 | LeftFoot  L 158 -55 | LeftToeBase  L 158 -62
Hips location = (0, 0.02, +0.20) 월드 / 프레임 30에서 z −0.05
```

왼다리는 2026-08-03 수정값이다. 초기값(UpLeg 130/−42, Leg 103/−58, Foot 108/−75, Toe 108/−80)은
① 다리가 뒤가 아니라 옆으로 벌어지고 ② 정강이 el이 허벅지보다 가팔라 **무릎이 뒤로 22° 꺾인
역관절**이었다(사용자 지적 "왼쪽다리가 돌아가 있다"). 판정 요령: 무릎캡 방향 ≈ normalize(허벅지방향
− 정강이방향)이 **전방-아래(−Y,−Z)** 를 향해야 정상. 트레일 다리는 정강이 el이 허벅지 el보다
**완만해야**(덜 음수) 자연 굴곡이 된다. 수정 시 프레임 30 사웨이는 기존 델타(본별 0.7~3.5°)를
쿼터니언 델타로 옮겨 보존했고, 60=0 봉합 유지.

### 9-3. 내보내기 절차 (★ Root 함정 포함 — 이대로 하지 않으면 클립이 안 생긴다)

1. 내보내기 전용 **복제 아마추어**를 만든다 (`src.data.copy()` → 새 오브젝트).
2. 복제본의 **모든 포즈 본 제약 제거** + `animation_data_clear()` (IK/CopyRot/DampedTrack이
   `Root["IK_*"]` 커스텀 프롭 드라이버에 의존하므로 Root를 지우면 에러가 난다).
3. EDIT 모드에서 **`Root` 본 삭제** → Hips가 최상위. `Mecha.fbx`의 MechaAvatar 계층
   (최상위=Hips, 24 디폼 본)과 일치시켜야 한다.
   - MCP에서 `bpy.ops.object.mode_set(mode='EDIT')`는 **컨텍스트 오버라이드 없이는 무시된다**
     (`armature.edit_bones`가 None으로 나옴). `bpy.context.temp_override(window=…, area=VIEW_3D,
     region=WINDOW, active_object=dup, selected_objects=[dup], object=dup)` 로 감쌀 것.
4. 복제본에 액션 연결(`animation_data.action` + `action_slot = act.slots[0]`) 후
   05 §4-확정 설정 + **`use_armature_deform_only=True`** 로 내보낸다.
5. 복제본 삭제 → 원본 리그 원상 복구 → blend 저장.

**증상 기억할 것**: Root가 남아 있으면 Generic으로는 테이크·클립이 정상이지만,
`animationType=Human` + `CopyFromOther`로 바꾸는 순간 **AnimationClip 서브에셋이 0개**가 되고
**콘솔에는 아무 에러도 안 뜬다.** 진단은 `importer.importedTakeInfos.Length`와
`LoadAllAssetsAtPath`의 Transform 개수 비교(정상=25, Root 포함=26)로 한다.

### 9-4. 검증 결과

- **실루엣 3요소** (§6 기준): ① 비대칭 X자 다리 ✅ ② 전경+비틀림 상체 ✅ ③ 총구·시선 일치 ✅
  — 레퍼런스와 동일 앵글 렌더 나란히 대조로 확인.
- **Play 모드 (RigLab)**: `PlayableGraph`로 클립을 프리뷰 Animator에 직접 물려 재생 확인.
  **레일건(오른손 마운트)·미사일 포드(등 마운트)가 포즈를 그대로 따라오고**, 총구가
  레퍼런스와 같은 좌하단 방향을 가리킨다. RigProfile 값 수정 없음.
- CLAUDE.md §1: `dotnet build` 오류 0 (경고 4 = 기존 MSB3277 노이즈) / `read_console` 에러 0 /
  EditMode 테스트 **255개 전부 통과**. A안은 신규 코드 0줄이라 회귀 확인 성격.
- 씬 변경 없음 (RigLab·Boot 모두 dirty 아님), 게임 에셋(AC_Mecha·RigProfile) 무변경.

### 9-5. 노출 배선 — RigLab 핫키 P ✅ (2026-08-01, 사용자 결정)

`AC_Mecha`에 상태를 만들지 않고 **RigLab에서만 `AnimationClipPlayable`을 Animator에 직접
물려** 재생한다. 확인용 클립으로 게임용 컨트롤러를 오염시키지 않기 위한 선택.

- **P**: 없음 → 포즈들 → 없음 순환. 우측 UI 패널에도 클립별 토글 버튼.
- 클립 목록은 `RigLabController._poseClips`(RigLab 씬 인스펙터). 앞으로 포즈가 늘면 배열에만 추가.
- 1~5 / 6~8 / 9·0 아무 키나 누르면 **포즈에서 자동 이탈**해 AC 상태로 복귀한다
  (그래프 파괴 → 컨트롤러 그래프 자동 복귀. `Animator.Rebind()`는 파라미터가 날아가서 안 쓴다).
- 공중 포즈는 루트 아래로 크게 뻗어 바닥에 파묻히므로 포즈 진입 시 **접지 오프셋을 다시 잰다**.
- `Animator.speed`는 PlayableGraph 출력에 안 걸린다 → `=/-` 속도는 플레이어블에 직접 준다.
- `OnDisable`·대상 전환 시 그래프 파괴 필수 (안 하면 Play 종료 시 누수 경고).
- 순환 규칙은 `RigLabMath.CycleWithNone`(신규 순수 함수, 마운트/총구 선택과 공용) — EditMode 3개 추가.

### 9-6. 잔여 결정 사항

1. **격납고/타이틀 노출**: 아직 안 함. 게임 화면에 히어로 포즈를 세우려면 별도 배선이 필요하다
   (RigLab은 에디터 전용 씬 — 빌드 미포함).
2. **B안 인게임 액션화** (§3·§5 P2): 대쉬 절정 프레임 교체 vs 점프 전용 상태 신설.
   착수 시 05 §10 백로그 형식으로 사양부터 작성.
