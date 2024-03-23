using UnityEngine;

public class ShaderHelper : MonoBehaviour
{
    public Main m;
    public void SetRMShaderBuffers(ComputeShader rmShader)
    {
        rmShader.SetBuffer(0, "TriObjects", m.B_TriObjects);
        rmShader.SetBuffer(0, "Tris", m.B_Tris);
        rmShader.SetBuffer(0, "Spheres", m.B_Spheres);
        rmShader.SetBuffer(0, "Materials", m.B_Materials);
    }

    public void SetPCShaderBuffers(ComputeShader pcShader)
    {
        pcShader.SetBuffer(0, "TriObjects", m.B_TriObjects);
        pcShader.SetBuffer(0, "Tris", m.B_Tris);
    }

    public void SetSSShaderBuffers(ComputeShader ssShader)
    {
        
    }
}