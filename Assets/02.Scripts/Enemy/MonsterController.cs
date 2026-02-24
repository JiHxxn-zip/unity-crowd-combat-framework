using UnityEngine;
using CrowdCombat.Core;

namespace CrowdCombat.Enemy
{
    /// <summary>
    /// 몬스터 AI 컨트롤러. Ground 레이어 기준으로 자율 이동.
    /// MovableObject를 상속하여 이동 로직 재사용.
    /// ITickable을 구현하여 TickManager에서 분산 호출됩니다.
    /// </summary>
    public class MonsterController : MovableObject, ITickable
    {
        [Header("AI Movement")]
        [SerializeField] protected float directionChangeInterval = 2f;
        [SerializeField] protected float groundCheckDistance = 1.5f;
        [SerializeField] protected float forwardCheckDistance = 1.0f;
        [SerializeField] protected LayerMask groundLayer = 1; // Default layer (Ground)
        [SerializeField] protected float aiMoveSpeed = 2f;    // 기본 이동 속도보다 느리게

        [Header("Hit Reaction")]
        [SerializeField] protected float lightKnockbackSpeed = 4f;
        [SerializeField] protected float lightKnockbackUpVelocity = 2f;
        [SerializeField] protected float launchKnockbackSpeed = 10f;
        [SerializeField] protected float launchKnockbackUpVelocity = 6f;
        [SerializeField] protected float lightReactionDuration = 0.25f;
        [SerializeField] protected float launchReactionDuration = 0.6f;
        [SerializeField] protected float knockUpVelocity = 8f;
        [SerializeField] protected float extraStunDuration = 0.3f; // 넉백 후 추가 스턴 시간

        protected Vector3 moveDir;
        protected float directionTimer;
        protected float lastTickTime;
        protected float hitReactionTimer;

        protected override void Awake()
        {
            base.Awake();

            // 몬스터 이동 속도를 전체 기본(moveSpeed)보다 줄여서 세팅
            moveSpeed = aiMoveSpeed;

            PickNewDirection();
        }

        protected virtual void OnEnable()
        {
            // TickManager에 등록
            TickManager.Instance.Register(this);
        }

        protected virtual void OnDisable()
        {
            // TickManager에서 해제
            if (TickManager.Instance != null)
            {
                TickManager.Instance.Unregister(this);
            }
        }

        protected virtual void Update()
        {
            // 피격/넉백 스턴 중에는 이동하지 않음
            if (hitReactionTimer > 0f)
                return;

            // 매 프레임 이동은 계속 실행 (부드러운 이동을 위해)
            // AI 로직은 Tick()에서 처리
            Move(moveDir);
        }

        /// <summary>
        /// TickManager에서 호출되는 AI 로직.
        /// 10프레임마다 실행되며, 한 프레임에 20마리만 처리됩니다.
        /// </summary>
        public void Tick()
        {
            // 시간 기반 타이머 업데이트 (프레임 간격이 일정하지 않으므로)
            float deltaTime = Time.time - lastTickTime;
            if (lastTickTime == 0f)
                deltaTime = Time.fixedDeltaTime * 10f; // 첫 호출 시 대략적인 값 사용

            directionTimer -= deltaTime;
            lastTickTime = Time.time;

            // 방향 변경 시간이 지났거나 앞에 땅이 없으면 새 방향 선택
            if (directionTimer <= 0f || !HasGroundAhead())
            {
                PickNewDirection();
            }
        }

        /// <summary>
        /// 새로운 랜덤 방향 선택 (XZ 평면)
        /// </summary>
        protected virtual void PickNewDirection()
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            moveDir = new Vector3(randomDir.x, 0f, randomDir.y);
            directionTimer = directionChangeInterval;
        }

        /// <summary>
        /// 앞쪽에 Ground 레이어가 있는지 확인
        /// </summary>
        protected virtual bool HasGroundAhead()
        {
            // 몬스터 앞쪽 위치에서 아래로 레이캐스트
            Vector3 checkOrigin = transform.position + moveDir.normalized * forwardCheckDistance + Vector3.up * 0.5f;
            return Physics.Raycast(checkOrigin, Vector3.down, groundCheckDistance, groundLayer);
        }

        public override void OnInteract(GameObject interactor)
        {
            // 나중에 플레이어와의 상호작용(공격 등) 확장 가능
            base.OnInteract(interactor);
        }

        /// <summary>
        /// 1~3타용 가벼운 넉백.
        /// </summary>
        public virtual void ApplyLightHit(Vector3 direction)
        {
            if (!useRigidbody || rb == null)
                return;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                direction = -transform.forward;

            direction.Normalize();

            Vector3 v = direction * lightKnockbackSpeed;
            v.y = lightKnockbackUpVelocity;
            rb.linearVelocity = v;
            hitReactionTimer = lightReactionDuration + extraStunDuration;
        }

        /// <summary>
        /// 4타용 멀리 날려 보내는 강한 넉백 (포물선 느낌).
        /// </summary>
        public virtual void ApplyLaunchHit(Vector3 direction)
        {
            if (!useRigidbody || rb == null)
                return;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                direction = -transform.forward;

            direction.Normalize();

            Vector3 v = direction * launchKnockbackSpeed;
            v.y = launchKnockbackUpVelocity;
            rb.linearVelocity = v;
            hitReactionTimer = launchReactionDuration + extraStunDuration;
        }

        protected override void FixedUpdate()
        {
            // 피격 리액션 중에는 AI 이동을 잠시 멈추고, 기존 속도로만 날아가게 둔다.
            if (useRigidbody && rb != null && hitReactionTimer > 0f)
            {
                hitReactionTimer -= Time.fixedDeltaTime;
                return;
            }

            base.FixedUpdate();
        }

        /// <summary>
        /// 플레이어 공격 등에 의해 위로 띄워질 때 사용.
        /// </summary>
        public virtual void ApplyKnockUp(float? customVelocity = null)
        {
            if (!useRigidbody || rb == null)
                return;

            float finalVelocity = customVelocity ?? knockUpVelocity;
            Vector3 v = rb.linearVelocity;
            if (v.y < finalVelocity)
            {
                v.y = finalVelocity;
                rb.linearVelocity = v;
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            // 디버그용: 이동 방향과 Ground 체크 위치 표시
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, moveDir * 2f);
            
            Gizmos.color = Color.yellow;
            Vector3 checkOrigin = transform.position + moveDir.normalized * forwardCheckDistance + Vector3.up * 0.5f;
            Gizmos.DrawRay(checkOrigin, Vector3.down * groundCheckDistance);
        }
    }
}
