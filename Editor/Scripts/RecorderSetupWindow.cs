using UnityEngine;
using UnityEditor;

public class RecorderSetupWindow : EditorWindow {
    private GameObject recorderPrefab;

    [MenuItem("Tools/Recorder Setup")]
    public static void ShowWindow() {
        var window = GetWindow<RecorderSetupWindow>("Recorder Setup");
        window.minSize = new Vector2(300, 150);
        window.LoadRecorderPrefab();
    }

    private void LoadRecorderPrefab() {
        recorderPrefab = Resources.Load<GameObject>("Prefabs/Recorder");
        if (recorderPrefab == null) {
            Debug.LogError("Failed to load Recorder prefab. Make sure it exists at Resources/Prefabs/Recorder");
        }
    }

    private void OnGUI() {
        EditorGUILayout.Space(10);
        GUILayout.Label("Recorder Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        if (recorderPrefab == null) {
            EditorGUILayout.HelpBox(
                "Recorder prefab not found in Resources/Prefabs/Recorder\n" +
                "Please ensure the prefab exists in the correct location.",
                MessageType.Error);
            return;
        }

        if (GUILayout.Button("Add Recorder to Scene")) {
            AddRecorderToScene();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "The Recorder prefab will be added at the scene origin (0,0,0).",
            MessageType.Info);
    }

    private void AddRecorderToScene() {
        // Check if a recorder already exists in the scene
        var existingRecorder = FindObjectOfType<MovieRecorderManager>(); // Assuming your recorder has this component
        if (existingRecorder != null) {
            bool proceed = EditorUtility.DisplayDialog("Recorder Already Exists",
                "There is already a Recorder in the scene. Do you want to add another one?",
                "Yes", "Cancel");

            if (!proceed) return;
        }

        // Create the prefab instance at origin
        GameObject recorderInstance = (GameObject)PrefabUtility.InstantiatePrefab(recorderPrefab);
        recorderInstance.transform.position = Vector3.zero;

        // Select the newly created recorder
        Selection.activeGameObject = recorderInstance;

        // Frame the recorder in the Scene view
        SceneView.lastActiveSceneView?.FrameSelected();

        // Register the action for undo
        Undo.RegisterCreatedObjectUndo(recorderInstance, "Add Recorder to Scene");
    }
}