




using Game.Sensors;
using UnityEditor;
using UnityEngine;
using Utility;
using System.Reflection;
using System;
using static Networking.SkirmishDedicatedServerConfig;
using UnityEngine.InputSystem.HID;

[ExecuteInEditMode]
public class CustomSignature : Signature
{

    protected override void Awake()
    {
        mesh = new Mesh();
        IcoSphere.Create(mesh, 1f, CrossSection.IcosphereRecursions);

        base.Awake();
    }
    public enum BakeMode
    {
        Vanilla,
        Tartiflette,
        AGM
    }

    public BakeMode bakeMode = BakeMode.Vanilla;
    public bool ContinouslyRecalculateRCS = true;
    public bool SimulateSingleBake = true;
    public float MaxRCS = 1;
    public float MinRCS = 1;
    public float SuggestedMinRCS = 1;
    public float RadiusMultiplier = 30;
    public int Steps = 60;
    private GameObject radarsource = null;
    private Mesh mesh;
    float radius => RadiusMultiplier;
    [ContextMenu("Generate XSection Sampler")]

    public float TartReturn(Ray r)
    {
        if (Physics.Raycast(r, out RaycastHit hit, radius * 2.5f, SpecialLayers.Mask_Default, QueryTriggerInteraction.Ignore))
            return 1.0f - Mathf.Abs(Vector3.Dot(r.direction, hit.normal));
        return 1;
    }
    public float VanillaReturn(Ray r)
    {
        if (Physics.Raycast(r, out RaycastHit hit, radius * 2.5f, SpecialLayers.Mask_Default, QueryTriggerInteraction.Ignore))
            return 1.0f - Mathf.Abs(Vector3.Dot(r.direction, hit.normal));
        return 0.0f;
    }
    public float AGMReturn(Ray r)
    {
        if (Physics.Raycast(r, out RaycastHit hit, radius * 2.5f, SpecialLayers.Mask_Default, QueryTriggerInteraction.Ignore))
            return Mathf.Abs(Vector3.Dot(r.direction, hit.normal));
        return 0.0f;
    }


    public float VanillaSum(float dotSum)
    {
        return 1.0f - (dotSum / (Steps * Steps));
    }
    public float AGMSum(float dotSum)
    {
        return dotSum;
    }
    public void GenerateCrossSectionSampler()
    {
        CalculateSigSize(out float largest, out float smallest, out float _);
        MaxRCS = largest * 10;
        MinRCS = smallest * 10;
        Vector2[] uv = mesh.uv;
        IcoSphere.Create(mesh, 1f, CrossSection.IcosphereRecursions);
        for (int i = 0; i < mesh.vertexCount; ++i)
        {
            Vector3 normal = mesh.normals[i];
            if (Mathf.Abs(normal.x) < 0.01f && Mathf.Abs(normal.z) < 0.01f)
                normal = new Vector3(0.01f, 1, 0.01f);
            Vector3 direction = -normal.normalized;
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * (radius / (float)Steps);
            Vector3 up = Vector3.Cross(right, direction).normalized * (radius / (float)Steps);
            Vector3 start = (normal * radius) - (right.normalized * radius / 2f) - (up.normalized * radius / 2f);
            float dotSum = 0.0f;
            for (int x = 0; x < Steps; ++x)
            {
                for (int y = 0; y < Steps; ++y)
                {
                    Ray r = new Ray(start + (right * x) + (up * y), direction);
                    switch(bakeMode)
                    {
                        case BakeMode.AGM:
                            dotSum += AGMReturn(r);
                            break;
                        case BakeMode.Tartiflette:
                            dotSum += TartReturn(r);
                            break;
                        default:
                            dotSum += VanillaReturn(r);
                            break;
                    }
                }
            }
            switch (bakeMode)
            {
                case BakeMode.AGM:
                    dotSum = AGMSum(dotSum);
                    break;
                default:
                    dotSum = VanillaSum(dotSum);
                    break;
            }
            uv[i] = new Vector2(dotSum, 0f);
            //Debug.Log(mesh.normals[i] + " " + dotSum);
        }
        float minValue = 1.0f;
        float maxValue = -1.0f;
        for (int i = 0; i < uv.Length; ++i)
        {
            minValue = Mathf.Min(minValue, uv[i].x);
            maxValue = Mathf.Max(maxValue, uv[i].x);
        }
        SuggestedMinRCS = MaxRCS * (minValue / maxValue);
        for (int i = 0; i < uv.Length; ++i)
            uv[i] = new Vector2(uv[i].x.Remap(minValue, maxValue, 0.0f, 1.0f), 0f);
        mesh.uv = uv;
        SetVal(this, "_crossSectionSampler", CrossSection.CreateFromIcosohedron(mesh));
    }

