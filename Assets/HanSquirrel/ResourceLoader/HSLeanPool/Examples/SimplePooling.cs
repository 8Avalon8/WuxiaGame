#if UNITY_EDITOR  && false
using UnityEngine;
using System.Collections.Generic;
using HanSquirrel.ResourceManager;

// This script shows you how you can easily spawn and despawn a prefab
public class SimplePooling : MonoBehaviour
{
	public GameObject Prefab;
	
	private List<GameObject> clones = new List<GameObject>();
	
	public void SpawnPrefab()
	{
		var position = (Vector3)Random.insideUnitCircle * 6.0f;
		var clone    = ResourceLoader.Spawn(Prefab, position, Quaternion.identity, null);
		
		clones.Add(clone);
	}
	
	public void DespawnPrefab()
	{
		if (clones.Count > 0)
		{
			// Get the last clone
			var index = clones.Count - 1;
			var clone = clones[index];
			
			// Remove it
			clones.RemoveAt(index);

            // Despawn it
            ResourceLoader.Despawn(clone);
		}
	}
}
#endif
