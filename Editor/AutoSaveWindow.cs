using UnityEngine;
using UnityEditor;
using System.IO;
using System.Net;
using System;

public class AutoSaveWindow : EditorWindow
{
    private static int saveInterval;
    private static double nextSaveTime;
    private static bool isAutoSaveEnabled;
    private static int saveType;
    private static bool showNotifications;
    private static bool showDebug;

    private const string PREFS_SAVE_INTERVAL = "AutoSave_SaveInterval";
    private const string PREFS_AUTO_SAVE_ENABLED = "AutoSave_Enabled";
    private const string PREFS_SAVE_TYPE = "AutoSave_SaveType";
    private const string PREFS_SHOW_NOTIFICATIONS = "AutoSave_ShowNotifications";
    private const string PREFS_SHOW_DEBUG = "AutoSave_ShowDebug";
    
    private const string currentVersion = "1.0.0";
    private const string versionURL = "https://raw.githubusercontent.com/BabahWork/UnityAutoSave/main/autosave_version.txt";
private const string scriptURL = "https://raw.githubusercontent.com/BabahWork/UnityAutoSave/main/Editor/AutoSaveWindow.cs";
    private const string scriptPath = "Assets/Editor/AutoSaveWindow.cs";
    private static string latestVersion = "Unknown";
    private static bool isUpdateAvailable = false;

    [MenuItem("Tools/Auto Save Settings")]
    public static void ShowWindow()
    {
        var window = GetWindow<AutoSaveWindow>("Auto Save Settings");
        window.minSize = new Vector2(300, 340);
    }

    private void OnEnable()
    {
        LoadSettings();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("⚙️ Auto Save Settings", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16 });
        GUILayout.Space(10);

        saveInterval = EditorGUILayout.IntSlider("⏳ Save Interval (sec)", saveInterval, 30, 600);
        GUILayout.Space(10);

        GUILayout.Label("💾 Save Type:");
        saveType = GUILayout.Toolbar(saveType, new string[] { "Scene", "Assets", "All" });
        GUILayout.Space(10);
        
        showNotifications = EditorGUILayout.Toggle("🔔 Show Notifications", showNotifications);
        showDebug = EditorGUILayout.Toggle("🐞 Show Debug Logs", showDebug);
        GUILayout.Space(10);

        if (GUILayout.Button("🔄 Check for Updates", GUILayout.Height(35)))
        {
            CheckForUpdates();
        }

        if (isUpdateAvailable)
        {
            EditorGUILayout.HelpBox($"New version available: {latestVersion}. Please update!", MessageType.Warning);
            if (GUILayout.Button("🔽 Download & Update", GUILayout.Height(35)))
            {
                DownloadAndUpdateScript();
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("💾 Apply Settings", GUILayout.Height(35)))
        {
            SaveSettings();
            nextSaveTime = EditorApplication.timeSinceStartup + saveInterval;
        }
    }

    private static void LoadSettings()
    {
        saveInterval = EditorPrefs.GetInt(PREFS_SAVE_INTERVAL, 300);
        isAutoSaveEnabled = EditorPrefs.GetBool(PREFS_AUTO_SAVE_ENABLED, true);
        saveType = EditorPrefs.GetInt(PREFS_SAVE_TYPE, 0);
        showNotifications = EditorPrefs.GetBool(PREFS_SHOW_NOTIFICATIONS, true);
        showDebug = EditorPrefs.GetBool(PREFS_SHOW_DEBUG, false);
    }

    private static void SaveSettings()
    {
        EditorPrefs.SetInt(PREFS_SAVE_INTERVAL, saveInterval);
        EditorPrefs.SetBool(PREFS_AUTO_SAVE_ENABLED, isAutoSaveEnabled);
        EditorPrefs.SetInt(PREFS_SAVE_TYPE, saveType);
        EditorPrefs.SetBool(PREFS_SHOW_NOTIFICATIONS, showNotifications);
        EditorPrefs.SetBool(PREFS_SHOW_DEBUG, showDebug);
    }

    private static void CheckForUpdates()
    {
        WebClient client = new WebClient();
        try
        {
            latestVersion = client.DownloadString(versionURL).Trim();
            if (latestVersion != currentVersion)
            {
                isUpdateAvailable = true;
                Debug.Log($"[AutoSave] New version available: {latestVersion}. Current: {currentVersion}");
            }
            else
            {
                Debug.Log("[AutoSave] You have the latest version.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AutoSave] Update check failed: {e.Message}");
        }
    }

    private static void DownloadAndUpdateScript()
    {
        WebClient client = new WebClient();
        try
        {
            string newScript = client.DownloadString(scriptURL);
            File.WriteAllText(scriptPath, newScript);
            
            AssetDatabase.Refresh();
            Debug.Log("[AutoSave] Script updated successfully! Please reload the editor.");
            
            if (EditorUtility.DisplayDialog("Auto Save Update", "Update completed! Please restart Unity.", "Restart Now", "Later"))
            {
                EditorApplication.Exit(0);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AutoSave] Update failed: {e.Message}");
            EditorUtility.DisplayDialog("Auto Save Update", "Update failed. Check the console.", "OK");
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
        if (isAutoSaveEnabled && EditorApplication.timeSinceStartup >= nextSaveTime)
        {
            SaveProject();
            nextSaveTime = EditorApplication.timeSinceStartup + saveInterval;
        }
    }

    private static void SaveProject()
    {
        if (showDebug) Debug.Log("[AutoSave] Начало сохранения...");
        
        if (saveType == 0)
        {
            EditorApplication.SaveScene(EditorApplication.currentScene);
            LogNotification("Сохранена только сцена.");
        }
        else if (saveType == 1)
        {
            AssetDatabase.SaveAssets();
            LogNotification("Сохранены только ассеты.");
        }
        else
        {
            EditorApplication.SaveScene(EditorApplication.currentScene);
            AssetDatabase.SaveAssets();
            LogNotification("Сохранены сцена и ассеты.");
        }
        
        if (showDebug) Debug.Log("[AutoSave] Сохранение завершено.");
    }

    private static void LogNotification(string message)
    {
        if (showNotifications)
        {
            EditorUtility.DisplayDialog("Auto Save", message, "OK");
        }
        
        if (showDebug) Debug.Log("[AutoSave] " + message);
    }
}
