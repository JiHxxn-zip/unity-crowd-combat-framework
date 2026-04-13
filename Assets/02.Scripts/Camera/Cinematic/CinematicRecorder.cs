using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CrowdCombat.Camera.Cinematic;

namespace CrowdCombat.Camera
{
    /// <summary>
    /// 에디터에서 시네마틱 키프레임을 기록하고 JSON으로 저장합니다.
    /// Inspector의 ContextMenu 버튼으로 프레임을 추가/삭제하고,
    /// Scene 뷰에 카메라 경로를 Gizmo로 표시합니다.
    ///
    /// 사용법:
    ///   1. 카메라 오브젝트에 컴포넌트 추가
    ///   2. Inspector 우클릭 → "Add Frame" 으로 현재 포즈 기록
    ///   3. "Save to JSON" 으로 StreamingAssets/Cinematics/ 에 저장
    /// </summary>
    [ExecuteAlways]
    public class CinematicRecorder : MonoBehaviour
    {
        [Header("클립 설정")]
        [SerializeField] private string clipName       = "new_cinematic";
        [SerializeField] private SpaceMode spaceMode   = SpaceMode.World;
        [SerializeField] private string targetMemo     = "";           // 에디터 메모 전용
        [SerializeField] private Transform localTarget;                // Local 모드 기준 오브젝트

        [Header("프레임 기본값")]
        [SerializeField] private EaseType defaultEaseType = EaseType.EaseInOut;
        [SerializeField] private float timeStep           = 0.5f;     // AddFrame 시 자동 증가 시간
        [SerializeField] private float defaultFov         = 60f;

        [Header("Gizmo")]
        [SerializeField] private Color pathColor        = Color.cyan;
        [SerializeField] private Color frameColor       = Color.yellow;
        [SerializeField] private float frameRadius      = 0.12f;
        [SerializeField] private float forwardLength    = 0.6f;

        [SerializeField] private List<CinematicFrame> frames = new();

        // ── Editor에서 호출하는 public 진입점 ────────────────────────────

        public void AddFrameFromEditor()        => AddFrame();
        public void RemoveLastFrameFromEditor() => RemoveLastFrame();
        public void ClearFramesFromEditor()     => ClearFrames();
        public void SaveToJsonFromEditor()      => SaveToJson();
        public void LoadFromJsonFromEditor()    => LoadFromJson();

        /// <summary>에디터 프리뷰용 클립 빌드</summary>
        public CinematicClip BuildClip() => new CinematicClip
        {
            clipName   = clipName,
            spaceMode  = spaceMode,
            targetName = targetMemo,
            frames     = new List<CinematicFrame>(frames)
        };

        /// <summary>Local 모드 기준 오브젝트 (에디터 프리뷰용)</summary>
        public Transform LocalTarget => localTarget;

        // ── Inspector 버튼 (ContextMenu 유지 — 에디터 없을 때 백업) ──────

        [ContextMenu("Add Frame")]
        private void AddFrame()
        {
            float time = frames.Count > 0 ? frames[^1].time + timeStep : 0f;

            Vector3 pos   = transform.position;
            Quaternion rot = transform.rotation;

            if (spaceMode == SpaceMode.Local && localTarget != null)
            {
                pos = localTarget.InverseTransformPoint(pos);
                rot = Quaternion.Inverse(localTarget.rotation) * rot;
            }

            var frame = new CinematicFrame { time = time, easeType = defaultEaseType, fov = defaultFov };
            frame.SetPosition(pos);
            frame.SetRotation(rot);

            frames.Add(frame);
            Debug.Log($"[CinematicRecorder] Frame {frames.Count} added  t={time:F2}  pos={pos}");
        }

        [ContextMenu("Remove Last Frame")]
        private void RemoveLastFrame()
        {
            if (frames.Count == 0) return;
            frames.RemoveAt(frames.Count - 1);
            Debug.Log("[CinematicRecorder] Last frame removed.");
        }

        [ContextMenu("Clear Frames")]
        private void ClearFrames()
        {
            frames.Clear();
            Debug.Log("[CinematicRecorder] All frames cleared.");
        }

        [ContextMenu("Save to JSON")]
        private void SaveToJson()
        {
            if (frames.Count == 0)
            {
                Debug.LogWarning("[CinematicRecorder] 저장할 프레임이 없습니다.");
                return;
            }

            var clip = new CinematicClip
            {
                clipName   = clipName,
                spaceMode  = spaceMode,
                targetName = targetMemo,
                frames     = new List<CinematicFrame>(frames)
            };

            string dir  = Path.Combine(Application.streamingAssetsPath, "Cinematics");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, clipName + ".json");
            File.WriteAllText(path, clip.ToJson());
            Debug.Log($"[CinematicRecorder] Saved → {path}");
        }

        [ContextMenu("Load from JSON")]
        private void LoadFromJson()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Cinematics", clipName + ".json");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[CinematicRecorder] 파일 없음: {path}");
                return;
            }

            var clip   = CinematicClip.FromJson(File.ReadAllText(path));
            frames     = clip.frames;
            spaceMode  = clip.spaceMode;
            targetMemo = clip.targetName;
            Debug.Log($"[CinematicRecorder] Loaded {frames.Count} frames ← {path}");
        }

        // ── Gizmo (Scene 뷰 경로 표시) ───────────────────────────────────

        private void OnDrawGizmos()
        {
            if (frames == null || frames.Count == 0) return;

            for (int i = 0; i < frames.Count; i++)
            {
                Vector3    pos = ToWorldPos(frames[i]);
                Quaternion rot = ToWorldRot(frames[i]);

                // 프레임 위치 구
                Gizmos.color = frameColor;
                Gizmos.DrawWireSphere(pos, frameRadius);

                // 카메라 forward 방향
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(pos, rot * Vector3.forward * forwardLength);

                // 프레임 번호 표시는 Handles 필요 → Gizmo 구로 대체
                // 이전 프레임과 연결선
                if (i > 0)
                {
                    Gizmos.color = pathColor;
                    Gizmos.DrawLine(ToWorldPos(frames[i - 1]), pos);
                }
            }

            // 첫 프레임 강조 (초록)
            if (frames.Count > 0)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(ToWorldPos(frames[0]), frameRadius * 1.4f);
            }

            // 마지막 프레임 강조 (빨강)
            if (frames.Count > 1)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(ToWorldPos(frames[^1]), frameRadius * 1.4f);
            }
        }

        // ── 내부 유틸 ─────────────────────────────────────────────────────

        private Vector3 ToWorldPos(CinematicFrame frame)
        {
            if (spaceMode == SpaceMode.Local && localTarget != null)
                return localTarget.TransformPoint(frame.Position);
            return frame.Position;
        }

        private Quaternion ToWorldRot(CinematicFrame frame)
        {
            if (spaceMode == SpaceMode.Local && localTarget != null)
                return localTarget.rotation * frame.Rotation;
            return frame.Rotation;
        }
    }
}
