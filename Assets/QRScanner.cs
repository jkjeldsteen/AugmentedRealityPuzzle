using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using System.Collections;
using UnityEngine.Windows.WebCam;
using System;
using System.Drawing;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static UnityEngine.UI.GridLayoutGroup;
using Vuforia;
using UnityEngine.PlayerLoop;
using Unity.XR.CoreUtils;

public class QRScanner : MonoBehaviour
{
    GameObject videoBackground;

    [Header("Targets")]
    public float errorMargain = 10f;
    Quaternion upTarget = new Quaternion();
    Quaternion rightTarget = new Quaternion();
    Quaternion downTarget = new Quaternion();
    Quaternion leftTarget = new Quaternion();
    float[] diffs = new float[4];

    public float customZValue = 100f;
    public GameObject qrPosTracker;
    public RawImage rawCamOutput;
    int camHeight;
    int camWidth;
    byte[] imageBytes;
    System.Func<RGBLuminanceSource, LuminanceSource> f;
    List<Tuple<Vector2, Vector3>> qrTransforms = new List<Tuple<Vector2, Vector3>>();
    VuforiaCameraTexture vuforiaCam;

    private WebCamTexture webcamTexture;
    private BarcodeReader<RGBLuminanceSource> barcodeReader;

    void Start()
    {
        vuforiaCam = GetComponent<VuforiaCameraTexture>();

        barcodeReader = new BarcodeReader<RGBLuminanceSource>(f)
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            }
        };


        upTarget.eulerAngles = new Vector3(-90, 0, 0);
        rightTarget.eulerAngles = new Vector3(0, 90, -90);
        downTarget.eulerAngles = new Vector3(90, 0, -180);
        leftTarget.eulerAngles = new Vector3(0, -90, 90);

        // Start the webcam
        //webcamTexture = new WebCamTexture(720, 480, 60);
        //webcamTexture = new WebCamTexture();


        //webcamTexture.Play();
        //InvokeRepeating(nameof(ScanQRCode), 0.5f, 0.5f);
    }

    void ScanQRCode()
    {
        if (videoBackground == null)
        {
            videoBackground = GameObject.Find("VideoBackground");
        }
        
        if (videoBackground != null)
        {
            if (videoBackground.activeSelf)
            {
                videoBackground.SetActive(false);
            }
        }

        //if (webcamTexture.width > 100 && webcamTexture.height > 100)
        {
            //try
            {
                Texture2D tx = vuforiaCam.GetCameraTexture();
                if (tx == null)
                {
                    return;
                }
                rawCamOutput.texture = tx;

                Color32[] pixels = vuforiaCam.GetColorArray();
                int width = vuforiaCam.camWidth;
                int height = vuforiaCam.camHeight;
                byte[] rawRGB = ConvertColor32ToByteArray(pixels);
                LuminanceSource source = new RGBLuminanceSource(rawRGB, width, height, RGBLuminanceSource.BitmapFormat.RGB32);

                Result[] results = barcodeReader.DecodeMultiple(source);
                string allCodes = "QRs detected: ";
                qrTransforms.Clear();

                if (results == null || results.Length == 0)
                {
                    return;
                }

                for (int i = 0; i < results.Length; i++)
                {
                    if (i != 0)
                    {
                        allCodes += ", ";
                    }
                    allCodes += results[i].Text;
                    ResultPoint[] points = results[i].ResultPoints;
                    //object orientation;
                    //results[i].ResultMetadata.TryGetValue(ResultMetadataType.ORIENTATION, out orientation);
                    //int orientationDegress = Convert.ToInt32(orientation);

                    //float averageX = (points[0].X + points[1].X + points[2].X) / 3f;
                    //float averageY = (points[0].Y + points[1].Y + points[2].Y) / 3f;

                    Vector2 p0 = new Vector2(points[0].X, points[0].Y);
                    Vector2 p1 = new Vector2(points[1].X, points[1].Y);
                    Vector2 p2 = new Vector2(points[2].X, points[2].Y);

                    //Vector2 dir = Vector3.Normalize(p0 - p2);

                    //float angle = Vector2.Angle(p0, p2);

                    float averageX = (points[0].X + points[2].X) / 2f;
                    float averageY = (points[0].Y + points[2].Y) / 2f;
                    Vector2 averagePosition = new Vector2(averageX, averageY);
                    Vector3 dir = Vector3.Normalize(averagePosition - p1);

                    qrTransforms.Add(new Tuple<Vector2, Vector3>(averagePosition, dir));


                    if (allCodes != "QRs detected: , ")
                    {
                        //Debug.Log(dir);
                        //Debug.Log(points[0]);
                        //Debug.Log(allCodes + " - At Position: " + averagePosition);
                        //Debug.Log(allCodes + " - With rotation degrees: " + orientationDegress);
                    }
                }

            }
            //catch (Exception e)
            //{
                //Debug.Log("QR Scanning Error: " + e.Message);
            //}
        }
        //else
        //{
        //    Debug.LogWarning("Image was smaller than 100x100");
        //}
    }
    

    private void Update()
    {
        //if (videoBackground == null)
        //{
        //    return;
        //}

        ScanQRCode();

        foreach (Tuple<Vector2, Vector3> qr in qrTransforms)
        {
            Resolution currentResolution = Screen.currentResolution;
            int w = currentResolution.width;

            Vector2 invertedYPos = new Vector2(qr.Item1.x, (float)vuforiaCam.camHeight - qr.Item1.y);
            invertedYPos -= (new Vector2((float)vuforiaCam.camWidth, (float)vuforiaCam.camHeight) / 2f);
            //Vector3 screenpoint = Camera.main.transform.InverseTransformPoint(invertedYPos);
            //Vector3 screenpoint = Camera.main.ScreenToWorldPoint

            //Vector2 worldPoint = new Vector2(
            //    Mathf.Lerp(0f, videoBackground.transform.localScale.x, screenpoint.x / (float)vuforiaCam.camWidth),
            //    Mathf.Lerp(0f, videoBackground.transform.localScale.y, screenpoint.y / (float)vuforiaCam.camHeight)
            //    );

            qrPosTracker.transform.localPosition = new Vector3(invertedYPos.x, invertedYPos.y, 1900f);
            qrPosTracker.transform.localRotation = Quaternion.Inverse(Camera.main.transform.localRotation);
            qrPosTracker.transform.localRotation.SetLookRotation(qr.Item2, Vector3.forward);

            DetectDirection(qrPosTracker.transform.localRotation);

            //Gizmos.matrix = Camera.main.transform.worldToLocalMatrix;
            //Gizmos.DrawCube(qrPosition, Vector3.one);
        }
    }

    private void DetectDirection(Quaternion _rotation)
    {
        diffs[0] = Quaternion.Angle(upTarget, _rotation);
        diffs[1] = Quaternion.Angle(rightTarget, _rotation);
        diffs[2] = Quaternion.Angle(downTarget, _rotation);
        diffs[3] = Quaternion.Angle(leftTarget, _rotation);

        float smallest = Mathf.Min(diffs);

        if (smallest == diffs[0])
        {
            Debug.Log("UP");
        }
        else if (smallest == diffs[1])
        {
            Debug.Log("RIGHT");
        }
        else if (smallest == diffs[2])
        {
            Debug.Log("DOWN");
        }
        else if (smallest == diffs[3])
        {
            Debug.Log("LEFT");
        }
    }

    // Convert Color32 array to byte array in RGB32 format (RGBA)
    private byte[] ConvertColor32ToByteArray(Color32[] colors)
    {
        byte[] bytes = new byte[colors.Length * 4]; // RGBA (4 bytes per pixel)
        for (int i = 0; i < colors.Length; i++)
        {
            bytes[i * 4] = colors[i].r;
            bytes[i * 4 + 1] = colors[i].g;
            bytes[i * 4 + 2] = colors[i].b;
            bytes[i * 4 + 3] = colors[i].a; // Alpha channel (not needed for QR detection)
        }
        return bytes;
    }

    //void OnDestroy()
    //{
    //    webcamTexture.Stop();
    //}
}