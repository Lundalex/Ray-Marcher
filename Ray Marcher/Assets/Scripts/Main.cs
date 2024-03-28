using UnityEngine;
using Unity.Mathematics;
using System;

// Import utils from Resources.cs
using Resources;

public class Main : MonoBehaviour
{
    [Header("Render settings")]
    public float fieldOfView;
    public int2 Resolution;
    public int3 NoiseResolution;
    public int NoiseCellSize;

    [Header("RM settings")]
    public int MaxStepCount;
    public int RaysPerPixel;
    public float HitThreshold;
    [Range(0.0f, 1.0f)] public float ScatterProbability;
    [Range(0.0f, 2.0f)] public float DefocusStrength;
    public float focalPlaneFactor; // focalPlaneFactor must be positive
    public float MaxStepSize;
    public int FrameCount;
    [Range(1, 1000)] public int ChunksPerObject;

    [Header("Scene settings")]
    public float3 MinWorldBounds;
    public float3 MaxWorldBounds;
    public float CellSize;

    [Header("Scene objects")]
    public bool RenderTris;
    public float3 OBJ_Pos;
    public float3 OBJ_Rot;
    public float4[] SpheresInput; // xyz: pos; w: radii
    public float4[] MatTypesInput1; // xyz: emissionColor; w: emissionStrength
    public float4[] MatTypesInput2; // x: smoothness

    [Header("References")]
    public ComputeShader rmShader;
    public ComputeShader pcShader;
    public ComputeShader ssShader;
    public ComputeShader ngShader;
    [NonSerialized] public RenderTexture renderTexture; // Texture drawn to screen
    [NonSerialized] public RenderTexture T_VectorMap;
    [NonSerialized] public RenderTexture T_PerlinNoise;
    [NonSerialized] public RenderTexture T_PointsMap;
    [NonSerialized] public RenderTexture T_VoronoiNoise;
    public ShaderHelper shaderHelper;
    public Mesh testMesh;

    // Shader settings
    private int rmShaderThreadSize = 8; // /32
    private int pcShaderThreadSize = 512; // / 1024
    private int ssShaderThreadSize = 512; // / 1024
    private int ngShaderThreadSize = 8; // /~10
    private int Stride_TriObject = sizeof(float) * 10 + sizeof(int) * 2;
    private int Stride_Tri = sizeof(float) * 12 + sizeof(int) * 2;
    private int Stride_Sphere = sizeof(float) * 4 + sizeof(int) * 1;
    private int Stride_Material = sizeof(float) * 8 + sizeof(int) * 0;

    // Non-inpector-accessible variables

    // Scene objects
    public TriObject[] TriObjects;
    public Tri[] Tris;
    public Sphere[] Spheres;
    public Material2[] Materials;
    public ComputeBuffer B_TriObjects;
    public ComputeBuffer B_Tris;
    public ComputeBuffer B_Spheres;
    public ComputeBuffer B_Materials;

    // Spatial sort
    public ComputeBuffer B_SpatialLookup;
    public ComputeBuffer B_StartIndices;
    public ComputeBuffer AC_OccupiedChunks;
    public ComputeBuffer CB_A;
    private bool ProgramStarted = false;
    private bool SettingsChanged = true;
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;

    // Constants calculated at start
    [NonSerialized] public int NumObjects;
    [NonSerialized] public int NumSpheres;
    [NonSerialized] public int NumTriObjects;
    [NonSerialized] public int NumTris;
    [NonSerialized] public int NumObjects_NextPow2;
    [NonSerialized] public int4 NumChunks;
    [NonSerialized] public int NumChunksAll;
    [NonSerialized] public float3 ChunkGridOffset;

    void Start()
    {
        Camera.main.cullingMask = 0;
        FrameCount = 0;
        lastCameraPosition = transform.position;

        SetSceneObjects();
        LoadOBJ();

        SetConstants();

        InitBuffers();

        // PreCalc
        shaderHelper.SetPCShaderBuffers(pcShader);

        // SpatialSort
        shaderHelper.SetSSSettings(ssShader);
        shaderHelper.SetPCSettings(pcShader);

        // RayMarcher
        shaderHelper.UpdateRMVariables(rmShader);
        shaderHelper.SetRMSettings(rmShader);

        // NoiseGenerator
        shaderHelper.SetNGShaderBuffers(ngShader);
        shaderHelper.SetNGSettings(ngShader);

        RunNGShader(); // NoiseGenerator

        ProgramStarted = true;
    }
    
