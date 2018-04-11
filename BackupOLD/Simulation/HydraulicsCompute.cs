using UnityEngine;
using System.Collections;

public class HydraulicsCompute : MonoBehaviour
{
    public ComputeShader shader;
    
    public int TexResolution = 256;
    
    public RenderTexture[] flowMaps;
    public RenderTexture waterMap;

    public Texture2D waterTex;

    int currTex = 0;
    const int numTex = 2;

    // Use this for initialization
    void Start()
    {
        flowMaps = new RenderTexture[numTex];
        for (int i = 0; i < numTex; i++)
        {
            flowMaps[i] = new RenderTexture(TexResolution, TexResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true,
                antiAliasing = 1,
                filterMode = FilterMode.Point,
                
            };
            flowMaps[i].Create();
        }

        waterMap = new RenderTexture(TexResolution, TexResolution, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        waterMap.enableRandomWrite = true;
        waterMap.Create();

        int kernelHandle = shader.FindKernel("CSInit");
        shader.SetTexture(kernelHandle, "WaterMap", waterMap);
        shader.SetFloat("TexSize", TexResolution);
        shader.Dispatch(kernelHandle, TexResolution / 8, TexResolution / 8, 1);
        GetComponent<Renderer>().material.SetTexture("_MainTex", waterMap);
    }

    private void ComputeStepFrame()
    {
        int prevTex = currTex;
        currTex = (currTex + 1) % numTex;

        int kernelHandle = shader.FindKernel("CSMain");
        shader.SetTexture(kernelHandle, "WaterMap", waterMap);
        shader.SetTexture(kernelHandle, "FlowPrevious", flowMaps[prevTex]);
        shader.SetTexture(kernelHandle, "FlowCurrent", flowMaps[currTex]);
        shader.SetFloat("DeltaTime", Time.deltaTime);
        shader.SetInt("TexSize", TexResolution);
        shader.Dispatch(kernelHandle, TexResolution / 8, TexResolution / 8, 1);

        GetComponent<Renderer>().material.SetTexture("_MainTex", flowMaps[currTex]);
    }

    void Update()
    {
         ComputeStepFrame();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < flowMaps.Length; i++)
        {
            flowMaps[i].Release();
        }
    }
}
