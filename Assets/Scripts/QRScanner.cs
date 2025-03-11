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
using UnityEngine.SceneManagement;

public enum PieceDirection { UP, RIGHT, DOWN, LEFT }

[SerializeField, Serializable] public class QRPiece
{
    public Vector2 position;
    public float scale;
    public Vector3 direction;
    public int id;
    public bool inView;
}

public class QRScanner : MonoBehaviour
{
    [Header("Puzzle Board")]
    [SerializeField] private bool disableObjectsOutsideView = false;
    [SerializeField] private bool useSeed = false;
    [SerializeField] private int puzzleSeed = 0;
    [SerializeField] private Vector2 puzzleSize = new Vector2(2, 2);
    [SerializeField, UnityEngine.Range(0.5f, 2f)] private float overallPuzzlePieceScale = 1f;
    [SerializeField] private Mesh defualtMesh;
    [SerializeField] private Material defaultMat;
    [SerializeField] private float maxDistanceBetweenPieces = 300f;

    [HideInInspector] public KeyValuePair<int, PieceDirection>[,] intendedSolutionBoard;
    [HideInInspector] public List<PuzzlePiece> puzzlePieces;
    private float resolutionScale;
    private GameObject newGameButton;

    /*
    [Header("Target directions")]
    private Quaternion upTarget = new Quaternion();
    private Quaternion rightTarget = new Quaternion();
    private Quaternion downTarget = new Quaternion();
    private Quaternion leftTarget = new Quaternion();
    private float[] diffs = new float[4];
    */

    [Header("Camera")]
    [SerializeField] private RawImage rawCamOutput;
    private int camHeight;
    private int camWidth;
    public List<QRPiece> detectedQRPieces = new List<QRPiece>();
    private VuforiaCameraTexture vuforiaCam;
    private GameObject videoBackground;

    [Header("Decoding")]
    private BarcodeReaderGeneric barcodeReader;
    private RGBLuminanceSource source;
    private Vector2 p0;
    private Vector2 p1;
    private Vector2 p2;
    private ResultPoint[] points;
    private float averageX;
    private float averageY;
    private float scaleByDistance;
    private Vector2 averagePosition;
    private Vector3 dir;
    private int pieceNumber;
    private Vector2 invertedYPos;
    private System.Func<RGBLuminanceSource, LuminanceSource> f;

