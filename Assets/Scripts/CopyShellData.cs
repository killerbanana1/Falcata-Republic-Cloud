using HarmonyLib;
using Modding;
using Munitions;
using UnityEngine;
using UnityEngine.VFX;
using System;
using System.Reflection;
using System.Collections.Generic;
using Ships;
using Bundles;
using Utility;

public class CopyShellData : IModEntryPoint
{
    public void PreLoad()
    {
        Debug.Log("[Falcata] PreLoad called.");
    }

    public void PostLoad()
    {
        Debug.Log("[Falcata] PostLoad started.");
        try
        {
            updateAllTurrets();
            updateAllMunitions();

            Harmony harmony = new Harmony("nebulous.Falcata");
            harmony.PatchAll();
            Debug.Log("[Falcata] PostLoad Harmony call complete.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Falcata] Exception in PostLoad: {e}");
        }
    }

    public static void updateAllTurrets()
    {
        Debug.Log("[Falcata] updateAllTurrets started.");

        Dictionary<string, HullComponent> componentDictionary = (Dictionary<string, HullComponent>)GetPrivateField(BundleManager.Instance, "_components");

        if (componentDictionary == null)
        {
            Debug.LogError("[Falcata] componentDictionary is null in updateAllTurrets.");
            return;
        }

        foreach (string componentName in componentDictionary.Keys)
        {
            Debug.Log($"[Falcata] Found component: {componentName}");
        }

        // List of turret mappings
        string[,] turretPairs = new string[,]
        {
            { "Stock/Mk68 Cannon", "Falcata Republic/RHI-4380 Cannon" },
            { "Stock/Mk68 Cannon", "Falcata Republic/RHI-3380 Cannon" },
            { "Stock/Mk68 Cannon", "Falcata Republic/RHI-2380 Cannon" },
            { "Stock/Mk65 Cannon", "Falcata Republic/RHI-4150 Cannon" },
            { "Stock/Mk65 Cannon", "Falcata Republic/RHI-2150 Cannon" },
            { "Stock/Mk65 Cannon", "Falcata Republic/RHI-3220 Recoilless Rifle" },
            { "Stock/C65 Cannon", "Falcata Republic/RHI-S1620 Recoilless Rifle" },
            { "Stock/C65 Cannon", "Falcata Republic/RHI-S1700 Kinetic Artillery" },
            { "Stock/C53 Cannon", "Falcata Republic/RHI-S1150 Cannon" },
            { "Stock/C53 Cannon", "Falcata Republic/RHI-S4150 Cannon" },
            { "Stock/Mk81 Railgun", "Falcata Republic/WY-137 Railgun" },
            { "Stock/Mk81 Railgun", "Falcata Republic/WY-237 Railgun" },
            { "Stock/Mk64 Cannon", "Falcata Republic/WY-1220 ETC Gun" },
            { "Stock/T20 Cannon", "Falcata Republic/WY-190 'Merit' PDT" },
            { "Stock/Mk29 'Stonewall' PDT", "Falcata Republic/MED-240 'Fleur' PDT" }
        };

        for (int i = 0; i < turretPairs.GetLength(0); i++)
        {
            updateTurret(componentDictionary, turretPairs[i, 0], turretPairs[i, 1]);
        }

        Debug.Log("[Falcata] updateAllTurrets complete.");
    }

    public static void updateTurret(Dictionary<string, HullComponent> componentDictionary, string keySource, string keyDestination)
    {
        Debug.Log($"[updateTurret] Source: '{keySource}', Destination: '{keyDestination}'");

        if (!componentDictionary.TryGetValue(keySource, out HullComponent componentSource))
        {
            Debug.LogError($"[updateTurret] Missing source component: {keySource}");
            return;
        }

        if (!componentDictionary.TryGetValue(keyDestination, out HullComponent componentDestination))
        {
            Debug.LogError($"[updateTurret] Missing destination component: {keyDestination}");
            return;
        }

        try
        {
            var sourceDVP = (DynamicVisibleParticles)GetPrivateField(componentSource, "_disabledParticles");
            var destinationDVP = (DynamicVisibleParticles)GetPrivateField(componentDestination, "_disabledParticles");

            Debug.Log($"[updateTurret] Source DVP: {sourceDVP}, Destination DVP: {destinationDVP}");

            if (sourceDVP == null || destinationDVP == null) return;

            var sourceVFX = (VisualEffect)GetPrivateField(sourceDVP, "_particles");
            var destinationVFX = (VisualEffect)GetPrivateField(destinationDVP, "_particles");

            Debug.Log($"[updateTurret] Source VFX: {sourceVFX}, Destination VFX: {destinationVFX}");

            if (sourceVFX != null && destinationVFX != null)
                destinationVFX.visualEffectAsset = sourceVFX.visualEffectAsset;

            var sourceMuzzles = (Muzzle[])GetPrivateField(componentSource, "_muzzles");
            var destinationMuzzles = (Muzzle[])GetPrivateField(componentDestination, "_muzzles");

            if (sourceMuzzles == null || sourceMuzzles.Length == 0)
            {
                Debug.LogWarning("[updateTurret] sourceMuzzles is null or empty.");
                return;
            }

            var sourceFlash = (VisualEffect)GetPrivateField((RezzingMuzzle)sourceMuzzles[0], "_flash");
            foreach (var destMuzzle in destinationMuzzles)
            {
                var destFlash = (VisualEffect)GetPrivateField((RezzingMuzzle)destMuzzle, "_flash");
                if (destFlash != null && sourceFlash != null)
                {
                    destFlash.visualEffectAsset = sourceFlash.visualEffectAsset;
                }
            }

            Debug.Log($"[updateTurret] Successfully copied muzzle effects from {keySource} to {keyDestination}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[updateTurret] Exception occurred: {ex}");
        }
    }

