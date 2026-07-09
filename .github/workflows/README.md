# CI/CD (GameCI) 설정 안내

`ci.yml`은 push/PR마다 **테스트 → Windows(PC) 빌드**를 실행합니다.
동작시키려면 GitHub 저장소에 Unity 라이선스 시크릿을 등록해야 합니다.

## 1. 저장소 준비

이 프로젝트는 아직 로컬 git 저장소입니다. GitHub에 올려야 Actions가 동작합니다.

```bash
# GitHub에서 빈 저장소 생성 후
git remote add origin https://github.com/<계정>/<저장소>.git
git push -u origin main
```

> LFS 사용 중이므로 원격도 LFS를 지원해야 합니다(GitHub 기본 지원).

## 2. Unity 라이선스 시크릿 (개인/Personal 기준)

개인 라이선스는 활성화 파일(`.ulf`)의 내용을 시크릿으로 넣습니다.

1. 활성화 파일 요청 워크플로를 한 번 실행하거나, 아래 방법으로 `.ulf` 확보:
   - 로컬 Unity에서 로그인되어 있다면 활성화 파일 위치:
     - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - 또는 GameCI 문서의 `activation.yml`(request-manual-activation-file) 절차로
     `Unity_v6000.x.alf` 발급 → https://license.unity3d.com/manual 에서 `.ulf` 변환.
2. GitHub 저장소 **Settings → Secrets and variables → Actions → New repository secret**:

| 시크릿 | 값 |
|--------|-----|
| `UNITY_LICENSE` | `.ulf` 파일 **전체 내용**(XML) |
| `UNITY_EMAIL` | Unity 계정 이메일 |
| `UNITY_PASSWORD` | Unity 계정 비밀번호 |

> **Professional/Plus 라이선스**는 `UNITY_LICENSE` 대신 `UNITY_SERIAL`(+ EMAIL/PASSWORD)을 사용.

## 3. 주의사항

- **Unity 버전 이미지**: GameCI가 `ProjectSettings/ProjectVersion.txt`(6000.5.3f1)에 맞는
  Docker 이미지를 자동 선택합니다. 매우 최신 버전은 이미지가 아직 없을 수 있으니,
  실패 시 GameCI가 지원하는 근접 버전으로 임시 조정하거나 이미지 배포를 기다립니다.
- **Windows 빌드 백엔드**: 현재 Mono 백엔드는 Linux 러너에서 Windows 타깃 빌드가 가능합니다.
  IL2CPP로 전환하면 Windows 러너(`runs-on: windows-latest`)가 필요합니다.
- **로컬 CLI 빌드**: `Assets/_Project/Scripts/Editor/BuildScript.cs` 참고.
  ```
  "C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe" ^
    -quit -batchmode -nographics ^
    -projectPath "C:\Users\iam12\Mecha Survivor" ^
    -executeMethod MechaSurvivor.Editor.BuildScript.PerformWindowsBuild ^
    -buildPath "Builds\Windows\MechaSurvivor.exe" ^
    -logFile -
  ```