    void LoadOBJ()
    {
        Vector3[] vertices = testMesh.vertices;
        int[] triangles = testMesh.triangles;
        int triNum = triangles.Length / 3;

        // Set Tris data
        Tris = new Tri[triNum];
        for (int triCount = 0; triCount < triNum; triCount++)
        {
            int triCount3 = 3 * triCount;
            int indexA = triangles[triCount3];
            int indexB = triangles[triCount3 + 1];
            int indexC = triangles[triCount3 + 2];

            Tris[triCount] = new Tri
            {
                vA = vertices[indexA] * 1.5f,
                vB = vertices[indexB] * 1.5f,
                vC = vertices[indexC] * 1.5f,
                normal = new float3(0.0f, 0.0f, 0.0f), // init data
                materialKey = 0,
                parentKey = 0,
            };
        }
        B_Tris ??= new ComputeBuffer(Tris.Length, Stride_Tri);
        B_Tris.SetData(Tris);

        SetTriObjectData();
    }

    void SetConstants()
    {
        NumSpheres = Spheres.Length;
        NumTris = Tris.Length;
        NumObjects = NumSpheres + NumTris;
        NumObjects_NextPow2 = Func.NextPow2(NumObjects);

        NumTriObjects = TriObjects.Length;

        float3 ChunkGridDiff = MaxWorldBounds - MinWorldBounds;
        NumChunks = new(Mathf.CeilToInt(ChunkGridDiff.x / CellSize),
                        Mathf.CeilToInt(ChunkGridDiff.y / CellSize),
                        Mathf.CeilToInt(ChunkGridDiff.z / CellSize), 0);
        NumChunks.w = NumChunks.x * NumChunks.y;
        NumChunksAll = NumChunks.x * NumChunks.y * NumChunks.z;

        ChunkGridOffset = new float3(
            Mathf.Max(-MinWorldBounds.x, 0.0f),
            Mathf.Max(-MinWorldBounds.y, 0.0f),
            Mathf.Max(-MinWorldBounds.z, 0.0f)
        );
    }

    void InitBuffers()
    {
        B_SpatialLookup ??= new ComputeBuffer(Func.NextPow2(NumObjects * ChunksPerObject), sizeof(int) * 2);
        B_StartIndices ??= new ComputeBuffer(NumChunksAll, sizeof(int));

        AC_OccupiedChunks ??= new ComputeBuffer(Func.NextPow2(NumObjects * ChunksPerObject), sizeof(int) * 2, ComputeBufferType.Append);
        CB_A = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        T_VectorMap = Init.CreateTexture(NoiseResolution / NoiseCellSize, 3);
        T_PerlinNoise = Init.CreateTexture(NoiseResolution, 1);

        T_PointsMap = Init.CreateTexture(NoiseResolution / NoiseCellSize, 3);
        T_VoronoiNoise = Init.CreateTexture(NoiseResolution, 1);
    }

    void SetTriObjectData()
    {
        // Set new TriObjects data
        TriObjects = new TriObject[1];
        for (int i = 0; i < TriObjects.Length; i++)
        {
            TriObjects[i] = new TriObject
            {
                pos = OBJ_Pos,
                rot = OBJ_Rot,
                lastRot = 0,
                containedRadius = 0.0f,
                triStart = 0,
                triEnd = NumTris - 1,
            };
        }

        // Fill in relevant previous TriObjects data
        if (NumTriObjects != 0)
        {
            TriObject[] LastTriObjects = new TriObject[NumTriObjects];
            B_TriObjects.GetData(LastTriObjects);

            for (int i = 0; i < TriObjects.Length; i++)
            {
                TriObjects[i].lastRot = LastTriObjects[i].lastRot;
            }
        }

        B_TriObjects ??= new ComputeBuffer(TriObjects.Length, Stride_TriObject);
        B_TriObjects.SetData(TriObjects);
    }

    void Update()
    {
        shaderHelper.UpdateRMVariables(rmShader);
        shaderHelper.UpdateNGVariables(ngShader);
    }

    void LateUpdate()
    {
        if (transform.position != lastCameraPosition || transform.rotation != lastCameraRotation)
        {
            FrameCount = 0;

            SetTriObjectData();
            SetSceneObjects();
            shaderHelper.SetRMSettings(rmShader);

            lastCameraPosition = transform.position;
            lastCameraRotation = transform.rotation;
        }
    }

    private void OnValidate()
    {
        if (ProgramStarted)
        {
            FrameCount = 0;

            SetTriObjectData();
            SetSceneObjects();
            shaderHelper.SetRMSettings(rmShader);

            SettingsChanged = true;
        }
    }

    void SetSceneObjects()
    {
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
        B_Spheres ??= new ComputeBuffer(SpheresInput.Length, Stride_Sphere);
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
        B_Materials ??= new ComputeBuffer(MatTypesInput1.Length, Stride_Material);
        B_Materials.SetData(Materials);
    }

    void RunRMShader()
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(Resolution.x, Resolution.y, 24)
            {
                enableRandomWrite = true
            };
            renderTexture.Create();

