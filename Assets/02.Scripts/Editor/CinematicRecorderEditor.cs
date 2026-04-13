#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CrowdCombat.Camera;
using CrowdCombat.Camera.Cinematic;

namespace CrowdCombat.Editor
{
    [CustomEditor(typeof(CinematicRecorder))]
    [CanEditMultipleObjects]
    public class CinematicRecorderEditor : UnityEditor.Editor
    {
        // ── SerializedProperty ───────────────────────────────────────────
        private SerializedProperty _clipName;
        private SerializedProperty _spaceMode;
        private SerializedProperty _targetMemo;
        private SerializedProperty _localTarget;
        private SerializedProperty _defaultEaseType;
        private SerializedProperty _timeStep;
        private SerializedProperty _defaultFov;
        private SerializedProperty _pathColor;
        private SerializedProperty _frameColor;
        private SerializedProperty _frameRadius;
        private SerializedProperty _forwardLength;
        private SerializedProperty _frames;

        // ── 프리뷰 상태 ──────────────────────────────────────────────────
        private bool _isPreviewing;
        private double _previewStartTime;
        private CinematicClip _previewClip;
        private Transform _previewTransform;
        private UnityEngine.Camera _previewCamera;

        // 프리뷰 종료 후 복원용 원본 포즈
        private Vector3 _savedPos;
        private Quaternion _savedRot;
        private float _savedFov;

        private Vector2 _scrollPos;
        private bool _showFrameList = true;

        // ── 생명주기 ──────────────────────────────────────────────────────

        private void OnEnable()
        {
            _clipName        = serializedObject.FindProperty("clipName");
            _spaceMode       = serializedObject.FindProperty("spaceMode");
            _targetMemo      = serializedObject.FindProperty("targetMemo");
            _localTarget     = serializedObject.FindProperty("localTarget");
            _defaultEaseType = serializedObject.FindProperty("defaultEaseType");
            _timeStep        = serializedObject.FindProperty("timeStep");
            _defaultFov      = serializedObject.FindProperty("defaultFov");
            _pathColor       = serializedObject.FindProperty("pathColor");
            _frameColor      = serializedObject.FindProperty("frameColor");
            _frameRadius     = serializedObject.FindProperty("frameRadius");
            _forwardLength   = serializedObject.FindProperty("forwardLength");
            _frames          = serializedObject.FindProperty("frames");
        }

        private void OnDisable()
        {
            StopPreview(restore: true);
        }

        // ── Inspector GUI ────────────────────────────────────────────────

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var recorder = (CinematicRecorder)target;

