using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using System;

[InitializeOnLoad]
public class PawlygonVRCFTPackageInstaller : EditorWindow
{
    /// <summary>
    /// //Github Repository Owner
    /// </summary>
    public const string owner = "PawlygonStudio";
    /// <summary>
    /// Github Repository Name
    /// </summary>
    public const string repo = "VRC-Facetracking";
    /// <summary>
    /// Set this variable with a name that is common between all your unity packages. This is required since its used on OnImportPackageCompleted to only run when needed.
    /// </summary>
    public const string patcher_file_name = "FT_Patcher";
    /// <summary>
    /// Temporary file name, this will on be used to save the download on Application.persistentDataPath, but will be deleted after import.
    /// </summary>
    public const string temp_file_name = "latest-facetracking-template";
    /// <summary>
    /// Only used when an error is displayed, this URL is where the user will be redirected in case anything goes wrong during Template Installation.
    /// </summary>
    public const string error_url = "https://vcc.pawlygon.net/";

    static PawlygonVRCFTPackageInstaller()
    {
        AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
        AssetDatabase.importPackageCancelled += OnImportPackageCancelled;
    }

    private static void OnImportPackageCompleted(string packageName)
    {
        if (packageName.Contains(patcher_file_name))
        {
            ImportFacetrackingPackage();
        }
    }

    private static void OnImportPackageCancelled(string packageName)
    {
        if(packageName == temp_file_name)
        {
            if(EditorUtility.DisplayDialog("Template Installation Cancelled","Pawlygon Facetracking Template import was cancelled, this is required for Facetracking to work properly.","Install Package","Continue Without Package (Advanced)"))
            {
                ImportFacetrackingPackage();
            }
        }
    }
    
    //Menu item for manually install/update
    [MenuItem("Tools/!Pawlygon/Install Latest Facetracking Template")]
    private static void ImportFacetrackingPackage()
    {
        string downloadUrl = FetchLatestRelease();
        DownloadAndImportPackage(downloadUrl);
    }

    private static void DownloadAndImportPackage(string downloadUrl)
    {
        Debug.Log("Downloading template package from: " + downloadUrl);

        using (UnityWebRequest webRequest = UnityWebRequest.Get(downloadUrl))
        {
            webRequest.SendWebRequest();

            while (!webRequest.isDone) { }

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Failed to download package: {webRequest.error}");
                DisplayError();
                return;
            }

            Debug.Log("Template package downloaded. Importing...");

            string packagePath = Path.Combine(Application.persistentDataPath, $"{temp_file_name}.unitypackage");
            File.WriteAllBytes(packagePath, webRequest.downloadHandler.data);

            AssetDatabase.ImportPackage(packagePath, true);
            AssetDatabase.Refresh();

            File.Delete(packagePath);

            Debug.Log("Face Tracking Template imported successfully!");
        }
    }

    private static string FetchLatestRelease()
    {
        string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(apiUrl))
        {
            webRequest.SendWebRequest();

            while (!webRequest.isDone) { }

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Failed to fetch latest release: {webRequest.error}");
                DisplayError();
                return null;
            }

            string json = webRequest.downloadHandler.text;
            GitHubRelease latestRelease = JsonUtility.FromJson<GitHubRelease>(json);

            return FindUnityPackageDownloadUrl(latestRelease.assets);
        }
    }

    private static string FindUnityPackageDownloadUrl(GitHubAsset[] assets)
    {
        foreach (var asset in assets)
        {
            if (asset.browser_download_url.EndsWith(".unitypackage"))
            {
                return asset.browser_download_url;
            }
        }
        Debug.LogError("Failed to find .unitypackage file in assets.");
        DisplayError();
        return null;
    }

    private static void DisplayError()
    {
        if(EditorUtility.DisplayDialog("Couldn't Install Face Tracking Template","Something went wrong downloading the Face Tracking Template, please install it using VCC","Install using VCC","Cancel"))
            Application.OpenURL(error_url);
    }

    [Serializable]
    public class GitHubRelease
    {
        public string url;
        public string tag_name;
        public GitHubAsset[] assets;
    }

    [Serializable]
    public class GitHubAsset
    {
        public string browser_download_url;
    }
}
