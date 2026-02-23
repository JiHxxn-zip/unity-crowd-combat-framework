# Crowd Combat Framework (핵앤슬래시 기반)

## 구조

### Core
- **Interaction** (추상 클래스)  
  `MonoBehaviour` 상속. 모든 상호작용 오브젝트의 베이스.  
  `OnInteract(GameObject interactor)` 구현 필요.

- **MovableObject**  
  `Interaction` 상속. 3D 오브젝트 이동/회전.  
  `Move(Vector3)`, `RotateToward(Vector3)` 사용. Rigidbody 옵션 지원.

- **UIManager**  
  싱글톤. 캔버스, 패널, 인터랙션 프롬프트 관리.  
  `ShowPanel()`, `ShowInteractionPrompt()`, `SetCursorLock()` 등.

### Camera
- **ThirdPersonCamera**  
  3인칭 오빗 카메라. 마우스 Look으로 시선 회전, Target(플레이어) 추적.  
  Input System `Player/Look` 액션 사용.

### Player
- **PlayerController**  
  WASD 이동 (`Player/Move`), 카메라가 보는 방향을 앞으로 보고 이동.  
  `ThirdPersonCamera` 또는 `Camera.main` 기준 방향 사용.

### Enemy
- **MonsterController**  
  `MovableObject` 상속. Ground 레이어 기준 자율 이동.  
  랜덤 방향으로 이동하며, 앞에 땅이 없으면 방향 전환.

- **MonsterPool**  
  몬스터 풀링 매니저. 지정 영역 내 Ground 레이어 위에 스폰.  
  기본 200마리 풀링 지원.

## 씬 설정 방법

1. **플레이어 프리팹 만들기**  
   메뉴: `Crowd Combat Framework > Create Player Prefab`  
   → `Assets/03.Prefabs/Player.prefab` 생성 (Rigidbody, CapsuleCollider, Visual 캡슐 포함).

2. **씬에 적용**  
   메뉴: `Crowd Combat Framework > Setup Scene (Player + Third Person Camera)`  
   - 씬에 Player 생성(또는 기존 Player 사용), Tag를 `Player`로 설정 권장.  
   - Main Camera에 `ThirdPersonCamera` 추가 및 Target = Player, Input Actions 할당.

3. **몬스터 프리팹 만들기**  
   메뉴: `Crowd Combat Framework > Create Monster Prefab`  
   → `Assets/03.Prefabs/Monster.prefab` 생성 (MonsterController, Rigidbody, CapsuleCollider 포함).

4. **몬스터 풀 설정**  
   - 빈 GameObject 생성 → `MonsterPool` 컴포넌트 추가.  
   - `monsterPrefab`에 위에서 만든 Monster 프리팹 할당.  
   - `poolSize` = 200, `groundLayer` = Ground 레이어 설정.  
   - `center`, `halfExtents`로 스폰 영역 조정.

5. **Input**  
   `InputSystem_Actions.inputactions`의 **Player** 맵에  
   - **Move** (Vector2, WASD)  
   - **Look** (Vector2, 마우스 델타)  
   가 바인딩되어 있어야 합니다. (프로젝트 기본 설정에 포함됨)

6. **Ground 레이어 설정**  
   - Layer 설정에서 "Ground" 레이어 생성 (없는 경우).  
   - 바닥 오브젝트의 Layer를 "Ground"로 설정.  
   - MonsterController와 MonsterPool의 `groundLayer`에 Ground 레이어 할당.

## 네임스페이스

- `CrowdCombat.Core` — Interaction, MovableObject, UIManager  
- `CrowdCombat.Camera` — ThirdPersonCamera  
- `CrowdCombat.Player` — PlayerController  
- `CrowdCombat.Enemy` — MonsterController, MonsterPool  
