using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace com.utkaka.InstancedVoxels.Runtime {
	public class RotatingCamera : MonoBehaviour {
		[SerializeField]
		private float _speedMod = 360.0f;
		[SerializeField]
		private Vector3 _point;
	
		private void Start () {
			//Application.targetFrameRate = 60;
			transform.LookAt(_point);
		}

		public void Update () {
			//transform.RotateAround (_point,Vector3.up,Time.deltaTime * _speedMod);
			transform.RotateAround (_point,Vector3.up,0.02f);
			transform.LookAt(_point);
		}	
	}
}