using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class MapGenerator : MonoBehaviour
{
	[Header("Map Generation Settings")]
    public DrawMode drawMode;
    public enum DrawMode { Default, HeightMap, MoistureMap, BiomeColorMap, Mesh, VegetationMap };
    public GameObject mesh;
	public GameObject water;
	public Material terrain;
	public Material grass;
    public Material skybox;
    public GameObject waterObject;
	public Vector2 offset;
	[Range(0, 6)]
	public int editorPreviewLOD;
	[Range(0, 1)]
    public float persistance;
    public AnimationCurve meshHeightCurve;
    public float meshHeightMultiplier;
	public float noiseScale;
	public int octaves;
	public float lacunarity;
	public int scale;
	public const int mapChunkSize = 241;
    public int newPointsCount;
    public int seed;
    public bool isMenu;

    [Header("Day Night Cycle")]
    public Light sun;
    public float dayNightMultiplier;
    [Range(0, 1)]
    public float time = 0;
    private bool day;
    private bool night;
	private int dayNumber;

    [Header("Music Player")]
    public AudioSource audioSource;
    public AudioClip[] music;

	[Header("Biome Generation Settings")]
    public Difficulty difficulty;
    public enum Difficulty { Easy, Normal, Hardcore };
    public TerrainType[] regions;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void Start()
	{
        if (!isMenu)
		{
            mesh.SetActive(false);
			water.SetActive(false);
        }
        terrain.mainTexture = TextureGenerator.TextureFromColorMap(GenerateMapData(Vector2.zero).biomeMap, mapChunkSize, mapChunkSize);
        audioSource.clip = music[Random.Range(0, music.Length)];
		audioSource.Play();
	}

    private void Update()
	{
		if (mapDataThreadInfoQueue.Count > 0)
		{
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
			{
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}

		if (meshDataThreadInfoQueue.Count > 0)
		{
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
			{
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}

		if (!audioSource.isPlaying)
		{
			audioSource.clip = music[Random.Range(0, music.Length)];
			audioSource.Play();
		}

        DayNightCycle();
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);

        if (drawMode == DrawMode.Default)
        {
            GenerateMap();
        }
        else if (drawMode == DrawMode.HeightMap)
        {
            terrain.mainTexture = TextureGenerator.TextureFromHeightMap(mapData.heightMap);
        }
        else if (drawMode == DrawMode.MoistureMap)
        {
            terrain.mainTexture = TextureGenerator.TextureFromHeightMap(mapData.moistureMap);
        }
        else if (drawMode == DrawMode.BiomeColorMap)
        {
            terrain.mainTexture = TextureGenerator.TextureFromColorMap(mapData.biomeMap, mapChunkSize, mapChunkSize);
        }
        else if (drawMode == DrawMode.Mesh)
        {
            mesh.GetComponent<MeshFilter>().sharedMesh = MeshGenerator.GenerateTerrainMesh(GenerateMapData(Vector2.zero).heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD).CreateMesh();
            DestroyImmediate(mesh.GetComponent<MeshCollider>());
            mesh.AddComponent<MeshCollider>();
        }
        else if (drawMode == DrawMode.VegetationMap)
        {
            terrain.mainTexture = TextureGenerator.TextureFromVegetationList(mapData.poissonDiskSamples, mapData.heightMap.GetLength(0), mapData.heightMap.GetLength(1));
        }
    }

    public void GenerateMap()
	{
        if (isMenu)
        {
            difficulty = (Difficulty)FindObjectOfType<StoreVariables>().difficultyInt;
        }
        terrain.mainTexture = TextureGenerator.TextureFromColorMap(GenerateMapData(Vector2.zero).biomeMap, mapChunkSize, mapChunkSize);
        mesh.GetComponent<MeshFilter>().sharedMesh = MeshGenerator.GenerateTerrainMesh(GenerateMapData(Vector2.zero).heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD).CreateMesh();
        DestroyImmediate(mesh.GetComponent<MeshCollider>());
		mesh.AddComponent<MeshCollider>();
		mesh.GetComponent<MeshRenderer>().materials = new Material[] { terrain, grass };
		mesh.transform.localScale = Vector3.one * scale;
		water.transform.position = new Vector3(0, 22, 0);
        ClearObjects();
        CreateVegetation();
        CreateStructure();
    }

    public void CreateVegetation()
    {
		MapData mapData = GenerateMapData(Vector2.zero);
        int width = mapData.heightMap.GetLength(0);
        int height = mapData.heightMap.GetLength(1);

        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        foreach (PoissonSampleData sample in mapData.poissonDiskSamples)
        {
            float posX = (topLeftX + sample.position.x) * scale;
            float treeHeight = mapData.heightCurve.Evaluate(mapData.heightMap[(int)sample.position.x, (int)sample.position.y]) * mapData.heightMultiplier * scale;
            float posZ = (topLeftZ - sample.position.y) * scale;
            var vegetation = sample.vegetations[Random.Range(0, sample.vegetations.Length)];
            float randomScale = Random.Range(vegetation.scale.x, vegetation.scale.y);

            GameObject veg = Instantiate(vegetation.vegetation, mesh.transform.position + new Vector3(posX, treeHeight, posZ), Quaternion.identity, mesh.transform);
            veg.transform.eulerAngles = new Vector3(Random.Range(-10, 10), Random.Range(-180, 180f), Random.Range(-10, 10));
            veg.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
            veg.transform.position += new Vector3(0, vegetation.addHeight, 0);
        }
    }

    public void CreateStructure()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        int width = mapData.heightMap.GetLength(0);
        int height = mapData.heightMap.GetLength(1);

        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;
        int randStructure = Random.Range(0, mapData.poissonDiskSamples[Random.Range(0, mapData.poissonDiskSamples.Count)].structures.Length);
        float randPercent = Random.value;

        foreach (PoissonSampleData sample in mapData.poissonDiskSamples)
        {
            if (randPercent > 1 - sample.structures[randStructure].structureSpawnPercent)
            {
                float posX = (topLeftX + sample.position.x) * scale;
                float objectHeight = mapData.heightCurve.Evaluate(mapData.heightMap[(int)sample.position.x, (int)sample.position.y]) * mapData.heightMultiplier * scale;
                float posZ = (topLeftZ - sample.position.y) * scale;
                var structure = sample.structures[randStructure];
                var prop = structure.props[Random.Range(0, structure.props.Length)];
                float randomScale = Random.Range(prop.scale.x, prop.scale.y);

                GameObject thisStructure = Instantiate(prop.prop, mesh.transform.position + new Vector3(posX, objectHeight, posZ), Quaternion.identity, mesh.transform);
                thisStructure.transform.eulerAngles = new Vector3(0, Random.Range(-180, 180f), 0);
                thisStructure.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
                thisStructure.transform.position += new Vector3(0, prop.addHeight, 0);
            }
        }
    }

    public void ClearObjects()
	{
		for (int i = mesh.transform.childCount - 1; i >= 0; i--)
		{
			DestroyImmediate(mesh.transform.GetChild(i).gameObject);
		}
		DestroyImmediate(mesh.GetComponent<MeshCollider>());
		mesh.AddComponent<MeshCollider>();
	}

    public void DayNightCycle()
    {
        if (time <= 0)
        {
            day = true;
            night = false;
            StartCoroutine(UpdateDay());
        }
        else if (time >= 1)
        {
            day = false;
            night = true;
        }

        if (day)
        {
            time += Time.deltaTime * dayNightMultiplier;
        }
        if (night)
        {
            time -= Time.deltaTime * dayNightMultiplier;
        }

        sun.transform.localRotation = Quaternion.Euler(new Vector3((time * 360f) - 90f, 170f, 0));

        skybox.SetFloat("_CubemapTransition", time);
        skybox.SetFloat("_Exposure", 2 - time * 2);
    }

	private IEnumerator UpdateDay()
	{
		yield return new WaitForEndOfFrame();
        dayNumber++;
        if (Player.playerInstance != null)
            Player.playerInstance.dayText.text = "Day " + dayNumber.ToString();
    }

    //Multithreading
    public void RequestMapData(Vector2 centre, Action<MapData> callback)
	{
		ThreadStart threadStart = delegate {
			MapDataThread(centre, callback);
		};

		new Thread(threadStart).Start();
	}

	void MapDataThread(Vector2 centre, Action<MapData> callback)
	{
		MapData mapData = GenerateMapData(centre);
		lock (mapDataThreadInfoQueue)
		{
			mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
		}
	}

	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
	{
		ThreadStart threadStart = delegate {
			MeshDataThread(mapData, lod, callback);
		};

		new Thread(threadStart).Start();
	}

	void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
	{
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
		lock (meshDataThreadInfoQueue)
		{
			meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

	MapData GenerateMapData(Vector2 centre)
	{
        float[,] heightMap = Noise.GenerateNoiseMap(seed, mapChunkSize, mapChunkSize, noiseScale, octaves, persistance, lacunarity, centre + offset);

        float[,] moistureMap = Noise.GenerateNoiseMap(seed * 2, mapChunkSize, mapChunkSize, noiseScale, octaves, persistance, lacunarity, centre + offset);

        Color[] biomeMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {

                float currentHeight = heightMap[x, y];
                float currentMoisture = moistureMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].height && currentMoisture >= regions[i].moisture)
                    {
                        biomeMap[y * mapChunkSize + x] = regions[i].color;
                    }
                }

            }
        }

        List<PoissonSampleData> poissonDiskSamples = new List<PoissonSampleData>();

        for (int i = 0; i < regions.Length; i++)
        {
            List<Vector2> poissonDiskSamplesRegion = Noise.GeneratePoissonDiskSampling(seed, mapChunkSize, mapChunkSize, newPointsCount, regions[i].vegMinDistance);

            for (int k = 0; k < poissonDiskSamplesRegion.Count; k++)
            {

                Color biomeColor = biomeMap[(int)poissonDiskSamplesRegion[k].y * mapChunkSize + (int)poissonDiskSamplesRegion[k].x];

                if (biomeColor.Equals(regions[i].color))
                {
                    poissonDiskSamples.Add(new PoissonSampleData(poissonDiskSamplesRegion[k], regions[i].vegetations, regions[i].structures));
                }
            }
        }

        return new MapData(heightMap, moistureMap, biomeMap, poissonDiskSamples, meshHeightCurve, meshHeightMultiplier);
    }

	struct MapThreadInfo<T>
	{
		public readonly Action<T> callback;
		public readonly T parameter;

		public MapThreadInfo(Action<T> callback, T parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}

	}
}

