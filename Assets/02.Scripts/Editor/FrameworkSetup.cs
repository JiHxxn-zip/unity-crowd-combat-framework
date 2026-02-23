#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrowdCombat.Player;
using CrowdCombat.Camera;
using CrowdCombat.Enemy;

namespace CrowdCombat.Editor
{
    /// <summary>
    /// 메뉴에서 플레이어 프리팹 생성 및 씬 셋업을 돕는 유틸리티.
    /// </summary>
    public static class FrameworkSetup
    {
        private const string PrefabPath = "Assets/03.Prefabs/Player.prefab";
        private const string MonsterPrefabPath = "Assets/03.Prefabs/Monster.prefab";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Crowd Combat Framework/Create Player Prefab")]
        public static void CreatePlayerPrefab()
        {
            var go = new GameObject("Player");
            go.AddComponent<PlayerController>();
            var rb = go.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.5f;
            col.center = new Vector3(0f, 1f, 0f);

            // 시각용 자식 (캡슐)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform);
            visual.transform.localPosition = new Vector3(0f, 1f, 0f);
            visual.transform.localScale = Vector3.one;
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());

            var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
            var controller = go.GetComponent<PlayerController>();
            var so = new SerializedObject(controller);
            so.FindProperty("inputActions").objectReferenceValue = inputAsset;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!AssetDatabase.IsValidFolder("Assets/03.Prefabs"))
                AssetDatabase.CreateFolder("Assets/03.Prefabs", "Prefabs");
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.Refresh();
            Debug.Log($"Player prefab saved to {PrefabPath}");
        }

        [MenuItem("Crowd Combat Framework/Setup Scene (Player + Third Person Camera)")]
        public static void SetupScene()
        {
            var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
            if (inputAsset == null)
            {
                Debug.LogWarning("InputSystem_Actions.inputactions not found at " + InputActionsPath);
            }

            // 플레이어
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab != null)
                    player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                else
                {
                    player = new GameObject("Player");
                    player.tag = "Player";
                    player.AddComponent<PlayerController>();
                    var rb = player.AddComponent<Rigidbody>();
                    rb.constraints = RigidbodyConstraints.FreezeRotation;
                    player.AddComponent<CapsuleCollider>();
                }
            }
            player.transform.position = new Vector3(0f, 0f, 0f);

            var playerController = player.GetComponent<PlayerController>();
            if (playerController != null && inputAsset != null)
            {
                var so = new SerializedObject(playerController);
                so.FindProperty("inputActions").objectReferenceValue = inputAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 메인 카메라에 3인칭 카메라
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                var tpc = cam.GetComponent<ThirdPersonCamera>();
                if (tpc == null)
                    tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
                var so = new SerializedObject(tpc);
                so.FindProperty("target").objectReferenceValue = player.transform;
                so.FindProperty("inputActions").objectReferenceValue = inputAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.AddComponent<UnityEngine.Camera>();
                camGo.AddComponent<AudioListener>();
                var tpc = camGo.AddComponent<ThirdPersonCamera>();
                var so = new SerializedObject(tpc);
                so.FindProperty("target").objectReferenceValue = player.transform;
                so.FindProperty("inputActions").objectReferenceValue = inputAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Scene setup: Player and Third Person Camera configured.");
        }

        [MenuItem("Crowd Combat Framework/Create Monster Prefab")]
        public static void CreateMonsterPrefab()
        {
            var go = new GameObject("Monster");
            go.AddComponent<MonsterController>();
            var rb = go.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.5f;
            col.center = new Vector3(0f, 1f, 0f);

            // 시각용 자식 (캡슐)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform);
            visual.transform.localPosition = new Vector3(0f, 1f, 0f);
            visual.transform.localScale = Vector3.one;
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());

            var controller = go.GetComponent<MonsterController>();
            var so = new SerializedObject(controller);
            so.FindProperty("groundLayer").intValue = LayerMask.GetMask("Ground");
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!AssetDatabase.IsValidFolder("Assets/03.Prefabs"))
                AssetDatabase.CreateFolder("Assets/03.Prefabs", "Prefabs");
            PrefabUtility.SaveAsPrefabAsset(go, MonsterPrefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.Refresh();
            Debug.Log($"Monster prefab saved to {MonsterPrefabPath}");
        }
    }
}
#endif
