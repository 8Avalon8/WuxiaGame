#if UNITY_EDITOR && false
using UnityEngine;
using System.Collections.Generic;
using HanSquirrel.ResourceManager;

// This script shows you how you can easily spawn and despawn a prefab using the delay functionality - same as Destroy(obj, __delay__)
public class SimpleDelayedPooling : MonoBehaviour
{
	public GameObject Prefab;
	
	public float DespawnDelay = 1.0f;
	
	public void SpawnPrefab()
	{
		var position = (Vector3)Random.insideUnitCircle * 6.0f;
		var clone    = ResourceLoader.Spawn(Prefab, position, Quaternion.identity, null);

        ResourceLoader.Despawn(clone, DespawnDelay);
	}
}
#endif