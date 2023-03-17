using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class EndlessTerrain : NetworkBehaviour
{
	public LODInfo[] detailLevels;
	public static float maxViewDst;

	public Transform viewer;
	public static Player playerInstance;
	public static Vector2 viewerPosition;
	Vector2 viewerPositionOld;
	static MapGenerator mapGenerator;
	int chunkSize;
	int chunksVisibleInViewDst;
	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    public override void OnNetworkSpawn()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        viewer = Player.playerInstance.transform;

        if (FindObjectOfType<StoreVariables>() != null)
        {
            var storeVariables = FindObjectOfType<StoreVariables>();
            mapGenerator.seed = storeVariables.seedInt;
            mapGenerator.difficulty = (MapGenerator.Difficulty)(int)storeVariables.difficultyInt;
        }

        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);

        UpdateVisibleChunks();
    }

    private void Update()
	{
		if (viewer == null)
		{
			return;
		}

        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.scale;

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

	private void UpdateVisibleChunks()
	{
		for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
		{
			terrainChunksVisibleLastUpdate[i].SetVisible(false);
		}
		terrainChunksVisibleLastUpdate.Clear();

		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
		{
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
			{
				Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
				{
					terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
				}
				else
				{
					terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, mapGenerator.scale, chunkSize, detailLevels, transform, mapGenerator.terrain, mapGenerator.grass, mapGenerator.waterObject));
				}
			}
		}
	}

	public class TerrainChunk
	{
		GameObject chunk;
		GameObject meshObject;
		GameObject water;
		GameObject vegetation;

		Vector2 position;
		Bounds bounds;

        MeshRenderer meshRenderer;
		MeshFilter meshFilter;
		MeshCollider meshCollider;

		LODInfo[] detailLevels;
		LODMesh[] lodMeshes;
		LODMesh collisionLODMesh;

		MapData mapData;
		bool mapDataReceived;
		int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int scale, int size, LODInfo[] detailLevels, Transform parent, Material material, Material material2, GameObject waterObject)
		{
			this.detailLevels = detailLevels;
			position = coord * size;
			bounds = new Bounds(position, Vector2.one * size);
			Vector3 positionV3 = new Vector3(position.x, 0, position.y);

			meshObject = new GameObject("Terrain Chunk");
			meshObject.layer = 6;
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshCollider = meshObject.AddComponent<MeshCollider>();
			meshRenderer.materials = new Material[] { material, material2 };
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			meshObject.transform.position = positionV3 * scale;
			meshObject.transform.localScale = Vector3.one * scale;
			meshObject.isStatic = true;

			water = Instantiate(waterObject, positionV3 * scale + new Vector3(0, 22, 0), Quaternion.identity);

			vegetation = new GameObject("Vegetation");
			vegetation.transform.position = positionV3 * scale;
			vegetation.isStatic = true;

            chunk = new GameObject("Chunk");
			meshObject.transform.SetParent(chunk.transform);
			water.transform.SetParent(chunk.transform);
			vegetation.transform.SetParent(chunk.transform);
			chunk.transform.parent = parent;

            SetVisible(false);

			lodMeshes = new LODMesh[detailLevels.Length];
			for (int i = 0; i < detailLevels.Length; i++)
			{
				lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
				if (detailLevels[i].useForCollider)
				{
					collisionLODMesh = lodMeshes[i];
				}
			}

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

		void OnMapDataReceived(MapData mapData)
		{
			this.mapData = mapData;
			mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.biomeMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

			if (meshObject.activeSelf)
			{
                CreateVegetationClientRpc();
            }
			//CreateStructure();

            UpdateTerrainChunk();
		}        

        public void UpdateTerrainChunk()
		{
			if (mapDataReceived)
			{
				float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
				bool visible = viewerDstFromNearestEdge <= maxViewDst;

                if (visible)
				{
					int lodIndex = 0;

					for (int i = 0; i < detailLevels.Length - 1; i++)
					{
						if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
						{
							lodIndex = i + 1;
						}
						else
						{
							break;
						}
					}

					if (lodIndex != previousLODIndex)
					{
						LODMesh lodMesh = lodMeshes[lodIndex];
						if (lodMesh.hasMesh)
						{
							previousLODIndex = lodIndex;
							meshFilter.mesh = lodMesh.mesh;
						}
						else if (!lodMesh.hasRequestedMesh)
						{
							lodMesh.RequestMesh(mapData);
						}
					}

					if (lodIndex == 0 || lodIndex == 1)
					{
						if (collisionLODMesh.hasMesh)
						{
							meshCollider.sharedMesh = collisionLODMesh.mesh;
                        }
						else if (!collisionLODMesh.hasRequestedMesh)
						{
							collisionLODMesh.RequestMesh(mapData);
						}
					}

                    terrainChunksVisibleLastUpdate.Add(this);
				}

                SetVisible(visible);
			}
		}

		[ClientRpc]
        public void CreateVegetationClientRpc()
        {
            int width = mapData.heightMap.GetLength(0);
            int height = mapData.heightMap.GetLength(1);

            float topLeftX = (width - 1) / -2f;
            float topLeftZ = (height - 1) / 2f;
            float scale = mapGenerator.scale;

            foreach (PoissonSampleData sample in mapData.poissonDiskSamples)
			{
				float posX = (topLeftX + sample.position.x) * scale;
                float treeHeight = mapData.heightCurve.Evaluate(mapData.heightMap[(int)sample.position.x, (int)sample.position.y]) * mapData.heightMultiplier * scale;
				float posZ = (topLeftZ - sample.position.y) * scale;
                var thisVegetation = sample.vegetations[Random.Range(0, sample.vegetations.Length)];
                float randomScale = Random.Range(thisVegetation.scale.x, thisVegetation.scale.y) * scale;

                GameObject veg = Instantiate(thisVegetation.vegetation, vegetation.transform.position + new Vector3(posX, treeHeight, posZ), Quaternion.identity, vegetation.transform);
                veg.transform.eulerAngles = new Vector3(Random.Range(-10, 10), Random.Range(-180, 180f), Random.Range(-10, 10));
                veg.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
                veg.transform.position += new Vector3(0, thisVegetation.addHeight, 0);
				var vegNetwork = veg.GetComponent<NetworkObject>();
				if (vegNetwork != null)
					vegNetwork.Spawn();
            }
        }

        public void CreateStructure()
        {
            int width = mapData.heightMap.GetLength(0);
            int height = mapData.heightMap.GetLength(1);

            float topLeftX = (width - 1) / -2f;
            float topLeftZ = (height - 1) / 2f;
            float scale = mapGenerator.scale;
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
                    float randomScale = Random.Range(prop.scale.x, prop.scale.y) * scale;

                    GameObject thisStructure = Instantiate(prop.prop, vegetation.transform.position + new Vector3(posX, objectHeight, posZ), Quaternion.identity, vegetation.transform);
                    thisStructure.transform.eulerAngles = new Vector3(0, Random.Range(-180, 180f), 0);
                    thisStructure.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
                    thisStructure.transform.position += new Vector3(0, prop.addHeight, 0);
                }
            }
        }

        public void SetVisible(bool visible)
		{
			if (chunk != null)
			{
                chunk.SetActive(visible);
            }
		}
	}

	class LODMesh
	{

		public Mesh mesh;
		public bool hasRequestedMesh;
		public bool hasMesh;
		int lod;
		System.Action updateCallback;

		public LODMesh(int lod, System.Action updateCallback)
		{
			this.lod = lod;
			this.updateCallback = updateCallback;
		}

		void OnMeshDataReceived(MeshData meshData)
		{
			mesh = meshData.CreateMesh();
			hasMesh = true;

			updateCallback();
		}

		public void RequestMesh(MapData mapData)
		{
			hasRequestedMesh = true;
			mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
		}

	}

	[System.Serializable]
	public struct LODInfo
	{
		public int lod;
		public float visibleDstThreshold;
		public bool useForCollider;
	}

}
