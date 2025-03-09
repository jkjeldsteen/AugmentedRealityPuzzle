using System.Collections.Generic;
using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    public bool statusUp = false;
    public bool statusRight = false;
    public bool statusDown = false;
    public bool statusLeft = false;

    public KeyValuePair<int, PieceDirection> correctUp;
    public KeyValuePair<int, PieceDirection> correctRight;
    public KeyValuePair<int, PieceDirection> correctDown;
    public KeyValuePair<int, PieceDirection> correctLeft;

    public Vector2 fullGridSize;
    public int pieceName;
    public Vector2 boardPosition;
    public PieceDirection currentDirection;

    //public PuzzlePiece(KeyValuePair<int, PieceDirection> up, KeyValuePair<int, PieceDirection> right, KeyValuePair<int, PieceDirection> down, KeyValuePair<int, PieceDirection> left)
    //public PuzzlePiece(int pieceNumber)
    //{
    //    //correctUp = up;
    //    //correctRight = right;
    //    //correctDown = down;
    //    //correctLeft = left;
    //    pieceIndex = pieceNumber;
    //}

    Renderer r;
    MaterialPropertyBlock matBlock;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        r = GetComponent<Renderer>();
        matBlock = new MaterialPropertyBlock();
    }

    // Update is called once per frame
    void Update()
    {
        if (matBlock != null)
        {
            matBlock.SetVector("_samplePosition", boardPosition);
            matBlock.SetVector("_gridSize", fullGridSize);
            matBlock.SetFloat("_isDone", IsDone());
            r.SetPropertyBlock(matBlock);
            //Debug.Log("SET BLOCK : " + boardPosition);
        }
    }

    public bool IsDoneBool()
    {
        return statusUp && statusRight && statusDown && statusLeft;
    }

    public float IsDone()
    {
        float result = 0f;

        if (IsDoneBool())
        {
            result = 1f;
        }

        return result;
    }
}
