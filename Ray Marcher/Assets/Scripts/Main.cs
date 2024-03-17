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
    public int DefocusStrength;
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
    private OpaqueSphere[] OpaqueSpheres;
    private OpaqueMaterial[] OpaqueMaterials;
    public ComputeBuffer OpaqueSphereBuffer;
    public ComputeBuffer OpaqueMaterialsBuffer;

    private bool ProgramStarted = false;
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;

    void Start()
    {
        Camera.main.cullingMask = 0;
        
        lastCameraPosition = transform.position;

        OpaqueSphereBuffer = new ComputeBuffer(SpheresInput.Length, sizeof(float) * 4 + sizeof(int) * 1);
        OpaqueMaterialsBuffer = new ComputeBuffer(MatTypesInput1.Length, sizeof(float) * 8 + sizeof(int) * 0);

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

        rmShader.SetInt("SpheresNum", OpaqueSpheres.Length);

        int[] resolutionArray = new int[] { Resolution.x, Resolution.y };
        rmShader.SetInts("Resolution", resolutionArray);

        // Ray setup settings
        rmShader.SetInt("MaxStepCount", MaxStepCount);
        rmShader.SetInt("RaysPerPixel", RaysPerPixel);
        rmShader.SetInt("DefocusStrength", DefocusStrength);

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
        // Set spheres data
        OpaqueSpheres = new OpaqueSphere[SpheresInput.Length];
        for (int i = 0; i < OpaqueSpheres.Length; i++)
        {
            OpaqueSpheres[i] = new OpaqueSphere
            {
                position = new float3(SpheresInput[i].x, SpheresInput[i].y, SpheresInput[i].z),
                radius = SpheresInput[i].w,
                materialFlag = i == 0 ? 1 : 0
            };
        }
        OpaqueSphereBuffer.SetData(OpaqueSpheres);

        // Set Material2 types data
        OpaqueMaterials = new OpaqueMaterial[MatTypesInput1.Length];
        for (int i = 0; i < OpaqueMaterials.Length; i++)
        {
            OpaqueMaterials[i] = new OpaqueMaterial
            {
                color = new float3(MatTypesInput1[i].x, MatTypesInput1[i].y, MatTypesInput1[i].z),
                specularColor = new float3(1, 1, 1), // Specular color is currently set to white for all Material2 types
                brightness = MatTypesInput1[i].w,
                smoothness = MatTypesInput2[i].x
            };
        }
        OpaqueMaterialsBuffer.SetData(OpaqueMaterials);
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
        OpaqueSphereBuffer?.Release();
        OpaqueMaterialsBuffer?.Release();
    }
}