using UnityEngine;
public class DebugCamera : MonoBehaviour {

    public bool lockZRotation;

    void Update() {
        if(lockZRotation) {
            Vector3 eulerAngles = transform.rotation.eulerAngles;
            eulerAngles.z = 0;
            transform.rotation = Quaternion.Euler(eulerAngles);
        }
    }
}