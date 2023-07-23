using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System.Collections;

public class EndlessTerrain : NetworkBehaviour
{
	public LODInfo[] detailLevels;
	public static float maxViewDst;
	public static Transform viewer;
	public static Vector2 viewerPosition;
	Vector2 viewerPositionOld;
	static MapGenerator mapGenerator;
	int chunkSize;
	int chunksVisibleInViewDst;
	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
	public static bool checkServer;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    public override void OnNetworkSpawn()
    {
		StartCoroutine(startGame());
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

	private IEnumerator startGame()
	{
		yield return new WaitForSeconds(0.1f);
        mapGenerator = FindObjectOfType<MapGenerator>();
		viewer = Player.instance.transform;
        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
        checkServer = IsServer;
        if (FindObjectOfType<LobbySaver>() != null)
        {
            var storeVariables = FindObjectOfType<LobbySaver>();
            mapGenerator.seed = storeVariables.seedInt;
			mapGenerator.difficulty = storeVariables.difficultyInt;
        }
        UpdateVisibleChunks();
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

	public class TerrainChunk : MonoBehaviour
	{
		GameObject chunk;
		GameObject meshObject;
		GameObject water;
		GameObject vegetation;
		Material grassMat;

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
			grassMat = Instantiate(material2);
            meshRenderer.materials = new Material[] { material, grassMat };
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			meshObject.transform.position = positionV3 * scale;
			meshObject.transform.localScale = Vector3.one * scale;
			meshObject.isStatic = true;

            water = Instantiate(waterObject, positionV3 * scale, Quaternion.identity);
			water.transform.localScale *= mapGenerator.scale;

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
			grassMat.color = texture.GetPixelBilinear(MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);

            CreateVegetationServerRpc();
            CreateStructureServerRpc();

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

        [ServerRpc(RequireOwnership = false)]
        public void CreateVegetationServerRpc()
        {
            if (!checkServer) { return; }
            CreateVegetationClientRpc();
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
                if (sample.vegetations.Length <= 0) { return; }
                float posX = (topLeftX + sample.position.x) * scale;
                float treeHeight = mapData.heightCurve.Evaluate(mapData.heightMap[(int)sample.position.x, (int)sample.position.y]) * mapData.heightMultiplier * scale;
                float posZ = (topLeftZ - sample.position.y) * scale;
                var thisVegetation = sample.vegetations[Random.Range(0, sample.vegetations.Length)];
				if (thisVegetation.vegetation == null) { return; }
                GameObject veg = Instantiate(thisVegetation.vegetation, vegetation.transform.position + new Vector3(posX, treeHeight, posZ), Quaternion.identity);
                veg.transform.eulerAngles = new Vector3(Random.Range(-10, 10), Random.Range(-180, 180f), Random.Range(-10, 10));
                veg.transform.position += new Vector3(0, thisVegetation.addHeight, 0);
                if (veg.GetComponent<Rigidbody>() != null)
                {
                    veg.GetComponent<Rigidbody>().isKinematic = true;
                }
                if (veg.GetComponent<NetworkObject>() != null)
                {
                    veg.GetComponent<NetworkObject>().Spawn(true);
                }
				veg.transform.SetParent(mapGenerator.vegetationHolder.transform);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void CreateStructureServerRpc()
		{
            if (!checkServer) { return; }
			CreateStructureClientRpc();
        }

        [ClientRpc]
        public void CreateStructureClientRpc()
        {
            int width = mapData.heightMap.GetLength(0);
            int height = mapData.heightMap.GetLength(1);

            float topLeftX = (width - 1) / -2f;
            float topLeftZ = (height - 1) / 2f;
            float scale = mapGenerator.scale;
			if (mapData.poissonDiskSamples2.Count <= 0) { return; }
            int randStructure = Random.Range(0, mapData.poissonDiskSamples2[Random.Range(0, mapData.poissonDiskSamples2.Count)].structures.Length);

            foreach (PoissonSampleData sample in mapData.poissonDiskSamples2)
            {
                if (sample.structures.Length <= 0) { return; }
                float posX = (topLeftX + sample.position.x) * scale;
                float objectHeight = mapData.heightCurve.Evaluate(mapData.heightMap[(int)sample.position.x, (int)sample.position.y]) * mapData.heightMultiplier * scale;
                float posZ = (topLeftZ - sample.position.y) * scale;
                var structure = sample.structures[randStructure];
                var prop = structure.props[Random.Range(0, structure.props.Length)];
				if (prop.prop == null) { return; }
                GameObject thisProp = Instantiate(prop.prop, vegetation.transform.position + new Vector3(posX, objectHeight, posZ), Quaternion.identity);
                thisProp.transform.eulerAngles = new Vector3(0, Random.Range(-180, 180f), 0);
                thisProp.transform.position += new Vector3(0, prop.addHeight, 0);
                if (thisProp.GetComponent<Rigidbody>() != null)
                {
                    thisProp.GetComponent<Rigidbody>().isKinematic = true;
                }
                if (thisProp.GetComponent<NetworkObject>() != null)
                {
                    thisProp.GetComponent<NetworkObject>().Spawn(true);
                }
                thisProp.transform.SetParent(mapGenerator.vegetationHolder.transform);
            }
        }

        public void SetVisible(bool visible)
		{
			if (chunk == null) { return; }
            chunk.SetActive(visible);
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
