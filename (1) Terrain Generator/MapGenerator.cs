using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;
using TMPro;
using Random = UnityEngine.Random;
using IL3DN;

public class MapGenerator : MonoBehaviour
{
	[Header("Map Generation Settings")]
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
	public const int mapChunkSize = 239;
    public int seed;
	public bool randomSeed;
    public bool isMenu;

	[Header("Falloff Map")]
	public bool useFalloff;
	float[,] falloffMap;
	[Range(0, 1)]
	public float falloffStart;
	[Range(0, 1)]
	public float falloffEnd;

    [Header("Day Night Cycle")]
    public Light sun;
    public float dayNightMultiplier;
    [Range(0, 1)]
    public float time = 0;
    private bool day;
    private bool night;

    [Header("Music Player")]
    public AudioSource audioSource;
    public AudioClip[] music;

	[Header("Biome Generation Settings")]
	public Biome biome;
	public enum Biome { temperateForest, taiga, grassland, savanna, arctic };
	public TMP_Text biomeText;
    public IL3DN_Snow IL3DN_Snow;
    public int spawnRadius;
	public int waterLevel;
	public int maxHeight;
	public Biomes[] biomes;
	public Materials[] materials;

	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void Start()
	{
        if (!isMenu)
		{
            mesh.SetActive(false);
			water.SetActive(false);
        }
		CheckBiome();

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

	public void GenerateMap()
	{
		CheckBiome();
		mesh.GetComponent<MeshFilter>().sharedMesh = MeshGenerator.GenerateTerrainMesh(GenerateMapData(Vector2.zero).heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD).CreateMesh();
        DestroyImmediate(mesh.GetComponent<MeshCollider>());
		mesh.AddComponent<MeshCollider>();
		mesh.GetComponent<MeshRenderer>().materials = new Material[] { terrain, grass };
		mesh.transform.localScale = Vector3.one * scale;
		water.transform.position = new Vector3(0, waterLevel, 0);
		ClearObjects();
		GenerateVegetation();
		GenerateStructure();
    }

    public void CheckBiome()
	{
		if (isMenu)
		{
            biome = (Biome)FindObjectOfType<StoreVariables>().biomeInt;
            ClearObjects();
            GenerateVegetation();
        }
		if (randomSeed)
		{
			seed = Random.Range(0, 100000000);
		}
        if (biome == Biome.temperateForest)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].material.SetColor("_Color", materials[i].temperateForest);
            }
        }
        else if (biome == Biome.taiga)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].material.SetColor("_Color", materials[i].taiga);
            }
        }
        else if (biome == Biome.grassland)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].material.SetColor("_Color", materials[i].grassland);
            }
        }
        else if (biome == Biome.savanna)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].material.SetColor("_Color", materials[i].savanna);
            }
        }
        else if (biome == Biome.arctic)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].material.SetColor("_Color", materials[i].arctic);
            }
        }

		if (biome == Biome.arctic)
		{
            IL3DN_Snow.Snow = true;
        } 
		else
		{
            IL3DN_Snow.Snow = false;
        }
        materials[0].material.SetColor("Rock_Color", biomes[(int)biome].rockColor);
		RenderSettings.fogColor = biomes[(int)biome].fogColor;
		RenderSettings.fogDensity = biomes[(int)biome].fogDensity;
		if (!isMenu && biomeText != null)
		{
            biomeText.text = biomes[(int)biome].biome;
            biomeText.GetComponent<Animator>().Play("PopIn");
        }
	}

	public void GenerateVegetation()
	{
		RaycastHit hit;
		var biomeType = biomes[(int)biome];
		for (int x = 0; x < biomeType.vegetations.Length; x++)
		{
			for (int i = 0; i < biomeType.vegetations[x].amount; i++)
			{
                Vector3 position = new Vector3(Random.Range(-spawnRadius, spawnRadius) + mesh.transform.position.x, 0, Random.Range(-spawnRadius, spawnRadius) + mesh.transform.position.z);
				if (Physics.Raycast(position + new Vector3(0, maxHeight, 0), Vector3.down, out hit, maxHeight) && hit.point.y > biomeType.vegetations[x].spawnHeight.x && hit.point.y < biomeType.vegetations[x].spawnHeight.y && hit.collider.gameObject.tag == "Untagged")
				{
					for (int j = 0; j < biomeType.vegetations[x].amount; j++)
					{
                        Collider[] colliders = Physics.OverlapSphere(transform.position, 1);
                        foreach (Collider col in colliders)
                        {
                            if (col.tag == "Vegetation")
                            {
								break;
                            }
                        }

                        GameObject gameObject = Instantiate(biomeType.vegetations[x].vegetation, hit.point, Quaternion.identity);
                        biomeType.vegetations[x].vegetationName = biomeType.vegetations[x].vegetation.name;
                        if (biomeType.vegetations[x].terrainRotation)
                        {
                            gameObject.transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * gameObject.transform.rotation;
                        }
                        else
                        {
                            gameObject.transform.eulerAngles = new Vector3(Random.Range(0, 8f), Random.Range(0, 360f), Random.Range(0, 8f));
                        }

                        gameObject.transform.position += new Vector3(0, biomeType.vegetations[x].addHeight, 0);
                        gameObject.transform.SetParent(mesh.transform);
                        float randomScale = Random.Range(biomeType.vegetations[x].scale.x, biomeType.vegetations[x].scale.y);
                        gameObject.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
                        break;
					}
				}
			}
		}
	}

	public void GenerateStructure()
	{
		RaycastHit hit;
		for (int x = 0; x < biomes[(int)biome].structures.Length; x++) //Structures per biome
		{
			if (Random.value > 1 - biomes[(int)biome].structures[x].structureSpawnPercent)
			{
				for (int i = 0; i < biomes[(int)biome].structures[x].props.Length; i++) //Props per Structure
				{
					for (int j = 0; j < biomes[(int)biome].structures[x].props[i].amount; j++) //Each individual Props
					{
                        Vector3 position = new Vector3(Random.Range(-biomes[(int)biome].structures[x].props[i].spawnRadius, biomes[(int)biome].structures[x].props[i].spawnRadius) + mesh.transform.position.x, 0, Random.Range(-biomes[(int)biome].structures[x].props[i].spawnRadius, biomes[(int)biome].structures[x].props[i].spawnRadius) + mesh.transform.position.z);
						if (Physics.Raycast(position + new Vector3(0, maxHeight, 0), Vector3.down, out hit, maxHeight) && hit.point.y > biomes[(int)biome].structures[x].props[i].spawnHeight.x && hit.point.y < biomes[(int)biome].structures[x].props[i].spawnHeight.y && hit.collider.gameObject.tag == "Untagged")
						{
							for (int k = 0; k < biomes[(int)biome].structures[x].props[i].amount; k++)
							{
                                Collider[] colliders = Physics.OverlapSphere(transform.position, 2);
                                foreach (Collider col in colliders)
                                {
                                    if (col.tag == "Structure" || col.tag == "Vegetation")
                                    {
                                        break;
                                    }
                                }
                                GameObject gameObject = Instantiate(biomes[(int)biome].structures[x].props[i].prop, hit.point, Quaternion.identity);
								biomes[(int)biome].structures[x].props[i].propName = biomes[(int)biome].structures[x].props[i].prop.name;
								if (biomes[(int)biome].structures[x].props[i].terrainRotation)
								{
                                    gameObject.transform.eulerAngles = new Vector3(Random.Range(0, 8f), Random.Range(0, 360f), Random.Range(0, 8f));
                                    gameObject.transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * gameObject.transform.rotation;
                                }
								else
								{
									gameObject.transform.eulerAngles = new Vector3(Random.Range(0, 8f), Random.Range(0, 360f), Random.Range(0, 8f));
								}

								gameObject.transform.position += new Vector3(0, biomes[(int)biome].structures[x].props[i].addHeight, 0);
								gameObject.transform.SetParent(mesh.transform);
                                float randomScale = Random.Range(biomes[(int)biome].structures[x].props[i].scale.x, biomes[(int)biome].structures[x].props[i].scale.y);
                                gameObject.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
                                break;
							}
						}
					}
				}
				return;
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
		float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, seed, noiseScale, octaves, persistance, lacunarity, centre + offset);

		if (useFalloff)
        {
			noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, centre + offset);
			falloffMap = FalloffGenerator.Generate(new Vector2Int(mapChunkSize, mapChunkSize), falloffStart, falloffEnd);

			for (int y = 0; y < mapChunkSize; y++)
			{
				for (int x = 0; x < mapChunkSize; x++)
				{
					noiseMap[x, y] = falloffMap[x, y] - Mathf.Clamp01(noiseMap[x, y]);
				}
			}
		}
        return new MapData(noiseMap);
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

//Biome Generation
[System.Serializable]
public struct Biomes
{
	public String biome;
	public Color fogColor;
	public float fogDensity;
	public Color rockColor;
	public Vegetation[] vegetations;
	public Structure[] structures;
}

[System.Serializable]
public struct Vegetation
{
	public String vegetationName;
	public GameObject vegetation;
	public Vector2 spawnHeight;
	public Vector2 scale;
	public bool terrainRotation;
	public float addHeight;
	public float amount;
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
	public Vector2 spawnHeight;
	public Vector2 scale;
	public bool terrainRotation;
	public int spawnRadius;
    public float addHeight;
	public float amount;
}

[System.Serializable]
public struct Materials
{
	public String materialName;
	public Material material;
	public Color temperateForest;
	public Color taiga;
	public Color grassland;
	public Color savanna;
	public Color arctic;
}

public struct MapData
{
	public readonly float[,] heightMap;

	public MapData(float[,] heightMap)
	{
		this.heightMap = heightMap;
	}
}

