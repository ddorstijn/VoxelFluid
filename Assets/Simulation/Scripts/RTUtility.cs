using UnityEngine;

static public class RTUtility
{

    static public RenderTexture[] CreateRenderTextures(int size, RenderTextureFormat format)
    {
        RenderTexture[] tex = new RenderTexture[2];

        tex[0] = new RenderTexture(size, size, 0, format);
        tex[0].wrapMode = TextureWrapMode.Clamp;
        tex[0].filterMode = FilterMode.Point;
        tex[1] = new RenderTexture(size, size, 0, format);
        tex[1].wrapMode = TextureWrapMode.Clamp;
        tex[1].filterMode = FilterMode.Point;

        return tex; 
    }

    static public void Swap(RenderTexture[] texs)
    {
        RenderTexture temp = texs[0];
        texs[0] = texs[1];
        texs[1] = temp;
    }

    static public void ClearColor(RenderTexture tex)
    {
        if (tex == null) return;
        if (!SystemInfo.SupportsRenderTextureFormat(tex.format)) return;

        Graphics.SetRenderTarget(tex);
        GL.Clear(false, true, Color.clear);
    }

    static public void ClearColor(RenderTexture[] texs)
    {
        for (int i = 0; i < texs.Length; i++)
        {
            if (texs[i] == null) continue;
            if (!SystemInfo.SupportsRenderTextureFormat(texs[i].format)) continue;

            Graphics.SetRenderTarget(texs[i]);
            GL.Clear(false, true, Color.clear);
        }
    }

}

