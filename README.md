# Unity Musou Optimization Framework
### Unity 기반 무쌍 스타일 대규모 몬스터 전투 최적화 프레임워크 실험 프로젝트
<img width="1152" height="643" alt="image" src="https://github.com/user-attachments/assets/a10a57d0-daa9-4b28-aa0a-58730f6f3638" />

### 📌 Overview
본 프로젝트는 다수 몬스터가 등장하는 무쌍형 전투 구조에서의 성능 병목 분석 및 최적화 전략 구현을 목표로 합니다.

단순 게임 제작이 아닌,
군집 처리 아키텍처 설계와 성능 실험에 초점을 둔 기술 연구 프로젝트입니다.

### Roadmap
 200 Enemy Object Pool 구현
 Tick 분산 업데이트 시스템
 거리 기반 LOD 전환
 Animator 비용 비교 실험
 NavMesh 제거 후 군집 이동 구조 설계
 300~500 Enemy 안정화 실험

###  🔍 Research Focus
이 프로젝트는 다음 질문에 대한 실험을 포함합니다:
Update 200개는 어디서 병목이 발생하는가?
NavMeshAgent는 몇 개부터 위험한가?
SkinnedMeshRenderer의 실제 GPU 비용은?
Tick 분산은 체감 없이 성능 개선이 가능한가?
