using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace CrowdCombat.Core
{
    /// <summary>
    /// UI 전반을 관리하는 싱글톤 매니저.
    /// 캔버스, 패널, 상호작용 프롬프트 등을 제어합니다.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Canvas")]
        [SerializeField] protected Canvas mainCanvas;
        [SerializeField] protected CanvasGroup canvasGroup;

        [Header("Panels")]
        [SerializeField] protected List<GameObject> panels = new List<GameObject>();

        [Header("Interaction Prompt (Optional)")]
        [SerializeField] protected GameObject interactionPromptRoot;
        [SerializeField] protected Text interactionPromptText;

        protected bool cursorLocked = true;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (mainCanvas != null && canvasGroup == null)
                canvasGroup = mainCanvas.GetComponent<CanvasGroup>();
        }

        /// <summary>패널 표시</summary>
        public virtual void ShowPanel(int index)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] != null)
                    panels[i].SetActive(i == index);
            }
        }

        /// <summary>패널 표시 (이름으로)</summary>
        public virtual void ShowPanel(string panelName)
        {
            foreach (var p in panels)
            {
                if (p != null)
                    p.SetActive(p.name == panelName);
            }
        }

        /// <summary>특정 패널만 켜고 나머지 끔</summary>
        public virtual void ShowPanelOnly(GameObject panel)
        {
            foreach (var p in panels)
            {
                if (p != null)
                    p.SetActive(p == panel);
            }
        }

        /// <summary>모든 패널 숨김</summary>
        public virtual void HideAllPanels()
        {
            foreach (var p in panels)
            {
                if (p != null)
                    p.SetActive(false);
            }
        }

        /// <summary>인터랙션 프롬프트 표시 (예: "E - 상호작용")</summary>
        public virtual void ShowInteractionPrompt(string text)
        {
            if (interactionPromptRoot != null)
                interactionPromptRoot.SetActive(true);
            if (interactionPromptText != null)
                interactionPromptText.text = text;
        }

        /// <summary>인터랙션 프롬프트 숨김</summary>
        public virtual void HideInteractionPrompt()
        {
            if (interactionPromptRoot != null)
                interactionPromptRoot.SetActive(false);
        }

        /// <summary>캔버스 전체 표시/숨김</summary>
        public virtual void SetCanvasVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.blocksRaycasts = visible;
                canvasGroup.interactable = visible;
            }
            if (mainCanvas != null)
                mainCanvas.enabled = visible;
        }

        /// <summary>마우스 커서 잠금/해제 (메뉴 열 때 등)</summary>
        public virtual void SetCursorLock(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
