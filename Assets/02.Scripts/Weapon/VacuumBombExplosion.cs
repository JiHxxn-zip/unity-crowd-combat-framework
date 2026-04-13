using System.Collections;
using UnityEngine;
using CrowdCombat.Camera;
using CrowdCombat.Core;
using CrowdCombat.Enemy;

namespace CrowdCombat.Weapon
{
    /// <summary>
    /// 진공 폭탄 폭발 처리기.
    /// VacuumBombProjectile이 Instantiate한 뒤 Explode()를 호출합니다.
    ///
    /// 처리 순서:
    ///   1. ApplyShockwave  — OverlapSphereNonAlloc으로 즉각 데미지
    ///   2. VortexRoutine   — vortexDuration 동안 적을 중심으로 흡입
    ///      (중심 가까울수록 강한 힘 → 거리 기반 가중치)
    ///
    /// [GC 최적화]
    ///   - Collider[30] 배열 재사용 (매 탐색마다 new 하지 않음)
    ///   - WaitForSeconds 캐싱 (코루틴 yield 할당 방지)
    /// </summary>
    public class VacuumBombExplosion : MonoBehaviour
    {
        [Header("충격파 설정")]
        [SerializeField] private float explosionRadius  = 8f;   // 감지 반경 (미터)
        [SerializeField] private float shockwaveDamage  = 50f;  // 즉발 데미지

        [Header("스냅 풀 설정")]
        [SerializeField] private float pullDelay     = 0.15f;  // 폭발 후 끌어당기기까지 딜레이 (초)
        [SerializeField] private float pullSpeed     = 22f;    // 중심으로 끌어당기는 수평 속도 (m/s)
        [SerializeField] private float liftSpeed     = 5f;     // 위로 띄우는 수직 속도 (m/s)

        [Header("레이어 설정")]
        [SerializeField] private LayerMask enemyLayer;  // Enemy 레이어만 선택

        // ── GC 방지용 재사용 리소스 ────────────────────────────────────────
        private readonly Collider[] _colliders = new Collider[30];
        private WaitForSeconds _pullDelayWait;

        private void Awake()
        {
            _pullDelayWait = new WaitForSeconds(pullDelay);
        }

        /// <summary>
        /// 폭발 실행. VacuumBombProjectile에서 호출됩니다.
        /// </summary>
        public void Explode(Vector3 center, GameObject instigator = null)
        {
            transform.position = center;
            ApplyShockwave(center, instigator);
            StartCoroutine(SnapPullRoutine(center));
        }

        // ── Phase 1: 충격파 즉각 데미지 ──────────────────────────────────

        private void ApplyShockwave(Vector3 center, GameObject instigator)
        {
            CameraManager.Instance?.TriggerHitEffect(0.08f, 0.6f);

            int hitCount = Physics.OverlapSphereNonAlloc(center, explosionRadius, _colliders, enemyLayer);

            for (int i = 0; i < hitCount; i++)
            {
                if (_colliders[i] == null) continue;

                // IDamageable을 구현한 컴포넌트에 데미지 전달
                if (_colliders[i].TryGetComponent(out IDamageable damageable) && !damageable.IsDead)
                {
                    damageable.TakeDamage(shockwaveDamage, instigator);
                }
            }
        }

        // ── Phase 2: 스냅 풀 ───────────────────────

        /// <summary>
        /// pullDelay 후 범위 내 모든 적의 velocity를 한 번에 덮어써
        /// 중심으로 "퉁" 끌려오는 느낌을 냅니다.
        ///
        /// rb.linearVelocity 직접 할당 → 기존 속도를 무시하고 즉각 이동.
        /// liftRatio로 살짝 위로 뜨게 해 공중에 뜨는 피격감을 강조합니다.
        /// </summary>
        private IEnumerator SnapPullRoutine(Vector3 center)
        {
            // 짧은 딜레이 — "폭발 → 흡입" 사이 찰나의 간격
            yield return _pullDelayWait;

            int hitCount = Physics.OverlapSphereNonAlloc(center, explosionRadius, _colliders, enemyLayer);

            for (int i = 0; i < hitCount; i++)
            {
                if (_colliders[i] == null) continue;
                if (!_colliders[i].TryGetComponent(out Rigidbody rb)) continue;

                Vector3 toCenter = center - _colliders[i].transform.position;
                float distance   = toCenter.magnitude;
                if (distance < 0.1f) continue;

                // 거리 기반 속도 스케일: 멀수록 더 빠르게 끌어야 같이 모임
                float speedScale = Mathf.Lerp(0.6f, 1f, distance / explosionRadius);

                // 수평(끌어당기기)과 수직(띄우기)을 독립적으로 합산
                Vector3 horizontal  = toCenter.normalized * pullSpeed * speedScale;
                Vector3 velocity    = horizontal + Vector3.up * liftSpeed;

                // 중심까지 도달하는 데 걸리는 시간만큼 AI 이동 억제
                float stunDuration  = distance / (pullSpeed * speedScale);

                // MonsterController가 있으면 hitReactionTimer도 함께 설정
                if (_colliders[i].TryGetComponent(out MonsterController monster))
                    monster.ApplyPull(velocity, stunDuration);
                else
                    rb.linearVelocity = velocity;
            }

            // 적들이 중심에 도달할 시간 확보 후 제거
            yield return new WaitForSeconds(0.8f);
            Destroy(gameObject);
        }

        // ── 디버그 Gizmo ──────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            // 반투명 구: 폭발 반경 시각화
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, explosionRadius);

            // 와이어프레임 구: 외곽선
            Gizmos.color = new Color(1f, 0.3f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
