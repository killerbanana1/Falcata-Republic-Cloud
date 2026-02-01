using Modding;
using UnityEngine;
using System;
using System.Reflection;
using System.IO;

public class CopyFleetFolder : IModEntryPoint
{
    private const string SourceFolderName = "Starter Fleets - Falcata";

    public void PreLoad()
    {
        Debug.Log("[CopyFleetFolder] PreLoad");
    }

    public void PostLoad()
    {
        Debug.Log("[CopyFleetFolder] PostLoad");

        try
        {
            CopyFleetFolderFromModRoot();
        }
        catch (Exception e)
        {
            Debug.LogError("[CopyFleetFolder] Exception:\n" + e);
        }
    }

    private void CopyFleetFolderFromModRoot()
    {
        string modRoot = Path.GetDirectoryName(typeof(CopyFleetFolder).Assembly.Location);

        string source = Path.Combine(modRoot, SourceFolderName);

        string dest = Path.Combine(Directory.GetCurrentDirectory(), "Saves", "Fleets", SourceFolderName);

        Debug.Log("[CopyFleetFolder] Source: " + source);
        Debug.Log("[CopyFleetFolder] Dest:   " + dest);

        if (!Directory.Exists(source))
        {
            Debug.LogError("[CopyFleetFolder] Source folder not found!");
            return;
        }

        if (Directory.Exists(dest))
        {
            Debug.Log("[Falcata Folder] Removing existing fleet folder");
            Directory.Delete(dest, recursive: true);
        }

        CopyDirectory(source, dest);
        Debug.Log("[Falcata Folder] Fleet folder copied (overwrite complete)");
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: false);
        }

        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
    }
}