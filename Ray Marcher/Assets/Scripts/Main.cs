using UnityEngine;
using Unity.Mathematics;

// Import utils from Resources.cs
using Resources;
// Usage: Utils.(functionName)()

public class Main : MonoBehaviour
{
    [Header("Render settings")]
    public float fieldOfView;
    public int2 Resolution;

    [Header("RM settings")]
    public int MaxStepCount;
    public int RaysPerPixel;
    public float HitThreshold;
    [Range(0.0f, 1.0f)] public float ScatterProbability;
    [Range(0.0f, 2.0f)] public float DefocusStrength;
    public float focalPlaneFactor; // focalPlaneFactor must be positive
    public int FrameCount;

    [Header("Scene")]
    public float4[] SpheresInput; // xyz: pos; w: radii
    public float4[] MatTypesInput1; // xyz: emissionColor; w: emissionStrength
    public float4[] MatTypesInput2; // x: smoothness

    [Header("References")]
    public ComputeShader rmShader;
    public ShaderHelper shaderHelper;

    // Private variables
    private RenderTexture renderTexture;
    private int RayTracerThreadSize = 16; // /32
    private int Stride_TriObject = sizeof(float) * 4 + sizeof(int) * 2;
    private int Stride_Tri = sizeof(float) * 12 + sizeof(int) * 2;
    private int Stride_Sphere = sizeof(float) * 4 + sizeof(int) * 1;
    private int Stride_Material = sizeof(float) * 8 + sizeof(int) * 0;
    private TriObject[] TriObjects;
    private Tri[] Tris;
    private Sphere[] Spheres;
    private Material2[] Materials;
    public ComputeBuffer B_TriObjects;
    public ComputeBuffer B_Tris;
    public ComputeBuffer B_Spheres;
    public ComputeBuffer B_Materials;

    private bool ProgramStarted = false;
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;

    void Start()
    {
        Camera.main.cullingMask = 0;
        
        lastCameraPosition = transform.position;

        B_TriObjects = new ComputeBuffer(SpheresInput.Length, Stride_TriObject);
        B_Tris = new ComputeBuffer(MatTypesInput1.Length, Stride_Tri);
        B_Spheres = new ComputeBuffer(SpheresInput.Length, Stride_Sphere);
        B_Materials = new ComputeBuffer(MatTypesInput1.Length, Stride_Material);

        UpdateSetData();

        shaderHelper.SetRMShaderBuffers(rmShader);

        UpdatePerFrame();
        UpdateSettings();

        ProgramStarted = true;
    }

    void Update()
    {
        UpdatePerFrame();
    }

    void LateUpdate()
    {
        if (transform.position != lastCameraPosition || transform.rotation != lastCameraRotation)
        {
            UpdateSettings();
            lastCameraPosition = transform.position;
            lastCameraRotation = transform.rotation;
        }
    }

    void UpdatePerFrame()
    {
        // Frame set variables
        int FrameRand = UnityEngine.Random.Range(0, 999999);
        rmShader.SetInt("FrameRand", FrameRand);
        rmShader.SetInt("FrameCount", FrameCount++);

        // Camera position
        float3 worldSpaceCameraPos = transform.position;
        float[] worldSpaceCameraPosArray = new float[] { worldSpaceCameraPos.x, worldSpaceCameraPos.y, worldSpaceCameraPos.z };
        rmShader.SetFloats("WorldSpaceCameraPos", worldSpaceCameraPosArray);

        // Camera orientation
        float3 cameraRot = transform.rotation.eulerAngles;
        float[] cameraRotArray = new float[] { cameraRot.x, cameraRot.y, cameraRot.z };
        rmShader.SetFloats("CameraRotation", Func.DegreesToRadians(cameraRotArray));
    }

    private void OnValidate()
    {
        if (ProgramStarted)
        {
            UpdateSettings();
        }
    }

