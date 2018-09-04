using UnityEngine;

public class FluidSim : MonoBehaviour
{
    [SerializeField]
    GameObject sun;

    [SerializeField]
    Material landMat, waterMat;

    [SerializeField]
    Material waterInputMat, updateFlowMat, updateWaterMat;

    [SerializeField]
    float waterInputAmount = 2.0f;

    [SerializeField]
    float waterInputRadius = 0.008f;

    [SerializeField]
    float viscosity = 0.01f;

    GameObject[] gridLand, gridWater;

    RenderTexture terrainMap;
    RenderTexture[] flowMap;
    RenderTexture[] waterMap;

    // Higher resolution gives more accurate simulation
    const int TEX_SIZE = 1024;

    // The heightmap gets scaled with this factor
    const int TERRAIN_HEIGHT = 128;

    // The size of the terrain
    const int TOTAL_GRID_SIZE = 512;

    // Delta time is to slow so we speed it up a little
    const float TIME_SCALE = 7.5f;

    const int GRID_SIZE = 128;
    const float PIPE_LENGTH = 1.0f;
    const float CELL_LENGTH = 1.0f;
    const float CELL_AREA = 1.0f;
    const float GRAVITY = 9.81f;

    // A lot of the maps need data from the previous timestep.
    // This is why we need to textures. One to read from and one to write to.
    const int READ = 0;
    const int WRITE = 1;

    void Start()
    {
        // Here we can get away with a lower resolution for the render texture as it doesn't need the full float
        flowMap = RTUtility.CreateRenderTextures(TEX_SIZE, RenderTextureFormat.ARGBHalf);
        waterMap = RTUtility.CreateRenderTextures(TEX_SIZE, RenderTextureFormat.RFloat);

        terrainMap = new RenderTexture(TEX_SIZE, TEX_SIZE, 0, RenderTextureFormat.RFloat) {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        GenerateWorld();

        // Clear all the render textures and generate the heightmap
        RTUtility.ClearColor(terrainMap);
        RTUtility.ClearColor(flowMap);
        RTUtility.ClearColor(waterMap);

        terrainMap = HeightmapGenerator.GenerateHeightMap(terrainMap, TERRAIN_HEIGHT, 8, 0.3f, 3f);
    }

    void WaterInput(Vector2 position)
    {
        waterInputMat.SetVector("_InputUV", position);
        waterInputMat.SetFloat("_Radius", waterInputRadius);
        waterInputMat.SetFloat("_Amount", waterInputAmount);

        Graphics.Blit(waterMap[READ], waterMap[WRITE], waterInputMat);
        RTUtility.Swap(waterMap);
    }

    void CalculateFlow()
    {
        updateFlowMat.SetFloat("_TexSize", TEX_SIZE);
        updateFlowMat.SetFloat("_TimeStep", Time.deltaTime * TIME_SCALE);
        updateFlowMat.SetFloat("_Length", PIPE_LENGTH);
        updateFlowMat.SetFloat("_Area", CELL_AREA);
        updateFlowMat.SetFloat("_Gravity", GRAVITY);
        updateFlowMat.SetFloat("_Viscosity", viscosity);
        updateFlowMat.SetTexture("_TerrainMap", terrainMap);
        updateFlowMat.SetTexture("_WaterMap", waterMap[READ]);

        Graphics.Blit(flowMap[READ], flowMap[WRITE], updateFlowMat);
        RTUtility.Swap(flowMap);

        updateWaterMat.SetFloat("_TexSize", TEX_SIZE);
        updateWaterMat.SetFloat("_TimeStep", Time.deltaTime * TIME_SCALE);
        updateWaterMat.SetFloat("_Length", PIPE_LENGTH);
        updateWaterMat.SetTexture("_FlowMap", flowMap[READ]);

        Graphics.Blit(waterMap[READ], waterMap[WRITE], updateWaterMat);
        RTUtility.Swap(waterMap);
    }

    void Update()
    {
        // Set maps to point filter for precise data
        terrainMap.filterMode = FilterMode.Point;
        waterMap[0].filterMode = FilterMode.Point;
        waterMap[1].filterMode = FilterMode.Point;

        if (Input.GetKey(KeyCode.Space)) {
            float distance = Input.mousePosition.y / 2f;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 pos = ray.origin + (ray.direction * distance);

            Vector2 inputPosition = new Vector2((pos.x + TOTAL_GRID_SIZE / 2f) / TOTAL_GRID_SIZE,
                                                (pos.z + TOTAL_GRID_SIZE / 2f) / TOTAL_GRID_SIZE);

            WaterInput(inputPosition);
        }

        CalculateFlow();

        // Set the maps to bilinear filtering so it looks good when rendered
        terrainMap.filterMode = FilterMode.Trilinear;
        waterMap[0].filterMode = FilterMode.Trilinear;
        waterMap[1].filterMode = FilterMode.Trilinear;

        // Scale y to match texture size and terrain size
        float scaleY = TOTAL_GRID_SIZE / (float)TEX_SIZE;

        landMat.SetFloat("_ScaleY", scaleY);
        landMat.SetFloat("_TexSize", TEX_SIZE);
        landMat.SetTexture("_MainTex", terrainMap);

        waterMat.SetFloat("_ScaleY", scaleY);
        waterMat.SetFloat("_TexSize", TEX_SIZE);
        waterMat.SetTexture("_WaterMap", waterMap[READ]);
        waterMat.SetTexture("_MainTex", terrainMap);
    }

    void OnDestroy()
    {
        Destroy(terrainMap);
        Destroy(flowMap[0]);
        Destroy(waterMap[0]);
        Destroy(waterMap[1]);

        int numGrids = TOTAL_GRID_SIZE / GRID_SIZE;

        for (int x = 0; x < numGrids; x++) {
            for (int y = 0; y < numGrids; y++) {
                int idx = x + y * numGrids;

                Destroy(gridLand[idx]);
                Destroy(gridWater[idx]);
            }
        }
    }

    void GenerateWorld()
    {
        // Divide the big terrain in smaller parts. There is a limit of vertices per mesh
        int numGrids = TOTAL_GRID_SIZE / GRID_SIZE;

        gridLand = new GameObject[numGrids * numGrids];
        gridWater = new GameObject[numGrids * numGrids];

        for (int x = 0; x < numGrids; x++) {
            for (int y = 0; y < numGrids; y++) {
                int idx = x + y * numGrids;

                // Calculate the offsets
                int posX = x * (GRID_SIZE - 1);
                int posY = y * (GRID_SIZE - 1);

                // Delegate generation of planes to a static class
                Mesh mesh = PlaneGenerator.GeneratePlane(GRID_SIZE, TOTAL_GRID_SIZE, posX, posY).CreateMesh();
                mesh.bounds = new Bounds(new Vector3(GRID_SIZE / 2, 0, GRID_SIZE / 2), new Vector3(GRID_SIZE, TERRAIN_HEIGHT * 2, GRID_SIZE));

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
}
