using HarmonyLib;

using Networking;
using UnityEngine;

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

using Bundles;

using Game;
using Game.UI;

using Modding;

using Missions;
using Missions.Nodes;
using Missions.Nodes.Scenario;
using Missions.Nodes.Sequenced;
using Utility;

using XNode;


public class ScenarioEntryPoint : IModEntryPoint
{
    public void PreLoad()
    {
        Debug.Log("[Falcata] PreLoad started.");

        try
        {
            MethodInfo cachePortsMethodInfo = typeof(NodeDataCache)
                .GetMethod("CachePorts", BindingFlags.NonPublic | BindingFlags.Static);

            if (cachePortsMethodInfo == null)
            {
                Debug.LogError("[Falcata] ERROR: CachePorts method not found! XNode may have changed!");
                return;
            }

            Debug.Log("[Falcata] PreLoad completed successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Falcata] Exception during PreLoad: {ex}");
        }
    }

    public void PostLoad()
    {
        Debug.Log("[Falcata] PostLoad started.");

        try
        {
            PreCacheXmlSerializers();

            Harmony harmony = new Harmony("nebulous.Falcata");
            harmony.PatchAll();
            Debug.Log("[Falcata] Harmony PatchAll complete.");

            UpdateAllNodes();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Falcata] Exception in PostLoad(): {e}");
        }
    }

    // --------------------------------------------------------
    // UpdateAllNodes — Now Fully Safe
    // --------------------------------------------------------
    public static void UpdateAllNodes()
    {
        Debug.Log("[Falcata] Updating all scenario nodes...");

        Dictionary<string, ScenarioGraph> scenariosByName;

        try
        {
            scenariosByName = GetScenariosByName();
        }
        catch (Exception ex)
        {
            Debug.LogError("[Falcata] Failed to get scenario list: " + ex);
            return;
        }

        ScenarioGraph scenarioControl = GetScenarioSafe(scenariosByName, "Control");
        ScenarioGraph scenarioCommand = GetScenarioSafe(scenariosByName, "Command");
        //ScenarioGraph scenarioSupremacy = GetScenarioSafe(scenariosByName, "Supremacy");

        if (scenarioControl == null)
        {
            Debug.LogError("[Falcata] ERROR: Required scenario 'Control' was not found. Aborting UpdateAllNodes.");
            return;
        }

        // Safe Get UI
        ObjectiveStatusDisplay osdControl = SafeGetObjectiveStatusDisplay(scenarioControl, "Control");

        SafeSetObjectiveStatusDisplay(scenarioCommand, osdControl, "Command");
        //SafeSetObjectiveStatusDisplay(scenarioSupremacy, osdControl, "Supremacy");

        // Safe Copy CapturePoint Prefab
        GameObject capturePointPrefab = SafeGetCapturePointPrefab(scenarioControl, "Control");

        SafeSetCapturePointPrefab(scenarioCommand, capturePointPrefab, "Command");
        //SafeSetCapturePointPrefab(scenarioSupremacy, capturePointPrefab, "Supremacy");

        Debug.Log("[Falcata] UpdateAllNodes() finished.");
    }
    
    // --------------------------------------------------------
    // SAFE HELPERS — Never throw, always log
    // --------------------------------------------------------

    private static ScenarioGraph GetScenarioSafe(Dictionary<string, ScenarioGraph> dict, string key)
    {
        if (!dict.TryGetValue(key, out ScenarioGraph graph))
        {
            Debug.LogWarning($"[Falcata] Scenario '{key}' not found.");
            return null;
        }

        if (graph == null)
            Debug.LogWarning($"[Falcata] Scenario '{key}' was found but is NULL!");

        return graph;
    }

    private static ObjectiveStatusDisplay SafeGetObjectiveStatusDisplay(ScenarioGraph graph, string name)
    {
        if (graph == null)
        {
            Debug.LogWarning($"[Falcata] Cannot get ObjectiveStatusDisplay: scenario '{name}' is null.");
            return null;
        }

        try
        {
            ObjectiveStatusDisplay prefab = GetObjectiveStatusDisplay(graph);

            if (prefab == null)
                Debug.LogWarning($"[Falcata] Scenario '{name}' returned NULL ObjectiveStatusDisplay!");

            return prefab;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Falcata] ERROR getting ObjectiveStatusDisplay for '{name}': {e}");
            return null;
        }
    }

    private static void SafeSetObjectiveStatusDisplay(ScenarioGraph graph, ObjectiveStatusDisplay prefab, string name)
    {
        if (graph == null)
        {
            Debug.LogWarning($"[Falcata] Cannot set ObjectiveStatusDisplay for '{name}' — scenario null.");
            return;
        }

        if (prefab == null)
        {
            Debug.LogWarning($"[Falcata] Not assigning NULL UI to '{name}'. Operation skipped.");
            return;
        }

        try
        {
            SetObjectiveStatusDisplay(graph, prefab);
            Debug.Log($"[Falcata] Assigned ObjectiveStatusDisplay to {name}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Falcata] ERROR setting ObjectiveStatusDisplay for '{name}': {e}");
        }
    }

    private static GameObject SafeGetCapturePointPrefab(ScenarioGraph graph, string name)
    {
        if (graph == null)
        {
            Debug.LogWarning($"[Falcata] Cannot get CapturePoint Prefab: scenario '{name}' is null.");
            return null;
        }

        try
        {
            GameObject prefab = GetCreateCapturePointPrefab(graph);

            if (prefab == null)
                Debug.LogWarning($"[Falcata] CapturePoint Prefab for '{name}' is NULL!");

            return prefab;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Falcata] ERROR getting CapturePoint Prefab from '{name}': {e}");
            return null;
        }
    }

    private static void SafeSetCapturePointPrefab(ScenarioGraph graph, GameObject prefab, string name)
    {
        if (graph == null)
        {
            Debug.LogWarning($"[Falcata] Cannot set CapturePoint Prefab for '{name}' — scenario null.");
            return;
        }

        if (prefab == null)
        {
            Debug.LogWarning($"[Falcata] Not assigning NULL CapturePoint Prefab to '{name}'. Operation skipped.");
            return;
        }

        try
        {
            SetCreateCapturePointPrefab(graph, prefab);
            Debug.Log($"[Falcata] Assigned CapturePoint Prefab to {name}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Falcata] ERROR setting CapturePoint Prefab for '{name}': {e}");
        }
    }

    // --------------------------------------------------------
    // ORIGINAL METHODS (unchanged)
    // --------------------------------------------------------

    public static ObjectiveStatusDisplay GetObjectiveStatusDisplay(ScenarioGraph scenarioGraph)
    {
        foreach (XNode.Node node in scenarioGraph.nodes)
        {
            if (node is SetObjectiveOverviewUI ui)
                return ui.Prefab;
        }

        throw new InvalidOperationException("ScenarioGraph missing SetObjectiveOverviewUI node");
    }

    public static void SetObjectiveStatusDisplay(ScenarioGraph scenarioGraph, ObjectiveStatusDisplay prefab)
    {
        foreach (XNode.Node node in scenarioGraph.nodes)
        {
            if (node is SetObjectiveOverviewUI ui)
                ui.Prefab = prefab;
        }
    }

    public static GameObject GetCreateCapturePointPrefab(ScenarioGraph scenarioGraph)
    {
        foreach (XNode.Node node in scenarioGraph.nodes)
        {
            if (node is CreateCapturePoint ccp)
                return ccp.Prefab;
        }

        throw new InvalidOperationException("ScenarioGraph missing CreateCapturePoint node");
    }

    public static void SetCreateCapturePointPrefab(ScenarioGraph scenarioGraph, GameObject prefab)
    {
        foreach (XNode.Node node in scenarioGraph.nodes)
        {
            if (node is CreateCapturePoint ccp)
                ccp.Prefab = prefab;
        }
    }

    // --------------------------------------------------------
    // Scenario lookup helpers, reflection helpers, serializers
    // (unchanged, but wrapped in try-catch when used)
    // --------------------------------------------------------

    public static Dictionary<string, ScenarioGraph> GetScenariosByName()
    {
        List<ScenarioGraph> scenarioList = (List<ScenarioGraph>)GetPrivateField(BundleManager.Instance, "_scenarios");
        Dictionary<string, ScenarioGraph> byName = new Dictionary<string, ScenarioGraph>();

        foreach (ScenarioGraph scenario in scenarioList)
        {
            byName.Add(scenario.ScenarioName, scenario);
        }

        Debug.Log($"[Falcata] Loaded {byName.Count} scenarios.");
        return byName;
    }

    public static object GetPrivateField(object instance, string fieldName)
    {
        static object Internal(object instance, string fieldName, Type type)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                return field.GetValue(instance);

            return type.BaseType != null ? Internal(instance, fieldName, type.BaseType) : null;
        }

        return Internal(instance, fieldName, instance.GetType());
    }

    private static void PreCacheXmlSerializers()
    {
        Debug.Log("[Falcata] Pre-caching XML serializers...");
        GetXmlSerializer(typeof(SavedSkirmishGame), new Type[] { typeof(SavedNodeState) });
    }

    public static Dictionary<Type, XmlSerializer> generatedSerializers = new Dictionary<Type, XmlSerializer>();

    public static XmlSerializer GetXmlSerializer(Type serializerType, IEnumerable<Type> parentExtraTypes)
    {
        if (generatedSerializers.TryGetValue(serializerType, out XmlSerializer existing))
            return existing;

        List<Type> extra = new List<Type>();

        foreach (Type t in parentExtraTypes)
        {
            var children = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(x => t.IsAssignableFrom(x));

            extra.AddRange(children);
        }

        XmlSerializer serializer = new XmlSerializer(serializerType, extra.ToArray());
        generatedSerializers[serializerType] = serializer;
        Debug.Log($"[Falcata] Cached XML Serializer for {serializerType.Name}");

        return serializer;
    }
}