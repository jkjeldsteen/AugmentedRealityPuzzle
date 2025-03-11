using UnityEngine;
using System.IO;
using Vuforia;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


public class CameraIntrinsicsLoader : MonoBehaviour
{

    ARCameraManager arCameraManager;
    void Start()
    {
        arCameraManager = FindFirstObjectByType<ARCameraManager>();
        arCameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (arCameraManager.TryGetIntrinsics(out XRCameraIntrinsics intrinsics))
        {
            Debug.Log("FX: " + intrinsics.focalLength.x);
            Debug.Log("CX: " + intrinsics.principalPoint.x);
        }
    }
}
