using System.Collections.Generic;
using UnityEngine;

namespace CrowdCombat.Enemy
{
    /// <summary>
    /// 몬스터 풀링 매니저. 200마리 풀링 및 Ground 레이어 기준 스폰.
    /// </summary>
    public class MonsterPool : MonoBehaviour
    {
        [Header("Monster Pool")]
        [SerializeField] protected GameObject monsterPrefab;
        [SerializeField] protected int poolSize = 200;

        [Header("Spawn Area")]
        [SerializeField] protected Vector3 center = Vector3.zero;
        [SerializeField] protected Vector3 halfExtents = new Vector3(20f, 0f, 20f); // XZ 범위
        [SerializeField] protected float spawnHeight = 10f;
        [SerializeField] protected LayerMask groundLayer = 1; // Default layer (Ground)

        protected List<GameObject> pool = new List<GameObject>();

        protected virtual void Start()
        {
            if (monsterPrefab == null)
            {
                Debug.LogWarning($"[MonsterPool] monsterPrefab이 설정되지 않았습니다. {gameObject.name}");
                return;
            }

            InitializePool();
        }

        /// <summary>
        /// 풀 초기화 및 몬스터 스폰
        /// </summary>
        protected virtual void InitializePool()
        {
            pool.Clear();

            for (int i = 0; i < poolSize; i++)
            {
                GameObject monster = Instantiate(monsterPrefab, transform);
                PositionOnGround(monster.transform);
                monster.SetActive(true);
                pool.Add(monster);
            }

            Debug.Log($"[MonsterPool] {poolSize}마리 몬스터 스폰 완료.");
        }

        /// <summary>
        /// Ground 레이어 위에 위치시키기
        /// </summary>
        protected virtual void PositionOnGround(Transform monsterTransform)
        {
            // 스폰 영역 내 랜덤 XZ 위치
            float x = Random.Range(-halfExtents.x, halfExtents.x);
            float z = Random.Range(-halfExtents.z, halfExtents.z);
            Vector3 spawnStart = center + new Vector3(x, spawnHeight, z);

            // 아래로 레이캐스트하여 Ground 찾기
            if (Physics.Raycast(spawnStart, Vector3.down, out RaycastHit hit, spawnHeight * 2f, groundLayer))
            {
                monsterTransform.position = hit.point + Vector3.up * 0.1f;
            }
            else
            {
                // Ground를 못 찾으면 기본 위치에 배치
                monsterTransform.position = center + new Vector3(x, 1f, z);
                Debug.LogWarning($"[MonsterPool] Ground 레이어를 찾지 못했습니다. 기본 위치에 배치: {monsterTransform.position}");
            }
        }

        /// <summary>
        /// 모든 몬스터 비활성화 (나중에 재사용용)
        /// </summary>
        public virtual void DeactivateAll()
        {
            foreach (var monster in pool)
            {
                if (monster != null)
                    monster.SetActive(false);
            }
        }

        /// <summary>
        /// 모든 몬스터 재활성화
        /// </summary>
        public virtual void ReactivateAll()
        {
            foreach (var monster in pool)
            {
                if (monster != null)
                {
                    PositionOnGround(monster.transform);
                    monster.SetActive(true);
                }
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            // 스폰 영역 표시
            Gizmos.color = Color.cyan;
            Vector3 size = new Vector3(halfExtents.x * 2f, 0.1f, halfExtents.z * 2f);
            Gizmos.DrawWireCube(center, size);
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, center + Vector3.up * spawnHeight);
        }
    }
}
