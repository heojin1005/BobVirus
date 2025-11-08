1. 최초 설정 (PC에 한 번만, 윈도우는 안 해도 됨.)

git config --global user.name "이름"

기능: 이 PC에서 커밋하는 사람의 이름을 설정합니다. (GitHub 닉네임 권장)

git config --global user.email "이메일"

기능: 이 PC에서 커밋하는 사람의 이메일을 설정합니다. (GitHub 계정 이메일)



2. 프로젝트 시작 (프로젝트당 한 번만)

git clone [URL]

기능: GitHub에 있는 원격 저장소를 내 컴퓨터로 복제합니다. (협업 시작 시)

예: git clone https://github.com/heojin1005/BobVirus.git

git init

기능: 내 컴퓨터의 기존 폴더(예: 새 Unity 프로젝트)에 .git 저장소를 생성하여 Git 관리를 시작합니다.



3. 일상적인 로컬 작업 (가장 자주 사용)

git status

기능: 현재 파일들의 상태 (수정된 파일, 추가된 파일 등)를 확인합니다. (수시로 확인)

git add [파일이름]

기능: 특정 파일의 변경 사항을 "커밋할 목록"(Staging Area)에 올립니다.

git add .

기능: 변경된 모든 파일을 "커밋할 목록"에 한꺼번에 올립니다. (가장 많이 씀)

git commit -m "커밋 메시지"

기능: add한 파일들을 하나의 작업 단위(커밋)로 묶어 내 로컬 저장소에 저장합니다.

예: git commit -m "플레이어 점프 기능 추가"



4. 협업 및 동기화 (GitHub)

git pull origin [브랜치명]

기능: GitHub의 최신 변경 사항을 다운로드하여 내 로컬 브랜치에 즉시 합칩(Merge)니다. (작업 시작 전 필수!)

예: git pull origin main

git push origin [브랜치명]

기능: 내가 로컬에서 commit한 작업 내역을 GitHub 서버로 업로드합니다. (팀원 공유)

예: git push origin feature/player-move

git fetch origin

기능: GitHub의 최신 변경 사항을 다운로드만 하고, 일단 내 브랜치에 합치지는 않습니다. (pull보다 안전하게 상황만 볼 때)



5. 브랜치 (가지치기 및 합치기)

git branch

기능: 내 로컬 저장소의 모든 브랜치 목록을 봅니다. (현재 브랜치는 * 표시)

git checkout -b [새 브랜치명]

기능: 새로운 브랜치를 생성함과 동시에 그 브랜치로 이동(전환)합니다. (가장 많이 씀)

예: git checkout -b feature/login

git checkout [기존 브랜치명]

기능: 이미 존재하는 다른 브랜치로 이동(전환)합니다.

예: git checkout main

git merge [합칠 브랜치명]

기능: 현재 내가 있는 브랜치(예: main)에 다른 브랜치(예: feature/login)의 작업 내용을 합칩니다. (보통 GitHub의 Pull Request로 대체함)



6. 과거 기록 확인 및 되돌리기

git log

기능: 현재까지의 모든 커밋 히스토리를 시간순으로 봅니다. (커밋 ID, 작성자, 메시지 확인. q 눌러서 종료)

git revert [커밋 ID]

기능: (안전) 특정 커밋에서 했던 작업을 취소하는 '새로운 커밋'을 만듭니다. (공유된 히스토리를 되돌릴 때 사용)

git reset --hard [커밋 ID]

기능: (위험) 내 로컬 저장소를 특정 커밋의 상태로 강제로 되돌립니다. (그 이후의 모든 커밋이 사라짐. push하기 전 실수에만 사용)
