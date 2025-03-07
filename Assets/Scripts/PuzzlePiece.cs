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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
