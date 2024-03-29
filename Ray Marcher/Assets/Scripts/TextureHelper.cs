using Unity.Mathematics;
using UnityEngine;

// Import utils from Resources.cs
using Resources;
using System;
public class TextureHelper : MonoBehaviour
{
    public ShaderHelper shaderHelper;
    public ComputeShader ngShader;
    public ComputeShader tcShader;
    private int ngShaderThreadSize = 8; // /~10
    private int tbShaderThreadSize = 8; // /~10
    [NonSerialized] public RenderTexture T_VectorMap;
    [NonSerialized] public RenderTexture T_PointsMap;
    private int3 LastResolution;
    private int LastCellSize;

    public void Copy (ref RenderTexture texture, RenderTexture textureA, int3 resolution)
    {
        tcShader.SetTexture(0, "Texture_A", textureA);
        tcShader.SetTexture(0, "Texture_Output", texture);

        shaderHelper.DispatchKernel(tcShader, "Copy_3D_F1", resolution, tbShaderThreadSize);
    }

    public void Blend (ref RenderTexture textureOutput, RenderTexture textureA, RenderTexture textureB, int3 resolution, float lerpWeight)
    {
        tcShader.SetFloat("LerpWeight", lerpWeight);

        tcShader.SetTexture(1, "Texture_A", textureA);
        tcShader.SetTexture(1, "Texture_B", textureB);
        tcShader.SetTexture(1, "Texture_Output", textureOutput);

        shaderHelper.DispatchKernel(tcShader, "Blend_3D_F1", resolution, tbShaderThreadSize);
    }

    public void Invert (ref RenderTexture texture, int3 resolution)
    {
        tcShader.SetTexture(2, "Texture_Output", texture);

        shaderHelper.DispatchKernel(tcShader, "Invert_3D_F1", resolution, tbShaderThreadSize);
    }

    public void Saturate (ref RenderTexture texture, int3 resolution)
    {
        tcShader.SetTexture(3, "Texture_Output", texture);

        shaderHelper.DispatchKernel(tcShader, "Saturate_3D_F1", resolution, tbShaderThreadSize);
    }

    public void ChangeBrightness (ref RenderTexture texture, int3 resolution, float brightnessFactor)
    {
        tcShader.SetTexture(4, "Texture_Output", texture);

        tcShader.SetFloat("BrightnessFactor", brightnessFactor);

        shaderHelper.DispatchKernel(tcShader, "ChangeBrightness_3D_F1", resolution, tbShaderThreadSize);
    }

    public void AddBrightnessFixed (ref RenderTexture texture, int3 resolution, float brightnessAddOn)
    {
        tcShader.SetTexture(5, "Texture_Output", texture);

        tcShader.SetFloat("BrightnessAddOn", brightnessAddOn);

        shaderHelper.DispatchKernel(tcShader, "AddBrightnessFixed_3D_F1", resolution, tbShaderThreadSize);
    }

    public void AddBrightnessByTexture (ref RenderTexture texture, RenderTexture textureA, int3 resolution, float brightnessFactor = 1.0f)
    {
        tcShader.SetTexture(6, "Texture_A", textureA);
        tcShader.SetTexture(6, "Texture_Output", texture);

        tcShader.SetFloat("BrightnessFactor", brightnessFactor);

        shaderHelper.DispatchKernel(tcShader, "AddBrightnessByTexture_3D_F1", resolution, tbShaderThreadSize);
    }

    public void SubtractBrightnessByTexture (ref RenderTexture texture, RenderTexture textureA, int3 resolution, float brightnessFactor = 1.0f)
    {
        tcShader.SetTexture(7, "Texture_A", textureA);
        tcShader.SetTexture(7, "Texture_Output", texture);

        tcShader.SetFloat("BrightnessFactor", brightnessFactor);

        shaderHelper.DispatchKernel(tcShader, "SubtractBrightnessByTexture_3D_F1", resolution, tbShaderThreadSize);
    }

    public void GaussianBlur (ref RenderTexture texture, int3 resolution, int smoothingRadius = 2, int iterations = 1)
    {
        for (int i = 0; i < iterations; i++)
        {
            RenderTexture texCopy = Init.CreateTexture(resolution, 1);
            Copy(ref texCopy, texture, resolution);

            tcShader.SetTexture(8, "Texture_A", texCopy);
            tcShader.SetTexture(8, "Texture_Output", texture);

            tcShader.SetVector("Resolution", new Vector3(resolution.x, resolution.y, resolution.z));
            tcShader.SetFloat("SmoothingRadius", smoothingRadius);

            shaderHelper.DispatchKernel(tcShader, "GaussianBlur_3D_F1", resolution, tbShaderThreadSize);
        }
    }

    public void BoxBlur (ref RenderTexture texture, int3 resolution, int smoothingRadius = 2, int iterations = 1)
    {
        for (int i = 0; i < iterations; i++)
        {
            RenderTexture texCopy = Init.CreateTexture(resolution, 1);
            Copy(ref texCopy, texture, resolution);

            tcShader.SetTexture(9, "Texture_A", texCopy);
            tcShader.SetTexture(9, "Texture_Output", texture);

            tcShader.SetVector("Resolution", new Vector3(resolution.x, resolution.y, resolution.z));
            tcShader.SetFloat("SmoothingRadius", smoothingRadius);

            shaderHelper.DispatchKernel(tcShader, "BoxBlur_3D_F1", resolution, tbShaderThreadSize);
        }
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