using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using CrowdCombat.Camera.Cinematic;

namespace CrowdCombat.Camera
{
    /// <summary>
    /// 시네마틱 클립을 재생합니다.
    ///
    /// 재생 흐름:
    ///   [블렌드 인]  현재 카메라 포즈 → 첫 프레임  (blendInDuration)
    ///   [재생]       키프레임 간 이징 보간
    ///   [블렌드 아웃] 마지막 프레임 → ThirdPersonCamera 포즈  (blendOutDuration)
    ///
    /// 안전장치:
    ///   - StopCinematic() : 명시적 정지 (블렌드 아웃 포함)
    ///   - OnDisable / OnDestroy : 즉시 강제 복구 (코루틴 실행 불가)
    ///   - SceneManager.sceneUnloaded : 씬 전환 시 즉시 강제 복구
    /// </summary>
    public class CinematicPlayer : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private ThirdPersonCamera thirdPersonCamera;

        [Header("블렌드")]
        [SerializeField] private float blendInDuration  = 0.25f;
        [SerializeField] private float blendOutDuration = 0.25f;

        [Header("시간 제어")]
        [SerializeField] private bool pauseTimeOnPlay = false;  // true면 재생 중 Time.timeScale = 0

        private UnityEngine.Camera _camera;
        private float _defaultFov = 60f;
        private Coroutine _playbackCoroutine;

        public bool IsPlaying { get; private set; }

        // ── 생명주기 ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (thirdPersonCamera != null)
                _camera = thirdPersonCamera.GetComponent<UnityEngine.Camera>();

