using UnityEngine;

namespace CrowdCombat.Core
{
    /// <summary>
    /// 데미지를 받을 수 있는 객체를 위한 인터페이스.
    /// MonsterController, PlayerController 등에서 구현합니다.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>데미지 적용. instigator는 공격자 GameObject (null 허용).</summary>
        void TakeDamage(float damage, GameObject instigator);

        /// <summary>사망 여부.</summary>
        bool IsDead { get; }
    }
}
