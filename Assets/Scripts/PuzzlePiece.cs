using System.Collections.Generic;
using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    [HideInInspector] public int pieceName;
    [HideInInspector] public Vector2 fullGridSize;
    [HideInInspector] public Vector2 boardPosition;
    [HideInInspector] public PieceDirection currentDirection;

    [HideInInspector] public bool statusUp = false;
    [HideInInspector] public bool statusRight = false;
    [HideInInspector] public bool statusDown = false;
    [HideInInspector] public bool statusLeft = false;

    [HideInInspector] public KeyValuePair<int, PieceDirection> correctUp;
    [HideInInspector] public KeyValuePair<int, PieceDirection> correctRight;
    [HideInInspector] public KeyValuePair<int, PieceDirection> correctDown;
    [HideInInspector] public KeyValuePair<int, PieceDirection> correctLeft;

    private Renderer r;
    private MaterialPropertyBlock matBlock;

    void Start()
    {
        r = GetComponent<Renderer>();
        matBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (matBlock != null)
        {
            matBlock.SetVector("_samplePosition", boardPosition);
            matBlock.SetVector("_gridSize", fullGridSize);
            matBlock.SetFloat("_isDone", IsDone());
            r.SetPropertyBlock(matBlock);
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