    public static void updateAllMunitions()
    {
        Debug.Log("[Falcata] updateAllMunitions started.");

        var munitionDictionary = (Dictionary<string, IMunition>)GetPrivateField(BundleManager.Instance, "_munitionsBySaveKey");

        if (munitionDictionary == null)
        {
            Debug.LogError("[Falcata] munitionDictionary is null.");
            return;
        }

        foreach (var key in munitionDictionary.Keys)
        {
            Debug.Log($"[Falcata] Munition key: {key}");
        }

        void Map<T>(string src, string dest) => updateMunition<T>(munitionDictionary, src, dest);

        Map<LightweightKineticShell>("Stock/450mm AP Shell", "Falcata Republic/380mm AP Shell");
        Map<LightweightExplosiveShell>("Stock/450mm HE Shell", "Falcata Republic/380mm HE Shell");
        //Map<LightweightProximityShell>("Stock/250mm HE-RPF Shell", "Falcata Republic/380mm HE-ABF Shell");
        Map<LightweightKineticShell>("Stock/120mm AP Shell", "Falcata Republic/200mm RP-HEI Shell");
        Map<LightweightKineticShell>("Stock/250mm AP Shell", "Falcata Republic/220mm AP Shell");
        Map<LightweightKineticShell>("Stock/450mm HE Shell", "Falcata Republic/700mm KART-HEI Shell");
        Map<LightweightKineticShell>("Stock/450mm AP Shell", "Falcata Republic/700mm KART-KP Shell");
        Map<LightweightKineticShell>("Stock/300mm AP Rail Sabot", "Falcata Republic/37mm Pinshot");
        Map<LightweightClusterShell>("Stock/15mm Sandshot", "Falcata Republic/37mm Sandshot");
        Map<LightweightExplosiveShell>("Stock/250mm HE Shell", "Falcata Republic/150mm HE Shell");
        Map<LightweightKineticShell>("Stock/250mm AP Shell", "Falcata Republic/150mm AP-S Shell");
        Map<LightweightAirburstShell>("Stock/100mm Flak", "Falcata Republic/150mm Flak Shell");
        Map<LightweightAirburstShell>("Stock/100mm Flak", "Falcata Republic/90mm Flak Shell");
        Map<LightweightAirburstShell>("Stock/Flak Round", "Falcata Republic/40mm Flak Shell");
        //Map<NonphysicalMunition>("Stock/20mm Slug", "Falcata Republic/30mm Slug");

        Debug.Log("[Falcata] updateAllMunitions complete.");
    }

    public static void updateMunition<T>(Dictionary<string, IMunition> dict, string keySrc, string keyDst)
    {
        Debug.Log($"[updateMunition] {keySrc} -> {keyDst}");

        if (!dict.TryGetValue(keySrc, out IMunition source))
        {
            Debug.LogError($"[updateMunition] Source munition not found: {keySrc}");
            return;
        }

        if (!dict.TryGetValue(keyDst, out IMunition dest))
        {
            Debug.LogError($"[updateMunition] Destination munition not found: {keyDst}");
            return;
        }

        try
        {
            T typedSrc = (T)source;
            T typedDst = (T)dest;

            SetPrivateField(typedDst, "_effectsGroups", GetPrivateField(typedSrc, "_effectsGroups"));

            var tracerSrc = GetPrivateField(typedSrc, "_tracerEffect");
            var tracerDst = GetPrivateField(typedDst, "_tracerEffect");

            if (tracerSrc != null && tracerDst != null)
            {
                var vfxAsset = (VisualEffectAsset)GetPrivateField(tracerSrc, "_effectAsset");
                SetPrivateField(tracerDst, "_effectAsset", vfxAsset);
                SetPrivateField(typedDst, "_tracerEffect", tracerDst);
            }

            Debug.Log("[updateMunition] Success.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[updateMunition] Exception: {ex}");
        }
    }

    public static object GetPrivateField(object instance, string fieldName)
    {
        if (instance == null)
        {
            Debug.LogError($"[GetPrivateField] instance is null for field '{fieldName}'");
            return null;
        }

        Type type = instance.GetType();

        while (type != null)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(instance);
            }
            type = type.BaseType;
        }

        Debug.LogWarning($"[GetPrivateField] Field '{fieldName}' not found in type hierarchy.");
        return null;
    }

    public static void SetPrivateField(object instance, string fieldName, object value)
    {
        if (instance == null)
        {
            Debug.LogError($"[SetPrivateField] instance is null. Field: {fieldName}");
            return;
        }

        Type type = instance.GetType();
        while (type != null)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(instance, value);
                Debug.Log($"[SetPrivateField] Set {fieldName} = {value}");
                return;
            }
            type = type.BaseType;
        }

        Debug.LogWarning($"[SetPrivateField] Field '{fieldName}' not found in type hierarchy.");
    }
}