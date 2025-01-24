using UnityEngine;
using UnityEditor;
using System.IO;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using System.Net;

public class YouTubeUploaderWindow : EditorWindow {
    private string videoPath = "";
    private string videoTitle = "";
    private string videoDescription = "";
    private string privacyStatus = "private";
    private bool isUploading = false;
    private float uploadProgress = 0f;
    private string uploadStatus = "";
    private Vector2 scrollPosition;
    private Rect dropArea;
    private YouTubeUploadService uploadService;

    private string[] privacyOptions = new string[] { "private", "unlisted", "public" };
    private int selectedPrivacyIndex = 0;

    // Add a reference to the YouTubeCredentials ScriptableObject
    [SerializeField]
    private YouTubeCredentials credentials;

    // GUI field for assigning the ScriptableObject
    private SerializedObject serializedCredentials;

    [MenuItem("Tools/YouTube Uploader")]
    public static void ShowWindow() {
        var window = GetWindow<YouTubeUploaderWindow>("YouTube Uploader");
        window.minSize = new Vector2(400, 550);
        window.Show();
    }

    private void OnEnable() {
        // Load existing credentials asset or prompt to create one
        if (credentials == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:YouTubeCredentials");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                credentials = AssetDatabase.LoadAssetAtPath<YouTubeCredentials>(path);
            }
        }

        serializedCredentials = new SerializedObject(this);

        uploadService = new YouTubeUploadService(
            credentials.clientId,
            credentials.clientSecret,
            (progress, status) => {
                uploadProgress = progress;
                uploadStatus = status;
                Repaint();
            },
            (videoId) => {
                isUploading = false;
                EditorUtility.DisplayDialog("Upload Complete",
                    $"Video uploaded successfully!\nVideo ID: {videoId}", "OK");
                Repaint();
            },
            (error) => {
                isUploading = false;
                EditorUtility.DisplayDialog("Error", error, "OK");
                Repaint();
            }
        );
    }

    private void OnGUI() {
        serializedCredentials.Update();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        DrawVideoSelection();
        DrawSpatialMetadata();
        DrawMetadataFields();
        DrawPrivacySettings();
        DrawUploadButton();
        DrawProgressBar();

        EditorGUILayout.EndScrollView();

        HandleDragAndDrop();

        serializedCredentials.ApplyModifiedProperties();
    }

    private void DrawHeader() {
        EditorGUILayout.Space(10);
        GUILayout.Label("YouTube Video Uploader", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
    }

    private void DrawVideoSelection() {
        EditorGUILayout.LabelField("Video File", EditorStyles.boldLabel);

        var dropRect = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        dropArea = dropRect;
        GUI.Box(dropRect, "Drag and Drop Video File Here\nor\nClick to Browse", EditorStyles.helpBox);

        if (Event.current.type == EventType.MouseDown && dropRect.Contains(Event.current.mousePosition)) {
            BrowseVideo();
        }

        if (!string.IsNullOrEmpty(videoPath)) {
            EditorGUILayout.LabelField("Selected:", Path.GetFileName(videoPath));
        }

        EditorGUILayout.Space(10);
    }

    private void DrawSpatialMetadata() {
        if (!string.IsNullOrEmpty(videoPath)) {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Spatial Metadata Injection", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Process the video with spatial metadata before upload.",
                MessageType.Info);

            EditorGUI.BeginDisabledGroup(isUploading);

            if (GUILayout.Button("Inject Spherical (360°) Metadata")) {
                InjectMetadata(true, false);
            }

            if (GUILayout.Button("Inject Stereo Left-Right Metadata")) {
                InjectMetadata(false, true);
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(5);
        }
    }

    private void DrawMetadataFields() {
        EditorGUILayout.LabelField("Video Details", EditorStyles.boldLabel);

        videoTitle = EditorGUILayout.TextField("Title", videoTitle);

        EditorGUILayout.LabelField("Description");
        videoDescription = EditorGUILayout.TextArea(videoDescription, GUILayout.Height(100));

        EditorGUILayout.Space(10);
    }

    private void DrawPrivacySettings() {
        EditorGUILayout.LabelField("Privacy Settings", EditorStyles.boldLabel);
        selectedPrivacyIndex = EditorGUILayout.Popup("Privacy Status",
            selectedPrivacyIndex, privacyOptions);
        privacyStatus = privacyOptions[selectedPrivacyIndex];

        EditorGUILayout.Space(10);
    }

    private void DrawUploadButton() {
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(videoPath) ||
                                   string.IsNullOrEmpty(videoTitle) ||
                                   isUploading);

        if (GUILayout.Button(isUploading ? "Uploading..." : "Upload to YouTube")) {
            if (EditorUtility.DisplayDialog("Upload Confirmation",
                "Are you sure you want to upload this video to YouTube?",
                "Yes", "Cancel")) {
                StartUpload();
            }
        }

        EditorGUI.EndDisabledGroup();
    }

    private void DrawProgressBar() {
        if (isUploading) {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Upload Progress", EditorStyles.boldLabel);
            EditorGUI.ProgressBar(GUILayoutUtility.GetRect(0, 20),
                uploadProgress, uploadStatus);
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Cancel Upload")) {
                // TODO: Implement upload cancellation
            }
        }
    }

    private void HandleDragAndDrop() {
        Event evt = Event.current;
        switch (evt.type) {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform) {
                    DragAndDrop.AcceptDrag();
                    foreach (string path in DragAndDrop.paths) {
                        if (IsVideoFile(path)) {
                            videoPath = path;
                            break;
                        }
                    }
                }
                evt.Use();
                break;
        }
    }

    private void BrowseVideo() {
        string path = EditorUtility.OpenFilePanel("Select Video", "", "mp4,mov,avi,wmv");
        if (!string.IsNullOrEmpty(path)) {
            videoPath = path;
        }
    }

    private bool IsVideoFile(string path) {
        string ext = Path.GetExtension(path).ToLower();
        return ext == ".mp4" || ext == ".mov" || ext == ".avi" || ext == ".wmv";
    }

    private async void StartUpload() {
        try {
            isUploading = true;
            uploadProgress = 0f;
            uploadStatus = "Starting upload...";
            Repaint();

            await uploadService.UploadVideo(videoPath, videoTitle, videoDescription, privacyStatus);
        } catch (Exception ex) {
            EditorUtility.DisplayDialog("Error",
                $"Failed to start upload: {ex.Message}", "OK");
            isUploading = false;
            Repaint();
        }
    }

    private void InjectMetadata(bool spherical, bool stereo) {
        string outputPath = Path.Combine(
            Path.GetDirectoryName(videoPath),
            Path.GetFileNameWithoutExtension(videoPath) + "_processed" + Path.GetExtension(videoPath)
        );

        bool success = false;
        if (spherical) {
            success = PythonScriptExecutor.InjectSphericalMetadata(videoPath, outputPath);
        } else if (stereo) {
            success = PythonScriptExecutor.InjectStereoMetadata(videoPath, outputPath);
        }

        if (success) {
            if (EditorUtility.DisplayDialog("Metadata Injection Complete",
                $"Metadata has been injected successfully.\nNew file created at:\n{outputPath}\n\nWould you like to use this processed file for upload?",
                "Yes", "No")) {
                videoPath = outputPath;
            }
        } else {
            EditorUtility.DisplayDialog("Error",
                "Failed to inject metadata. Check console for details.",
                "OK");
        }
    }

    private void OnInspectorUpdate() {
        if (isUploading) {
            Repaint();
        }
    }
}


