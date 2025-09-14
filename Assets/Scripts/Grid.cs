using UnityEngine;
using System.Collections;
using System.Collections.Generic; //to get access to dictionary class



public class Grid : MonoBehaviour {
    
    public enum PieceType {  //names of pieces
    
        EMPTY,
        NORMAL,
        COUNT,
    };

    [System.Serializable] //to make our custom struct be seen in the inspector
    public struct PiecePrefab
    {
        public PieceType type;
        public GameObject prefab;
    };
    
    public int xDim;
    public int yDim;

    public PiecePrefab[] piecePrefabs; //types of pieces to have in the inspector
    public GameObject backgroundPrefab;

    private Dictionary<PieceType, GameObject> piecePrefabDict; //dictionaries can't be displayed in the inspectpr; we have the array of PiecePrefab for that

    //2D array of GameObjects
    private GamePiece[,] pieces; //2D array
    //TODO: undesrstand why it's needed, we aleardy have an array of GameObjects --> answer: this is for the grid itself; the other array just holds the type, and does not instantiate objects
    //we create an array for this and not the BG for each cell, cuz we want to manipulate those

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        //-----copy the values from the PiecePrefab array, to the dictionary----

        piecePrefabDict = new Dictionary<PieceType, GameObject>();
        //loop through the array
        for (int i = 0; i < piecePrefabs.Length; i++) {
            //check if the dict already does not contains the key --> add the key+value
            if (!piecePrefabDict.ContainsKey(piecePrefabs[i].type)) {
                piecePrefabDict.Add(piecePrefabs[i].type, piecePrefabs[i].prefab);
            }

            //TODO: what if the key already exists (and if it's not happeening why use the if)
        }


        //---Instantiate BG for each cell of the grid---
        for (int x = 0; x < xDim; x++) { //rows
            for (int y = 0; y < yDim; y++)
            { //cols
                GameObject background = (GameObject)Instantiate(
                    backgroundPrefab,
                    /*position*/GetWorldPos(x, y),
                    /*rotation*/ Quaternion.identity //not rotated at all
                    );
                //make the BG a child of the Grid object
                background.transform.parent = transform;
            }
        }

        //---Instantiate Game Piece for each cell of the grid---
        pieces = new GamePiece[xDim, yDim];
        for (int x = 0; x < xDim; x++) { //rows
            for (int y = 0; y < yDim; y++) { //cols
                //Debug.Log("inside loop, x: " + x + ", y: " + y);
                SpwanNewPiece(x, y, PieceType.EMPTY); //init as empty
                //set the position
                //if (pieces[x,y].IsMovable()) {
                    //pieces[x, y].MovableComponent.Move(x, y);
                //}

                //set the color to a random coloer
                //if (pieces[x, y].IsColored())
                //{
                    //int number_of_colors = pieces[x, y].ColorComponent.ColorsNum;
                    //ColorPiece.ColorType random_color = (ColorPiece.ColorType)Random.Range(0, number_of_colors);
                    //pieces[x, y].ColorComponent.SetColor(random_color);

                //}
            }
        }

        Fill();
    }




    // Update is called once per frame
    void Update()
    {
       

    }

    //---centralize the gird---
    //funciton to convert a grid cordinate to a world position
    //public so the GamePiece could use it as well
    public Vector2 GetWorldPos(int x, int y)
    {
        return new Vector2(transform.position.x - xDim / 2.0f + x,
            transform.position.y + yDim / 2.0f - y); //our grid starts at the top
    }

    public GamePiece SpwanNewPiece(int x, int y, PieceType type) {
        GameObject newPiece = (GameObject)Instantiate(
            piecePrefabDict[type], //using the dict to get the game object assoicates to this type
            /*position*/ GetWorldPos(x, y),
            /*rotation*/ Quaternion.identity //not rotated at all
            );

        //change the name
        newPiece.name = "Piece(" + x + ", " + y + ")";
        
        //make the new piece a child of the Grid object
        newPiece.transform.parent = transform;

        //store the game piece in our pieces array
        pieces[x, y] = newPiece.GetComponent<GamePiece>();

        //initialzie the piece's info/fields
        pieces[x, y].Init(x, y, this, type);

        return pieces[x, y];    

    }

    //calls FillStep untill the board is filled
    public void Fill()
    {
        while (FillStep()) {}
    }

    //moves each piece only one step
    //return TRUE if any pieces were moved
    public bool FillStep() {
        bool movePiece = false;
        //loop through all cols in reverse order, bottom to top
        //we ignore the last one cuz we are looking for pieces we can move down, and the lase one (dim-1) - we can't
        for (int y = yDim-2; y >=0; y--)
        {
            for (int x = 0; x<xDim; x++)
            {
                Debug.Log("inside FillStep loop, x: " + x + ", y: " + y);
                GamePiece piece = pieces[x, y];

                //if it's not movable - we can't move it down to fill the empty space, so we can just ignore it
                if (piece.IsMovable())
                {
                    GamePiece pieceBelow = pieces[x, y + 1];
                    if (pieceBelow.Type == PieceType.EMPTY)
                    {
                        piece.MovableComponent.Move(x, y + 1);
                        pieces[x, y + 1] = piece;
                        SpwanNewPiece(x, y, PieceType.EMPTY); //actually we are swapping a movable piece with an empty piece below it
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
            Debug.Log("inside FillStep loop top row, x: " + x + ", y: 0");
            GamePiece pieceBelow = pieces[x, 0];
            if (pieceBelow.Type == PieceType.EMPTY)
            {
                //we don't use the SpwanNewPiece() func cuz of the -1 thing
                GameObject newPiece = (GameObject)Instantiate(
                     piecePrefabDict[PieceType.NORMAL],
                     GetWorldPos(x, -1), //create in the "non-existing" row, above the top row
                     Quaternion.identity);
                newPiece.transform.parent = transform;  

                pieces[x, 0] = newPiece.GetComponent<GamePiece>();
                pieces[x, 0].Init(x, -1, this, PieceType.NORMAL); //we set it for -1 and not 0, for animation
                pieces[x, 0].MovableComponent.Move(x, 0);
                pieces[x, 0].ColorComponent.SetColor( (ColorPiece.ColorType)Random.Range(0, pieces[x,0].ColorComponent.ColorsNum) );
                movePiece = true;

            }
        }
        return movePiece;
    }


}




