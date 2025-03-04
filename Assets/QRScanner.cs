using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using System.Collections;
using UnityEngine.Windows.WebCam;
using System;

public class QRScanner : MonoBehaviour
{
    /*
    BarcodeReader<RGBLuminanceSource> barReader;
    WebCamTexture camTexture;

    //RGBLuminanceSource RGBsource;
    */

    public RawImage rawCamOutput;
    int camHeight;
    int camWidth;
    byte[] imageBytes;
    /*
    byte value = 0;
    Color[] pixels;

    int webcamResolutionX = 3840;
    int webcamResolutionY = 2160;
    int webcamFPS = 30;
    */
    System.Func<RGBLuminanceSource, LuminanceSource> f;
    

    private WebCamTexture webcamTexture;
    private BarcodeReader<RGBLuminanceSource> barcodeReader;

    void Start()
    {
        f = Sum;
        barcodeReader = new BarcodeReader<RGBLuminanceSource>(f)
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            }
        };

        // Initialize ZXing barcode reader
        /*
        barcodeReader = new BarcodeReader(f)
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            },
            
        };
        */

        // Start the webcam
        webcamTexture = new WebCamTexture(720, 480, 60);

        //webcamTexture = new WebCamTexture();
        
        rawCamOutput.texture = webcamTexture;
        //GetComponent<Renderer>().material.mainTexture = webcamTexture;
        webcamTexture.Play();

        // Start scanning loop
        InvokeRepeating(nameof(ScanQRCode), 0.5f, 0.5f);
    }

    void ScanQRCode()
    {
        if (webcamTexture.width > 100 && webcamTexture.height > 100)
        {
            try
            {
                Color32[] pixels = webcamTexture.GetPixels32();
                int width = webcamTexture.width;
                int height = webcamTexture.height;
                // Convert Color32[] to a byte array

                byte[] rawRGB = ConvertColor32ToByteArray(pixels);

                // Create Luminance Source
                LuminanceSource source = new RGBLuminanceSource(rawRGB, width, height, RGBLuminanceSource.BitmapFormat.RGB32);

                Result[] results = barcodeReader.DecodeMultiple(source);
                //var result = barcodeReader.Decode(pixels, width, height, RGBLuminanceSource.BitmapFormat.RGB24);

                string allCodes = "QRs detected: ";

                for (int i = 0; i < results.Length; i++)
                {
                    if (i != 0)
                    {
                        allCodes += ", ";
                    }
                    allCodes += results[i].Text;
                    ResultPoint[] points = results[i].ResultPoints;

                    if (allCodes != "QRs detected: , ")
                    {
                        //Debug.Log(output + " : " + points[1]);
                        Debug.Log(allCodes);
                    }
                }

                //if (result != null)
                //{
                //    Debug.Log("QR Code Detected: " + result.Text);
                //}
            }
            catch (Exception e)
            {
                //Debug.Log("QR Scanning Error: " + e.Message);
            }
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

    void OnDestroy()
    {
        webcamTexture.Stop();
    }

    RGBLuminanceSource Sum(RGBLuminanceSource _source)
    {
        //ReadTexture();
        return new RGBLuminanceSource(imageBytes, camWidth, camHeight);
    }

}
    /*

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //WebCam webCam = new WebCam();
        camTexture = new WebCamTexture(webcamResolutionX, webcamResolutionY, webcamFPS);
        camHeight = camTexture.height;
        camWidth = camTexture.width;
        camTexture.Play();
        rawCamOutput.texture = camTexture;
        f = Sum;
        barReader = new BarcodeReader<RGBLuminanceSource>(f);
    }

    

    private void ReadTexture()
    {
        pixels = camTexture.GetPixels();
        imageBytes = new byte[pixels.Length * 3];

        for (int i = 0; i < pixels.Length; i++)
        {
            ConvertColorToBW(pixels[i], i);
        }
    }

    
    public void ConvertColorToBW(Color _color, int _index)
    {
        float r, g, b;
        r = _color.r;
        g = _color.g;
        b = _color.b;
        
        if (r + g + b > 1.5f)
        {
            r = 1f;
            g = 1f;
            b = 1f;
        }
        else
        {
            r = 0f;
            g = 0f;
            b = 0f;
        }

        imageBytes[0 + _index] = (byte)(r * 255);
        imageBytes[1 + _index] = (byte)(g * 255);
        imageBytes[2 + _index] = (byte)(b * 255);

        //v = (byte)r;

        //return new Color(r, g, b);

        //float hue, sat, val;
        //Color.RGBToHSV(new Color(r, g, b), out hue, out sat, out val);
        //return Color.HSVToRGB(hue, 0f, val, false);
    }
    

    // Update is called once per frame
    void Update()
    {
        if (!camTexture.isPlaying)
        {
            Debug.Log("Camera offline");
            return;
        }

        ReadTexture();
        Result[] results = barReader.DecodeMultiple(imageBytes, camWidth, camHeight, RGBLuminanceSource.BitmapFormat.RGB24);
        //Result[] results = barReader.DecodeMultiple();

        if (results != null && results.Length > 0)
        {
            for (int i = 0; i < results.Length; i++)
            {
                string output = results[i].Text;
                ResultPoint[] points = results[i].ResultPoints;

                if (output != "")
                {
                    //Debug.Log(output + " : " + points[1]);
                    Debug.Log(output);
                }
            }
        }
    }
}
*/


/*
public class BinarizerCustom : Binarizer
{
    public override BitMatrix BlackMatrix => throw new System.NotImplementedException();

    public override Binarizer createBinarizer(LuminanceSource source)
    {
        throw new System.NotImplementedException();
    }

    public override BitArray getBlackRow(int y, BitArray row)
    {
        throw new System.NotImplementedException();
    }
}
*/