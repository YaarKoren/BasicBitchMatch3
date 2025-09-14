using UnityEngine;
using System.Collections;


public class GamePiece : MonoBehaviour
{
    private int x;
    private int y;

    private Grid.PieceType type;

    private Grid gridRef; //reference to the grid, in case the piece needs information about the board and other pieces

    //Getters
    public int getX {
        get { return x; } 
    }
    public int getY {
        get { return x; } 
    }
    public Grid.PieceType getType
    {
        get { return type; }
    }

    public Grid getGridRef
    {
        get { return gridRef; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {

    }

    // Update is called once per frame
    void Update() {


    }

    public void init(int _x, int _y, Grid _gridRef, Grid.PieceType _type) {

    }
}

