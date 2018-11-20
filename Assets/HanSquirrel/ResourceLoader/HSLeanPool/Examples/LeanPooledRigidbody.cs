#if UNITY_EDITOR && false
using UnityEngine;
using System.Collections;

namespace HanSquirrel.ResourceManager
{
	// This component will automatically reset a Rigidbody when it gets spawned/despawned
	[RequireComponent(typeof(Rigidbody))]
	public class LeanPooledRigidbody : MonoBehaviour
	{
		protected virtual void OnSpawn(params object[] paras)
		{
			// Do nothing
		}
		
		protected virtual void OnDespawn()
		{
			var rigidbody = GetComponent<Rigidbody>();
			
			// Reset velocities
			rigidbody.velocity        = Vector3.zero;
			rigidbody.angularVelocity = Vector3.zero;
		}
	}
}
#endif