    [ContextMenu("Save Sampler")]
    public void SaveSampler()
    {
        CrossSection _crossSectionSampler = GetVal<CrossSection>(this, "_crossSectionSampler");
        if (_crossSectionSampler == null)
            return;
        string path = EditorUtility.SaveFilePanel("Save Mesh", "Assets/", "Radar Cross Section", "asset");
        if (string.IsNullOrEmpty(path))
            return;
        path = FileUtil.GetProjectRelativePath(path);
        AssetDatabase.CreateAsset(_crossSectionSampler, path);
        AssetDatabase.SaveAssets();
    }

    void Update()
    {
        if(ContinouslyRecalculateRCS)
            GenerateCrossSectionSampler();
    }

    void OnRenderObject()
    {   
        if(SimulateSingleBake)
        {
            if (radarsource == null)
                radarsource = new GameObject("radarsource");
            Vector3 direction = -radarsource.transform.position.normalized;
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * (radius / (float)Steps);
            Vector3 up = Vector3.Cross(right, direction).normalized * (radius / (float)Steps);
            Vector3 start = (radarsource.transform.position.normalized * radius) - (right.normalized * radius / 2f) - (up.normalized * radius / 2f);
            //Debug.DrawLine(start, Color.blue, Time.deltaTime);
            Debug.DrawLine(start, start + (right * Steps), Color.white, Time.deltaTime);
            Debug.DrawLine(start + (up * Steps), start + (right * Steps) + (up * Steps), Color.white, Time.deltaTime);
            Debug.DrawLine(start + (up * Steps), start, Color.white, Time.deltaTime);
            Debug.DrawLine(start + (right * Steps) , start + (right * Steps) + (up * Steps), Color.white, Time.deltaTime);
            //Debug.DrawLine(start + (up * Steps), start + (right * Steps), Color.white, Time.deltaTime);
            //Debug.DrawLine(start, start + (right * Steps) + (up * Steps), Color.white, Time.deltaTime);
            for (int x = 0; x < Steps; ++x)
            {
                for (int y = 0; y < Steps; ++y)
                {
                    
                    Ray r = new Ray(start + (right * x) + (up * y), direction);

                    Color color = Color.blue;

                    if(x == 0 || y == 0 || x == Steps - 1 || y == Steps - 1)
                        color = Color.red;

                    float dot = AGMReturn(r);
                    if (Physics.Raycast(r, out RaycastHit hit, radius * 2.5f, SpecialLayers.Mask_Default, QueryTriggerInteraction.Ignore))
                    {
                        
                        DebugExtension.DebugPoint(hit.point, Color.white, 0.1f, Time.deltaTime);
                        Debug.DrawLine(hit.point, hit.point + (hit.normal) * dot, color, Time.deltaTime);
                        Debug.DrawLine(r.origin, r.origin + (r.direction) * dot, color, Time.deltaTime);
                    }
                    else
                    {
                        //DebugExtension.DebugPoint(r.origin, Color.white, 0.1f, Time.deltaTime);
                    }
                }
            }
        }
        CrossSection _crossSectionSampler = GetVal<CrossSection>(this, "_crossSectionSampler");


        if (_crossSectionSampler != null)
        {

            CalculateSigSize(out float largest, out float smallest, out float _);
            MaxRCS = largest * 10;
            MinRCS= smallest * 10;
            for (int i = 0; i < mesh.vertexCount; ++i)
            {
                float valueatpoint = _crossSectionSampler.QueryViewingAngle(mesh.vertices[i]);
                float sigatpoint = Mathf.Sqrt(Mathf.Lerp(smallest, largest, valueatpoint)) ;
                //DebugExtension.DebugWireSphere(mesh.vertices[i] * (_crossSectionSampler.QueryViewingAngle(mesh.vertices[i]) + 1) * 5, _color, duration: 1f, radius: 0.1f);
                //Debug.DrawRay(Vector3.zero, mesh.vertices[i] * (_crossSectionSampler.QueryViewingAngle(mesh.vertices[i])+1) * 5, Color.red, 1f);
                Debug.DrawLine(mesh.vertices[i] * Mathf.Sqrt(smallest) * -1f, mesh.vertices[i] * sigatpoint * -1f, new Color(0, 0.25f, 0.5f), Time.deltaTime);
                DebugExtension.DebugWireSphere(mesh.vertices[i] * sigatpoint * -1, Color.blue, duration: Time.deltaTime, radius: Mathf.Lerp(0.1f, 0.3f, valueatpoint));

            }
        }
    }

    public static T GetVal<T>(object instance, string fieldName, Type type = null)
    {
        if (type == null)
            type = instance.GetType();
        FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
            return (T)field.GetValue(instance);
        else if (type.BaseType != null)
            return (T)GetVal<T>(instance, fieldName, type.BaseType);
        return default(T);
    }
    public static void SetVal(System.Object instance, string fieldName, System.Object value, Type type = null)
    {
        if (type == null)
            type = instance.GetType();
        FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
            field.SetValue(instance, value);
        else if (type.BaseType != null)
            SetVal(instance, fieldName, value, type.BaseType);
    }

}




