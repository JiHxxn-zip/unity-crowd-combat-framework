using UnityEngine;
using CrowdCombat.Enemy;

namespace CrowdCombat.Player
{
    /// <summary>
    /// 플레이어 범위 공격용 트리거 영역.
    /// 콜라이더를 IsTrigger로 설정한 오브젝트에 붙여 사용합니다.
    /// 몬스터와 충돌 시 몬스터의 색을 변경합니다.
    /// </summary>
    public class PlayerAttackArea : MonoBehaviour
    {
        [SerializeField] protected float lifeTime = 0.2f;
        [SerializeField] protected Color hitColor = Color.red;
        [SerializeField] protected float knockUpVelocity = 8f;
        [SerializeField] protected float comboSizeStep = 0.2f; // 타수마다 XZ로 커지는 비율

        protected Transform attacker;
        protected int comboIndex = 1;
        protected Vector3 originalScale;

        protected virtual void Awake()
        {
            originalScale = transform.localScale;
        }

        protected virtual void Start()
        {
            if (lifeTime > 0f)
            {
                Destroy(gameObject, lifeTime);
            }
        }

        public void Initialize(Transform attacker, int comboIndex)
        {
            this.attacker = attacker;
            this.comboIndex = comboIndex;

             // 콤보 수에 따라 XZ 크기 확대 (1타는 기본 크기)
            float multiplier = 1f + Mathf.Max(0, comboIndex - 1) * comboSizeStep;
            transform.localScale = new Vector3(
                originalScale.x * multiplier,
                originalScale.y,
                originalScale.z * multiplier
            );
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            MonsterController monster = other.GetComponent<MonsterController>();
            if (monster == null)
            {
                monster = other.GetComponentInParent<MonsterController>();
            }

            if (monster == null)
                return;

            Renderer renderer = monster.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = hitColor;
            }

            // 플레이어 기준으로 몬스터를 뒤로 밀어내는 방향 계산 (XZ 평면)
            Vector3 fromPosition = attacker != null ? attacker.position : transform.position;
            Vector3 dir = monster.transform.position - fromPosition;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = transform.forward;
            }

            dir.Normalize();

            // 1~3타: 살짝씩 뒤로 밀기, 4타: 포물선으로 멀리 날리기
            if (comboIndex >= 4)
            {
                monster.ApplyLaunchHit(dir);
            }
            else
            {
                monster.ApplyLightHit(dir);
            }
        }
    }
}

