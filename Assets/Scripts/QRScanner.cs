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
using UnityEditor.Playables;
using UnityEngine.UIElements;

public enum PieceDirection { UP, RIGHT, DOWN, LEFT }

public class QRPiece
{
    public Vector2 position;
    public float scale;
    public Vector3 direction;
    public int id;
    public bool inView;
}

public class QRScanner : MonoBehaviour
{
    GameObject videoBackground;

    [SerializeField] private int puzzleSeed = 0;
    [SerializeField, UnityEngine.Range(0.5f, 2f)] private float overallPuzzlePieceScale = 1f;
    public Mesh defualtMesh;
    public Material defaultMat;
    public float maxDistanceBetweenPieces = 300f;

    [Header("Targets")]
    //public float errorMargain = 10f;
    Quaternion upTarget = new Quaternion();
    Quaternion rightTarget = new Quaternion();
    Quaternion downTarget = new Quaternion();
    Quaternion leftTarget = new Quaternion();
    float[] diffs = new float[4];

    public float customZValue = 100f;
    public RawImage rawCamOutput;
    int camHeight;
    int camWidth;
    byte[] imageBytes;
    System.Func<RGBLuminanceSource, LuminanceSource> f;
    List<QRPiece> detectedQRPieces = new List<QRPiece>();
    VuforiaCameraTexture vuforiaCam;

    private WebCamTexture webcamTexture;
    private BarcodeReader<RGBLuminanceSource> barcodeReader;

    [Header("Puzzle Board")]
    public KeyValuePair<int, PieceDirection>[,] intendedSolutionBoard;
    public List<PuzzlePiece> puzzlePieces;


    //Matrix<float> intrinsics;
    //Matrix<float> distCoeffs;
    
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


        //Resolution currentResolution = Screen.currentResolution;
        //int w = currentResolution.width;

