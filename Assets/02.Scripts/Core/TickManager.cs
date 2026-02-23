using System.Collections.Generic;
using UnityEngine;

namespace CrowdCombat.Core
{
    /// <summary>
    /// Tick 시스템 매니저. ITickable 객체들을 배치 단위로 분산 호출하여 성능을 최적화합니다.
    /// 한 프레임에 batchSize만큼만 처리하고, tickInterval 프레임마다 실행합니다.
    /// </summary>
    public class TickManager : MonoBehaviour
    {
        private static TickManager _instance;
        public static TickManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("TickManager");
                    _instance = go.AddComponent<TickManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Tick Settings")]
        [SerializeField] private int batchSize = 20; // 한 프레임에 처리할 객체 수
        [SerializeField] private int tickInterval = 10; // 몇 프레임마다 실행할지

        private List<ITickable> tickables = new List<ITickable>();
        private int currentIndex = 0; // 현재 처리할 시작 인덱스
        private int frameCounter = 0; // 프레임 카운터

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            frameCounter++;

            // tickInterval 프레임마다 실행
            if (frameCounter % tickInterval != 0)
                return;

            // 활성화된 객체만 필터링
            List<ITickable> activeTickables = new List<ITickable>();
            foreach (var tickable in tickables)
            {
                if (tickable != null)
                {
                    MonoBehaviour mb = tickable as MonoBehaviour;
                    if (mb != null && mb.gameObject.activeInHierarchy)
                    {
                        activeTickables.Add(tickable);
                    }
                }
            }

            if (activeTickables.Count == 0)
                return;

            // 현재 인덱스부터 batchSize만큼 처리
            int processed = 0;
            int startIndex = currentIndex;

            for (int i = 0; i < batchSize && processed < activeTickables.Count; i++)
            {
                int index = (startIndex + i) % activeTickables.Count;
                try
                {
                    activeTickables[index].Tick();
                    processed++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[TickManager] Tick 실행 중 오류 발생: {e.Message}");
                }
            }

            // 다음 프레임을 위해 인덱스 업데이트 (순환)
            currentIndex = (startIndex + processed) % activeTickables.Count;
        }

        /// <summary>
        /// ITickable 객체를 등록합니다.
        /// </summary>
        public void Register(ITickable tickable)
        {
            if (tickable != null && !tickables.Contains(tickable))
            {
                tickables.Add(tickable);
            }
        }

        /// <summary>
        /// ITickable 객체를 등록 해제합니다.
        /// </summary>
        public void Unregister(ITickable tickable)
        {
            tickables.Remove(tickable);
        }

        /// <summary>
        /// 등록된 모든 객체를 제거합니다.
        /// </summary>
        public void Clear()
        {
            tickables.Clear();
            currentIndex = 0;
        }

        /// <summary>
        /// 현재 등록된 객체 수를 반환합니다.
        /// </summary>
        public int GetRegisteredCount()
        {
            return tickables.Count;
        }
    }
}
