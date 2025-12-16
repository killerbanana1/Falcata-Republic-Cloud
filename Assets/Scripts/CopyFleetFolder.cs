/*using System.IO;
using Modding;
using UnityEngine;

public class CopyFleetFolder : IModEntry
{
    public string folderNameToCopy = "Starter Fleets - Falcata";
    public string newFolderName    = "Starter Fleets - Falcata";

    void Start()
    {
        CopyFolder();
    }

    void CopyFolder()
    {
        string gameRoot = Directory.GetCurrentDirectory();

        string source = Path.Combine(Application.streamingAssetsPath, folderNameToCopy);
        string dest   = Path.Combine(gameRoot, "Saves", "Fleets", newFolderName);

        if (!Directory.Exists(source))
        {
            Debug.LogError("Source folder not found: " + source);
            return;
        }

        CopyDirectory(source, dest);
        Debug.Log("Copied fleet folder to: " + dest);
    }

    void CopyDirectory(string sourceDir, string destDir)
    {
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string dest = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, dest, overwrite: true);
        }

        foreach (string folder in Directory.GetDirectories(sourceDir))
        {
            string destFolder = Path.Combine(destDir, Path.GetFileName(folder));
            CopyDirectory(folder, destFolder);
        }
    }
}*/