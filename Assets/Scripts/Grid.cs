using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// add the logic namespace
using Match3;

public class Grid : MonoBehaviour
{
    public enum PieceType
    {
        EMPTY,
        NORMAL,
        BUBBLE,
        COUNT,
    };

    [System.Serializable]
    public struct PiecePrefab
    {
        public PieceType type;
        public GameObject prefab;
    };

    [Header("Board Size")]
    public int xDim;
    public int yDim;

    [Header("Animation")]
    public float fillTime = 0.2f; // still used by MovablePiece if/when you animate

    [Header("Prefabs")]
    public PiecePrefab[] piecePrefabsArr; // must include EMPTY and NORMAL at least
    public GameObject backgroundPrefab;

    [Header("Colors")]
    public int customColorsNum; // must be <= number of ColorPiece.ColorType variants you use

    // lookup for piece prefabs
    private Dictionary<PieceType, GameObject> piecePrefabDict;

    // visual board (Unity pieces)
    private GamePiece[,] pieces;

    // rules engine (pure logic)
    private Match3Board logicBoard;

    // --------------------------------------------
    // Unity lifecycle
    // --------------------------------------------
    void Start()
    {
        // 1) build prefab lookup
        piecePrefabDict = new Dictionary<PieceType, GameObject>();
        for (int i = 0; i < piecePrefabsArr.Length; i++)
        {
            if (!piecePrefabDict.ContainsKey(piecePrefabsArr[i].type))
            {
                piecePrefabDict.Add(piecePrefabsArr[i].type, piecePrefabsArr[i].prefab);
            }
        }

        // 2) spawn background tiles
        for (int x = 0; x < xDim; x++)
        {
            for (int y = 0; y < yDim; y++)
            {
                GameObject background = (GameObject)Instantiate(
                    backgroundPrefab,
                    GetWorldPos(x, y),
                    Quaternion.identity
                );
                background.transform.parent = transform;
            }
        }

        // 3) create placeholders (EMPTY) so pieces[,] is allocated & positioned
        pieces = new GamePiece[xDim, yDim];
        for (int x = 0; x < xDim; x++)
        {
            for (int y = 0; y < yDim; y++)
            {
                SpawnNewPiece(x, y, PieceType.EMPTY);
            }
        }

        // 4) construct the logic board (rows = yDim, cols = xDim)
        // optional: you can pass a seed: new Match3Board(yDim, xDim, customColorsNum, seed);
        logicBoard = new Match3Board(yDim, xDim, customColorsNum);

        // 5) build initial visual board from logic (replaces EMPTY placeholders with NORMAL + colors)
        //BuildInitialFromLogic(); 
        StartCoroutine(Fill()); 

        // NOTE:
        // Removed demo lines and Unity-side Fill/FillStep(). The logic board now owns rules/grav/refill.
        // If you want animated falling later, we’ll add a diff-based animation step instead of repainting.
    }

    void Update()
    {
        // no per-frame rule work here
    }

    // --------------------------------------------
    // Public API
    // --------------------------------------------

    /// <summary>
    /// Attempt to swap two board positions from input (Unity coords: x=col, y=row).
    /// If valid, logic will resolve matches/cascades; we then repaint to the final state.
    /// Hook your drag script to call this.
    /// </summary>
    public void TrySwap(int x1, int y1, int x2, int y2)
    {
        var a = new Coord(y1, x1); // logic expects (row, col)
        var b = new Coord(y2, x2);

        if (logicBoard.TrySwap(a, b, out int cleared, out int cascades))
        {
            // TODO (optional): play swap / clear / drop / refill animations using your MovablePiece
            // For now, immediately repaint to the resolved state:
            RenderFromLogic();
        }
        else
        {
            // TODO (optional): animate swap-back for invalid move
        }
    }

    // --------------------------------------------
    // Rendering helpers (logic -> visuals)
    // --------------------------------------------

    /// <summary>
    /// First-time construction:
    /// Replaces EMPTY placeholders with NORMAL pieces and assigns colors from logic.
    /// </summary>
    private void BuildInitialFromLogic()
    {
        int[,] state = logicBoard.GetBoard(); // [row, col]

        for (int r = 0; r < yDim; r++)
        {
            for (int c = 0; c < xDim; c++)
            {
                // replace placeholder EMPTY with a NORMAL piece
                if (pieces[c, r] != null)
                {
                    Destroy(pieces[c, r].gameObject);
                }

                var p = SpawnNewPiece(c, r, PieceType.NORMAL);

                if (p.IsColored())
                {
                    int colorId = state[r, c];
                    p.ColorComponent.SetColor((ColorPiece.ColorType)colorId);
                }
            }
        }
    }

