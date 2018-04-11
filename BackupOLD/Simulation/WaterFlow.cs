using UnityEngine;

public class WaterFlow : MonoBehaviour
{
    [SerializeField]
    Material waterMat;

    [SerializeField]
    Material landMat;

    [SerializeField]
    Material flowMapMat;

    [SerializeField]
    Material waterMapMat;

    //[SerializeField]
    //Material terrainMat;
    [SerializeField]
    Texture2D terrainMap;

    [SerializeField]
    int tex_size;

    GameObject[] gridLand, gridWater;

    public Vector2 waterInputPoint = new Vector2(0.5f, 0.5f);
    public float waterInputAmount = 2.0f;
    public float waterInputRadius = 0.008f;

    const int MAX_TERRAIN_HEIGHT = 128;
    const int TOTAL_GRID_SIZE = 512;
    const int GRID_SIZE = 256;
    const float PIPE_LENGTH = 1.0f;
    const float CELL_LENGTH = 1.0f;
    const float CELL_AREA = 1.0f;
    const float GRAVITY = 9.81f;
    const int READ = 0;
    const int WRITE = 1;

    const float TIME_STEP = 0.1f;

    RenderTexture[] waterMap;
    //RenderTexture[] terrainMap;
    RenderTexture[] flowMap;

    void Start()
    {
        waterMap = new RenderTexture[2];
        flowMap = new RenderTexture[2];
        //terrainMap = new RenderTexture[2];

        //terrainMap[0] = new RenderTexture(tex_size, tex_size, 0, RenderTextureFormat.ARGBFloat)
        //{
        //    wrapMode = TextureWrapMode.Clamp,
        //    filterMode = FilterMode.Point,
        //    name = "Terrain Field 0"
        //};
        //terrainMap[1] = new RenderTexture(tex_size, tex_size, 0, RenderTextureFormat.ARGBFloat)
        //{
        //    wrapMode = TextureWrapMode.Clamp,
        //    filterMode = FilterMode.Point,
        //    name = "Terrain Field 1"
        //};

        flowMap[0] = new RenderTexture(tex_size, tex_size, 0, RenderTextureFormat.ARGBHalf)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            name = "Water outflow 0"
        };
        flowMap[1] = new RenderTexture(tex_size, tex_size, 0, RenderTextureFormat.ARGBHalf)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            name = "Water outflow 1"
        };

        waterMap[0] = new RenderTexture(tex_size, tex_size, 0, RenderTextureFormat.RFloat)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            name = "Water Field 0"
        };
        waterMap[1] = new RenderTexture(tex_size, tex_size, 0, RenderTextureFormat.RFloat)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            name = "Water Field 1"
        };

        MakeGrids();

        InitMaps();
    }

    private void CalculateFlow()
    {
        flowMapMat.SetFloat("_TexSize", tex_size);
        flowMapMat.SetFloat("T", TIME_STEP);
        flowMapMat.SetFloat("L", PIPE_LENGTH);
        flowMapMat.SetFloat("A", CELL_AREA);
        flowMapMat.SetFloat("G", GRAVITY);
        flowMapMat.SetTexture("_TerrainMap", terrainMap/*[READ]*/);
        flowMapMat.SetTexture("_WaterMap", waterMap[READ]);

        Graphics.Blit(flowMap[READ], flowMap[WRITE], flowMapMat);
        Swap(flowMap);

        waterMat.SetFloat("_TexSize", tex_size);
        waterMat.SetFloat("T", TIME_STEP);
        waterMat.SetFloat("L", PIPE_LENGTH);
        waterMat.SetTexture("_OutFlowField", flowMap[READ]);

        Graphics.Blit(waterMap[READ], waterMap[WRITE], flowMapMat);
        Swap(waterMap);
    }

    private void WaterInput()
    {
        waterMat.SetVector("_Point", waterInputPoint);
        waterMat.SetFloat("_Radius", waterInputRadius);
        waterMat.SetFloat("_Amount", waterInputAmount);

        Graphics.Blit(waterMap[READ], waterMap[WRITE], waterMat);
        Swap(waterMap);
    }

    void Swap(RenderTexture[] tex)
    {
        RenderTexture temp = tex[0];
        tex[0] = tex[1];
        tex[1] = temp;
    }
    
    void Update()
    {
        WaterInput();
        CalculateFlow();
    }

    void ClearColor(RenderTexture[] tex)
    {
        for (int i = 0; i < tex.Length; i++)
        {
            if (tex[i] == null) continue;
            if (!SystemInfo.SupportsRenderTextureFormat(tex[i].format)) continue;

            Graphics.SetRenderTarget(tex[i]);
            GL.Clear(false, true, Color.clear);
        }
    }

    private void InitMaps()
    {
        ClearColor(flowMap);
        ClearColor(waterMap);
    }

    private void OnDestroy()
    {
        Destroy(flowMap[0]);
        Destroy(flowMap[1]);
        Destroy(waterMap[0]);
        Destroy(waterMap[1]);

        int numGrids = TOTAL_GRID_SIZE / GRID_SIZE;

        for (int x = 0; x < numGrids; x++)
        {
            for (int y = 0; y < numGrids; y++)
            {
                int idx = x + y * numGrids;

                Destroy(gridLand[idx]);
                Destroy(gridWater[idx]);

            }
        }

    }

    private void MakeGrids()
    {
        int numGrids = TOTAL_GRID_SIZE / GRID_SIZE;

        gridLand = new GameObject[numGrids * numGrids];
        gridWater = new GameObject[numGrids * numGrids];

        for (int x = 0; x < numGrids; x++)
        {
            for (int y = 0; y < numGrids; y++)
            {
                int idx = x + y * numGrids;

                int posX = x * (GRID_SIZE - 1);
                int posY = y * (GRID_SIZE - 1);

                Mesh mesh = MakeMesh(GRID_SIZE, TOTAL_GRID_SIZE, posX, posY);

                mesh.bounds = new Bounds(new Vector3(GRID_SIZE / 2, 0, GRID_SIZE / 2), new Vector3(GRID_SIZE, MAX_TERRAIN_HEIGHT * 2, GRID_SIZE));

                gridLand[idx] = new GameObject("Grid Land " + idx.ToString());
                gridLand[idx].AddComponent<MeshFilter>();
                gridLand[idx].AddComponent<MeshRenderer>();
                gridLand[idx].GetComponent<Renderer>().material = landMat;
                gridLand[idx].GetComponent<MeshFilter>().mesh = mesh;
                gridLand[idx].transform.localPosition = new Vector3(-TOTAL_GRID_SIZE / 2 + posX, 0, -TOTAL_GRID_SIZE / 2 + posY);

                gridWater[idx] = new GameObject("Grid Water " + idx.ToString());
                gridWater[idx].AddComponent<MeshFilter>();
                gridWater[idx].AddComponent<MeshRenderer>();
                gridWater[idx].GetComponent<Renderer>().material = waterMat;
                gridWater[idx].GetComponent<MeshFilter>().mesh = mesh;
                gridWater[idx].transform.localPosition = new Vector3(-TOTAL_GRID_SIZE / 2 + posX, 0, -TOTAL_GRID_SIZE / 2 + posY);

            }
        }
    }

    private Mesh MakeMesh(int size, int totalSize, int posX, int posY)
    {
        Vector3[] vertices = new Vector3[size * size];
        Vector2[] texcoords = new Vector2[size * size];
        Vector3[] normals = new Vector3[size * size];
        int[] indices = new int[size * size * 6];

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 uv = new Vector3((posX + x) / (totalSize - 1.0f), (posY + y) / (totalSize - 1.0f));
                Vector3 pos = new Vector3(x, 0.0f, y);
                Vector3 norm = new Vector3(0.0f, 1.0f, 0.0f);

                texcoords[x + y * size] = uv;
                vertices[x + y * size] = pos;
                normals[x + y * size] = norm;
            }
        }

        int num = 0;
        for (int x = 0; x < size - 1; x++)
        {
            for (int y = 0; y < size - 1; y++)
            {
                indices[num++] = x + y * size;
                indices[num++] = x + (y + 1) * size;
                indices[num++] = (x + 1) + y * size;

                indices[num++] = x + (y + 1) * size;
                indices[num++] = (x + 1) + (y + 1) * size;
                indices[num++] = (x + 1) + y * size;
            }
        }

        Mesh mesh = new Mesh
        {
            vertices = vertices,
            uv = texcoords,
            triangles = indices,
            normals = normals
        };

        return mesh;
    }
}