public class YouTubeUploadService {
    private static string CLIENT_ID;
    private static string CLIENT_SECRET;
    private static string REDIRECT_URI = "http://localhost:8080/";
    private static string VIDEO_CATEGORY_ID = "22"; // Category: "People & Blogs"
    private static string[] VIDEO_TAGS = { "tag1", "tag2" };

    private IAuthorizationCodeFlow flow;
    private Action<float, string> progressCallback;
    private Action<string> completionCallback;
    private Action<string> errorCallback;

    public YouTubeUploadService(string clientId, string clientSecret, Action<float, string> onProgress = null,
                              Action<string> onComplete = null,
                              Action<string> onError = null) {
        progressCallback = onProgress;
        completionCallback = onComplete;
        errorCallback = onError;
        InitializeFlow();
    }

    private void InitializeFlow() {
        flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer {
            ClientSecrets = new ClientSecrets { ClientId = CLIENT_ID, ClientSecret = CLIENT_SECRET },
            DataStore = new FileDataStore("YouTubeUploaderStore", true),
            Scopes = new[] { YouTubeService.Scope.YoutubeUpload }
        });
    }

    public async Task UploadVideo(string videoPath, string title, string description, string privacyStatus = "private") {
        try {
            var result = await new AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver(REDIRECT_URI))
                .AuthorizeAsync("user", CancellationToken.None);

            if (result != null) {
                var youtubeService = new YouTubeService(new BaseClientService.Initializer {
                    HttpClientInitializer = result,
                    ApplicationName = "UnityYouTubeUploader"
                });

                var video = new Video {
                    Snippet = new VideoSnippet {
                        Title = title,
                        Description = description,
                        Tags = VIDEO_TAGS,
                        CategoryId = VIDEO_CATEGORY_ID
                    },
                    Status = new VideoStatus {
                        PrivacyStatus = privacyStatus
                    }
                };

                using (var fileStream = new FileStream(videoPath, FileMode.Open)) {
                    var request = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");

                    request.ProgressChanged += (progress) => {
                        float percentage = (float)progress.BytesSent / fileStream.Length;
                        string status = $"Uploaded {FormatBytes(progress.BytesSent)} of {FormatBytes(fileStream.Length)}";

                        // Ensure UI updates happen on the main thread
                        EditorApplication.delayCall += () => {
                            progressCallback?.Invoke(percentage, status);
                        };
                    };

                    request.ResponseReceived += (video) => {
                        // Ensure UI updates happen on the main thread
                        EditorApplication.delayCall += () => {
                            completionCallback?.Invoke(video.Id);
                        };
                    };

                    await request.UploadAsync();
                }
            } else {
                EditorApplication.delayCall += () => {
                    errorCallback?.Invoke("Authorization failed or was canceled.");
                };
            }
        } catch (Exception ex) {
            EditorApplication.delayCall += () => {
                errorCallback?.Invoke(ex.Message);
            };
        }
    }

    private string FormatBytes(long bytes) {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1) {
            order++;
            size = size / 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}