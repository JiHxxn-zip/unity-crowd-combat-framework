using UnityEngine;
using UnityEngine.Events;

namespace CrowdCombat.Weapon
{
    /// <summary>
    /// 진공 폭탄 발사체.
    /// Launch() 호출 시 시작점 → 목표점까지 수학적 포물선 궤적으로 비행합니다.
    /// 지면 또는 적에 충돌하거나 목표 지점에 도달하면 OnExplode 이벤트를 발생시킵니다.
    ///
    /// [프리팹 설정]
    /// - Rigidbody: IsKinematic = true
    /// - SphereCollider: IsTrigger = true
    /// - CollisionLayers: Ground + Enemy 레이어 선택
    /// - ExplosionPrefab: VacuumBombExplosion 프리팹 연결
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class VacuumBombProjectile : MonoBehaviour
    {
        [Header("비행 설정")]
        [SerializeField] private float arcHeight = 3f;        // 포물선 최고점 높이 (미터)
        [SerializeField] private float flightDuration = 1.5f; // 총 비행 시간 (초)

        [Header("폭발 프리팹")]
        [SerializeField] private VacuumBombExplosion explosionPrefab;

        [Header("충돌 감지 레이어")]
        [SerializeField] private LayerMask collisionLayers; // Ground + Enemy 레이어

        /// <summary>폭발 위치를 Vector3로 전달하는 이벤트.</summary>
        public UnityEvent<Vector3> OnExplode;

        private Vector3 _startPos;
        private Vector3 _targetPos;
        private float _elapsed;
        private bool _isFlying;
        private bool _hasExploded;
        private GameObject _instigator;
        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        /// <summary>
        /// 포물선 비행 시작.
        /// </summary>
        /// <param name="startPos">발사 위치</param>
        /// <param name="targetPos">목표 위치</param>
        /// <param name="instigator">발사자 (데미지 귀속용, null 허용)</param>
        public void Launch(Vector3 startPos, Vector3 targetPos, GameObject instigator = null)
        {
            _startPos     = startPos;
            _targetPos    = targetPos;
            _instigator   = instigator;
            _elapsed      = 0f;
            _isFlying     = true;
            _hasExploded  = false;
            transform.position = startPos;
        }

        private void Update()
        {
            if (!_isFlying) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / flightDuration);

            // ── 포물선 위치 계산 ──────────────────────────────────────────
            // XZ: 시작→목표 선형 보간
            // Y : arcHeight * sin(π*t) → t=0.5 에서 최고점, t=0/1 에서 0
            Vector3 flatPos = Vector3.Lerp(_startPos, _targetPos, t);
            Vector3 nextPos = new Vector3(
                flatPos.x,
                flatPos.y + arcHeight * Mathf.Sin(Mathf.PI * t),
                flatPos.z
            );

            // 이동 방향으로 회전
            Vector3 moveDir = nextPos - transform.position;
            if (moveDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(moveDir.normalized);

            transform.position = nextPos;

            if (t >= 1f)
                TriggerExplosion(nextPos);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasExploded || !_isFlying) return;

            // collisionLayers에 포함된 레이어에 충돌 시 폭발
            if ((collisionLayers.value & (1 << other.gameObject.layer)) != 0)
                TriggerExplosion(transform.position);
        }

        private void TriggerExplosion(Vector3 position)
        {
            if (_hasExploded) return;
            _hasExploded = true;
            _isFlying    = false;

            if (explosionPrefab != null)
            {
                VacuumBombExplosion explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
                explosion.Explode(position, _instigator);
            }

            OnExplode?.Invoke(position);
            gameObject.SetActive(false);
        }

        // 에디터에서 예상 비행 궤적 미리보기
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.yellow;
            const int steps = 20;
            Vector3 prev = _startPos;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 flat = Vector3.Lerp(_startPos, _targetPos, t);
                Vector3 p = new Vector3(flat.x, flat.y + arcHeight * Mathf.Sin(Mathf.PI * t), flat.z);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