    /// <summary>
    /// Repaint existing NORMAL pieces to match current logic colors.
    /// This version "teleports" state (no falling animation yet).
    /// </summary>
    private void RenderFromLogic()
    {
        int[,] state = logicBoard.GetBoard(); // [row, col]

        for (int r = 0; r < yDim; r++)
        {
            for (int c = 0; c < xDim; c++)
            {
                var p = pieces[c, r];
                if (p == null) continue;
                if (p.Type == PieceType.EMPTY) continue;
                if (!p.IsColored()) continue;

                int colorId = state[r, c];
                p.ColorComponent.SetColor((ColorPiece.ColorType)colorId);
            }
        }
    }

    /// <summary>
    /// moves each piece only one space (if any move occures)
    /// return TRUE if any pieces were moved, FALSE otherwise
    /// </summary>
    // TODO: update logic board
    public bool FillStep()
    {
        bool movePiece = false;
        //loop through all cols in reverse order, bottom to top
        //we ignore the last one cuz we are looking for pieces we can move down, and the lase one (dim-1) - we can't
        for (int y = yDim - 2; y >= 0; y--)
        {
            for (int x = 0; x < xDim; x++)
            {
                //Debug.Log("inside FillStep loop, x: " + x + ", y: " + y);
                GamePiece piece = pieces[x, y];

                //if it's not movable - we can't move it down to fill the empty space, so we can just ignore it
                if (piece.IsMovable())
                {
                    GamePiece pieceBelow = pieces[x, y + 1];
                    if (pieceBelow.Type == PieceType.EMPTY) 
                    {
                        Destroy(pieceBelow.gameObject); //destroy the empty piece, otherwise this object stays alive

                        piece.MovableComponent.Move(x, y + 1, fillTime);
                        pieces[x, y + 1] = piece;
                        SpawnNewPiece(x, y, PieceType.EMPTY); //the upper piece is now EMPTY; actually we are swapping a movable piece with an empty piece below it
                        movePiece = true;

                    }

                }

            }
        }

        //handle top row
        //the top row is a special case, since empty spaces there will be filled by new pieces created (and not swapping exising movable objects)
        //loop throuh all the cells in the top row
        for (int x = 0; x < xDim; x++)
        {
            //Debug.Log("inside FillStep loop top row, x: " + x + ", y: 0");
            GamePiece pieceBelow = pieces[x, 0];
            if (pieceBelow.Type == PieceType.EMPTY)
            {
                Destroy(pieceBelow.gameObject); //destroy the empty piece, otherwise this object stays alive

                //we don't use the SpwanNewPiece() func cuz of the -1 thing
                GameObject newPiece = (GameObject)Instantiate(
                     piecePrefabDict[PieceType.NORMAL],
                     GetWorldPos(x, -1), //create in the "non-existing" row, above the top row
                     Quaternion.identity);
                newPiece.transform.parent = transform;

                pieces[x, 0] = newPiece.GetComponent<GamePiece>();
                pieces[x, 0].Init(x, -1, this, PieceType.NORMAL); //we set it for -1 and not 0, for animation
                pieces[x, 0].MovableComponent.Move(x, 0, fillTime);
                pieces[x, 0].ColorComponent.SetColor((ColorPiece.ColorType)Random.Range(0, pieces[x, 0].ColorComponent.ColorsNum));
                movePiece = true;

            }
        }
        return movePiece;
    }


    /// <summary>
    /// calls FillStep untill the board is filled
    /// it's a Coroutine fucntion, meaning it can execute in multiplw frames and not just one frame
    /// </summary>
    public IEnumerator Fill()
    {
        while (FillStep())
        {
            //wait fillTime seconds in between fill steps
            yield return new WaitForSeconds(fillTime);

        }
    }

    // --------------------------------------------
    // Piece & grid utilities
    // --------------------------------------------

    // Convert grid coords to world position (grid origin centered, y grows downward)
    public Vector2 GetWorldPos(int x, int y)
    {
        return new Vector2(
            transform.position.x - xDim / 2.0f + x,
            transform.position.y + yDim / 2.0f - y
        );
    }

    // Spawns a piece prefab of the given type at (x,y), parents to this Grid, stores in pieces[,]
    public GamePiece SpawnNewPiece(int x, int y, PieceType type)
    {
        GameObject newPiece = (GameObject)Instantiate(
            piecePrefabDict[type],
            GetWorldPos(x, y),
            Quaternion.identity
        );

        newPiece.name = $"Piece({x}, {y})";
        newPiece.transform.parent = transform;

        pieces[x, y] = newPiece.GetComponent<GamePiece>();
        pieces[x, y].Init(x, y, this, type);

        return pieces[x, y];
    }

    // --------------------------------------------
    // (REMOVED) Unity duplicate rule code
    // --------------------------------------------

    // Removed:
    // - Demo: Destroy(pieces[4,4]) / SpawnNewPiece(4,4,BUBBLE)
    // The logic board (Match3Board) owns: starting layout, matches, clear, gravity, refill, cascades.
}
