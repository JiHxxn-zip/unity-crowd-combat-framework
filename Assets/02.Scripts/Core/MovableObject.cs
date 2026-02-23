using UnityEngine;

namespace CrowdCombat.Core
{
    /// <summary>
    /// 3D 오브젝트를 움직이는 컴포넌트.
    /// Interaction을 상속하여 '움직임' 자체를 인터랙션으로 사용하거나,
    /// 단순히 트랜스폼을 제어할 때 사용합니다.
    /// </summary>
    public class MovableObject : Interaction
    {
        [Header("Movement")]
        [SerializeField] protected float moveSpeed = 5f;
        [SerializeField] protected float rotationSpeed = 720f;
        [SerializeField] protected bool useRigidbody = true;

        protected Rigidbody rb;
        protected Vector3 targetVelocity;

        protected virtual void Awake()
        {
            if (useRigidbody)
                TryGetComponent(out rb);
        }

        /// <summary>
        /// 월드 방향으로 이동 (호출자에서 매 프레임/고정프레임 호출).
        /// </summary>
        public virtual void Move(Vector3 direction)
        {
            targetVelocity = direction.normalized * moveSpeed;
            if (direction.sqrMagnitude > 0.01f)
                RotateToward(direction);
        }

        /// <summary>
        /// 목표 방향을 바라보도록 회전.
        /// </summary>
        public virtual void RotateToward(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.01f) return;

            Quaternion targetRot = Quaternion.LookRotation(worldDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }

        protected virtual void FixedUpdate()
        {
            if (useRigidbody && rb != null)
            {
                rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
                targetVelocity = Vector3.zero;
            }
        }

        protected virtual void Update()
        {
            if (!useRigidbody || rb == null)
            {
                transform.position += targetVelocity * Time.deltaTime;
                targetVelocity = Vector3.zero;
            }
        }

        public override void OnInteract(GameObject interactor)
        {
            // 이동 가능 오브젝트를 밀거나 끌 때 등 확장용
            // 기본은 아무 동작 없음.
        }
    }
}
