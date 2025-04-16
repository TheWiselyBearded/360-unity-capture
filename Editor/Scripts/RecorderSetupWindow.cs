using UnityEngine;
using UnityEditor;
using System.IO;

public class RecorderSetupWindow : EditorWindow
{
    private bool showMissingPresetsWarning = false;

    [MenuItem("Tools/Recorder Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<RecorderSetupWindow>("Recorder Setup");
        window.minSize = new Vector2(300, 200);
        window.CheckPresetsFolder();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        GUILayout.Label("Recorder Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        if (showMissingPresetsWarning)
        {
            EditorGUILayout.HelpBox("Presets folder not found in Assets. Click below to copy it from the package.", MessageType.Warning);
            if (GUILayout.Button("Copy Presets Folder"))
            {
                CopyPresetsFolder();
                showMissingPresetsWarning = false;
            }
        }

        DrawButtons();
        DrawInfo();
    }

    private void CheckPresetsFolder()
    {
        string targetPath = Path.Combine(Application.dataPath, "Presets");
        if (!Directory.Exists(targetPath))
        {
            showMissingPresetsWarning = true;
        }
    }

    private void CopyPresetsFolder()
    {
        string[] results = AssetDatabase.FindAssets("RecorderSetupWindow t:Script");

        foreach (var guid in results)
        {
            string scriptPath = AssetDatabase.GUIDToAssetPath(guid);
            if (scriptPath.EndsWith("RecorderSetupWindow.cs"))
            {
                // Go up from: Packages/360 Unity Capture/Editor/Scripts
                string scriptDir = Path.GetDirectoryName(scriptPath);
                string packageRoot = Path.GetFullPath(Path.Combine(scriptDir, "../../"));  // up to 360 Unity Capture

                string sourcePath = Path.Combine(packageRoot, "Presets");
                string targetPath = Path.Combine(Application.dataPath, "Presets");

                if (Directory.Exists(sourcePath))
                {
                    FileUtil.CopyFileOrDirectory(sourcePath, targetPath);
                    AssetDatabase.Refresh();
                    Debug.Log("Presets folder copied successfully to Assets.");
                }
                else
                {
                    Debug.LogError($"Could not find source Presets folder at: {sourcePath}");
                }

                return;
            }
        }

        Debug.LogError("Could not locate RecorderSetupWindow.cs in AssetDatabase.");
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
        var existingRecorder = FindObjectOfType<MovieRecorderManager>();
        if (existingRecorder != null)
        {
            bool proceed = EditorUtility.DisplayDialog("Recorder Already Exists",
                "There is already a Recorder in the scene. Do you want to add another one?",
                "Yes", "Cancel");
            if (!proceed) return;
        }

        GameObject recorderObject = new GameObject(objectName);
        var manager = recorderObject.AddComponent<MovieRecorderManager>();
        manager.SelectedRecorderType = type;
        manager.AutoRecordOnPlay = false;

        Selection.activeGameObject = recorderObject;
        SceneView.lastActiveSceneView?.FrameSelected();
        Undo.RegisterCreatedObjectUndo(recorderObject, $"Create {objectName}");
    }
}