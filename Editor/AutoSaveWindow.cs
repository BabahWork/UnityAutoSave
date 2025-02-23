using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Net.Http;
using System;
using System.Threading.Tasks;

public class AutoSaveWindow : EditorWindow
{
    private static int saveInterval;
    private static double nextSaveTime;
    private static bool isAutoSaveEnabled;
    private static int saveType;
    private static bool showNotifications;
    private static bool showDebug;
    
    private const string currentVersion = "1.0.0";
    private const string versionURL = "https://raw.githubusercontent.com/BabahWork/UnityAutoSave/main/autosave_version.txt";
    private const string scriptURL = "https://raw.githubusercontent.com/BabahWork/UnityAutoSave/main/Editor/AutoSaveWindow.cs";
    private const string scriptPath = "Assets/Editor/AutoSaveWindow.cs";
    private static string latestVersion = "Unknown";
    private static bool isUpdateAvailable;
    private static bool hasLoggedPlayModeWarning = false;

    [MenuItem("Tools/Auto Save Settings")]
    public static void ShowWindow()
    {
        var window = GetWindow<AutoSaveWindow>("Auto Save Settings");
        window.minSize = new Vector2(400, 500);
    }

    private void OnEnable() => LoadSettings();

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("⚙️ Auto Save Settings", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 20 });
        GUILayout.Space(15);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("🔧 General Settings", EditorStyles.boldLabel);
        saveInterval = EditorGUILayout.IntSlider(new GUIContent("⏳ Save Interval (sec)", "Time between autosaves"), saveInterval, 30, 600);
        isAutoSaveEnabled = EditorGUILayout.Toggle(new GUIContent("✅ Enable Auto Save"), isAutoSaveEnabled);
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("💾 Save Options", EditorStyles.boldLabel);
        saveType = GUILayout.Toolbar(saveType, new string[] { "Scene", "Assets", "All" });
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("🔔 Notifications", EditorStyles.boldLabel);
        showNotifications = EditorGUILayout.Toggle(new GUIContent("📢 Show Notifications"), showNotifications);
        showDebug = EditorGUILayout.Toggle(new GUIContent("🐞 Show Debug Logs"), showDebug);
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal("box");
        GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
        statusStyle.normal.textColor = isAutoSaveEnabled ? Color.green : Color.red;
        GUILayout.Label("●", statusStyle);
        GUILayout.Label(isAutoSaveEnabled ? "Auto Save Enabled" : "Auto Save Disabled", EditorStyles.boldLabel);
        if (GUILayout.Button("🔄", GUILayout.Width(30), GUILayout.Height(25))) isAutoSaveEnabled = true;
        GUI.enabled = isAutoSaveEnabled;
        if (GUILayout.Button("■ Stop", GUILayout.Width(60), GUILayout.Height(25))) isAutoSaveEnabled = false;
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        if (GUILayout.Button("🔄 Check for Updates", GUILayout.Height(35))) CheckForUpdates();

        if (isUpdateAvailable)
        {
            EditorGUILayout.HelpBox($"New version available: {latestVersion}. Please update!", MessageType.Warning);
            if (GUILayout.Button("🔽 Download & Update", GUILayout.Height(40))) DownloadAndUpdateScript();
        }
        if (!isUpdateAvailable && latestVersion != "Unknown")
        {
            EditorGUILayout.HelpBox($"You are using the latest version ({currentVersion})", MessageType.Info);
        }


        GUILayout.Space(10);
        if (GUILayout.Button("💾 Apply Settings", GUILayout.Height(40)))
        {
            SaveSettings();
            nextSaveTime = EditorApplication.timeSinceStartup + saveInterval;
        }
    }

    private static void LoadSettings()
    {
        saveInterval = EditorPrefs.GetInt("AutoSave_SaveInterval", 300);
        isAutoSaveEnabled = EditorPrefs.GetBool("AutoSave_Enabled", true);
        saveType = EditorPrefs.GetInt("AutoSave_SaveType", 0);
        showNotifications = EditorPrefs.GetBool("AutoSave_ShowNotifications", true);
        showDebug = EditorPrefs.GetBool("AutoSave_ShowDebug", false);
    }

    private static void SaveSettings()
    {
        EditorPrefs.SetInt("AutoSave_SaveInterval", saveInterval);
        EditorPrefs.SetBool("AutoSave_Enabled", isAutoSaveEnabled);
        EditorPrefs.SetInt("AutoSave_SaveType", saveType);
        EditorPrefs.SetBool("AutoSave_ShowNotifications", showNotifications);
        EditorPrefs.SetBool("AutoSave_ShowDebug", showDebug);
        nextSaveTime = EditorApplication.timeSinceStartup + saveInterval;
    }

    private static async void CheckForUpdates()
    {
        using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        try
        {
            latestVersion = (await client.GetStringAsync(versionURL)).Trim();
            isUpdateAvailable = latestVersion != currentVersion;

            if (!isUpdateAvailable)
            {
                Debug.Log("[AutoSave] You are using the latest version");
            }
        }
        catch (Exception e) 
        { 
            Debug.LogError($"[AutoSave] Update check failed: {e.Message}"); 
        }
    }


    private static async void DownloadAndUpdateScript()
    {
        try
        {
            EditorUtility.DisplayProgressBar("Auto Save Update", "Downloading new version...", 0.5f);
            using HttpClient client = new HttpClient();
            string newScript = await client.GetStringAsync(scriptURL);
            File.WriteAllText(scriptPath, newScript);
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            Debug.Log("[AutoSave] Script updated successfully! Please restart Unity.");
            if (EditorUtility.DisplayDialog("Auto Save Update", "Update completed! Please restart Unity.", "Restart Now", "Later"))
                EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[AutoSave] Update failed: {e.Message}");
        }
    }

    [InitializeOnLoadMethod]
    private static void Init()
    {
        LoadSettings();
        nextSaveTime = EditorApplication.timeSinceStartup + saveInterval;
        EditorApplication.update += Update;
    }

    private static void Update()
    {
        if (!isAutoSaveEnabled || EditorApplication.timeSinceStartup < nextSaveTime) return;

        if (EditorApplication.isPlaying) 
        {
            if (!hasLoggedPlayModeWarning && showDebug) 
            {
                Debug.Log("[AutoSave] Autosave is disabled in game mode");
                hasLoggedPlayModeWarning = true;
            }
            return;
        }
        hasLoggedPlayModeWarning = false;
        SaveProject();
        nextSaveTime = EditorApplication.timeSinceStartup + saveInterval;
    }

    private static void SaveProject()
    {
        if (EditorApplication.isPlaying) return;
        
        if (showDebug) Debug.Log("[AutoSave] Start saving...");

        switch (saveType)
        {
            case 0: 
                EditorSceneManager.SaveOpenScenes(); 
                LogNotification("Only the scene has survived"); 
                break;
            case 1: 
                AssetDatabase.SaveAssets(); 
                LogNotification("Only assets saved"); 
                break;
            default: 
                EditorSceneManager.SaveOpenScenes(); 
                AssetDatabase.SaveAssets(); 
                LogNotification("Scene and assets saved"); 
                break;
        }

        if (showDebug) Debug.Log("[AutoSave] Saving complete");
    }


    private static void LogNotification(string message)
    {
        if (showNotifications) EditorUtility.DisplayDialog("Auto Save", message, "OK");
        if (showDebug) Debug.Log("[AutoSave] " + message);
    }
}
