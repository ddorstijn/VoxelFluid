using UnityEngine;

public class PlaneGenerator {

    // Use this for initialization
    public static MeshData GeneratePlane(int size, int totalSize, int posX, int posY)
    {
        Vector3[] vertices = new Vector3[size * size];
        int[] indices = new int[(size - 1) * (size - 1) * 6];
        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            int x = i % size;
            int z = i / size;

            vertices[i] = new Vector3(x, 0, z);
            uv[i] = new Vector2((posX + x) / (totalSize - 1.0f), (posY + z) / (totalSize - 1.0f));

            if (x < size - 1 && z < size - 1)
            {
                // Every quad is 6 points, first 3 points are the lower triangle
                // second 3 points are bottom.
                // The Indices are offset by z because it skips 1 vertex when i is width
                // This has to be calculated for because else there will be gaps in the array
                int indicesOffset = (i - z) * 6;

                // Create lower triangle 
                indices[indicesOffset + 0] = i;
                indices[indicesOffset + 1] = i + size;
                indices[indicesOffset + 2] = i + 1;

                // Create upper triangle
                indices[indicesOffset + 3] = i + size;
                indices[indicesOffset + 4] = i + size + 1;
                indices[indicesOffset + 5] = i + 1;
            }
        }

        MeshData meshData;
        meshData.vertices = vertices;
        meshData.triangles = indices;
        meshData.uvs = uv;        

        return meshData;
    }
}

public struct MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles,
            uv = uvs
        };

        return mesh;
    }
}
