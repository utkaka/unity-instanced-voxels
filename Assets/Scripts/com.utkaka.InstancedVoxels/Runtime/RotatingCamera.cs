using UnityEngine;

namespace com.utkaka.InstancedVoxels.Runtime {
	public class RotatingCamera : MonoBehaviour {
		[SerializeField]
		private float _speedMod = 12.0f;
		[SerializeField]
		private Vector3 _point;
	
		private void Start () {
			transform.LookAt(_point);
		}

		public void Update () {
			transform.RotateAround (_point,Vector3.up,Time.deltaTime * _speedMod);
			transform.LookAt(_point);
		}	
	}
}