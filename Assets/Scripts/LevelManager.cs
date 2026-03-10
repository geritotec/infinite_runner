using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LevelData { public GridRow[] rows; }

[System.Serializable]
public class GridRow
{
    public int[] terrainLanes = new int[7];
    public int[] obstacleLanes = new int[7];
}

public class LevelManager : MonoBehaviour
{
    [Header("Level JSON Files")]
    public TextAsset[] chunkBlueprints;

    [Header("Treadmill Settings")]
    public float worldSpeed = 15f;
    public float maxWorldSpeed = 40f;
    public float speedIncreaseRate = 0.5f;
    public int chunksOnScreen = 4;
    public float despawnZ = -60f;

    [Header("Hit Penalty")]
    public float freezeDuration = 1.5f;
    public float recoveryRate = 5f;

    [Header("Track Dimensions & Visuals")]
    public float laneWidth = 3f;
    public float rowSpacing = 5f;
    public float elevationStep = 2.5f;
    public float foundationDepth = 10f;
    public Material trackMaterial;
    public float bounceDuration = 0.3f;
    private float bounceTimer = 0f;
    private bool isBouncing = false;
        public float bounceSpeed = 10f;


    [Header("Obstacles")]
    public float obstacleYOffset = 0.5f;
    public GameObject[] obstaclePrefabs;

    [Header("Wall Colliders")]
    [Tooltip("Must match the exact layer index of your Obstacle layer")]
    public int obstacleLayerIndex = 6;

    private List<GameObject> activeChunks = new List<GameObject>();
    private float nextSpawnZ = 0f;
    private float speedBeforeHit = 0f;
    private float freezeTimer = 0f;
    private bool isFrozen = false;

    void OnEnable() { PlayerMovement.OnPlayerHit += HandlePlayerHit; }
    void OnDisable() { PlayerMovement.OnPlayerHit -= HandlePlayerHit; }

    void HandlePlayerHit()
    {
        if (isBouncing) return;
        worldSpeed = -bounceSpeed;
        bounceTimer = bounceDuration;
        isBouncing = true;
    }

    void Start()
    {
        for (int i = 0; i < chunksOnScreen; i++)
            SpawnRandomChunk();
    }

    void Update()
    {
        if (isBouncing)
        {
            bounceTimer -= Time.deltaTime;
            if (bounceTimer <= 0f)
                isBouncing = false;
        }
        else
        {
            if (worldSpeed < maxWorldSpeed)
                worldSpeed = Mathf.MoveTowards(worldSpeed, maxWorldSpeed, recoveryRate * Time.deltaTime);
        }

        float move = worldSpeed * Time.deltaTime;
        foreach (GameObject chunk in activeChunks)
            chunk.transform.Translate(Vector3.back * move);
        nextSpawnZ -= move;

        if (activeChunks.Count > 0 && activeChunks[0].transform.position.z < despawnZ)
        {
            Destroy(activeChunks[0]);
            activeChunks.RemoveAt(0);
            SpawnRandomChunk();
        }
    }

    private void SpawnRandomChunk()
    {
        if (chunkBlueprints == null || chunkBlueprints.Length == 0) return;

        TextAsset jsonFile = chunkBlueprints[Random.Range(0, chunkBlueprints.Length)];
        LevelData data = JsonUtility.FromJson<LevelData>(jsonFile.text);
        if (data == null || data.rows == null) return;

        GameObject chunk = new GameObject("Chunk");
        chunk.transform.position = new Vector3(0, 0, nextSpawnZ);
        chunk.transform.SetParent(this.transform);

        Mesh mesh = BuildChunkMesh(data.rows, chunk.transform);
        chunk.AddComponent<MeshFilter>().mesh = mesh;
        chunk.AddComponent<MeshRenderer>().material = trackMaterial;
        chunk.AddComponent<MeshCollider>().sharedMesh = mesh;

        BuildWallColliders(data.rows, chunk.transform);

        activeChunks.Add(chunk);
        nextSpawnZ += data.rows.Length * rowSpacing;
    }

