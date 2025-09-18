using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using Unity.Android.Gradle;

// add the logic namespace
using Match3;


public class Grid : MonoBehaviour //Grid manages the whole board while GamePiece manages just one cell’s object
{
    public GameManager gameManager;
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
    public int xDim; //grid size (columns × rows).
    public int yDim;

    [Header("Animation")]
    public float fillTime = 0.2f; // still used by MovablePiece if/when you animate

    [Header("Prefabs")]
    public PiecePrefab[] piecePrefabsArr; // which prefab to spawn per PieceType //must include EMPTY and NORMAL at least and might add bubble
    public GameObject backgroundPrefab; //the tile behinf each cell

    [Header("Colors")]
    public int customColorsNum; // how many distinct colors the logic uses //must be <= number of ColorPiece.ColorType variants you use

    // lookup for piece prefabs
    private Dictionary<PieceType, GameObject> piecePrefabDict;

    // visual board (Unity pieces)
    private GamePiece[,] pieces;//the 2D array of visual GamePiece components //GamePiece manages just one cell’s object

    // rules engine (pure logic)
    private Match3Board logicBoard; //the rules engine (spawning, matching, clearing, gravity, refills)

    // ===== Selection & clicks =====
    private GamePiece selected;
    public LayerMask clickMask = ~0;   // default: everything


    /*
        // ===== Added: simple selection & input handling =====
        private GamePiece selected;            // currently selected visual piece (can be null)
        [Tooltip("Raycast layer for pieces/background if you want to filter mouse hits.")]
        public LayerMask clickMask = ~0;       // default: everything
        // ================================================
    */


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

        // 2) spawn background tiles in a grid
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
        pieces = new GamePiece[xDim, yDim];//GamePiece manages just one cell’s object
        for (int x = 0; x < xDim; x++)
        {
            for (int y = 0; y < yDim; y++)
            {
                SpawnNewPiece(x, y, PieceType.EMPTY); //for each [x,y] fill with PieceType.EMPTY
            }
        }

        // 4) construct the logic board (rows = yDim, cols = xDim)
        // optional: you can pass a seed: new Match3Board(yDim, xDim, customColorsNum, seed);
        logicBoard = new Match3Board(yDim, xDim, customColorsNum);

        // 5) build initial visual board from logic (replaces EMPTY placeholders with NORMAL + colors)
        BuildInitialFromLogic();