[System.Serializable]
public struct Vegetation
{
	public String vegetationName;
	public GameObject vegetation;
	public Vector2 scale;
	public float addHeight;
}

[System.Serializable]
public struct Structure
{
	public String structureName;
	public float structureSpawnPercent;
	public Props[] props;
}

[System.Serializable]
public struct Props
{
	public String propName;
	public GameObject prop;
	public Vector2 scale;
    public float addHeight;
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public float moisture;
    public float vegMinDistance;
    public float structMinDistance;
    public Color color;
    public Vegetation[] vegetations;
    public Structure[] structures;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly float[,] moistureMap;
    public readonly Color[] biomeMap;
    public readonly List<PoissonSampleData> poissonDiskSamples;
    public readonly AnimationCurve heightCurve;
    public readonly float heightMultiplier;

    public MapData(float[,] heightMap, float[,] moistureMap, Color[] biomeMap, List<PoissonSampleData> poissonDiskSamples, AnimationCurve heightCurve, float heightMultiplier)
    {
        this.heightMap = heightMap;
        this.moistureMap = moistureMap;
        this.biomeMap = biomeMap;
        this.poissonDiskSamples = poissonDiskSamples;
        this.heightCurve = heightCurve;
        this.heightMultiplier = heightMultiplier;
    }
}

public struct PoissonSampleData
{
    public readonly Vector2 position;
    public Vegetation[] vegetations;
    public Structure[] structures;

    public PoissonSampleData(Vector2 position, Vegetation[] vegetations, Structure[] structures)
    {
        this.position = position;
        this.vegetations = vegetations;
        this.structures = structures;
    }
}

