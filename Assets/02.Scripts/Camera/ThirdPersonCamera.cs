using UnityEngine;
using UnityEngine.InputSystem;

namespace CrowdCombat.Camera
{
    /// <summary>
    /// 3인칭 카메라. 마우스로 시선 회전, 타겟(플레이어) 뒤를 따라옵니다.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] protected Transform target;
        [SerializeField] protected Vector3 targetOffset = new Vector3(0f, 1.5f, 0f); // 플레이어 머리 위

        [Header("Orbit")]
        [SerializeField] protected float distance = 5f;
        [SerializeField] protected float minPitch = -20f;  // 땅까지 내려가지 않게
        [SerializeField] protected float maxPitch = 60f;
        [SerializeField] protected float mouseSensitivity = 2f;

        [Header("Input")]
        [SerializeField] protected InputActionAsset inputActions;
        [SerializeField] protected string lookActionName = "Look";

        protected float yaw;   // 수평 각도
        protected float pitch; // 수직 각도
        protected InputAction lookAction;

        protected virtual void Awake()
        {
            if (inputActions != null)
            {
                var map = inputActions.FindActionMap("Player");
                if (map != null)
                    lookAction = map.FindAction(lookActionName);
            }
        }

        protected virtual void OnEnable()
        {
            lookAction?.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        protected virtual void OnDisable()
        {
            lookAction?.Disable();
        }

        protected virtual void Start()
        {
            if (target != null)
            {
                yaw = target.eulerAngles.y;
                pitch = 20f;
            }
        }

        protected virtual void LateUpdate()
        {
            if (target == null) return;

            // 마우스 Look 입력 (델타 값)
            Vector2 look = Vector2.zero;
            if (lookAction != null)
                look = lookAction.ReadValue<Vector2>();

            // 마우스 움직임에 따라 카메라 회전 (델타 값이므로 Time.deltaTime 불필요)
            yaw += look.x * mouseSensitivity;
            pitch -= look.y * mouseSensitivity;
            
            // Y축 클램프 (땅까지 내려가지 않게)
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            // 오빗 위치 계산
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rot * new Vector3(0f, 0f, -distance);
            Vector3 targetPos = target.position + targetOffset;
            
            transform.position = targetPos + offset;
            transform.rotation = Quaternion.LookRotation((targetPos - transform.position).normalized);
        }

        /// <summary>카메라가 바라보는 수평 방향 (Y만 사용, 이동용)</summary>
        public Vector3 GetForwardXZ()
        {
            Vector3 f = transform.forward;
            f.y = 0f;
            return f.normalized;
        }

        /// <summary>카메라 기준 오른쪽 방향 (Y 제외)</summary>
        public Vector3 GetRightXZ()
        {
            Vector3 r = transform.right;
            r.y = 0f;
            return r.normalized;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