        // NOTE:
        // Removed demo lines and Unity-side Fill/FillStep(). The logic board now owns rules/grav/refill.
        // If you want animated falling later, we’ll add a diff-based animation step instead of repainting.
    }


    //---------------------------------------------------//


    /// <summary>
    /// selected: holds the currently highlighted tile the player picked first
    /// Left-click flow: 1)Click a cell → if nothing selected, just highlight it
    /// 2)Click another cell → if it’s adjacent to the first, call TrySwap(a, b)--- If it’s not adjacent, the highlight just moves to the new tile
    /// left-click the same tile → deselects
    /// left-click empty/outside → deselects (did i do it?)
    /// </summary>
    void Update() // Runs once per frame
    {
        // LEFT CLICK: pick a cell and maybe swap
        //Input.GetMouseButtonDown(0)  -> Did the player left-click this frame?
        //// Convert mouse to grid cell (gx, gy); only true if it's inside the board
        if (Input.GetMouseButtonDown(0))
        {
            // 1) Clicked outside the board? -> cancel selection and stop
            if (!TryGetCellUnderMouse(out int gx, out int gy))
            {
                if (selected != null) { selected.SetSelected(false); selected = null; }
                return;
            }

            var clicked = pieces[gx, gy]; // Grab the GamePiece reference stored at that cell

            //  left-click the same tile again to deselect
            if (selected == clicked && selected != null)
            {
                selected.SetSelected(false);
                selected = null;
                return;
            }

            // left-clicking empty//unplayable also cancels current selection (nice UX)
            if (clicked == null || clicked.Type == PieceType.EMPTY)
            {
                
                if (selected != null)
                {
                    selected.SetSelected(false);
                    selected = null;
                }
                return;
            }


            if (selected == null) // Case 1: nothing is selected yet
            {
                selected = clicked;// Remember this tile as "selected"
                selected.SetSelected(true);// Give it a small visual highlight (scale-up)
            }
            else// Case 2: a tile is already selected
            {
                if (AreAdjacent(selected.X, selected.Y, gx, gy)) //// If the newly clicked tile is adjacent to the selected one
                {
                    selected.SetSelected(false);// Turn off highlight on the previously selected tile
                    var a = selected; // Keep a temp ref to it (because we’re about to clear 'selected')
                    selected = null; // Clear selection state (we’re committing to a swap now)
                    TrySwap(a.X, a.Y, gx, gy);// Perform the swap attempt(animate, validate with logic, revert if invalid)
                }
                else // Not adjacent → just change which tile is selected
                {
                    selected.SetSelected(false);     // Unhighlight old selection
                    selected = clicked;              // Select the new tile
                    selected.SetSelected(true);      // Highlight the new selection
                }
            }
            
        }

       
    }



    // --------------------------------------------
    // Public API
    // --------------------------------------------



    /// <summary>
    /// Attempt to swap two board positions from input (Unity coords: x=col, y=row).
    /// If valid, logic will resolve matches/cascades; we then repaint to the final state.
    /// Hook your drag script to call this.
    /// </summary>
    /// 
    /*public void TrySwap(int x1, int y1, int x2, int y2)
        {
            if (!AreAdjacent(x1, y1, x2, y2)) return; //uses the helper func to check if the two cells we are trying to swap are adjacent
            StartCoroutine(TrySwapRoutine(x1, y1, x2, y2)); //the next func
        }

        private IEnumerator TrySwapRoutine(int x1, int y1, int x2, int y2) //Declares a coroutine that can yield (pause/resume across frames). It will handle the full swap attempt between two grid cells (x1,y1) and (x2,y2)
        {
            var p1 = pieces[x1, y1]; //saves the coord saved at first cell
            var p2 = pieces[x2, y2];
            if (p1 == null || p2 == null) yield break; //if either spot has no piece, abort the coroutine immediately (for safedty)
            if (!p1.IsMovable() || !p2.IsMovable()) yield break; //if either piece cannot move (e.g., is EMPTY or has no MovablePiece), abort.(safety)

            // visual: animate into each other's cells
            p1.MovableComponent.Move(x2, y2, fillTime); //Kick off the animation that moves p1 to the other cell over fillTime seconds
            p2.MovableComponent.Move(x1, y1, fillTime); //Kick off the animation that moves p2 to the first cell over fillTime seconds
            //Both moves start now; they run concurrently.


            // keep pieces[,] mapping in sync with what you see

            pieces[x1, y1] = p2;//Update the 2D array so the data model matches what we’re seeing: the second piece is now sitting at (x1,y1).
            pieces[x2, y2] = p1;

            //Pause the coroutine until the swap animation should be finished, so visuals are done before we ask the logic to validate
            yield return new WaitForSeconds(fillTime);

            // ask the logic to perform/validate the swap (rows=y, cols=x)
            var a = new Match3.Coord(y1, x1); //Build the logic coordinate for the first cell. Note: logic uses (row, col) = (y, x)
            var b = new Match3.Coord(y2, x2);


            //Ask the rules engine to commit this swap if it forms a match
            //Returns true if the swap makes ≥3 in a row somewhere; it will then clear, apply gravity, refill, and keep cascading until stable
            if (logicBoard.TrySwap(a, b, out int cleared, out int cascades))
            {
                // valid swap: repaint to the final logic state (teleports cascades for now)
                RenderFromLogic();
            }
            else //(no match): we need to swap back visually so the board returns to the original positions
            {
                // invalid swap: animate back and restore mapping
                p1.MovableComponent.Move(x1, y1, fillTime * 0.75f); //Start animating p1 back to its original cell, a bit faster (75% of normal time)
                p2.MovableComponent.Move(x2, y2, fillTime * 0.75f);

                //Restore the array mapping so data and visuals match again
                pieces[x1, y1] = p1;
                pieces[x2, y2] = p2;

                //Wait for the swap-back animation to finish before ending the coroutine.
                yield return new WaitForSeconds(fillTime * 0.75f);
            }
        }



        /* public void TrySwap(int x1, int y1, int x2, int y2)
         {
             var a = new Coord(y1, x1); // logic expects (row, col)
             var b = new Coord(y2, x2);

             if (logicBoard.TrySwap(a, b, out int cleared, out int cascades)) //its false if a and b are not adjacent
             {
                 // TODO (optional): play swap / clear / drop / refill animations using your MovablePiece
                 // For now, immediately repaint to the resolved state:
                 RenderFromLogic(); //happens everytime anything happens like swaps or cascades..
                 // Debug.Log($"Swap OK. Cleared={cleared}, Cascades={cascades}");
             }
             else
             {
                 // TODO (optional): animate swap-back for invalid move
                 // Debug.Log("Invalid swap (no match) -> swap back");
             }
         }*/

    // --------------------------------------------
    // Rendering helpers (logic -> visuals)
    // --------------------------------------------

    /// <summary>
    /// First-time construction: only at the very beginning (right after Start()).
    /// Replaces EMPTY placeholders with NORMAL pieces and assigns colors from logic.
    /// </summary>
    // Prevent double inputs while a swap/animation is running
    private bool inputLocked = false;

    /// <summary>
    /// Attempt to swap two cells (Unity coords: x=col, y=row).
    /// Guards game-over, bounds, adjacency, and input reentry,
    /// then runs the animated attempt via coroutine.
    /// </summary>
    public void TrySwap(int x1, int y1, int x2, int y2)
    {
        // If you wired a GameManager, block input after game over
        if (gameManager != null && gameManager.IsOver) return;

        // Bounds check (safety)
        if (x1 < 0 || x1 >= xDim || y1 < 0 || y1 >= yDim) return;
        if (x2 < 0 || x2 >= xDim || y2 < 0 || y2 >= yDim) return;

        // Only orthogonal neighbors
        if (!AreAdjacent(x1, y1, x2, y2)) return;

        if (!inputLocked)
            StartCoroutine(TrySwapRoutine(x1, y1, x2, y2));
    }

    /// <summary>
    /// Do the visual swap, ask the logic to validate & resolve,
    /// then either keep the new state (valid) or swap back (invalid).
    /// Also notifies GameManager on valid/invalid.
    /// </summary>
    private IEnumerator TrySwapRoutine(int x1, int y1, int x2, int y2)
    {
        inputLocked = true;

        var p1 = pieces[x1, y1];
        var p2 = pieces[x2, y2];
        if (p1 == null || p2 == null) { inputLocked = false; yield break; }
        if (!p1.IsMovable() || !p2.IsMovable()) { inputLocked = false; yield break; }

        // Kick off the visual swap
        p1.MovableComponent.Move(x2, y2, fillTime);
        p2.MovableComponent.Move(x1, y1, fillTime);

        // Keep the mapping in sync with what you see
        pieces[x1, y1] = p2;
        pieces[x2, y2] = p1;

        // Wait for the swap animation
        yield return new WaitForSeconds(fillTime);

        // Ask the logic (logic uses (row,col) = (y,x))
        var a = new Match3.Coord(y1, x1);
        var b = new Match3.Coord(y2, x2);

        if (logicBoard.TrySwap(a, b, out int cleared, out int cascades))
        {
            // Valid move: repaint to final state
            RenderFromLogic();

            // Update score/moves/win-lose
            if (gameManager != null)
                gameManager.OnValidSwapResolved(cleared, cascades);
        }
        else
        {
            // Invalid move: swap back visually (slightly faster)
            p1.MovableComponent.Move(x1, y1, fillTime * 0.75f);
            p2.MovableComponent.Move(x2, y2, fillTime * 0.75f);

            // Restore mapping
            pieces[x1, y1] = p1;
            pieces[x2, y2] = p2;

            yield return new WaitForSeconds(fillTime * 0.75f);

            // Optionally consume move on invalid attempts:
            if (gameManager != null)
                gameManager.OnInvalidSwap();
        }

        inputLocked = false;
    }

    /// <summary>Are (x1,y1) and (x2,y2) orthogonal neighbors?</summary>
    private bool AreAdjacent(int x1, int y1, int x2, int y2)
    {
        return Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) == 1;
    }

    private void BuildInitialFromLogic()
    {
        int[,] state = logicBoard.GetBoard(); // [row, col] //gets the matrix with the colors from logicBoard.GetBoard() (it wll start with -1 probs cuz its empty)

        for (int r = 0; r < yDim; r++)
        {
            for (int c = 0; c < xDim; c++)
            {
                // replace placeholder EMPTY with a NORMAL piece
                if (pieces[c, r] != null) //check if a piece exists
                {
                    Destroy(pieces[c, r].gameObject); //destroys the placeholder (it started with empty)
                }

                var p = SpawnNewPiece(c, r, PieceType.NORMAL); //spawns a NORMAL piece in [r,c]

                if (p.IsColored())  //p is the piece obvi
                {
                    int colorId = state[r, c];//finds what the color in the cell (the p piece we spawned
                    p.ColorComponent.SetColor((ColorPiece.ColorType)colorId); //matches the color to logic of the borad (meaning you cant start the board with 3 of the same color in a row
                    //basicallyyy tells the piece’s ColorComponent to paint itself with the enum color that matches the logic board’s number at (r,c)
                    //p.ColorComponent is a component attached to the piece that handles its color
                    //SetColor(...) is a method that assigns a specific color to the piece
                    //colorpiece is a script and colortype is an enum in it that lists the colors
                    //(ColorPiece.ColorType)colorId casts that int (colorid) into the matching enum value
                }
            }
        }
    }

    /// <summary>
    /// after moves, clears, cascades, refills. basicallllly any time the logic state changes.
    /// Keeps the existing GamePiece objects in place but updates their color to match the current logic state
    /// Does not spawn/destroy pieces unless they’re already EMPTY
    /// This version "teleports" state (no falling animation yet).
    /// </summary>
    private void RenderFromLogic()
    {
        int[,] state = logicBoard.GetBoard(); // [row, col] //gets the matrix with colors

        for (int r = 0; r < yDim; r++)
        {
            for (int c = 0; c < xDim; c++)
            {
                var p = pieces[c, r]; //save the color in p (it what defines the piece kind)
                if (p == null) continue;
                if (p.Type == PieceType.EMPTY) continue;
                if (!p.IsColored()) continue;
                
                int colorId = state[r, c];
                p.ColorComponent.SetColor((ColorPiece.ColorType)colorId);
            }
        }
    }

    /* private bool AreAdjacent(int x1, int y1, int x2, int y2) //Checks if two cells are next to each other in the grid (up/down/left/right).
     {
         return Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) == 1; //It measures the difference between their x and y. If the total distance is exactly 1, they’re neighbors.
     }
    */

    private bool TryGetCellUnderMouse(out int gx, out int gy) //Converts a mouse click in world space (Unity’s coordinates) into grid cell coordinates (gx, gy).
    {
        gx = gy = -1;
        var cam = Camera.main;
        if (cam == null) return false;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition); //Takes the mouse position (Input.mousePosition) and projects it into the world (Camera.main.ScreenToWorldPoint).
        float left = transform.position.x - xDim / 2.0f;
        float top = transform.position.y + yDim / 2.0f;

        //Figures out which grid cell that position corresponds to by comparing against the grid’s origin, width, and height.
        int x = Mathf.RoundToInt(world.x - left);
        int y = Mathf.RoundToInt(top - world.y);

        //If the position is outside the board, returns false. Otherwise, it outputs the correct grid indices
        if (x < 0 || x >= xDim || y < 0 || y >= yDim) return false;
        gx = x; gy = y;
        return true;
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
    public GamePiece SpawnNewPiece(int x, int y, PieceType type) //GamePiece manages just one cell’s object
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
/*
    // ===== Added: helpers used by the click-to-swap logic =====

    /// <summary>
    /// Are (x1,y1) and (x2,y2) orthogonally adjacent?
    /// </summary>
    private bool AreAdjacent(int x1, int y1, int x2, int y2)
    {
        return Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) == 1;
    }

    /// <summary>
    /// Convert a mouse click in world space to a grid cell (gx,gy).
    /// Returns false if outside the board bounds.
    /// </summary>
    private bool TryGetCellUnderMouse(out int gx, out int gy)
    {
        gx = gy = -1;

        var cam = Camera.main;
        if (cam == null) return false;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        // Inverse of GetWorldPos:
        float left = transform.position.x - xDim / 2.0f;
        float top = transform.position.y + yDim / 2.0f;

        int x = Mathf.RoundToInt(world.x - left);
        int y = Mathf.RoundToInt(top - world.y);

        if (x < 0 || x >= xDim || y < 0 || y >= yDim) return false;

        gx = x; gy = y;
        return true;
    }
*/
}
