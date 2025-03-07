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
using Vuforia;
using Unity.XR.CoreUtils;
using System.Linq;

public enum PieceDirection { UP, RIGHT, DOWN, LEFT }

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
    public PuzzlePiece qrPuzzlePiece;
    public RawImage rawCamOutput;
    int camHeight;
    int camWidth;
    byte[] imageBytes;
    System.Func<RGBLuminanceSource, LuminanceSource> f;
    List<Tuple<Vector2, Vector3, int>> qrPieces = new List<Tuple<Vector2, Vector3, int>>();
    VuforiaCameraTexture vuforiaCam;

    private WebCamTexture webcamTexture;
    private BarcodeReader<RGBLuminanceSource> barcodeReader;

    [Header("Puzzle Board")]
    public KeyValuePair<int, PieceDirection>[,] intendedSolutionBoard;
    public List<PuzzlePiece> puzzlePieces;

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

        GeneratePuzzleBoardSolution(5, 5);
    }

    void GeneratePuzzleBoardSolution(int _width, int _height)
    {
        intendedSolutionBoard = new KeyValuePair<int, PieceDirection>[_width, _height];
        List<int> usedPieces = new List<int>();
        puzzlePieces = new List<PuzzlePiece>();
        Transform cameraTransform = FindFirstObjectByType<VuforiaBehaviour>().transform;

        for (int i = 0; i < _width; i++)
        {
            for (int j = 0; j < _height; j++)
            {
                int piece = -1;

                do
                {
                    piece = UnityEngine.Random.Range(1, (_width * _height) + 1);
                } while (usedPieces.Contains(piece));

                usedPieces.Add(piece);

                PieceDirection direction = (PieceDirection)UnityEngine.Random.Range(0, 4);
                intendedSolutionBoard[i, j] = new KeyValuePair<int, PieceDirection>(piece, direction);
                GameObject puzzlePiece = new GameObject(piece.ToString());
                PuzzlePiece p = puzzlePiece.AddComponent<PuzzlePiece>();
                p.pieceName = piece;
                p.boardPosition = new Vector2(i, j);

                puzzlePieces.Add(p);
                
                puzzlePiece.transform.parent = cameraTransform;
                puzzlePiece.transform.localPosition = Vector3.zero;
                puzzlePiece.transform.localRotation = Quaternion.identity;
            }
        }

        int offsetXPos;
        int offsetYPos;
        KeyValuePair<int, PieceDirection> pair;

        foreach (PuzzlePiece p in puzzlePieces)
        {
            for (int i = 0; i < 4; i++)
            {
                offsetXPos = (int)p.boardPosition.x;
                offsetYPos = (int)p.boardPosition.y;

                if (i == 0) // Up
                {
                    offsetYPos += 1;
                }
                if (i == 1) // Right
                {
                    offsetXPos += 1;
                } 
                if (i == 2) // Down
                {
                    offsetYPos -= 1;
                }
                if (i == 3) // Left
                {
                    offsetXPos -= 1;
                }

                if (offsetXPos >= 0 && offsetXPos < _width && offsetYPos >= 0 && offsetYPos < _height)
                {
                    pair = new KeyValuePair<int, PieceDirection>(
                    intendedSolutionBoard[offsetXPos, offsetYPos].Key,
                    intendedSolutionBoard[offsetXPos, offsetYPos].Value);
                }
                else
                {
                    pair = new KeyValuePair<int, PieceDirection>(-1, PieceDirection.UP);
                }

                if (i == 0) // Up
                {
                    p.correctUp = pair;
                }
                if (i == 1) // Right
                {
                    p.correctRight = pair;
                }
                if (i == 2) // Down
                {
                    p.correctDown = pair;
                }
                if (i == 3) // Left
                {
                    p.correctLeft = pair;
                }
            }
        }
    }

    private void Update()
    {
        ScanForQRCodes();

        foreach (Tuple<Vector2, Vector3, int> qr in qrPieces)
        {
            Resolution currentResolution = Screen.currentResolution;
            int w = currentResolution.width;

            Vector2 invertedYPos = new Vector2(qr.Item1.x, (float)vuforiaCam.camHeight - qr.Item1.y);
            invertedYPos -= (new Vector2((float)vuforiaCam.camWidth, (float)vuforiaCam.camHeight) / 2f);

            qrPuzzlePiece = puzzlePieces.Find(x => x.pieceName == qr.Item3);
            qrPuzzlePiece.transform.localPosition = new Vector3(invertedYPos.x, invertedYPos.y, 1900f);
            qrPuzzlePiece.transform.localRotation = Quaternion.Inverse(Camera.main.transform.localRotation);
            qrPuzzlePiece.transform.localRotation.SetLookRotation(qr.Item2, Vector3.forward);
            qrPuzzlePiece.currentDirection = DetectDirection(qrPuzzlePiece.transform.localRotation);
        }
    }

    void ScanForQRCodes()
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
        qrPieces.Clear();

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

            Vector2 p0 = new Vector2(points[0].X, points[0].Y);
            Vector2 p1 = new Vector2(points[1].X, points[1].Y);
            Vector2 p2 = new Vector2(points[2].X, points[2].Y);

            float averageX = (points[0].X + points[2].X) / 2f;
            float averageY = (points[0].Y + points[2].Y) / 2f;

            Vector2 averagePosition = new Vector2(averageX, averageY);
            Vector3 dir = Vector3.Normalize(averagePosition - p1);

            int pieceNumber = -1;
            int.TryParse(results[i].Text, out pieceNumber);

            qrPieces.Add(new Tuple<Vector2, Vector3, int>(averagePosition, dir, pieceNumber));

            //TODO: Add list of visible puzzle pieces, always updated (remove from list if out of view)

            if (allCodes != "QRs detected: , ")
            {
                //Debug.Log(dir);
                //Debug.Log(points[0]);
                //Debug.Log(allCodes + " - At Position: " + averagePosition);
                //Debug.Log(allCodes + " - With rotation degrees: " + orientationDegress);
            }
        }
    }
    


    private PieceDirection DetectDirection(Quaternion _rotation)
    {
        diffs[0] = Quaternion.Angle(upTarget, _rotation);
        diffs[1] = Quaternion.Angle(rightTarget, _rotation);
        diffs[2] = Quaternion.Angle(downTarget, _rotation);
        diffs[3] = Quaternion.Angle(leftTarget, _rotation);

        float smallest = Mathf.Min(diffs);

        if (smallest == diffs[0])
        {
            return PieceDirection.UP;
        }
        else if (smallest == diffs[1])
        {
            return PieceDirection.RIGHT;
        }
        else if (smallest == diffs[2])
        {
            return PieceDirection.DOWN;
        }
        else if (smallest == diffs[3])
        {
            return PieceDirection.LEFT;
        }

        return PieceDirection.UP;
    }

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
}