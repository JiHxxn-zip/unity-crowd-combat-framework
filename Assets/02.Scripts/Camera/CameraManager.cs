using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CrowdCombat.Camera.Cinematic;

namespace CrowdCombat.Camera
{
    /// <summary>
    /// 카메라 관련 연출을 총괄하는 매니저.
    ///
    /// TriggerHitEffect(duration, intensity) — 히트스탑 + 카메라 쉐이크
    /// PlayCinematic(jsonFileName, localTarget) — JSON 캐시 로드 + 시네마틱 재생
    /// StopCinematic()                          — 시네마틱 명시적 정지
    ///
    /// JSON 캐시: 한 번 로드한 클립은 Dictionary에 보관해 재로드를 방지합니다.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [SerializeField] private ThirdPersonCamera thirdPersonCamera;
        [SerializeField] private CinematicPlayer cinematicPlayer;

        // ── JSON 캐시 ────────────────────────────────────────────────────
        private readonly Dictionary<string, CinematicClip> _clipCache = new();

        private Coroutine _hitStopCoroutine;

        // ── 생명주기 ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ── 히트 이펙트 ───────────────────────────────────────────────────

        /// <summary>
        /// 히트 연출 트리거. 시네마틱 재생 중이면 건너뜁니다.
        /// </summary>
        /// <param name="duration">히트스탑 지속 시간 (실제 시간 기준)</param>
        /// <param name="intensity">강도 0~1. timeScale 억제량과 쉐이크 크기에 동시 적용</param>
        public void TriggerHitEffect(float duration, float intensity)
        {
            if (cinematicPlayer != null && cinematicPlayer.IsPlaying) return;

            intensity = Mathf.Clamp01(intensity);

            if (_hitStopCoroutine != null)
                StopCoroutine(_hitStopCoroutine);
            _hitStopCoroutine = StartCoroutine(DoHitStop(duration, intensity));

            thirdPersonCamera?.TriggerShake(duration, intensity);
        }

        /// <summary>intensity → timeScale: intensity=0 → 0.15, intensity=1 → 0</summary>
        private IEnumerator DoHitStop(float duration, float intensity)
        {
            Time.timeScale = (1f - intensity) * 0.15f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            _hitStopCoroutine = null;
        }

        // ── 시네마틱 ──────────────────────────────────────────────────────

        /// <summary>
        /// JSON 파일을 로드(또는 캐시 반환)해 시네마틱을 재생합니다.
        /// </summary>
        /// <param name="jsonFileName">StreamingAssets/Cinematics/ 하위 파일명 (확장자 제외)</param>
        /// <param name="localTarget">Local 모드 기준 Transform. null이면 World 모드로 동작</param>
        public void PlayCinematic(string jsonFileName, Transform localTarget = null)
        {
            if (cinematicPlayer == null)
            {
                Debug.LogWarning("[CameraManager] CinematicPlayer가 연결되지 않았습니다.");
                return;
            }

            var clip = LoadClip(jsonFileName);
            if (clip == null) return;

            cinematicPlayer.Play(clip, localTarget);
        }

        /// <summary>현재 재생 중인 시네마틱을 명시적으로 정지합니다.</summary>
        public void StopCinematic()
        {
            cinematicPlayer?.StopCinematic();
        }

        /// <summary>캐시를 모두 비웁니다. 씬 전환 후 등 메모리 해제 시 사용합니다.</summary>
        public void ClearCache()
        {
            _clipCache.Clear();
        }

        // ── 내부: JSON 로드 + 캐시 ──────────────────────────────────────

        private CinematicClip LoadClip(string fileName)
        {
            if (_clipCache.TryGetValue(fileName, out var cached))
                return cached;

            string path = Path.Combine(Application.streamingAssetsPath, "Cinematics", fileName + ".json");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[CameraManager] JSON 파일 없음: {path}");
                return null;
            }

            var clip = CinematicClip.FromJson(File.ReadAllText(path));
            _clipCache[fileName] = clip;
            return clip;
        }
    }
}