    void UpdateSettings()
    {
        FrameCount = 0;
        UpdateSetData();

        shaderHelper.SetRMShaderBuffers(rmShader);

        rmShader.SetInt("NumTriObjects", TriObjects.Length);
        rmShader.SetInt("NumTris", Tris.Length);
        rmShader.SetInt("NumSpheres", Spheres.Length);
        rmShader.SetInt("NumMaterials", Materials.Length);

        int[] resolutionArray = new int[] { Resolution.x, Resolution.y };
        rmShader.SetInts("Resolution", resolutionArray);

        // Ray setup settings
        rmShader.SetInt("MaxStepCount", MaxStepCount);
        rmShader.SetInt("RaysPerPixel", RaysPerPixel);

        rmShader.SetFloat("HitThreshold", HitThreshold);

        rmShader.SetFloat("ScatterProbability", ScatterProbability);
        rmShader.SetFloat("DefocusStrength", DefocusStrength);

        // Screen settings
        float aspectRatio = Resolution.x / Resolution.y;
        float fieldOfViewRad = fieldOfView * Mathf.PI / 180;
        float viewSpaceHeight = Mathf.Tan(fieldOfViewRad * 0.5f);
        float viewSpaceWidth = aspectRatio * viewSpaceHeight;
        rmShader.SetFloat("viewSpaceWidth", viewSpaceWidth);
        rmShader.SetFloat("viewSpaceHeight", viewSpaceHeight);

        rmShader.SetFloat("focalPlaneFactor", focalPlaneFactor);
    }

    void UpdateSetData()
    {
        // Set TriObjects data
        TriObjects = new TriObject[1];
        for (int i = 0; i < TriObjects.Length; i++)
        {
            TriObjects[i] = new TriObject
            {
                pos = new float3(2.0f, 5.0f, 0.0f),
                containedRadius = 0.0f,
                triStart = 0,
                triEnd = 0,
            };
        }
        B_TriObjects.SetData(TriObjects);

        // Set Tris data
        Tris = new Tri[1];
        for (int i = 0; i < Tris.Length; i++)
        {
            Tris[i] = new Tri
            {
                vA = new float3(0.0f, 0.0f, 0.0f),
                vB = new float3(1.0f, 0.0f, 2.0f),
                vC = new float3(0.0f, 1.0f, 1.0f),
                normal = new float3(0.0f, 0.0f, 0.0f),
                materialKey = 0,
                parentKey = 0,
            };
        }
        B_Tris.SetData(Tris);

        // Set Spheres data
        Spheres = new Sphere[SpheresInput.Length];
        for (int i = 0; i < Spheres.Length; i++)
        {
            Spheres[i] = new Sphere
            {
                pos = new float3(SpheresInput[i].x, SpheresInput[i].y, SpheresInput[i].z),
                radius = SpheresInput[i].w,
                materialKey = i == 0 ? 1 : 0,
            };
        }
        B_Spheres.SetData(Spheres);

        // Set Materials data
        Materials = new Material2[MatTypesInput1.Length];
        for (int i = 0; i < Materials.Length; i++)
        {
            Materials[i] = new Material2
            {
                color = new float3(MatTypesInput1[i].x, MatTypesInput1[i].y, MatTypesInput1[i].z),
                specularColor = new float3(1, 1, 1), // Specular color is currently set to white for all Material2 types
                brightness = MatTypesInput1[i].w,
                smoothness = MatTypesInput2[i].x
            };
        }
        B_Materials.SetData(Materials);
    }

    void RunRenderShader()
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(Resolution.x, Resolution.y, 24)
            {
                enableRandomWrite = true
            };
            renderTexture.Create();
        }

        rmShader.SetTexture(0, "Result", renderTexture);
        int2 threadGroupNums = Utils.GetThreadGroupsNumsXY(Resolution, RayTracerThreadSize);
        rmShader.Dispatch(0, threadGroupNums.x, threadGroupNums.y, 1);
    }

    public void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        RunRenderShader();

        Graphics.Blit(renderTexture, dest);
    }

    void OnDestroy()
    {
        B_TriObjects?.Release();
        B_Tris?.Release();
        B_Spheres?.Release();
        B_Materials?.Release();
    }
}