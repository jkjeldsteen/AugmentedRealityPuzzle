using UnityEngine;
using Vuforia;

public class VuforiaCameraTexture : MonoBehaviour
{
    public int camWidth;
    public int camHeight;

    private Texture2D cameraTexture;
    private Color32[] colorArray;

    Vuforia.Image image;

    void Start()
    {
        // Ensure Vuforia is initialized before accessing the camera
        VuforiaApplication.Instance.OnVuforiaStarted += OnVuforiaStarted;
    }

    void OnVuforiaStarted()
    {
        //Debug.Log("Vuforia Started - Ready to capture camera frames");
        bool success = VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(PixelFormat.RGB888, true);

    }

    void Update()
    {
        if (VuforiaBehaviour.Instance.CameraDevice == null)
        {
            Debug.LogWarning("No camera found, bozo");
            return;
        }

        if (VuforiaBehaviour.Instance.CameraDevice.IsActive)
        {
            // Get the camera image from Vuforia
            //Debug.Log("setup complete?: " + success);
            image = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(PixelFormat.RGB888);

            if (image != null)
            {
                if (cameraTexture == null || cameraTexture.width != image.Width || cameraTexture.height != image.Height)
                {
                    // Initialize Texture2D to store the camera frame
                    cameraTexture = new Texture2D(image.Width, image.Height, TextureFormat.RGB24, false);
                    camWidth = image.Width;
                    camHeight = image.Height;
                }

                // Load the raw pixel data
                cameraTexture.LoadRawTextureData(image.Pixels);
                cameraTexture.Apply();

                // Convert to Color32 array
                colorArray = cameraTexture.GetPixels32();
            }
            else
            {
                Debug.LogWarning("image was null");
            }
        }
        else
        {
            Debug.LogWarning("camera was inactive");
        }
    }

    public Texture2D GetCameraTexture()
    {
        return cameraTexture;
    }

    public Color32[] GetColorArray()
    {
        return colorArray;
    }
}
