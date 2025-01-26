using System;
using System.IO;
using UnityEngine;
using UnityEditor.Presets;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using static UnityEngine.GraphicsBuffer;
using Object = UnityEngine.Object;

public class MovieRecorderManager : MonoBehaviour {
    public enum RecorderType {
        MovieRecorder,
        MovieRecorder360,
        MovieRecorder360Stereo
    }

    public RecorderType SelectedRecorderType;
    public bool AutoRecordOnPlay = false;
    private RecorderController m_RecorderController;
    public MovieRecorderSettings m_Settings;

    void Start() {
        if (AutoRecordOnPlay) {
            StartRecording();
        }
    }

    void OnDisable() {

        if (AutoRecordOnPlay && m_RecorderController != null &&
            m_RecorderController.IsRecording()) {
            m_RecorderController.StopRecording();
            Debug.Log($"Stopped recording. File saved at {m_Settings.OutputFile}");
        }
    }

    public void StartRecording() {
        //RecorderControllerSettings controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        //m_RecorderController = new RecorderController(controllerSettings);
        LoadRecorderSettings();
        if (m_RecorderController == null) {
            Debug.LogError("Recorder not initialized! Ensure a recorder type is selected and loaded.");
            return;
        }

        if (!m_RecorderController.IsRecording()) {
            m_RecorderController.PrepareRecording();
            m_RecorderController.StartRecording();
            Debug.Log($"Started recording status: {m_RecorderController.IsRecording()}");
        }
    }

    public void StopRecording() {
        if (m_RecorderController != null && m_RecorderController.IsRecording()) {
            m_RecorderController.StopRecording();
            Debug.Log("Recording stopped.");
        }
    }

    public void LoadRecorderSettingsOld() {
        string presetPath = "Assets/Presets/";        
        // Load the appropriate preset
        RecorderControllerSettings controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        m_RecorderController = new RecorderController(controllerSettings);
        m_Settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        switch (SelectedRecorderType) {
            case RecorderType.MovieRecorder:
                //m_Settings.ImageInputSettings = AssetDatabase.LoadAssetAtPath<MovieRecorderSettings>($"{presetPath}MovieRecorderSettings.preset").ImageInputSettings;
                ApplyPreset(presetPath, "MovieRecorderSettings", m_Settings);
                break;
            case RecorderType.MovieRecorder360:
                //m_Settings.ImageInputSettings = AssetDatabase.LoadAssetAtPath<MovieRecorderSettings>($"{presetPath}360MovieRecorderSettings.preset").ImageInputSettings;
                ApplyPreset(presetPath, "360MovieRecorderSettings", m_Settings);
                break;
            case RecorderType.MovieRecorder360Stereo:
                //m_Settings.ImageInputSettings = AssetDatabase.LoadAssetAtPath<MovieRecorderSettings>($"{presetPath}360StereoMovieRecorderSettings.preset").ImageInputSettings;
                ApplyPreset(presetPath, "360StereoMovieRecorderSettings", m_Settings);
                break;
        }
        //m_Settings.OutputFile = mediaOutputFolder + $"Capture_{DateTime.Now:yyyy-dd-HH-mm}.mp4";
        ConfigureMediaOutput();
        controllerSettings.AddRecorderSettings(m_Settings);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 60.0f;        
        Debug.Log($"Loaded settings for {SelectedRecorderType}");

        // Save the ScriptableObject asset so that the path is retained
        //EditorUtility.SetDirty(recorderData);
        //AssetDatabase.SaveAssets();
    }

    private string GetPackagePresetPath() {
        // Search in both package and project directories
        string[] guids = AssetDatabase.FindAssets("t:Preset", new[] {
            "Packages/com.reza.vrcapture/Presets",
            "Assets/Presets"
        });

        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetDirectoryName(path).Contains("Presets")) {
                return Path.GetDirectoryName(path);
            }
        }

        Debug.LogError("Presets directory not found in package or project");
        return null;
    }

    public void LoadRecorderSettings() {
        string presetPath = GetPackagePresetPath();
        if (string.IsNullOrEmpty(presetPath)) return;

        // Rest of the method remains the same
        RecorderControllerSettings controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        m_RecorderController = new RecorderController(controllerSettings);
        m_Settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();

        string presetName = SelectedRecorderType switch {
            RecorderType.MovieRecorder => "MovieRecorderSettings",
            RecorderType.MovieRecorder360 => "360MovieRecorderSettings",
            RecorderType.MovieRecorder360Stereo => "360StereoMovieRecorderSettings",
            _ => throw new ArgumentException("Invalid recorder type")
        };

        ApplyPreset(presetPath, presetName, m_Settings);
        ConfigureMediaOutput();
        controllerSettings.AddRecorderSettings(m_Settings);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 60.0f;

        Debug.Log($"Loaded settings for {SelectedRecorderType} from {presetPath}");
    }

    private void ApplyPreset(string presetPath, string presetName, Object target) {
        Preset preset = AssetDatabase.LoadAssetAtPath<Preset>($"{presetPath}{presetName}.preset");

        if (preset != null && preset.CanBeAppliedTo(target)) {
            preset.ApplyTo(target);
            Debug.Log($"Preset '{presetName}.preset' successfully applied to {target.name}.");
        } else {
            Debug.LogError($"Failed to apply preset '{presetName}.preset'. Ensure it exists and is compatible with the target object.");
        }
    }

    void ConfigureMediaOutput() {
        // Define the media output folder path
        string mediaOutputFolder = Path.Combine(UnityEngine.Application.streamingAssetsPath, "Captures");

        // Ensure the folder exists
        if (!Directory.Exists(mediaOutputFolder)) {
            Directory.CreateDirectory(mediaOutputFolder);
        }

        // Set the output file path
        m_Settings.OutputFile = Path.Combine(mediaOutputFolder, $"Capture_{DateTime.Now:yyyy-dd-HH-mm}.mp4");
    }


}
