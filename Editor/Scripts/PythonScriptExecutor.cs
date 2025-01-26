using UnityEngine;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;
using System;

public class PythonScriptExecutor {
    // Get the system-specific Python path
    private static string GetPythonPath() {
        if (Application.platform == RuntimePlatform.OSXEditor ||
            Application.platform == RuntimePlatform.LinuxEditor) {
            // Unix-like systems (macOS/Linux) common paths
            string[] unixPaths = new string[]
            {
                "/usr/bin/python",
                "/usr/local/bin/python",
                "/usr/bin/python3",
                "/usr/local/bin/python3",
                $"/Users/{System.Environment.UserName}/opt/anaconda3/bin/python",
                $"/Users/{System.Environment.UserName}/anaconda3/bin/python",
                $"/opt/anaconda3/bin/python",
                "python" // Fallback to PATH
            };

            foreach (string path in unixPaths) {
                try {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = path;
                    startInfo.Arguments = "--version";
                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;

                    using (Process process = Process.Start(startInfo)) {
                        process.WaitForExit();
                        if (process.ExitCode == 0) {
                            Debug.Log($"Found working Python at: {path}");
                            return path;
                        }
                    }
                } catch (System.Exception) {
                    continue;
                }
            }

            Debug.LogWarning("No Python installation found in common Unix paths, falling back to 'python'");
            return "python";
        } else // Windows
          {
            string userProfile = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            string programFiles = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
            string localAppData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);

            // Windows common paths including Anaconda
            string[] windowsPaths = new string[]
            {
                // Standard Python installations
                Path.Combine(programFiles, "Python39", "python.exe"),
                Path.Combine(programFiles, "Python37", "python.exe"),
                Path.Combine(localAppData, "Programs", "Python", "Python39", "python.exe"),
                Path.Combine(localAppData, "Programs", "Python", "Python37", "python.exe"),
                
                // Anaconda installations
                Path.Combine(userProfile, "Anaconda3", "python.exe"),
                Path.Combine(userProfile, "miniconda3", "python.exe"),
                Path.Combine(programFiles, "Anaconda3", "python.exe"),
                Path.Combine(localAppData, "Continuum", "anaconda3", "python.exe"),
                Path.Combine(localAppData, "Continuum", "miniconda3", "python.exe"),

                "python.exe" // Fallback to PATH
            };

            foreach (string path in windowsPaths) {
                try {
                    if (File.Exists(path)) {
                        // Test if Python is working
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = path;
                        startInfo.Arguments = "--version";
                        startInfo.UseShellExecute = false;
                        startInfo.RedirectStandardOutput = true;
                        startInfo.RedirectStandardError = true;

                        using (Process process = Process.Start(startInfo)) {
                            process.WaitForExit();
                            if (process.ExitCode == 0) {
                                Debug.Log($"Found working Python at: {path}");
                                return path;
                            }
                        }
                    }
                } catch (System.Exception) {
                    continue;
                }
            }

            Debug.LogWarning("No Python installation found in common Windows paths, falling back to 'python.exe'");
            return "python.exe";
        }
    }

    private static string GetAbsolutePath(string relativePath) {
        if (Path.IsPathRooted(relativePath))
            return relativePath;

        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), relativePath));
    }

    public static bool InjectSphericalMetadata(string inputPath, string outputPath) {
        string absoluteInputPath = GetAbsolutePath(inputPath);
        string absoluteOutputPath = GetAbsolutePath(outputPath);
        return ExecutePythonScript(absoluteInputPath, absoluteOutputPath, "--spherical-only");
    }

    public static bool InjectStereoMetadata(string inputPath, string outputPath) {
        string absoluteInputPath = GetAbsolutePath(inputPath);
        string absoluteOutputPath = GetAbsolutePath(outputPath);
        return ExecutePythonScript(absoluteInputPath, absoluteOutputPath, "--stereo left-right");
    }

    private static string GetPackagePythonPath() {
        // Get the package path regardless of whether it's in Packages or Assets
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Script PythonScriptExecutor");
        if (guids.Length == 0) {
            Debug.LogError("Could not locate PythonScriptExecutor script in project");
            return null;
        }

        string scriptPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
        string editorFolder = Path.GetDirectoryName(scriptPath);
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), Path.Combine(editorFolder, "../Python/spatial-media-2.1/inject_metadata_cli.py")));
    }

    private static string GetOutputPath(string originalPath) {
        string fileName = Path.GetFileName(originalPath);
        string downloadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads"
        );
        return Path.Combine(downloadsPath, fileName);
    }

    private static bool ExecutePythonScript(string inputPath, string outputPath, string arguments) {
        try {
            string pythonPath = GetPythonPath();
            string scriptPath = GetPackagePythonPath();
            string finalOutputPath = GetOutputPath(outputPath);

            if (scriptPath == null) {
                Debug.LogError("Failed to locate Python script path");
                return false;
            }

            // Build the full shell command
            string command = $"\"{pythonPath}\" \"{scriptPath}\" \"{inputPath}\" \"{finalOutputPath}\" {arguments}";
            Debug.Log($"Executing command: {command}");

            ProcessStartInfo startInfo = new ProcessStartInfo();

            if (Application.platform == RuntimePlatform.OSXEditor ||
                Application.platform == RuntimePlatform.LinuxEditor) {
                startInfo.FileName = "/bin/bash";
                startInfo.Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"";
            } else // Windows
              {
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = $"/C {command}";
            }

            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;

            using (Process process = Process.Start(startInfo)) {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                    Debug.Log($"Output: {output}");
                if (!string.IsNullOrEmpty(error))
                    Debug.LogError($"Error: {error}");

                return process.ExitCode == 0;
            }
        } catch (System.Exception e) {
            Debug.LogError($"Failed to execute command: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return false;
        }
    }
}