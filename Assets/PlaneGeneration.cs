using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneGeneration : MonoBehaviour {

	[SerializeField]
    int size;

    int step;

    Mesh mesh;

    void Start()
    {
        GeneratePlane();
    }

    // Use this for initialization
    public void GeneratePlane()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
        mesh.name = "Surface Fluid";

        int sizePower = size*size;

        Debug.Log((size + 1) * (size + 1));
        Vector3[] verts = new Vector3[(size + 1) * (size + 1)];
        int[] tris = new int[sizePower * 6];
        Vector2[] uv = new Vector2[verts.Length];

        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = new Vector3(i % (size + 1), 0, i / (size + 1));
            uv[i] = new Vector2((float)i % (float)(size+1) / (float)size, (float)i / (float)(size+1) / (float)size);

            if (i < sizePower)
            {
                // Every quad is 6 points, first 3 points are the lower triangle
                // second 3 points are bottom.

                int indicesOffset = i * 6;
                int vertexOffset = i + i / size;

                // Create lower triangle
                tris[indicesOffset + 0] = vertexOffset;
                tris[indicesOffset + 1] = vertexOffset + size + 1;
                tris[indicesOffset + 2] = vertexOffset + 1;

                // Create upper triangle
                tris[indicesOffset + 3] = vertexOffset + size + 1;
                tris[indicesOffset + 4] = vertexOffset + size + 2;
                tris[indicesOffset + 5] = vertexOffset + 1;
            }
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uv;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        step++;
    }

}
