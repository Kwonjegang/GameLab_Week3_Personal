# 타이틀 시퀀스 QA

## 확인 완료

- [x] `FishLooper` 씬의 `TitleSequenceController`에 플레이어, 선베드, 제목, 검은 페이드, 문구, 두 가이드가 연결됨.
- [x] 패드 및 키보드·마우스 가이드 이미지가 씬의 `RawImage`에 직접 연결됨.
- [x] 제목에 Parisienne 폰트 적용. `Assets/Fonts/Parisienne-OFL.txt`에 SIL Open Font License 1.1 포함.
- [x] `Assembly-CSharp.csproj` 컴파일 성공: 오류 0개 (`TargetFrameworkVersion=v4.7.2`, 기존 Unity 생성 firstpass DLL 사용).
- [x] `git diff --check` 통과.

## Unity 플레이 모드에서 확인할 항목

- [ ] 새로 플레이하면 캐릭터가 선베드로 떨어진 뒤 제목이 낙하하고 시작 안내가 나타난다.
- [ ] 제목 연출 중 입력은 무시되고, 안내가 나온 뒤 키보드·마우스 클릭 또는 패드 A/Start로 진행된다.
- [ ] 검은 페이드가 완료된 뒤 “근무 시간은 오전 9시부터 오후 3시까지입니다.”가 약 2.4초 표시된다.
- [ ] 패드 가이드가 먼저, 다음 입력 후 키보드·마우스 가이드가 표시된다.
- [ ] 마지막 입력 후 가이드가 사라지고 카메라·플레이어 조작과 9시 시계가 시작된다.
- [ ] 16:9 및 다른 화면 비율에서 제목과 두 가이드의 잘림이 없다.
- [ ] 재시작 시 같은 순서가 다시 재생되고 Console에 오류가 없다.

별도 .NET 빌드는 Unity 에디터의 Play 모드 화면 및 Console 확인을 대신하지 않습니다.
