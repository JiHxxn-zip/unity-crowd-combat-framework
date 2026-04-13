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

        [Header("Camera Shake")]
        [SerializeField] protected float maxShakeOffset = 0.4f;

        protected float yaw;   // 수평 각도
        protected float pitch; // 수직 각도
        protected InputAction lookAction;

        /// <summary>
        /// true이면 look 입력과 transform 적용을 건너뜁니다.
        /// CinematicPlayer가 재생 중일 때 설정합니다.
        /// </summary>
        public bool IsCinematicOverride { get; set; }

        private float _shakeIntensity;
        private float _shakeDuration;
        private float _shakeTimer;
        private float _shakeSeedX;
        private float _shakeSeedY;

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

            // 시네마틱 재생 중: 입력과 위치 적용을 모두 건너뜁니다
            if (IsCinematicOverride) return;

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

            ApplyShake();
        }

        /// <summary>
        /// 현재 yaw/pitch/distance 기준으로 카메라가 위치해야 할 포즈를 계산합니다.
        /// CinematicPlayer의 블렌드아웃 목표 지점으로 사용됩니다.
        /// </summary>
        public void CalculateDesiredPose(out Vector3 position, out Quaternion rotation)
        {
            if (target == null)
            {
                position = transform.position;
                rotation = transform.rotation;
                return;
            }

            Quaternion rot    = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset    = rot * new Vector3(0f, 0f, -distance);
            Vector3 targetPos = target.position + targetOffset;

            position = targetPos + offset;
            rotation = Quaternion.LookRotation((targetPos - position).normalized);
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

        /// <summary>
        /// CameraManager에서 호출. Perlin noise 기반 감쇠 쉐이크를 시작합니다.
        /// </summary>
        public void TriggerShake(float duration, float intensity)
        {
            _shakeIntensity = intensity;
            _shakeDuration  = duration;
            _shakeTimer     = duration;
            _shakeSeedX     = Random.value * 100f;
            _shakeSeedY     = Random.value * 100f;
        }

        /// <summary>
        /// yaw/pitch 원본값을 오염시키지 않고 최종 position에만 offset을 더합니다.
        /// Time.unscaledTime 사용 → timeScale=0 히트스탑 중에도 쉐이크 진행.
        /// </summary>
        private void ApplyShake()
        {
            if (_shakeTimer <= 0f) return;

            _shakeTimer -= Time.unscaledDeltaTime;

            float t      = Mathf.Clamp01(_shakeTimer / _shakeDuration); // 1→0 감쇠
            float amount = _shakeIntensity * t;

            float t2 = Time.unscaledTime * 20f;
            float x  = (Mathf.PerlinNoise(_shakeSeedX, t2)       * 2f - 1f) * maxShakeOffset * amount;
            float y  = (Mathf.PerlinNoise(_shakeSeedY, t2 + 50f) * 2f - 1f) * maxShakeOffset * amount;

            transform.position += transform.right * x + transform.up * y;
        }
    }
}
