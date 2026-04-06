using UnityEngine;
using UnityEngine.InputSystem;
using CrowdCombat.Core;
using CrowdCombat.Enemy;

namespace CrowdCombat.Player
{
    /// <summary>
    /// 지상 강타 스킬. PlayerController와 동일한 오브젝트에 추가해서 사용합니다.
    /// 공중에서 스킬 키를 누르면 빠르게 낙하하고, 착지 시 충격파로 주변 몬스터를 에어본시킵니다.
    /// </summary>
    public class GroundSlamSkill : MonoBehaviour
    {
        [Header("Ground Slam")]
        [SerializeField] private float slamDownSpeed = 25f;     // 강제 낙하 속도
        [SerializeField] private float shockwaveRadius = 5f;    // 착지 충격파 범위
        [SerializeField] private float slamDamage = 50f;        // 충격파 데미지
        [SerializeField] private float airborneVelocity = 10f;  // 에어본 상승 속도
        [SerializeField] private float cooldown = 2f;           // 스킬 쿨다운

        [Header("Ground Check")]
        [SerializeField] private float groundCheckDistance = 0.15f;
        [SerializeField] private LayerMask groundLayer = 1;

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string skillActionName = "GroundSlam";

        private Rigidbody rb;
        private CapsuleCollider col;
        private InputAction skillAction;

        private bool isSlamming;
        private bool wasGrounded;
        private float lastSlamTime = -999f;

        private void Awake()
        {
            TryGetComponent(out rb);
            TryGetComponent(out col);

            if (inputActions != null)
            {
                var map = inputActions.FindActionMap("Player");
                skillAction = map?.FindAction(skillActionName);
            }
        }

        private void OnEnable()
        {
            skillAction?.Enable();
        }

        private void OnDisable()
        {
            skillAction?.Disable();
            isSlamming = false;
        }

        private void Update()
        {
            bool isGrounded = CheckGrounded();

            // 착지 감지: 슬램 낙하 중에 지면에 닿은 순간
            if (isSlamming && !wasGrounded && isGrounded)
            {
                OnSlamLand();
                isSlamming = false;
            }

            wasGrounded = isGrounded;

            // 입력: 공중 + 쿨다운 완료 시에만 발동
            if (skillAction != null && skillAction.WasPressedThisFrame())
            {
                if (!isGrounded && !isSlamming && Time.time - lastSlamTime >= cooldown)
                {
                    StartSlam();
                }
            }
        }

        /// <summary>
        /// 슬램 시작: Y 속도를 강제로 아래로 설정해 빠르게 낙하합니다.
        /// </summary>
        private void StartSlam()
        {
            isSlamming = true;
            lastSlamTime = Time.time;

            if (rb != null)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -slamDownSpeed, rb.linearVelocity.z);
            }
        }

        /// <summary>
        /// 착지 시 충격파 처리: 범위 내 몬스터에게 에어본 + 데미지를 줍니다.
        /// </summary>
        private void OnSlamLand()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, shockwaveRadius);

            foreach (Collider hit in hits)
            {
                MonsterController monster = hit.GetComponent<MonsterController>()
                                         ?? hit.GetComponentInParent<MonsterController>();

                if (monster == null)
                    continue;

                // 사망한 몬스터는 스킵
                if (monster is IDamageable damageable && damageable.IsDead)
                    continue;

                // 에어본
                monster.ApplyKnockUp(airborneVelocity);

                // 데미지 (IDamageable 구현 시)
                if (monster is IDamageable target)
                    target.TakeDamage(slamDamage, gameObject);
            }
        }

        private bool CheckGrounded()
        {
            if (col != null)
            {
                Vector3 origin = transform.position + new Vector3(0f, col.center.y - (col.height * 0.5f - col.radius), 0f);
                return Physics.Raycast(origin, Vector3.down, groundCheckDistance + col.radius, groundLayer);
            }
            return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
        }

        private void OnDrawGizmosSelected()
        {
            // 충격파 범위 미리보기
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, shockwaveRadius);
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
        }
    }
}
