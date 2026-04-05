using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CrowdCombat.Camera;

namespace CrowdCombat.Weapon
{
    /// <summary>
    /// 플레이어가 진공 폭탄을 발사하는 컴포넌트.
    /// 발사체를 풀로 관리하며, 폭발 후 SetActive(false)된 오브젝트를 재사용합니다.
    ///
    /// [프리팹 설정]
    /// - ProjectilePrefab: VacuumBombProjectile 프리팹 연결
    /// - PoolSize: 사전 생성할 발사체 수 (기본 5)
    /// - LaunchPoint: 발사 위치 Transform (없으면 Player 위치 + 높이 오프셋 사용)
    /// - InputActions: PlayerController와 동일한 InputActionAsset 연결
    /// - TargetLayers: Ground + Enemy 레이어 선택 (Raycast 대상)
    /// </summary>
    public class VacuumBombLauncher : MonoBehaviour
    {
        [Header("발사체 설정")]
        [SerializeField] private VacuumBombProjectile projectilePrefab;
        [SerializeField] private int poolSize = 5;
        [SerializeField] private Transform launchPoint;          // null이면 Player 위치 + launchHeightOffset
        [SerializeField] private float launchHeightOffset = 1f;  // launchPoint 없을 때 Y 오프셋

        [Header("조준 설정")]
        [SerializeField] private float maxRange = 30f;           // 최대 사거리
        [SerializeField] private LayerMask targetLayers;         // Raycast 대상 레이어 (Ground + Enemy)

        [Header("쿨다운")]
        [SerializeField] private float cooldown = 2f;

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string fireActionName = "Fire";

        private InputAction _fireAction;
        private ThirdPersonCamera _thirdPersonCamera;
        private UnityEngine.Camera _mainCamera;
        private float _lastFireTime = -999f;

        private readonly List<VacuumBombProjectile> _pool = new List<VacuumBombProjectile>();

        private void Awake()
        {
            if (inputActions != null)
            {
                var map = inputActions.FindActionMap("Player");
                _fireAction = map?.FindAction(fireActionName);
            }

            _thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
            _mainCamera = UnityEngine.Camera.main;

            InitPool();
        }

        private void InitPool()
        {
            if (projectilePrefab == null) return;

            for (int i = 0; i < poolSize; i++)
            {
                VacuumBombProjectile p = Instantiate(projectilePrefab, transform);
                p.gameObject.SetActive(false);
                _pool.Add(p);
            }
        }

        private void OnEnable()  => _fireAction?.Enable();
        private void OnDisable() => _fireAction?.Disable();

        private void Update()
        {
            if (_fireAction == null || !_fireAction.WasPressedThisFrame()) return;
            if (Time.time - _lastFireTime < cooldown) return;

            Fire();
        }

        private void Fire()
        {
            VacuumBombProjectile projectile = GetFromPool();
            if (projectile == null) return;

            Vector3 start  = GetLaunchPosition();
            Vector3 target = GetTargetPosition();

            projectile.transform.position = start;
            projectile.gameObject.SetActive(true);
            projectile.Launch(start, target, gameObject);

            _lastFireTime = Time.time;
        }

        /// <summary>
        /// 비활성 발사체를 반환합니다. 모두 사용 중이면 새로 생성해 풀에 추가합니다.
        /// </summary>
        private VacuumBombProjectile GetFromPool()
        {
            foreach (var p in _pool)
            {
                if (p != null && !p.gameObject.activeSelf)
                    return p;
            }

            // 풀이 부족하면 동적 확장
            VacuumBombProjectile newProjectile = Instantiate(projectilePrefab, transform);
            newProjectile.gameObject.SetActive(false);
            _pool.Add(newProjectile);
            return newProjectile;
        }

        /// <summary>발사 시작 위치.</summary>
        private Vector3 GetLaunchPosition()
        {
            if (launchPoint != null)
                return launchPoint.position;

            return transform.position + Vector3.up * launchHeightOffset;
        }

        /// <summary>
        /// 카메라 중심에서 Raycast해 목표 지점을 구합니다.
        /// 아무것도 맞지 않으면 카메라 정면 maxRange 거리 지점을 사용합니다.
        /// </summary>
        private Vector3 GetTargetPosition()
        {
            Transform camTransform = GetCameraTransform();
            if (camTransform == null)
                return transform.position + transform.forward * maxRange;

            Ray ray = new Ray(camTransform.position, camTransform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, maxRange, targetLayers))
                return hit.point;

            return ray.GetPoint(maxRange);
        }

        private Transform GetCameraTransform()
        {
            if (_thirdPersonCamera != null)
                return _thirdPersonCamera.transform;
            if (_mainCamera != null)
                return _mainCamera.transform;
            return null;
        }
    }
}
