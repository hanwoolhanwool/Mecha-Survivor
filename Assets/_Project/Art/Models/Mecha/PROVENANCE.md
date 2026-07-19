# 기체 모델 생성 출처 기록 (Provenance)

이 폴더의 FBX 4종은 전부 Meshy.ai text-to-3D API로 생성했다 (MCP for Unity `generate_model` 경유).
참조 이미지 없이 순수 텍스트 프롬프트만 사용했으며, 프롬프트에 특정 IP·작품명·아티스트명은 포함하지 않았다.

- 계정: hanwoolisland@gmail.com 의 Meshy API 키 (에디터 보안 저장소)
- 라이선스: 무료 플랜이면 CC BY 4.0 (Meshy 크레딧 표기 필요), 유료 플랜이면 완전 소유
  - https://help.meshy.ai/en/articles/10137554-what-is-the-ownership-of-the-generated-models
- 공통 설정: mode=text, format=fbx, texture=true, target_size=2
- 공통 후처리: 내장 텍스처를 `Textures/<모델명>/`으로 추출, materialImportMode=ImportViaMaterialDescription,
  globalScale 재계산으로 전고 2m 보정 (원시 높이 ~190유닛)

---

## MechaHumanoid01.fbx — 플레이어 기체 (Game 씬 배선됨)

- 생성일: 2026-07-18
- Meshy job ID: `d49262a81eca4ef7af5459c418a6c385`
- 프롬프트:
  > Humanoid bipedal sci-fi mecha robot, game-ready character, sleek military armor plating with hard-surface panel lines, compact head with single glowing visor sensor, articulated arms and legs, gunmetal gray and steel blue color scheme with orange accent lights, standing A-pose, clean stylized low-poly friendly design, PBR textures

## MechaHeavy01.fbx — 중장갑 강습형 (레퍼런스)

- 생성일: 2026-07-19
- Meshy job ID: `2edf3bac3f844fdaa2a92e1eb0b60867`
  (1차 시도 `dcfaa5c75bea4cc4bde422904c4c9f1e`는 에디터 리로드로 중단 → 재제출)
- 프롬프트:
  > Heavy assault humanoid mecha robot, game-ready character, massive bulky armor plating, broad shoulders with missile pod launchers, thick stocky legs, fortress-like silhouette, dark olive green and gunmetal color scheme with yellow hazard stripes, standing A-pose, clean stylized low-poly friendly hard-surface design, PBR textures

## MechaScout01.fbx — 경량 정찰형 (레퍼런스)

- 생성일: 2026-07-19
- Meshy job ID: `abaeac0a268a41febdface362b3ad24d`
  (1차 시도 `fdd1a220b868428b9b29e5665dbf047e`는 에디터 리로드로 중단 → 재제출)
- 프롬프트:
  > Lightweight scout humanoid mecha robot, game-ready character, slim agile frame, aerodynamic angular armor panels, digitigrade reverse-joint legs with thruster calves, narrow sensor head, white and cyan color scheme with glowing blue accent lights, standing A-pose, clean stylized low-poly friendly hard-surface design, PBR textures

## MechaArtillery01.fbx — 포격 지원형 (레퍼런스)

- 생성일: 2026-07-19
- Meshy job ID: `c66aa83b250f49139f78f99f7a35f909`
  (1차 시도 `c0032658f0c34a6385959a7dcf223e46`는 에디터 리로드로 중단 → 재제출)
- 프롬프트:
  > Artillery support humanoid mecha robot, game-ready character, medium build with large back-mounted twin cannon battery, quad ammunition drums on hips, reinforced knee joints, sandy tan and dark brown desert color scheme with red warning markings, standing A-pose, clean stylized low-poly friendly hard-surface design, PBR textures

---

---

## 리깅/애니메이션 (2026-07-19, Meshy Remesh + Rigging + Animation API)

`Rigged/` 폴더의 FBX는 위 원본에서 파생: Remesh(quad 30k) → Rigging(height 2m) → Animation.
- MechaHumanoid01: remesh `019f7798-3280-7742-995d-3bd1aa6c2269`, rig `019f779a-8159-7acf-bc60-13d83a7e4015`, idle anim `019f779d-5860-788e-be41-826d85429977`
- MechaScout01: remesh `019f7798-3140-71fa-826e-c438ec15cdb5`, rig `019f779a-9b44-77ed-911c-10f36d7b37cb`, idle anim `019f779d-5b30-72fa-84b3-dbeea86fff88`
- MechaHeavy01: remesh `019f7798-33ac-7743-aa1d-60934f4e4528`, rig `019f779a-b567-7ad2-b66f-fde7341cf37d`, idle anim `019f779d-5dee-72fb-a0ce-d8c8bb7b6947`
- 걷기/달리기 클립은 리깅 태스크의 basic_animations 산출물 (Meshy 애니메이션 라이브러리 프리셋, 동일 라이선스)
- MechaArtillery01: 리깅 실패 (422 Pose estimation failed — 등의 쌍포신 실루엣이 인간형 인식 불가). 정적 모델로 유지.

주의: 출시 빌드에 이 모델을 직접 쓰게 되면 (무료 플랜인 경우) 크레딧 화면에 Meshy 표기를 넣을 것.
새 모델을 생성해 추가할 때는 이 파일에 같은 형식으로 기록을 덧붙인다.