            // Set texture in shader
            rmShader.SetTexture(0, "Result", renderTexture);
            rmShader.SetTexture(1, "Result", renderTexture);
        }

        shaderHelper.DispatchKernel(rmShader, "TraceRays", Resolution, rmShaderThreadSize);

        shaderHelper.DispatchKernel(rmShader, "RenderNoiseTextures", Resolution, rmShaderThreadSize);
    }
    
    void RunSSShader()
    {
        // Fill OccupiedChunks
        AC_OccupiedChunks.SetCounterValue(0);
        shaderHelper.DispatchKernel(ssShader, "CalcSphereChunkKeys", NumSpheres, ssShaderThreadSize);
        if (RenderTris) { shaderHelper.DispatchKernel(ssShader, "CalcTriChunkKeys", NumTris, ssShaderThreadSize); } 

        // Get OccupiedChunks length
        // THIS IS QUITE EXPENSIVE SINCE IT REQUIRES DATA TO BE SENT FROM THE GPU TO THE CPU!
        ComputeBuffer.CopyCount(AC_OccupiedChunks, CB_A, 0);
        int[] OC_lenArr = new int[1];
        CB_A.GetData(OC_lenArr);
        int OC_len = Func.NextPow2(OC_lenArr[0]); // NextPow2() since bitonic merge sort requires pow2 array length

        ssShader.SetInt("OC_len", OC_len);

        // Copy OccupiedChunks -> SpatialLookup
        shaderHelper.DispatchKernel(ssShader, "PopulateSpatialLookup", OC_len, ssShaderThreadSize);

        // Sort SpatialLookup
        int basebBlockLen = 2;
        while (basebBlockLen != 2*OC_len) // basebBlockLen == len is the last outer iteration
        {
            int blockLen = basebBlockLen;
            while (blockLen != 1) // blockLen == 2 is the last inner iteration
            {
                bool brownPinkSort = blockLen == basebBlockLen;

                shaderHelper.UpdateSortIterationVariables(ssShader, blockLen, brownPinkSort);

                shaderHelper.DispatchKernel(ssShader, "SortIteration", OC_len / 2, ssShaderThreadSize);

                blockLen /= 2;
            }
            basebBlockLen *= 2;
        }

        // Set StartIndices
        shaderHelper.DispatchKernel(ssShader, "PopulateStartIndices", OC_len, ssShaderThreadSize);

        // int2[] t_A = new int2[OC_len];
        // B_SpatialLookup.GetData(t_A);
        
        // int[] t_B = new int[NumChunksAll];
        // B_StartIndices.GetData(t_B);

    }

    void RunPCShader()
    {
        shaderHelper.DispatchKernel(pcShader, "CalcTriNormals", NumTris, pcShaderThreadSize);
        shaderHelper.DispatchKernel(pcShader, "SetLastRots", NumTriObjects, pcShaderThreadSize);
    }

    void RunNGShader()
    {
        // PERLIN_3D -> PerlinNoise
        int MaxNoiseCellSize = NoiseCellSize;
        int NumPasses = (int)Mathf.Log(MaxNoiseCellSize, 2);

        ngShader.SetInt("NumPasses", NumPasses);
        ngShader.SetInt("MaxNoiseCellSize", MaxNoiseCellSize);

        int NoiseCellSize2 = MaxNoiseCellSize*2;
        for (int pass = 0; pass < NumPasses; pass++)
        {
            NoiseCellSize2 /= 2;
            ngShader.SetInt("NoiseCellSize", NoiseCellSize2);
            ngShader.SetInt("PassCount", pass);

            shaderHelper.DispatchKernel(ngShader, "GenerateVectorMap", NoiseResolution / NoiseCellSize2, ngShaderThreadSize);
            shaderHelper.DispatchKernel(ngShader, "Perlin", NoiseResolution, ngShaderThreadSize);
        }

        // VORONOI_3D -> VoronoiNoise
        shaderHelper.DispatchKernel(ngShader, "GeneratePointsMap", NoiseResolution / NoiseCellSize, ngShaderThreadSize);
        shaderHelper.DispatchKernel(ngShader, "Voronoi", NoiseResolution, ngShaderThreadSize);
    }

    public void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // Main program loop
        if (SettingsChanged) { RunPCShader(); SettingsChanged = false; } // PreCalc
        RunSSShader(); // SpatialSort
        RunRMShader(); // RayMarcher
        // RunNGShader() located in Start()

        Graphics.Blit(renderTexture, dest);
    }

    void OnDestroy()
    {
        // Scene objects
        B_TriObjects?.Release();
        B_Tris?.Release();
        B_Spheres?.Release();
        B_Materials?.Release();

        // Spatial sort
        B_SpatialLookup?.Release();
        B_StartIndices?.Release();
        AC_OccupiedChunks?.Release();
        CB_A?.Release();
    }
}