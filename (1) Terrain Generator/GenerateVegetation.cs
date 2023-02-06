using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateVegetation : NetworkBehaviour
{
    private MapGenerator mapGenerator;

    void Start()
    {
		mapGenerator = FindObjectOfType<MapGenerator>();
		GenerateVegetations();
        GenerateStructures();
    }

    public void GenerateVegetations()
	{
        Random.InitState(mapGenerator.seed);
        RaycastHit hit;
		var biomeType = mapGenerator.biomes[(int)mapGenerator.biome];
		for (int x = 0; x < biomeType.vegetations.Length; x++)
		{
			for (int i = 0; i < biomeType.vegetations[x].amount; i++)
			{
				Vector3 position = new Vector3(Random.Range(-mapGenerator.spawnRadius, mapGenerator.spawnRadius) + transform.position.x, 0, Random.Range(-mapGenerator.spawnRadius, mapGenerator.spawnRadius) + transform.position.z);
				if (Physics.Raycast(position + new Vector3(0, mapGenerator.maxHeight, 0), Vector3.down, out hit, mapGenerator.maxHeight) && hit.point.y > biomeType.vegetations[x].spawnHeight.x && hit.point.y < biomeType.vegetations[x].spawnHeight.y && hit.collider.gameObject.tag == "Untagged")
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

                        if (biomeType.vegetations[x].terrainRotation)
						{
							gameObject.transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * gameObject.transform.rotation;
						}
						else
						{
							gameObject.transform.eulerAngles = new Vector3(Random.Range(0, 8f), Random.Range(0, 360f), Random.Range(0, 8f));
						}
						gameObject.transform.position += new Vector3(0, biomeType.vegetations[x].addHeight, 0);
                        float randomScale = Random.Range(biomeType.vegetations[x].scale.x, biomeType.vegetations[x].scale.y) * mapGenerator.scale;
                        gameObject.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
                        gameObject.transform.SetParent(transform);
						NetworkServer.Spawn(gameObject);
                        break;
					}
				}
			}
		}
	}

    public void GenerateStructures()
	{
        RaycastHit hit;
		var biomeType = mapGenerator.biomes[(int)mapGenerator.biome];
		for (int x = 0; x < biomeType.structures.Length; x++) //Structures per biome
		{
			if (Random.value > 1 - biomeType.structures[x].structureSpawnPercent) //Structure Spawn Percentage
			{
				for (int i = 0; i < biomeType.structures[x].props.Length; i++) //Props per Structure
				{
					for (int j = 0; j < biomeType.structures[x].props[i].amount; j++) //Each individual Props
					{
						Vector3 position = new Vector3(Random.Range(-mapGenerator.biomes[(int)mapGenerator.biome].structures[x].props[i].spawnRadius, mapGenerator.biomes[(int)mapGenerator.biome].structures[x].props[i].spawnRadius) + transform.position.x, 0, Random.Range(-mapGenerator.biomes[(int)mapGenerator.biome].structures[x].props[i].spawnRadius, mapGenerator.biomes[(int)mapGenerator.biome].structures[x].props[i].spawnRadius) + transform.position.z);
						if (Physics.Raycast(position + new Vector3(0, mapGenerator.maxHeight, 0), Vector3.down, out hit, mapGenerator.maxHeight) && hit.point.y > biomeType.structures[x].props[i].spawnHeight.x && hit.point.y < biomeType.structures[x].props[i].spawnHeight.y && hit.collider.gameObject.tag == "Untagged")
						{
							for (int k = 0; k < biomeType.structures[x].props[i].amount; k++)
							{
                                Collider[] colliders = Physics.OverlapSphere(transform.position, 2);
                                foreach (Collider col in colliders)
                                {
                                    if (col.tag == "Structure" || col.tag == "Vegetation")
                                    {
                                        break;
                                    }
                                }
                                GameObject gameObject = Instantiate(biomeType.structures[x].props[i].prop, hit.point, Quaternion.identity);
                                if (biomeType.structures[x].props[i].terrainRotation)
								{
                                    gameObject.transform.eulerAngles = new Vector3(Random.Range(0, 8f), Random.Range(0, 360f), Random.Range(0, 8f));
                                    gameObject.transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * gameObject.transform.rotation;
								}
								else
								{
									gameObject.transform.eulerAngles = new Vector3(Random.Range(0, 8f), Random.Range(0, 360f), Random.Range(0, 8f));
								}

								gameObject.transform.position += new Vector3(0, biomeType.structures[x].props[i].addHeight, 0);
                                float randomScale = Random.Range(biomeType.structures[x].props[i].scale.x, biomeType.structures[x].props[i].scale.y);
                                gameObject.transform.localScale = new Vector3(randomScale, randomScale, randomScale) * mapGenerator.scale;
                                gameObject.transform.SetParent(transform);
                                NetworkServer.Spawn(gameObject);
                                break;
							}
						}
					}
				}
            }
		}
	}
}