            // 클립 설정
            EditorGUILayout.LabelField("클립 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_clipName,   new GUIContent("Clip Name"));
            EditorGUILayout.PropertyField(_spaceMode,  new GUIContent("Space Mode"));
            EditorGUILayout.PropertyField(_targetMemo, new GUIContent("Target Memo (에디터 전용)"));

            if ((SpaceMode)_spaceMode.enumValueIndex == SpaceMode.Local)
                EditorGUILayout.PropertyField(_localTarget, new GUIContent("Local Target"));

            EditorGUILayout.Space(4);

            // 프레임 기본값
            EditorGUILayout.LabelField("프레임 기본값", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_defaultEaseType, new GUIContent("Default Ease"));
            EditorGUILayout.PropertyField(_timeStep,        new GUIContent("Time Step"));
            EditorGUILayout.PropertyField(_defaultFov,      new GUIContent("Default FOV"));

            EditorGUILayout.Space(4);

            // Gizmo
            EditorGUILayout.LabelField("Gizmo", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_pathColor);
            EditorGUILayout.PropertyField(_frameColor);
            EditorGUILayout.PropertyField(_frameRadius);
            EditorGUILayout.PropertyField(_forwardLength);

            EditorGUILayout.Space(8);

            // ── 녹화 버튼 ────────────────────────────────────────────────
            EditorGUILayout.LabelField("녹화", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Add Frame", GUILayout.Height(32)))
                {
                    Undo.RecordObject(recorder, "Add Cinematic Frame");
                    recorder.AddFrameFromEditor();
                    EditorUtility.SetDirty(recorder);
                }

                GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
                if (GUILayout.Button("Remove Last", GUILayout.Height(32)))
                {
                    Undo.RecordObject(recorder, "Remove Last Frame");
                    recorder.RemoveLastFrameFromEditor();
                    EditorUtility.SetDirty(recorder);
                }

                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                if (GUILayout.Button("Clear All", GUILayout.Height(32)))
                {
                    if (EditorUtility.DisplayDialog("Clear Frames", "모든 프레임을 삭제할까요?", "삭제", "취소"))
                    {
                        Undo.RecordObject(recorder, "Clear Cinematic Frames");
                        recorder.ClearFramesFromEditor();
                        EditorUtility.SetDirty(recorder);
                    }
                }

                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("Save JSON", GUILayout.Height(28)))
                    recorder.SaveToJsonFromEditor();

                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("Load JSON", GUILayout.Height(28)))
                {
                    Undo.RecordObject(recorder, "Load Cinematic JSON");
                    recorder.LoadFromJsonFromEditor();
                    EditorUtility.SetDirty(recorder);
                }

                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(8);

            // ── 프리뷰 버튼 ───────────────────────────────────────────────
            EditorGUILayout.LabelField("프리뷰", EditorStyles.boldLabel);

