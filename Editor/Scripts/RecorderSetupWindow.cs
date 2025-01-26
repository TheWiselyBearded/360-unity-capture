using UnityEngine;
using UnityEditor;

public class RecorderSetupWindow : EditorWindow
{
    [MenuItem("Tools/Recorder Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<RecorderSetupWindow>("Recorder Setup");
        window.minSize = new Vector2(300, 200);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        GUILayout.Label("Recorder Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        DrawButtons();
        DrawInfo();
    }

    private void DrawButtons()
    {
        if (GUILayout.Button("Add Simple Recorder"))
        {
            CreateRecorder("Recorder", MovieRecorderManager.RecorderType.MovieRecorder);
        }

        if (GUILayout.Button("Add 360° Recorder"))
        {
            CreateRecorder("360Recorder", MovieRecorderManager.RecorderType.MovieRecorder360);
        }

        if (GUILayout.Button("Add 360° Stereo Recorder"))
        {
            CreateRecorder("360StereoRecorder", MovieRecorderManager.RecorderType.MovieRecorder360Stereo);
        }
    }

    private void DrawInfo()
    {
        EditorGUILayout.Space(10);
        var existingRecorder = FindObjectOfType<MovieRecorderManager>();
        if (existingRecorder != null)
        {
            EditorGUILayout.HelpBox(
                $"Active recorder in scene: {existingRecorder.gameObject.name}\n" +
                $"Type: {existingRecorder.SelectedRecorderType}",
                MessageType.Info);
        }
    }

    private void CreateRecorder(string objectName, MovieRecorderManager.RecorderType type)
    {
        // Check if a recorder already exists in the scene
        var existingRecorder = FindObjectOfType<MovieRecorderManager>();
        if (existingRecorder != null)
        {
            bool proceed = EditorUtility.DisplayDialog("Recorder Already Exists",
                "There is already a Recorder in the scene. Do you want to add another one?",
                "Yes", "Cancel");
            if (!proceed) return;
        }

        // Create a new empty GameObject
        GameObject recorderObject = new GameObject(objectName);

        // Add the MovieRecorderManager component
        var manager = recorderObject.AddComponent<MovieRecorderManager>();
        manager.SelectedRecorderType = type;
        manager.AutoRecordOnPlay = false;


        // Set the new GameObject as the active selection in the Editor
        Selection.activeGameObject = recorderObject;
        SceneView.lastActiveSceneView?.FrameSelected();

        // Register the Undo operation
        Undo.RegisterCreatedObjectUndo(recorderObject, $"Create {objectName}");
    }
}
