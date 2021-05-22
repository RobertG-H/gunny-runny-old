using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PowerUpPatchSpawner : NetworkBehaviour
{
    [SerializeField] GameObject patchPrefab;
    [SerializeField] List<PowerUpPatchScriptableObject> patchValues;
    [SerializeField] BoxCollider col;
    Bounds bounds;

    [SerializeField] float patchesPerSecond;

    void Start()
    {
        bounds = col.bounds;
    }

    public void StartSpawning()
    {
        InvokeRepeating("SpawnRandomPatch", 0f, 1f / patchesPerSecond);
    }

    Vector3 GetRandomSpawnPoint()
    {
        float randX = Random.Range(bounds.min.x, bounds.max.x);
        float randZ = Random.Range(bounds.min.z, bounds.max.z);
        return new Vector3(randX, transform.position.y, randZ);
    }

    PowerUpPatchScriptableObject GetRandomPatchValues()
    {
        return patchValues[Random.Range(0, patchValues.Count)];
    }

    void SpawnRandomPatch()
    {
        GameObject newPatch = Instantiate(patchPrefab, GetRandomSpawnPoint(), Quaternion.identity);
        NetworkServer.Spawn(newPatch);
        PowerUpPatchScriptableObject randomValues = GetRandomPatchValues();
        newPatch.GetComponent<PowerUpPatch>().RpcInitialize(randomValues);
        newPatch.GetComponent<PowerUpPatch>().ServerInitialize(randomValues);
    }
}
