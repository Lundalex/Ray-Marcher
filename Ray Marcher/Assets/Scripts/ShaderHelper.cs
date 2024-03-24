using Unity.Mathematics;
using UnityEngine;

// Import utils from Resources.cs
using Resources;
using System;
public class ShaderHelper : MonoBehaviour
{
    public Main m;

    // Method overloading - int / int2 threadsNum
    public void DispatchKernel(ComputeShader cs, string kernelName, int threadsNum, int threadSize)
    {
        int threadGroupNum = Utils.GetThreadGroupsNum(threadsNum, threadSize);
        cs.Dispatch(cs.FindKernel(kernelName), threadGroupNum, 1, 1);
    }
    public void DispatchKernel(ComputeShader cs, string kernelName, int2 threadsNum, int threadSize)
    {
        int2 threadGroupNums = Utils.GetThreadGroupsNumsXY(threadsNum, threadSize);
        cs.Dispatch(cs.FindKernel(kernelName), threadGroupNums.x, threadGroupNums.y, 1);
    }

    public void SetRMShaderBuffers (ComputeShader rmShader)
    {
        rmShader.SetBuffer(0, "TriObjects", m.B_TriObjects);
        rmShader.SetBuffer(0, "Tris", m.B_Tris);
        rmShader.SetBuffer(0, "Spheres", m.B_Spheres);
        rmShader.SetBuffer(0, "Materials", m.B_Materials);
        rmShader.SetBuffer(0, "SpatialLookup", m.B_SpatialLookup);
        rmShader.SetBuffer(0, "StartIndices", m.B_StartIndices);
    }

    public void SetPCShaderBuffers (ComputeShader pcShader)
    {
        pcShader.SetBuffer(0, "TriObjects", m.B_TriObjects);
        pcShader.SetBuffer(0, "Tris", m.B_Tris);
    }

    public void SetSSShaderBuffers (ComputeShader ssShader)
    {
        ssShader.SetBuffer(0, "Spheres", m.B_Spheres);
        ssShader.SetBuffer(0, "OccupiedChunksAPPEND", m.AC_OccupiedChunks);

        ssShader.SetBuffer(1, "TriObjects", m.B_TriObjects);
        ssShader.SetBuffer(1, "Tris", m.B_Tris);
        ssShader.SetBuffer(1, "OccupiedChunksAPPEND", m.AC_OccupiedChunks);

        ssShader.SetBuffer(2, "OccupiedChunksCONSUME", m.AC_OccupiedChunks);
        ssShader.SetBuffer(2, "SpatialLookup", m.B_SpatialLookup);

        ssShader.SetBuffer(3, "SpatialLookup", m.B_SpatialLookup);

        ssShader.SetBuffer(4, "SpatialLookup", m.B_SpatialLookup);
        ssShader.SetBuffer(4, "StartIndices", m.B_StartIndices);
    }

    public void SetRMSettings (ComputeShader rmShader)
    {
        SetRMShaderBuffers(rmShader);

        rmShader.SetVector("NumChunks", new Vector4(m.NumChunks.x, m.NumChunks.y, m.NumChunks.z, m.NumChunks.w));
        rmShader.SetInt("NumTriObjects", m.TriObjects.Length);
        rmShader.SetInt("NumTris", m.Tris.Length);
        rmShader.SetInt("NumObjects", m.NumObjects);
        rmShader.SetInt("NumSpheres", m.Spheres.Length);
        rmShader.SetInt("NumMaterials", m.Materials.Length);

        rmShader.SetVector("Resolution", new Vector2(m.Resolution.x, m.Resolution.y));

        rmShader.SetVector("MinWorldBounds", new Vector3(m.MinWorldBounds.x, m.MinWorldBounds.y, m.MinWorldBounds.z));
        rmShader.SetVector("MaxWorldBounds", new Vector3(m.MaxWorldBounds.x, m.MaxWorldBounds.y, m.MaxWorldBounds.z));
        rmShader.SetVector("ChunkGridOffset", new Vector3(m.ChunkGridOffset.x, m.ChunkGridOffset.y, m.ChunkGridOffset.z));
        rmShader.SetFloat("CellSize", m.CellSize);

        // Ray setup settings
        rmShader.SetInt("MaxStepCount", m.MaxStepCount);
        rmShader.SetInt("RaysPerPixel", m.RaysPerPixel);
        rmShader.SetFloat("HitThreshold", m.HitThreshold);
        rmShader.SetFloat("ScatterProbability", m.ScatterProbability);
        rmShader.SetFloat("DefocusStrength", m.DefocusStrength);

        // Screen settings
        float aspectRatio = m.Resolution.x / m.Resolution.y;
        float fieldOfViewRad = m.fieldOfView * Mathf.Deg2Rad;
        float viewSpaceHeight = Mathf.Tan(fieldOfViewRad * 0.5f);
        float viewSpaceWidth = aspectRatio * viewSpaceHeight;
        rmShader.SetFloat("viewSpaceWidth", viewSpaceWidth);
        rmShader.SetFloat("viewSpaceHeight", viewSpaceHeight);

        rmShader.SetFloat("focalPlaneFactor", m.focalPlaneFactor);
    }

    public void SetSSSettings (ComputeShader ssShader)
    {
        SetSSShaderBuffers(ssShader);

        // Num constants
        ssShader.SetVector("NumChunks", new Vector4(m.NumChunks.x, m.NumChunks.y, m.NumChunks.z, m.NumChunks.w));
        ssShader.SetInt("NumChunksAll", m.NumChunksAll);
        ssShader.SetInt("NumObjects", m.NumObjects);
        ssShader.SetInt("NumSpheres", m.NumSpheres);
        ssShader.SetInt("NumObjects_NextPow2", Func.NextPow2(m.NumObjects));

        // World settings
        ssShader.SetVector("MinWorldBounds", new Vector3(m.MinWorldBounds.x, m.MinWorldBounds.y, m.MinWorldBounds.z));
        ssShader.SetVector("MaxWorldBounds", new Vector3(m.MaxWorldBounds.x, m.MaxWorldBounds.y, m.MaxWorldBounds.z));
        ssShader.SetVector("ChunkGridOffset", new Vector3(m.ChunkGridOffset.x, m.ChunkGridOffset.y, m.ChunkGridOffset.z));
        ssShader.SetFloat("CellSize", m.CellSize);
    }

    public void UpdateSortIterationVariables (ComputeShader ssShader, int blockLen, bool brownPinkSort)
    {
        ssShader.SetBool("BrownPinkSort", brownPinkSort);
        ssShader.SetInt("BlockLen", blockLen);
    }

    public void UpdateRMVariables (ComputeShader rmShader)
    {
        // Frame set variables
        int FrameRand = UnityEngine.Random.Range(0, 999999);
        rmShader.SetInt("FrameRand", FrameRand);
        rmShader.SetInt("FrameCount", m.FrameCount++);

        // Camera position
        float3 worldSpaceCameraPos = transform.position;
        rmShader.SetVector("WorldSpaceCameraPos", new Vector3(worldSpaceCameraPos.x, worldSpaceCameraPos.y, worldSpaceCameraPos.z));

        // Camera orientation
        float3 cameraRot = transform.rotation.eulerAngles * Mathf.Deg2Rad;
        rmShader.SetVector("CameraRotation", new Vector3(cameraRot.x, cameraRot.y, cameraRot.z));
    }
}