    private void BuildWallColliders(GridRow[] gridRows, Transform parent)
    {
        int laneCount = gridRows[0].terrainLanes.Length;
        float centerOffset = (laneCount - 1) / 2f;

        for (int z = 0; z < gridRows.Length; z++)
        {
            for (int x = 0; x < laneCount; x++)
            {
                int cur = gridRows[z].terrainLanes[x];
                if (cur < 0) continue;

                float xCenter = (x - centerOffset) * laneWidth;

                // Forward wall — cliff the player runs into
                if (z + 1 < gridRows.Length)
                {
                    int next = gridRows[z + 1].terrainLanes[x];
                    if (next < 0) next = cur;
                    if (next - cur >= 2)
                    {
                        float bottom = cur * elevationStep;
                        float height = (next - cur) * elevationStep;
                        MakeWallBox(parent,
                            new Vector3(xCenter, bottom + height / 2f, (z + 1) * rowSpacing),
                            new Vector3(laneWidth, height, 0.2f));
                    }
                }

                // Side walls — lateral cliffs
                foreach (int nx in new int[] { x - 1, x + 1 })
                {
                    if (nx < 0 || nx >= laneCount) continue;
                    int neighbor = gridRows[z].terrainLanes[nx];
                    if (neighbor < 0) neighbor = cur;
                    if (neighbor - cur >= 2)
                    {
                        float xFace = ((x - centerOffset) + (nx - centerOffset)) / 2f * laneWidth;
                        float bottom = cur * elevationStep;
                        float height = (neighbor - cur) * elevationStep;
                        MakeWallBox(parent,
                            new Vector3(xFace, bottom + height / 2f, z * rowSpacing + rowSpacing / 2f),
                            new Vector3(0.2f, height, rowSpacing));
                    }
                }
            }
        }
    }

    private void MakeWallBox(Transform parent, Vector3 localPos, Vector3 size)
    {
        GameObject w = new GameObject("Wall");
        w.transform.SetParent(parent);
        w.transform.localPosition = localPos;
        w.layer = obstacleLayerIndex; // Same layer as obstacle prefabs — OverlapSphere picks it up for free
        w.AddComponent<BoxCollider>().size = size;
    }

    private Mesh BuildChunkMesh(GridRow[] gridRows, Transform chunkTransform)
    {
        int laneCount = gridRows[0].terrainLanes.Length;
        float centerOffset = (laneCount - 1) / 2f;

        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();
        int vi = 0;

        void DrawFace(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            verts.Add(v1); verts.Add(v2); verts.Add(v3); verts.Add(v4);
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 1)); uvs.Add(new Vector2(1, 1));
            tris.Add(vi); tris.Add(vi + 2); tris.Add(vi + 1);
            tris.Add(vi + 1); tris.Add(vi + 2); tris.Add(vi + 3);
            vi += 4;
        }

        for (int z = 0; z < gridRows.Length; z++)
        {
            for (int x = 0; x < laneCount; x++)
            {
                int cur = gridRows[z].terrainLanes[x];
                int obsID = gridRows[z].obstacleLanes[x];
                if (cur < 0) continue;

                int next = cur;
                if (z + 1 < gridRows.Length)
                {
                    next = gridRows[z + 1].terrainLanes[x];
                    if (next < 0) next = cur;
                }

                float yS = cur * elevationStep;
                float yE = (Mathf.Abs(next - cur) == 1) ? next * elevationStep : yS;
                float bot = -foundationDepth;
                float xC = (x - centerOffset) * laneWidth;
                float hw = laneWidth / 2f;
                float zS = z * rowSpacing;
                float zE = (z + 1) * rowSpacing;

                Vector3 tBL = new Vector3(xC - hw, yS, zS), tBR = new Vector3(xC + hw, yS, zS);
                Vector3 tTL = new Vector3(xC - hw, yE, zE), tTR = new Vector3(xC + hw, yE, zE);
                Vector3 bBL = new Vector3(xC - hw, bot, zS), bBR = new Vector3(xC + hw, bot, zS);
                Vector3 bTL = new Vector3(xC - hw, bot, zE), bTR = new Vector3(xC + hw, bot, zE);

                DrawFace(tBL, tBR, tTL, tTR); // Top
                DrawFace(bBL, tBL, bTL, tTL); // Left
                DrawFace(tBR, bBR, tTR, bTR); // Right
                DrawFace(bBL, bBR, tBL, tBR); // Front
                DrawFace(bTL, tTR, bBL, tBR); // Back

                if (obsID > 0 && obsID < obstaclePrefabs.Length && obstaclePrefabs[obsID] != null)
                {
                    float midY = (yS + yE) / 2f;
                    Vector3 obsPos = new Vector3(xC, midY + obstacleYOffset, zS + rowSpacing / 2f);
                    Quaternion rot = Quaternion.LookRotation(new Vector3(0, yE - yS, rowSpacing).normalized);
                    Instantiate(obstaclePrefabs[obsID], chunkTransform.TransformPoint(obsPos), rot, chunkTransform);
                }
            }
        }

        Mesh m = new Mesh();
        m.vertices = verts.ToArray();
        m.triangles = tris.ToArray();
        m.uv = uvs.ToArray();
        m.RecalculateNormals();
        return m;
    }
}