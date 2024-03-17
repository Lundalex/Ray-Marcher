using UnityEngine;

public class ShaderHelper : MonoBehaviour
{
    public Main m;
    public void SetRMShaderBuffers(ComputeShader rtShader)
    {
        rtShader.SetBuffer(0, "Spheres", m.OpaqueSphereBuffer);
        rtShader.SetBuffer(0, "Materials", m.OpaqueMaterialsBuffer);
    }

    public void UpdateRMShaderVariables(ComputeShader rtShader)
    {

    }
}