        GeneratePuzzleBoardSolution(5, 5);
        //ReadIntrinsicsFromFile(out intrinsics, out distCoeffs);
        //Debug.Log(intrinsics);
        //Debug.Log(distCoeffs);
    }

    /*
    public void ReadIntrinsicsFromFile(out Matrix<float> intrinsics, out Matrix<float> distCoeffs)
    {
        Mat intrinsicsMat = new Mat();
        Mat distCoeffsMat = new Mat();
        Debug.Log("bruih");
        using FileStorage fs = new FileStorage("C:\\Users\\pxpet\\Desktop\\intrinsics.json", FileStorage.Mode.Read);

        FileNode intrinsicsNode = fs.GetNode("Intrinsics");
        FileNode distCoeffsNode = fs.GetNode("DistCoeffs");

        intrinsicsNode.ReadMat(intrinsicsMat);
        distCoeffsNode.ReadMat(distCoeffsMat);

        intrinsics = new Matrix<float>(3, 3);
        distCoeffs = new Matrix<float>(1, 5);

        intrinsicsMat.ConvertTo(intrinsics, DepthType.Cv32F);
        distCoeffsMat.ConvertTo(distCoeffs, DepthType.Cv32F);
    }
    */

    void GeneratePuzzleBoardSolution(int _width, int _height)
    {
        intendedSolutionBoard = new KeyValuePair<int, PieceDirection>[_width, _height];
        List<int> usedPieces = new List<int>();
        puzzlePieces = new List<PuzzlePiece>();
        Transform cameraTransform = FindFirstObjectByType<VuforiaBehaviour>().transform;
        UnityEngine.Random.InitState(puzzleSeed);

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
                p.fullGridSize = new Vector2(_width, _height);

                puzzlePieces.Add(p);
                
                puzzlePiece.transform.parent = cameraTransform;
                puzzlePiece.transform.localPosition = Vector3.zero;
                puzzlePiece.transform.localRotation = Quaternion.identity;
                puzzlePiece.transform.localScale = Vector3.one * 125f;
                MeshFilter mf = puzzlePiece.AddComponent<MeshFilter>();
                mf.mesh = defualtMesh;
                MeshRenderer mr = puzzlePiece.AddComponent<MeshRenderer>();
                mr.material = defaultMat;
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
        Vector2 invertedYPos;

        foreach (QRPiece qr in detectedQRPieces)
        {
            PuzzlePiece qrPuzzlePiece = puzzlePieces.Find(x => x.pieceName == qr.id);


            if (!qr.inView)
            {
                qrPuzzlePiece.gameObject.SetActive(false);
                continue;
            }


            qrPuzzlePiece.gameObject.SetActive(true);

            invertedYPos = new Vector2(qr.position.x, (float)vuforiaCam.camHeight - qr.position.y);
            invertedYPos -= (new Vector2((float)vuforiaCam.camWidth, (float)vuforiaCam.camHeight) / 2f);

            qrPuzzlePiece.transform.localPosition = new Vector3(invertedYPos.x, invertedYPos.y, 1900f);
            qrPuzzlePiece.transform.localScale = Vector3.one * qr.scale * overallPuzzlePieceScale;
            qrPuzzlePiece.transform.localRotation = Quaternion.Inverse(Camera.main.transform.localRotation);
            qrPuzzlePiece.transform.localRotation.SetLookRotation(qr.direction, Vector3.forward);
            qrPuzzlePiece.currentDirection = DetectDirection(qrPuzzlePiece.transform.localRotation);

            UpdatePieceStatus(qrPuzzlePiece);
        }
    }

    private void UpdatePieceStatus(PuzzlePiece p)
    {
        PuzzlePiece pieceToCheck;
        float distanceBetweenPieces = 0f;

        for (int i = 0; i < 4; i++)
        {
            pieceToCheck = null;

            if (i == 0)
            {
                if (p.correctUp.Key == -1) // Piece that should be at this position is empty (outside board)
                {
                    p.statusUp = true; // TODO: Maybe check to make sure space is empty
                }
                else
                {
                    pieceToCheck = puzzlePieces.Find(x => x.pieceName == p.correctUp.Key);
                    distanceBetweenPieces = Vector3.Distance(pieceToCheck.transform.position, p.transform.position);
                    p.statusUp = distanceBetweenPieces < maxDistanceBetweenPieces && pieceToCheck.transform.position.y > p.transform.position.y; // && pieceToCheck.currentDirection == p.correctUp.Value;
                }
            }
            if (i == 1)
            {
                if (p.correctRight.Key == -1) // Outside board check
                {
                    p.statusRight = true; // TODO: Maybe check to make sure space is empty
                }
                else
                {
                    pieceToCheck = puzzlePieces.Find(x => x.pieceName == p.correctRight.Key);
                    distanceBetweenPieces = Vector3.Distance(pieceToCheck.transform.position, p.transform.position);
                    p.statusRight = distanceBetweenPieces < maxDistanceBetweenPieces && pieceToCheck.transform.position.x > p.transform.position.x; // && pieceToCheck.currentDirection == p.correctRight.Value;
                }
            }
            if (i == 2)
            {
                if (p.correctDown.Key == -1) // Outside board check
                {
                    p.statusDown = true; // TODO: Maybe check to make sure space is empty
                }
                else
                {
                    pieceToCheck = puzzlePieces.Find(x => x.pieceName == p.correctDown.Key);
                    distanceBetweenPieces = Vector3.Distance(pieceToCheck.transform.position, p.transform.position);
                    p.statusDown = distanceBetweenPieces < maxDistanceBetweenPieces && pieceToCheck.transform.position.y < p.transform.position.y; // && pieceToCheck.currentDirection == p.correctDown.Value;
                }
            }
            if (i == 3)
            {
                if (p.correctLeft.Key == -1) // Outside board check
                {
                    p.statusLeft = true; // TODO: Maybe check to make sure space is empty
                }
                else
                {
                    pieceToCheck = puzzlePieces.Find(x => x.pieceName == p.correctLeft.Key);
                    distanceBetweenPieces = Vector3.Distance(pieceToCheck.transform.position, p.transform.position);
                    p.statusLeft = distanceBetweenPieces < maxDistanceBetweenPieces && pieceToCheck.transform.position.x < p.transform.position.x; // && pieceToCheck.currentDirection == p.correctLeft.Value;
                }
            }
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
        //visibleQRPieces.Clear();

        foreach (QRPiece qrp in detectedQRPieces)
        {
            qrp.inView = false;
        }

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

            float scaleByDistance = Vector2.Distance(p0, p2);
            float averageX = (points[0].X + points[2].X) / 2f;
            float averageY = (points[0].Y + points[2].Y) / 2f;

            Vector2 averagePosition = new Vector2(averageX, averageY);
            Vector3 dir = Vector3.Normalize(averagePosition - p1);

            int pieceNumber = -1;
            int.TryParse(results[i].Text, out pieceNumber);
            //Debug.Log("Piecenum: " + pieceNumber);

            QRPiece piece = detectedQRPieces.Find(x => x.id == pieceNumber);

            if (piece == null)
            {
                detectedQRPieces.Add(new QRPiece() { position = averagePosition, scale = scaleByDistance, direction = dir, id = pieceNumber, inView = true });
            }
            else
            {
                piece.position = averagePosition;
                piece.scale = scaleByDistance;
                piece.direction = dir;
                piece.inView = true;
            }

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