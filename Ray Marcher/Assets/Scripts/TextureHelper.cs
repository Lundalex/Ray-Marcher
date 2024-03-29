using Unity.Mathematics;
using UnityEngine;

// Import utils from Resources.cs
using Resources;
using System;
public class TextureHelper : MonoBehaviour
{
    public ShaderHelper shaderHelper;
    public ComputeShader ngShader;
    public ComputeShader tbShader;
    private int ngShaderThreadSize = 8; // /~10
    private int tbShaderThreadSize = 8; // /~10
    [NonSerialized] public RenderTexture T_VectorMap;
    // [NonSerialized] public RenderTexture T_PerlinNoise;
    [NonSerialized] public RenderTexture T_PointsMap;
    // [NonSerialized] public RenderTexture T_VoronoiNoise;

    private int3 LastResolution;
    private int LastCellSize;

    public void SetTextureBlend (ref RenderTexture textureOutput, RenderTexture textureA, RenderTexture textureB, int3 resolution, float lerpWeight)
    {
        tbShader.SetFloat("LerpWeight", lerpWeight);

        tbShader.SetTexture(0, "Texture_A", textureA);
        tbShader.SetTexture(0, "Texture_B", textureB);
        tbShader.SetTexture(0, "Texture_Output", textureOutput);

        shaderHelper.DispatchKernel(tbShader, "TextureBlend_3D_F1", resolution, tbShaderThreadSize);
    }

    public void InvertTexture (ref RenderTexture texture, int3 resolution)
    {
        tbShader.SetTexture(1, "Texture_Invert", texture);

        shaderHelper.DispatchKernel(tbShader, "TextureInvert_3D_F1", resolution, tbShaderThreadSize);
    }

    public void SetPerlin (ref RenderTexture texture, int3 resolution, int cellSize, int rngSeed)
    {
        // -- PERLIN_3D -> PerlinNoise --

        UpdateScriptTextures(resolution, cellSize);

        int NumPasses = (int)Mathf.Log(cellSize, 2);

        ngShader.SetInt("RngSeed", rngSeed);
        ngShader.SetInt("NumPasses", NumPasses);
        ngShader.SetInt("MaxNoiseCellSize", cellSize);

        int cellSizeIterator = cellSize*2;
        for (int pass = 0; pass < NumPasses; pass++)
        {
            cellSizeIterator /= 2;
            ngShader.SetInt("NoiseCellSize", cellSizeIterator);
            ngShader.SetInt("PassCount", pass);

            shaderHelper.DispatchKernel(ngShader, "GenerateVectorMap", resolution / cellSizeIterator, ngShaderThreadSize);

            ngShader.SetTexture(1, "PerlinNoise", texture);
            shaderHelper.DispatchKernel(ngShader, "Perlin", resolution, ngShaderThreadSize);
        }
    }

    public void SetVoronoi (ref RenderTexture texture, int3 resolution, int cellSize, int rngSeed)
    {
        // -- VORONOI_3D -> VoronoiNoise --

        UpdateScriptTextures(resolution, cellSize);

        ngShader.SetInt("RngSeed", rngSeed);

        shaderHelper.DispatchKernel(ngShader, "GeneratePointsMap", resolution / cellSize, ngShaderThreadSize);
        ngShader.SetTexture(3, "VoronoiNoise", texture);
        shaderHelper.DispatchKernel(ngShader, "Voronoi", resolution, ngShaderThreadSize);
    }

    public void UpdateScriptTextures (int3 newResolution, int newCellSize)
    {
        bool3 resolutionHasChanged = newResolution != LastResolution;
        bool cellSizeHasChanged = newCellSize != LastCellSize;
        bool settingsHasChanged = resolutionHasChanged.x || resolutionHasChanged.y || resolutionHasChanged.z || cellSizeHasChanged;

        if (!settingsHasChanged) { return; }

        T_VectorMap = Init.CreateTexture(newResolution / newCellSize, 3);

        T_PointsMap = Init.CreateTexture(newResolution / newCellSize, 3);

        LastResolution = newResolution;
        LastCellSize = newCellSize;
    }
}