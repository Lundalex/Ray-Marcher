using UnityEngine;

public class ShaderHelper : MonoBehaviour
{
    public Main m;
    public void SetRMShaderBuffers(ComputeShader rtShader)
    {
        rtShader.SetBuffer(0, "TriObjects", m.B_TriObjects);
        rtShader.SetBuffer(0, "Tris", m.B_Tris);
        rtShader.SetBuffer(0, "Spheres", m.B_Spheres);
        rtShader.SetBuffer(0, "Materials", m.B_Materials);
    }

    public void UpdateRMShaderVariables(ComputeShader rtShader)
    {

    }
}