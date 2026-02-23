using UnityEngine;

namespace CrowdCombat.Core
{
    /// <summary>
    /// 핵앤슬래시 프레임워크 - 상호작용 가능한 오브젝트의 추상 베이스 클래스.
    /// 모든 인터랙션 로직은 이 클래스를 상속해 구현합니다.
    /// </summary>
    public abstract class Interaction : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] protected bool canInteract = true;
        [SerializeField] protected string interactionPrompt = "Interact";

        /// <summary>현재 상호작용 가능 여부</summary>
        public virtual bool CanInteract => canInteract;

        /// <summary>UI에 표시할 프롬프트 텍스트</summary>
        public virtual string InteractionPrompt => interactionPrompt;

        /// <summary>
        /// 상호작용 시 호출. 파생 클래스에서 구현.
        /// </summary>
        /// <param name="interactor">상호작용을 시도한 주체 (예: 플레이어)</param>
        public abstract void OnInteract(GameObject interactor);

        /// <summary>
        /// 상호작용 가능 거리/조건 검사 시 사용. 필요 시 오버라이드.
        /// </summary>
        public virtual bool IsInteractionAvailable(GameObject interactor)
        {
            return canInteract;
        }
    }
}
