using UnityEngine;
using System.Collections;
using System.Collections.Generic; //to get access to dictionary class


public class Grid : MonoBehaviour {
    
    public enum PieceType
    {
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
    void Start()
    {
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
            for (int y = 0; y < yDim; y++) { //cols
                GameObject background = (GameObject)Instantiate(
                    backgroundPrefab,
                    /*position*/GetWorldPos(x,y),
                    /*rotation*/ Quaternion.identity//not rotated at all
                    ); 
                //make the BG a child of the Grid object
                background.transform.parent = transform;
            }
        }

        //---Instantiate Game Piece for each cell of the grid---
        pieces = new GamePiece[xDim, yDim];
        for (int x = 0; x < xDim; x++) { //rows
            for (int y = 0; y < yDim; y++) { //cols
                GameObject newPiece = (GameObject)Instantiate(
                    piecePrefabDict[PieceType.NORMAL], //TODO: WHY?!
                    /*position*/ GetWorldPos(x, y),
                    /*rotation*/ Quaternion.identity//not rotated at all
                    );   
                //change the name
                newPiece.name = "Piece(" + x + ", " + y + ")";
                //make the piece a child of the Grid object
                newPiece.transform.parent = transform;

                //keep the piece in the array
                pieces[x,y] = newPiece.GetComponent<GamePiece>();

                //update the piece's info
                pieces[x, y].init(x, y, this, PieceType.NORMAL);

            }
        }

    }

    // Update is called once per frame
    void Update()
    {
       

    }

    //---centralize the gird---
    //funciton to convert a grid cordinate to a world position
    Vector2 GetWorldPos(int x, int y)
    {
        return new Vector2(transform.position.x - xDim / 2.0f + x,
            transform.position.y + yDim / 2.0f - y); //our grid starts at the top
    }
}