            if (_camera != null)
                _defaultFov = _camera.fieldOfView;
        }

        private void OnEnable()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            ForceStop(); // OnDisable 시 코루틴 자동 중단 → 즉시 복구
        }

        private void OnDestroy()
        {
            ForceStop();
        }

        private void OnSceneUnloaded(Scene scene) => ForceStop();

        // ── 공개 API ──────────────────────────────────────────────────────

        public void Play(CinematicClip clip, Transform localTarget = null)
        {
            if (clip == null || clip.frames == null || clip.frames.Count == 0)
            {
                Debug.LogWarning("[CinematicPlayer] 재생할 프레임이 없습니다.");
                return;
            }

            if (_playbackCoroutine != null)
                StopCoroutine(_playbackCoroutine);

            _playbackCoroutine = StartCoroutine(PlayRoutine(clip, localTarget));
        }

        /// <summary>
        /// 명시적 정지. 블렌드 아웃 후 ThirdPersonCamera를 복구합니다.
        /// </summary>
        public void StopCinematic()
        {
            if (!IsPlaying) return;

            if (_playbackCoroutine != null)
            {
                StopCoroutine(_playbackCoroutine);
                _playbackCoroutine = null;
            }

            _playbackCoroutine = StartCoroutine(BlendOutAndStop());
        }

        // ── 내부: 즉시 강제 복구 (블렌드 없음) ──────────────────────────

        private void ForceStop()
        {
            _playbackCoroutine = null;

            if (thirdPersonCamera != null)
                thirdPersonCamera.IsCinematicOverride = false;

            if (_camera != null)
                _camera.fieldOfView = _defaultFov;

            if (pauseTimeOnPlay)
                Time.timeScale = 1f;

            IsPlaying = false;
        }

        // ── 메인 재생 루틴 ───────────────────────────────────────────────

        private IEnumerator PlayRoutine(CinematicClip clip, Transform localTarget)
        {
            IsPlaying = true;

            if (pauseTimeOnPlay)
                Time.timeScale = 0f;

            // IsCinematicOverride = true → ThirdPersonCamera LateUpdate 정지
            if (thirdPersonCamera != null)
                thirdPersonCamera.IsCinematicOverride = true;

            // 블렌드 인: 현재 포즈 → 첫 번째 프레임
            var firstFrame = clip.frames[0];
            yield return StartCoroutine(BlendToFrame(
                WorldPos(firstFrame, clip.spaceMode, localTarget),
                WorldRot(firstFrame, clip.spaceMode, localTarget),
                firstFrame.fov,
                blendInDuration));

            // 키프레임 순서 재생
            yield return StartCoroutine(PlayFrames(clip, localTarget));

            // 블렌드 아웃: 마지막 프레임 → ThirdPersonCamera 포즈
            yield return StartCoroutine(BlendOutAndStop());
        }

        // ── 키프레임 구간 보간 ───────────────────────────────────────────

        private IEnumerator PlayFrames(CinematicClip clip, Transform localTarget)
        {
            var frames = clip.frames;

            for (int i = 0; i < frames.Count - 1; i++)
            {
                var from = frames[i];
                var to   = frames[i + 1];

                float segDuration = to.time - from.time;
                if (segDuration <= 0f) continue;

                float elapsed = 0f;

                while (elapsed < segDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float easedT = Ease(Mathf.Clamp01(elapsed / segDuration), from.easeType);

                    SetCameraPose(
                        Vector3.Lerp(   WorldPos(from, clip.spaceMode, localTarget),
                                        WorldPos(to,   clip.spaceMode, localTarget), easedT),
                        Quaternion.Slerp(WorldRot(from, clip.spaceMode, localTarget),
                                         WorldRot(to,   clip.spaceMode, localTarget), easedT),
                        Mathf.Lerp(from.fov, to.fov, easedT));

                    yield return null;
                }

                // 구간 끝 정확히 맞추기
                SetCameraPose(
                    WorldPos(to, clip.spaceMode, localTarget),
                    WorldRot(to, clip.spaceMode, localTarget),
                    to.fov);
            }
        }

        // ── 블렌드 인 ────────────────────────────────────────────────────

        private IEnumerator BlendToFrame(Vector3 targetPos, Quaternion targetRot,
                                         float targetFov, float duration)
        {
            if (duration <= 0f)
            {
                SetCameraPose(targetPos, targetRot, targetFov);
                yield break;
            }

            Vector3    startPos = thirdPersonCamera != null
                                    ? thirdPersonCamera.transform.position
                                    : targetPos;
            Quaternion startRot = thirdPersonCamera != null
                                    ? thirdPersonCamera.transform.rotation
                                    : targetRot;
            float startFov = _camera != null ? _camera.fieldOfView : targetFov;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Ease(Mathf.Clamp01(elapsed / duration), EaseType.EaseInOut);
                SetCameraPose(
                    Vector3.Lerp(startPos, targetPos, t),
                    Quaternion.Slerp(startRot, targetRot, t),
                    Mathf.Lerp(startFov, targetFov, t));
                yield return null;
            }
        }

        // ── 블렌드 아웃 + 정리 ───────────────────────────────────────────

        private IEnumerator BlendOutAndStop()
        {
            if (thirdPersonCamera == null || blendOutDuration <= 0f)
            {
                ForceStop();
                yield break;
            }

            Vector3    startPos = thirdPersonCamera.transform.position;
            Quaternion startRot = thirdPersonCamera.transform.rotation;
            float startFov = _camera != null ? _camera.fieldOfView : _defaultFov;

            float elapsed = 0f;
            while (elapsed < blendOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Ease(Mathf.Clamp01(elapsed / blendOutDuration), EaseType.EaseInOut);

                // ThirdPersonCamera의 현재 목표 포즈로 매 프레임 갱신
                thirdPersonCamera.CalculateDesiredPose(out Vector3 targetPos, out Quaternion targetRot);

                SetCameraPose(
                    Vector3.Lerp(startPos, targetPos, t),
                    Quaternion.Slerp(startRot, targetRot, t),
                    Mathf.Lerp(startFov, _defaultFov, t));

                yield return null;
            }

            ForceStop();
        }

        // ── 유틸 ─────────────────────────────────────────────────────────

        private void SetCameraPose(Vector3 pos, Quaternion rot, float fov)
        {
            if (thirdPersonCamera != null)
            {
                thirdPersonCamera.transform.position = pos;
                thirdPersonCamera.transform.rotation = rot;
            }
            if (_camera != null)
                _camera.fieldOfView = fov;
        }

        private static Vector3 WorldPos(CinematicFrame frame, SpaceMode mode, Transform target)
        {
            if (mode == SpaceMode.Local && target != null)
                return target.TransformPoint(frame.Position);
            return frame.Position;
        }

        private static Quaternion WorldRot(CinematicFrame frame, SpaceMode mode, Transform target)
        {
            if (mode == SpaceMode.Local && target != null)
                return target.rotation * frame.Rotation;
            return frame.Rotation;
        }

        private static float Ease(float t, EaseType type) => type switch
        {
            EaseType.EaseIn    => t * t,
            EaseType.EaseOut   => 1f - (1f - t) * (1f - t),
            EaseType.EaseInOut => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f,
            _                  => t
        };
    }
}