    void Start()
    {
        vuforiaCam = GetComponent<VuforiaCameraTexture>();
        newGameButton = GameObject.Find("NewGame");
        newGameButton.SetActive(false);
        // Unused, for DataMatrix scans:
        /*
        barcodeReader = new BarcodeReaderGeneric()
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.DATA_MATRIX },
                TryHarder = true,
                ReturnCodabarStartEnd = false,
                PureBarcode = false
            }
        };
        */

        barcodeReader = new BarcodeReader<RGBLuminanceSource>(f)
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            }
        };

        /*
        upTarget.eulerAngles = new Vector3(-90, 0, 0);
        rightTarget.eulerAngles = new Vector3(0, 90, -90);
        downTarget.eulerAngles = new Vector3(90, 0, -180);
        leftTarget.eulerAngles = new Vector3(0, -90, 90);
        */

        GeneratePuzzleBoardSolution((int)puzzleSize.x, (int)puzzleSize.y);
    }

    void GeneratePuzzleBoardSolution(int _width, int _height)
    {
        intendedSolutionBoard = new KeyValuePair<int, PieceDirection>[_width, _height];
        puzzlePieces = new List<PuzzlePiece>();

        List<int> usedPieces = new List<int>();
        Transform cameraTransform = FindFirstObjectByType<VuforiaBehaviour>().transform;

        if (useSeed)
        {
            UnityEngine.Random.InitState(puzzleSeed);
        }

        for (int i = 0; i < _width; i++)
        {
            for (int j = 0; j < _height; j++)
            {
                int piece = -1;

                do
                {
                    piece = UnityEngine.Random.Range(1, (_width * _height) + 1);
                }
                while (usedPieces.Contains(piece));

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

                if (i == 0) { offsetYPos += 1; } // Up
                if (i == 1) { offsetXPos += 1; } // Right 
                if (i == 2) { offsetYPos -= 1; } // Down
                if (i == 3) { offsetXPos -= 1; } // Left

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

                if (i == 0) { p.correctUp = pair; }     // Up
                if (i == 1) { p.correctRight = pair; }  // Right
                if (i == 2) { p.correctDown = pair; }   // Down
                if (i == 3) { p.correctLeft = pair; }   // Left
            }
        }
    }

    private void Update()
    {
        ScanForCodes();

        foreach (QRPiece qr in detectedQRPieces)
        {
            PuzzlePiece qrPuzzlePiece = puzzlePieces.Find(x => x.pieceName == qr.id);

            if (qrPuzzlePiece == null)
            {
                continue;
            }

            if (!qr.inView)
            {
                if (disableObjectsOutsideView)
                {
                    qrPuzzlePiece.gameObject.SetActive(false);
                }
                continue;
            }


            qrPuzzlePiece.gameObject.SetActive(true);

            invertedYPos = new Vector2(qr.position.x, (float)vuforiaCam.camHeight - qr.position.y);
            invertedYPos -= (new Vector2((float)vuforiaCam.camWidth, (float)vuforiaCam.camHeight) / 2f);
            

            // Apply transform variables
            qrPuzzlePiece.transform.localPosition = new Vector3(invertedYPos.x * resolutionScale, invertedYPos.y * resolutionScale, 1900f);
            qrPuzzlePiece.transform.localScale = (Vector3.one * resolutionScale) * qr.scale * overallPuzzlePieceScale;

            
            qrPuzzlePiece.transform.localRotation = Quaternion.Inverse(Camera.main.transform.localRotation);
            qrPuzzlePiece.transform.localRotation.SetLookRotation(qr.direction, Vector3.forward);

            //NOTE: Unused due to not being able to track rotation of individual QRs
            /*
            qrPuzzlePiece.currentDirection = DetectDirection(qrPuzzlePiece.transform.localRotation);
            */

            UpdatePieceStatus(qrPuzzlePiece);
        }

        // If no unfinished piece is found, assume puzzle is finished correctly
        if (puzzlePieces.Find(x => !x.IsDoneBool()) == null)
        {
            newGameButton.SetActive(true);
        }
    }

    public void ResetPuzzle()
    {
        SceneManager.LoadScene(0);
    }

    private void UpdatePieceStatus(PuzzlePiece p)
    {
        PuzzlePiece pieceToCheck;
        float distanceBetweenPieces = 0f;

        for (int i = 0; i < 4; i++)
        {
            pieceToCheck = null;

            //NOTE: Disabled checking direction when assigning p.status_up/right/down/left, due to being unable to track QR rotations...

            if (i == 0) // Up
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
            if (i == 1) // Right
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
            if (i == 2) // Down
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
            if (i == 3) // Left
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

    void ScanForCodes()
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
        camWidth = vuforiaCam.camWidth;
        camHeight = vuforiaCam.camHeight;
        resolutionScale = 1920f / (float)vuforiaCam.camWidth;

        if (pixels == null)
        {
            return;
        }
        
        byte[] rawRGB = ConvertColor32ToByteArray(pixels);
        source = new RGBLuminanceSource(rawRGB, camWidth, camHeight, RGBLuminanceSource.BitmapFormat.RGB32);

        if (source == null)
        {
            return;
        }

        // NOTE: For DataMatrix scans:
        /*
        Tuple<int, Vector2[], float>[] results = DecodeDataMatrixFromColor32(pixels, rawRGB, camWidth, camHeight, source, RGBLuminanceSource.BitmapFormat.RGB32);
        foreach (var item in results)
        {
            int decodedNumber = item.Item1;
            Vector2[] corners = item.Item2;
            float rotation = item.Item3;
        }
        */

        Result[] results = null;

        //NOTE: For QR scans:
        try
        {
            results = barcodeReader.DecodeMultiple(source);
            //results = barcodeReader.DecodeMultiple(rawRGB, camWidth, camHeight, RGBLuminanceSource.BitmapFormat.RGB32);
        }
        catch (Exception e) { }

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
            points = results[i].ResultPoints;
            p0 = new Vector2(points[0].X, points[0].Y);
            p1 = new Vector2(points[1].X, points[1].Y);
            p2 = new Vector2(points[2].X, points[2].Y);

            scaleByDistance = Vector2.Distance(p0, p2);
            averageX = (points[0].X + points[2].X) / 2f;
            averageY = (points[0].Y + points[2].Y) / 2f;

            averagePosition = new Vector2(averageX, averageY);
            dir = Vector3.Normalize(averagePosition - p1);

            pieceNumber = -1;
            int.TryParse(results[i].Text, out pieceNumber);

            QRPiece piece = detectedQRPieces.Find(x => x.id == pieceNumber);

            if (piece == null)
            {
                detectedQRPieces.Add(new QRPiece() {
                    position = averagePosition, 
                    scale = scaleByDistance, 
                    direction = dir, 
                    id = pieceNumber, 
                    inView = true });
            }
            else
            {
                piece.position = averagePosition;
                piece.scale = scaleByDistance;
                piece.direction = dir;
                piece.inView = true;
            }
        }
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

    // NOTE: Attempted to use DataMatrix for faster decoding, ended up being less reliable than QR codes and is thus unusued:
    
    /*
    public Tuple<int, Vector2[], float>[] DecodeDataMatrixFromColor32(Color32[] image, byte[] raw, int width, int height, RGBLuminanceSource otherSource, RGBLuminanceSource.BitmapFormat bitformat)
    {
        byte[] luminance = new byte[width * height];

        for (int i = 0; i < luminance.Length; i++)
        {
            // Convert Color32 to grayscale using luminosity formula
            Color32 pixel = image[i];
            luminance[i] = (byte)(0.299 * pixel.r + 0.587 * pixel.g + 0.114 * pixel.b);
        }

        RGBLuminanceSource luminanceSource = new RGBLuminanceSource(luminance, width, height);

        List<Tuple<int, Vector2[], float>> decodedValuesWithPositionAndRotation = new List<Tuple<int, Vector2[], float>>();
        Result[] results = null;

        try
        {
            if (otherSource != null && barcodeReader != null)
            {
                //results = barcodeReader.DecodeMultiple(luminanceSource);
                results = barcodeReader.DecodeMultiple(raw, width, height, bitformat);
            }
        } catch (Exception e) { }

        if (results != null)
        {
            foreach (var result in results)
            {
                int decodedNum = -1;
                int.TryParse(result.Text, out decodedNum);

                Vector2[] points = new Vector2[result.ResultPoints.Length];
                for (int i = 0; i < result.ResultPoints.Length; i++)
                {
                    points[i] = new Vector2(result.ResultPoints[i].X, result.ResultPoints[i].Y);
                }

                float rotation = CalculateRotation(points[0], points[1]);

                decodedValuesWithPositionAndRotation.Add(new Tuple<int, Vector2[], float>(decodedNum, points, rotation));
            }
        }
        
        return decodedValuesWithPositionAndRotation.ToArray();
    }
    

    private static float CalculateRotation(Vector2 point1, Vector2 point2)
    {
        float angleInRadians = Mathf.Atan2(point2.y - point1.y, point2.x - point1.x);
        return Mathf.Rad2Deg * angleInRadians;
    }

    private PieceDirection DetectDirection(Quaternion _rotation)
    {
        //NOTE: Unused due to not being able to track rotation of individual QRs
        diffs[0] = Quaternion.Angle(upTarget, _rotation);
        diffs[1] = Quaternion.Angle(rightTarget, _rotation);
        diffs[2] = Quaternion.Angle(downTarget, _rotation);
        diffs[3] = Quaternion.Angle(leftTarget, _rotation);

        float smallest = Mathf.Min(diffs);

        if (smallest == diffs[0]) { return PieceDirection.UP; }
        else if (smallest == diffs[1]) { return PieceDirection.RIGHT; }
        else if (smallest == diffs[2]) { return PieceDirection.DOWN; }
        else if (smallest == diffs[3]) { return PieceDirection.LEFT; }

        return PieceDirection.UP;
    }
    */
}