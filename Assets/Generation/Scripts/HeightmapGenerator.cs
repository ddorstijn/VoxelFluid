using System.Collections;
using UnityEngine;

public static class HeightmapGenerator
{
	public static RenderTexture GenerateHeightMap(RenderTexture heightMap, int terrainHeight, int octaves, float gain, float lacunarity, float xOffset = 1.0f, float yOffset = 1.0f)
    {
        Material material = Resources.Load("HeightmapGenerator", typeof(Material)) as Material;
		material.SetFloat("_X", xOffset);
		material.SetFloat("_Y", yOffset);
		material.SetInt("_Octaves", octaves);
		material.SetFloat("_Gain", gain);
		material.SetFloat("_Lacunarity", lacunarity);

        material.SetFloat("_Height", terrainHeight);

        Graphics.Blit(heightMap, heightMap, material);

		return heightMap;
    }
}
