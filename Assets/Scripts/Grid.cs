using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
//using Unity.Android.Gradle;

// add the logic namespace
using Match3;

public class Grid : MonoBehaviour //Grid manages the whole board while GamePiece manages just one cell’s object
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
    public int xDim; //grid size (columns × rows).
    public int yDim;

    [Header("Animation")]
    public float fillTime = 0.2f; // still used by MovablePiece if/when you animate

    [Header("Prefabs")]
    public PiecePrefab[] piecePrefabsArr; // which prefab to spawn per PieceType //must include EMPTY and NORMAL at least and might add bubble
    public GameObject backgroundPrefab; //the tile behinf each cell

    [Header("DOTween Animation Durations")]
    public float moveTime = 0.20f;
    public float disappearTime = 0.20f;



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


    //-------------------------------------------------------------------------------------------------------------
    //                                                Animation functions--YAAR
    //-------------------------------------------------------------------------------------------------------------

    //---------------------------------------------------
    // Disappear
    //---------------------------------------------------

    // IMPORTANT: call this BEFORE changing pieces[,] and before destroying the piece
    /// <summary>
    /// Gets array indices (x,y) of a piece and animates it to disappear (scale to 0).
    /// This function does not change pieces[,] and does not destroy the object — animation only.
    /// Note: pos is NOT world position; it is indices in pieces[ , ].
    /// </summary>
    public void DisappearPieceAnimation(Vector2 pos)
    {
        int x = (int)pos.x;
        int y = (int)pos.y;
        GamePiece piece = pieces[x, y];
        if (piece == null) return;
        piece.transform.DOScale(0, disappearTime).SetEase(Ease.OutQuint);
    }

    // IMPORTANT: call this BEFORE changing pieces[,] and before destroying the pieces
    /// <summary>
    /// Gets an array of indices and makes those pieces disappear (animation only).
    /// </summary>
    public void DisappearMultiplePiecesAnimation(Vector2[] positions)
    {
        foreach (Vector2 v in positions)
            DisappearPieceAnimation(v);
    }

    //---------------------------------------------------
    // Swap (2 pieces move one step into each other's cell)
    //---------------------------------------------------

    // IMPORTANT: call this BEFORE changing pieces[,] and before changing X/Y
    /// <summary>
    /// piece1: start [x1,y1] → end [x2,y2]
    /// piece2: start [x2,y2] → end [x1,y1]
    /// Indices are entries in pieces[ , ], not world space.
    /// </summary>
    public void SwapAnimation(int x1, int y1, int x2, int y2)
    {
        MovePieceOneStepAnimation(x1, y1, x2, y2);
        MovePieceOneStepAnimation(x2, y2, x1, y1);
    }

    //---------------------------------------------------
    // Move (one piece one step)
    //---------------------------------------------------

    // IMPORTANT: call this BEFORE changing pieces[,] and before changing X/Y on the piece
    /// <summary>
    /// Move the piece at [currX,currY] to [newX,newY] (indices in pieces[ , ]).
    /// </summary>
    public void MovePieceOneStepAnimation(int currX, int currY, int newX, int newY)
    {
        GamePiece piece = pieces[currX, currY];
        if (piece == null) return;

        Vector2 endPoint = GetWorldPos(newX, newY);
        piece.transform.DOKill(); // kill any prior tween on this piece

        piece.transform.DOMove(endPoint, moveTime).SetEase(Ease.OutQuint);
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
    public void TrySwap(int x1, int y1, int x2, int y2)
    {
        if (!AreAdjacent(x1, y1, x2, y2)) return; //uses the helper func to check if the two cells we are trying to swap are adjacent
        StartCoroutine(TrySwapRoutine(x1, y1, x2, y2)); //the next func
    }
    

    private IEnumerator TrySwapRoutine(int x1, int y1, int x2, int y2)
    {
        var p1 = pieces[x1, y1]; //saves the coord saved at first cell
        var p2 = pieces[x2, y2];
        if (p1 == null || p2 == null) yield break; //if either spot has no piece, abort the coroutine immediately (for safedty)

        //added
        // Snapshot the logic board BEFORE we ask it to resolve anything
        int[,] pre = logicBoard.GetBoard();

        // Animate the visual swap FIRST (do NOT change pieces[,] yet)
        SwapAnimation(x1, y1, x2, y2);
        yield return new WaitForSeconds(moveTime);

        // Predict the FIRST match set on the swapped snapshot
        SwapInSnapshot(pre, x1, y1, x2, y2);
        var firstMatches = FindMatchesOn(pre);      // coords in (row, col)
        var toDisappear = ToPositions(firstMatches); // convert to (x,y) for pieces[,]



        // // Ask logic to do the real swap+resolve (cascades/refill all the way) (rows=y, cols=x)
        var a = new Match3.Coord(y1, x1); //Build the logic coordinate for the first cell. Note: logic uses (row, col) = (y, x)
        var b = new Match3.Coord(y2, x2);


        //Ask the rules engine to commit this swap if it forms a match
        //Returns true if the swap makes ≥3 in a row somewhere; it will then clear, apply gravity, refill, and keep cascading until stable
        if (logicBoard.TrySwap(a, b, out int cleared, out int cascades))
        {
            
            // Valid move:
            //    - Update the pieces[,] mapping so data matches what you now see on screen
            pieces[x1, y1] = p2;
            pieces[x2, y2] = p1;

            // Play your disappear animation on the first matched tiles (if any)
            if (toDisappear.Count > 0)
            {
                DisappearMultiplePiecesAnimation(toDisappear.ToArray());
                yield return new WaitForSeconds(disappearTime);

                // Remove first-wave cleared pieces so they are NOT treated as survivors
                for (int i = 0; i < toDisappear.Count; i++)
                {
                    int dx = (int)toDisappear[i].x;
                    int dy = (int)toDisappear[i].y;
                    var gone = pieces[dx, dy];
                    if (gone != null)
                    {
                        Destroy(gone.gameObject);
                        pieces[dx, dy] = null; // mark empty for the prev snapshot
                    }
                }
                // (optional) give Unity a breath to process Destroy() before snapshot
                // yield return null;

            }


            //replaced with next lines

            // Take a PREV snapshot from visuals (before we repaint anything)
            int[,] prev = SnapshotColorsFromPieces();

            // AFTER is the final stable board from logic
            int[,] after = logicBoard.GetBoard();

            // Kick falling animation to reach AFTER
            yield return StartCoroutine(AnimateResolveToState(prev, after));


            // Any pieces that were scaled to 0 need to be restored for the new frame
            ResetAllScales();

           
            
        }
        else //(no match): we need to swap back visually so the board returns to the original positions
        {
            //  Invalid move: animate back (again, do NOT change pieces[,])
            SwapAnimation(x2, y2, x1, y1);
            //Wait for the swap-back animation to finish before ending the coroutine.
            yield return new WaitForSeconds(moveTime);

            // Mapping stays unchanged (board returns visually to original state)
        }
    }






    // --------------------------------------------
    // Rendering helpers (logic -> visuals)
    // --------------------------------------------

    /// <summary>
    /// First-time construction: only at the very beginning (right after Start()).
    /// Replaces EMPTY placeholders with NORMAL pieces and assigns colors from logic.
    /// </summary>
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
                    int dropOffset = (yDim - r); // higher number = start higher, bigger fall
                    p.transform.position = GetWorldPosAbove(c, dropOffset);
                    p.transform.DOMove(GetWorldPos(c, r), moveTime * (0.35f + 0.10f * dropOffset))
                              .SetEase(Ease.OutQuad);


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

    private bool AreAdjacent(int x1, int y1, int x2, int y2) //Checks if two cells are next to each other in the grid (up/down/left/right).
    {
        return Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) == 1; //It measures the difference between their x and y. If the total distance is exactly 1, they’re neighbors.
    }

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

    // --- LOCAL: run match-finding on an int[,] board snapshot (same rules as logic) ---
    private List<Match3.Coord> FindMatchesOn(int[,] board)
    {
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        const int EMPTY = -1;

        var toClear = new List<Match3.Coord>();

        // horizontal runs
        for (int r = 0; r < rows; r++)
        {
            int run = 1;
            for (int c = 1; c < cols; c++)
            {
                if (board[r, c] != EMPTY && board[r, c] == board[r, c - 1])
                {
                    run++;
                    if (c == cols - 1 && run >= 3)
                        for (int k = c - run + 1; k <= c; k++) toClear.Add(new Match3.Coord(r, k));
                }
                else
                {
                    if (run >= 3)
                        for (int k = c - run; k < c; k++) toClear.Add(new Match3.Coord(r, k));
                    run = 1;
                }
            }
        }

        // vertical runs
        for (int c = 0; c < cols; c++)
        {
            int run = 1;
            for (int r = 1; r < rows; r++)
            {
                if (board[r, c] != EMPTY && board[r, c] == board[r - 1, c])
                {
                    run++;
                    if (r == rows - 1 && run >= 3)
                        for (int k = r - run + 1; k <= r; k++) toClear.Add(new Match3.Coord(k, c));
                }
                else
                {
                    if (run >= 3)
                        for (int k = r - run; k < r; k++) toClear.Add(new Match3.Coord(k, c));
                    run = 1;
                }
            }
        }

        return toClear;
    }


    // Clone + swap in a snapshot (note: logic is [row, col] = [y, x])
    private static void SwapInSnapshot(int[,] snap, int x1, int y1, int x2, int y2)
    {
        int tmp = snap[y1, x1];
        snap[y1, x1] = snap[y2, x2];
        snap[y2, x2] = tmp;
    }

    // Convert logic coords (row,col) -> visual array indices (x=col, y=row)
    private static List<Vector2> ToPositions(List<Match3.Coord> coords)
    {
        var list = new List<Vector2>(coords.Count);
        foreach (var c in coords) list.Add(new Vector2(c.C, c.R));
        return list;
    }

    // Optional: after teleporting state, ensure scaled pieces go back to normal
    private void ResetAllScales()
    {
        for (int x = 0; x < xDim; x++)
            for (int y = 0; y < yDim; y++)
                if (pieces[x, y] != null) pieces[x, y].transform.localScale = Vector3.one;
    }

    //addedddddddd
    // Snapshot current visual colors from pieces[,] into [row,col] ints (-1 for empty).
    private int[,] SnapshotColorsFromPieces()
    {
        int[,] snap = new int[yDim, xDim];
        for (int r = 0; r < yDim; r++)
            for (int c = 0; c < xDim; c++)
            {
                var p = pieces[c, r];
                snap[r, c] = (p == null || p.Type == PieceType.EMPTY || !p.IsColored())
                             ? -1
                             : (int)p.ColorComponent.Color;
            }
        return snap;
    }

    //addedddddddd
    private int CountNonEmptyInCol(int[,] board, int c)
    {
        int cnt = 0;
        for (int r = 0; r < yDim; r++) if (board[r, c] != -1) cnt++;
        return cnt;
    }

    //addedddddddd
    // Utility: get world pos for a "spawn row" above the board (e.g., -k)
    // World pos for a “spawn row” above the board (row = -k is above the top)
    private Vector2 GetWorldPosAbove(int x, int spawnRowOffset)
    {
        return new Vector2(
            transform.position.x - xDim / 2f + x,
            transform.position.y + yDim / 2f - (-spawnRowOffset)
        );
    }



    //addedddddddd
    private IEnumerator AnimateResolveToState(int[,] prev, int[,] after)
    {
        float maxDuration = 0f;                           // track longest tween so we can wait once
        var columnSequences = new List<DG.Tweening.Sequence>(); // one sequence per column

        var newPieces = new GamePiece[xDim, yDim];

        // Column-by-column gravity
        for (int c = 0; c < xDim; c++)
        {
            var seq = DG.Tweening.DOTween.Sequence();
            seq.Pause();   // build first, start later so all tweens begin together


            // Collect survivors from PREV (bottom→top)
            var survivors = new List<GamePiece>();
            for (int r = yDim - 1; r >= 0; r--)
                if (prev[r, c] != -1 && pieces[c, r] != null
                    && pieces[c, r].transform.localScale.x > 0.01f)   // ignore “disappeared” ones
                {
                    survivors.Add(pieces[c, r]);
                }


            int afterCount = CountNonEmptyInCol(after, c);
            int survivorCount = Mathf.Min(survivors.Count, afterCount);
            int newCount = afterCount - survivorCount;

            // Target rows for survivors are the bottom `survivorCount` non-empty rows in AFTER
            var targetRows = new List<int>(survivorCount);
            for (int r = yDim - 1; r >= 0 && targetRows.Count < survivorCount; r--)
                if (after[r, c] != -1) targetRows.Add(r);
            targetRows.Reverse();   // top-most survivor gets smallest target row
            survivors.Reverse();    // survivors top→bottom to index-match targetRows

            // 1) Tween survivors straight to their FINAL rows (multi-row fall)
            for (int i = 0; i < survivorCount; i++)
            {
                var piece = survivors[i];
                int currRow = piece.Y;
                int trgRow = targetRows[i];

                Vector2 end = GetWorldPos(c, trgRow);
                piece.transform.DOKill();
                // duration scales a bit with distance so longer falls feel weighty
                float dist = Mathf.Abs(trgRow - currRow);
                float dur = Mathf.Max(0.35f, moveTime) + 0.12f * dist;
                var t = piece.transform.DOMove(end, dur).SetEase(Ease.OutQuad);
                t.Pause();           // so it doesn’t start before we Play() the seq
                seq.Join(t);
                maxDuration = Mathf.Max(maxDuration, dur);
                newPieces[c, trgRow] = piece;
                piece.X = c; piece.Y = trgRow;
            }

            // 2) Spawn NEW tiles above the board and drop them into remaining rows
            if (newCount > 0)
            {
                var newRows = new List<int>();
                for (int r = 0; r < yDim; r++)
                    if (after[r, c] != -1 && newPieces[c, r] == null)
                        newRows.Add(r);

                for (int i = 0; i < newRows.Count; i++)
                {
                    int r = newRows[i];
                    var p = SpawnNewPiece(c, r, PieceType.NORMAL);
                    if (p.IsColored())
                        p.ColorComponent.SetColor((ColorPiece.ColorType)after[r, c]);

                    /*
                    int spawnOffset = (i + 1); // stack spawns: 1,2,3… higher spawns higher
                    p.transform.position = GetWorldPosAbove(c, spawnOffset);

                    float fallSpan = (yDim + spawnOffset - r);
                    p.transform
                     .DOMove(GetWorldPos(c, r), moveTime * Mathf.Max(0.40f, fallSpan * 0.10f))
                     .SetEase(Ease.OutQuad);

                    
                    */
                    // --- before tweening the new piece ---
                    int spawnOffset = (yDim - r);              // higher target row -> longer fall from above
                    p.transform.position = GetWorldPosAbove(c, spawnOffset);

                    // use a fall duration that scales with how far it travels
                    float distRows = r + spawnOffset;          // from -spawnOffset down to row r
                    p.transform.DOKill();   // clear any old tweens

                    float ndur = Mathf.Max(0.35f, moveTime) + 0.10f * distRows;
                    var nt = p.transform.DOMove(GetWorldPos(c, r), ndur).SetEase(Ease.OutQuad);
                    nt.Pause();
                    seq.Join(nt);
                    maxDuration = Mathf.Max(maxDuration, ndur);

                    newPieces[c, r] = p;
                    p.X = c; 
                    p.Y = r;
                }
            }

            // 3) Make sure empties in AFTER are null in the new mapping
            for (int r = 0; r < yDim; r++)
            {
                if (after[r, c] == -1)
                {
                    var old = pieces[c, r];
                    if (old != null && newPieces[c, r] != old)
                        Destroy(old.gameObject);
                }
            }
            columnSequences.Add(seq);   
        }
        foreach (var s in columnSequences) s.Play();         // everything starts NOW, in sync
        yield return new WaitForSeconds(maxDuration + 0.05f); // small safety margin



        // Let tweens run for a bit so columns sync visually
        //yield return new WaitForSeconds(moveTime * 0.9f);

        // Commit the new mapping
        pieces = newPieces;

        // Final color sanity (should already match)
        for (int r = 0; r < yDim; r++)
            for (int c = 0; c < xDim; c++)
                if (pieces[c, r] != null && pieces[c, r].IsColored() && after[r, c] != -1)
                    pieces[c, r].ColorComponent.SetColor((ColorPiece.ColorType)after[r, c]);
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
