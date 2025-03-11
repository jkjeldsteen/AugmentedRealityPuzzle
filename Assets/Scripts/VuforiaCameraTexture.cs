using UnityEngine;
using Vuforia;

public class VuforiaCameraTexture : MonoBehaviour
{
    [HideInInspector] public int camWidth;
    [HideInInspector] public int camHeight;
    private Texture2D cameraTexture;
    private Color32[] colorArray;
    private Vuforia.Image image;

    void Start()
    {
        VuforiaApplication.Instance.OnVuforiaStarted += OnVuforiaStarted;
    }

    void OnVuforiaStarted()
    {
        bool success = VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(PixelFormat.RGB888, true);
    }

    void Update()
    {
        if (VuforiaBehaviour.Instance.CameraDevice.IsActive)
        {
            image = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(PixelFormat.RGB888);

            if (image != null)
            {
                if (cameraTexture == null || cameraTexture.width != image.Width || cameraTexture.height != image.Height)
                {
                    cameraTexture = new Texture2D(image.Width, image.Height, TextureFormat.RGB24, false);
                    camWidth = image.Width;
                    camHeight = image.Height;
                }

                cameraTexture.LoadRawTextureData(image.Pixels);
                cameraTexture.Apply();

                colorArray = cameraTexture.GetPixels32();
            }
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