            if (_isPreviewing)
            {
                // 재생 중 진행 바
                float duration = _previewClip != null ? _previewClip.Duration : 1f;
                float elapsed  = Mathf.Clamp((float)(EditorApplication.timeSinceStartup - _previewStartTime), 0f, duration);
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(18)),
                    elapsed / duration, $"{elapsed:F2}s / {duration:F2}s");

                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("Stop Preview", GUILayout.Height(30)))
                    StopPreview(restore: true);
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
                using (new EditorGUI.DisabledScope(_frames.arraySize < 2))
                {
                    if (GUILayout.Button("Play Preview", GUILayout.Height(30)))
                        StartPreview(recorder);
                }
                if (_frames.arraySize < 2)
                    EditorGUILayout.HelpBox("프리뷰하려면 프레임이 2개 이상 필요합니다.", MessageType.Info);
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(8);

            // ── 프레임 목록 ───────────────────────────────────────────────
            int count    = _frames.arraySize;
            float dur    = count > 0
                ? _frames.GetArrayElementAtIndex(count - 1).FindPropertyRelative("time").floatValue
                : 0f;

            _showFrameList = EditorGUILayout.Foldout(
                _showFrameList,
                $"프레임 목록  [{count}개 / {dur:F2}s]",
                true, EditorStyles.foldoutHeader);

            if (_showFrameList && count > 0)
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(200));

                for (int i = 0; i < count; i++)
                {
                    var fp   = _frames.GetArrayElementAtIndex(i);
                    float t  = fp.FindPropertyRelative("time").floatValue;
                    float px = fp.FindPropertyRelative("px").floatValue;
                    float py = fp.FindPropertyRelative("py").floatValue;
                    float pz = fp.FindPropertyRelative("pz").floatValue;
                    float fov = fp.FindPropertyRelative("fov").floatValue;
                    var easeProp = fp.FindPropertyRelative("easeType");

                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField(
                            $"#{i}  t={t:F2}s  ({px:F1}, {py:F1}, {pz:F1})  fov={fov:F0}",
                            GUILayout.ExpandWidth(true));

                        easeProp.enumValueIndex = (int)(EaseType)EditorGUILayout.EnumPopup(
                            (EaseType)easeProp.enumValueIndex, GUILayout.Width(90));

                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("X", GUILayout.Width(22)))
                        {
                            Undo.RecordObject(recorder, "Remove Frame");
                            _frames.DeleteArrayElementAtIndex(i);
                            EditorUtility.SetDirty(recorder);
                            break;
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }

                EditorGUILayout.EndScrollView();
            }

            serializedObject.ApplyModifiedProperties();

            // 프리뷰 중 Inspector 갱신 (진행 바 업데이트용)
            if (_isPreviewing)
                Repaint();
        }

        // ── 프리뷰 로직 ──────────────────────────────────────────────────

        private void StartPreview(CinematicRecorder recorder)
        {
            _previewClip      = recorder.BuildClip();
            _previewTransform = recorder.transform;
            _previewCamera    = recorder.GetComponent<UnityEngine.Camera>();

            // 원본 포즈 저장 (종료 시 복원)
            _savedPos = _previewTransform.position;
            _savedRot = _previewTransform.rotation;
            _savedFov = _previewCamera != null ? _previewCamera.fieldOfView : 60f;

            _previewStartTime = EditorApplication.timeSinceStartup;
            _isPreviewing     = true;

            EditorApplication.update += OnPreviewUpdate;
        }

        private void StopPreview(bool restore)
        {
            if (!_isPreviewing) return;

            EditorApplication.update -= OnPreviewUpdate;
            _isPreviewing = false;

            if (restore && _previewTransform != null)
            {
                _previewTransform.position = _savedPos;
                _previewTransform.rotation = _savedRot;
                if (_previewCamera != null)
                    _previewCamera.fieldOfView = _savedFov;
            }

            SceneView.RepaintAll();
        }

        private void OnPreviewUpdate()
        {
            if (_previewClip == null || _previewTransform == null)
            {
                StopPreview(restore: true);
                return;
            }

            float elapsed  = (float)(EditorApplication.timeSinceStartup - _previewStartTime);
            float duration = _previewClip.Duration;

            if (elapsed >= duration)
            {
                // 마지막 프레임 정확히 적용 후 복원
                var last = _previewClip.frames[^1];
                ApplyFrame(WorldPos(last), WorldRot(last), last.fov);
                StopPreview(restore: true);
                return;
            }

            // 현재 구간 탐색 및 보간
            var frames = _previewClip.frames;
            for (int i = 0; i < frames.Count - 1; i++)
            {
                if (elapsed >= frames[i].time && elapsed < frames[i + 1].time)
                {
                    float seg  = frames[i + 1].time - frames[i].time;
                    float t    = Mathf.Clamp01((elapsed - frames[i].time) / seg);
                    float et   = Ease(t, frames[i].easeType);

                    ApplyFrame(
                        Vector3.Lerp(WorldPos(frames[i]), WorldPos(frames[i + 1]), et),
                        Quaternion.Slerp(WorldRot(frames[i]), WorldRot(frames[i + 1]), et),
                        Mathf.Lerp(frames[i].fov, frames[i + 1].fov, et));
                    break;
                }
            }

            SceneView.RepaintAll();
        }

        private void ApplyFrame(Vector3 pos, Quaternion rot, float fov)
        {
            _previewTransform.position = pos;
            _previewTransform.rotation = rot;
            if (_previewCamera != null)
                _previewCamera.fieldOfView = fov;
        }

        // ── 유틸 ─────────────────────────────────────────────────────────

        private Vector3 WorldPos(CinematicFrame frame)
        {
            var recorder = (CinematicRecorder)target;
            if (_previewClip.spaceMode == SpaceMode.Local && recorder.LocalTarget != null)
                return recorder.LocalTarget.TransformPoint(frame.Position);
            return frame.Position;
        }

        private Quaternion WorldRot(CinematicFrame frame)
        {
            var recorder = (CinematicRecorder)target;
            if (_previewClip.spaceMode == SpaceMode.Local && recorder.LocalTarget != null)
                return recorder.LocalTarget.rotation * frame.Rotation;
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
#endif
