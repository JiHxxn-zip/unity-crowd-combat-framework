using UnityEngine;
using UnityEngine.InputSystem;
using CrowdCombat.Camera;

namespace CrowdCombat.Player
{
    /// <summary>
    /// WASD로 이동, 스페이스바로 점프. 카메라 방향 기준으로 이동합니다.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] protected float moveSpeed = 5f;
        [SerializeField] protected float jumpForce = 8f;
        [SerializeField] protected float groundCheckDistance = 0.1f;
        [SerializeField] protected LayerMask groundLayer = 1; // Default layer

        [Header("References")]
        [SerializeField] protected ThirdPersonCamera thirdPersonCamera;
        [SerializeField] protected Transform cameraTransform;

        [Header("Input")]
        [SerializeField] protected InputActionAsset inputActions;
        [SerializeField] protected string moveActionName = "Move";
        [SerializeField] protected string jumpActionName = "Jump";
        [SerializeField] protected string attackActionName = "Attack";

        [Header("Attack")]
        [SerializeField] protected GameObject attackAreaPrefab;
        [SerializeField] protected float attackDistance = 1.5f;
        [SerializeField] protected float attackHeightOffset = 0.5f;
        [SerializeField] protected int maxComboCount = 4;
        [SerializeField] protected float comboResetTime = 0.4f;

        protected Vector2 moveInput;
        protected bool jumpInput;
        protected InputAction moveAction;
        protected InputAction jumpAction;
        protected InputAction attackAction;
        protected Rigidbody rb;
        protected bool useRigidbody = true;
        protected bool isGrounded;
        protected bool isJumping;
        protected int currentComboIndex;
        protected float lastAttackTime;

        protected virtual void Awake()
        {
            TryGetComponent(out rb);
            useRigidbody = rb != null;

            if (inputActions != null)
            {
                var map = inputActions.FindActionMap("Player");
                if (map != null)
                {
                    moveAction = map.FindAction(moveActionName);
                    jumpAction = map.FindAction(jumpActionName);
                    attackAction = map.FindAction(attackActionName);
                }
            }

            if (thirdPersonCamera == null)
                thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
            if (cameraTransform == null && UnityEngine.Camera.main != null)
                cameraTransform = UnityEngine.Camera.main.transform;
        }

        protected virtual void OnEnable()
        {
            moveAction?.Enable();
            jumpAction?.Enable();
            attackAction?.Enable();
        }

        protected virtual void OnDisable()
        {
            moveAction?.Disable();
            jumpAction?.Disable();
            attackAction?.Disable();
        }

        protected virtual void Update()
        {
            moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            jumpInput = jumpAction != null && jumpAction.WasPressedThisFrame();
            
            // 지면 체크
            CheckGrounded();

            // 공격 입력
            if (attackAction != null && attackAction.WasPressedThisFrame())
            {
                PerformAttack();
            }
        }

        protected virtual void FixedUpdate()
        {
            // 점프 처리
            if (jumpInput && isGrounded)
            {
                 if (useRigidbody && rb != null)
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
                    isJumping = true;
                }
                jumpInput = false;
            }

            // 점프가 끝났는지 체크 (위로 상승이 멈췄을 때)
            if (isJumping && useRigidbody && rb != null && rb.linearVelocity.y <= 0f)
            {
                isJumping = false;
            }

            // 이동 처리
            if (moveInput.sqrMagnitude < 0.01f)
            {
                if (useRigidbody && rb != null)
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
            else
            {
                // 카메라 기준 이동 방향 (Y 무시) - 플레이어는 회전하지 않음
                Vector3 forward = GetCameraForwardXZ();
                Vector3 right = GetCameraRightXZ();
                Vector3 moveDir = (forward * moveInput.y + right * moveInput.x).normalized;

                if (moveDir.sqrMagnitude > 0.01f)
                {
                    Vector3 velocity = moveDir * moveSpeed;
                    if (useRigidbody && rb != null)
                        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
                    else
                        transform.position += velocity * Time.deltaTime;
                }
            }

            // 내가 점프하지 않는 이상 몬스터 등에 떠밀려 위로 올라가지 않도록 Y속도 제한
            if (!isJumping && useRigidbody && rb != null && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            }
        }

        protected virtual void CheckGrounded()
        {
            // 캡슐 콜라이더나 레이캐스트로 지면 체크
            if (TryGetComponent<CapsuleCollider>(out var col))
            {
                Vector3 origin = transform.position + Vector3.up * (col.height * 0.5f - col.radius);
                isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + col.radius, groundLayer);
            }
            else
            {
                isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
            }
        }

        protected Vector3 GetCameraForwardXZ()
        {
            if (thirdPersonCamera != null)
                return thirdPersonCamera.GetForwardXZ();
            if (cameraTransform != null)
            {
                Vector3 f = cameraTransform.forward;
                f.y = 0f;
                return f.normalized;
            }
            return transform.forward;
        }

        protected Vector3 GetCameraRightXZ()
        {
            if (thirdPersonCamera != null)
                return thirdPersonCamera.GetRightXZ();
            if (cameraTransform != null)
            {
                Vector3 r = cameraTransform.right;
                r.y = 0f;
                return r.normalized;
            }
            return transform.right;
        }

        protected virtual void PerformAttack()
        {
            if (attackAreaPrefab == null)
                return;

            float now = Time.time;
            if (now - lastAttackTime > comboResetTime)
            {
                currentComboIndex = 0;
            }

            currentComboIndex = Mathf.Clamp(currentComboIndex + 1, 1, maxComboCount);
            lastAttackTime = now;

            Vector3 forward = GetCameraForwardXZ();
            if (forward.sqrMagnitude < 0.0001f)
                forward = transform.forward;

            Vector3 spawnPosition = transform.position
                                   + Vector3.up * attackHeightOffset
                                   + forward * attackDistance;

            Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
            GameObject go = Instantiate(attackAreaPrefab, spawnPosition, rotation);

            if (go != null && go.TryGetComponent<PlayerAttackArea>(out var area))
            {
                area.Initialize(transform, currentComboIndex);
            }
        }

    }
}
