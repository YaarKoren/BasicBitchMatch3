using UnityEngine;
using System.Collections;


public class GamePiece : MonoBehaviour
{
    private int x_;
    private int y_;

    private Grid.PieceType type_;

    private Grid gridRef_; //reference to the grid, in case the piece needs information about the board and other pieces

    private MovablePiece movableComponent_; //ref to the movable component

    private ColorPiece colorComponent_; //ref to the color component

    //Getters + Setters
    public int X
    {
        get { return x_; }
        set {
            if (IsMovable())
            {
                x_ = value;
            }
        }
    }
    public int Y
    {
        get { return y_; }
        set
        {
            if (IsMovable())
            {
                y_ = value;
            }
        }
    }
    public Grid.PieceType Type
    {
        get { return type_; }
    }

    public Grid GridRef
    {
        get { return gridRef_; }
    }
    public MovablePiece MovableComponent
    {
        get { return movableComponent_; }
    }

    public ColorPiece ColorComponent
    {
        get { return colorComponent_; }
    }


    private void Awake()
    {
        movableComponent_ = GetComponent<MovablePiece>(); //if the GameObject does not have this component, this will return null
        colorComponent_ = GetComponent<ColorPiece>(); //if the GameObject does not have this component, this will return null
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {


    }

    public void Init(int x, int y, Grid gridRef, Grid.PieceType type)
    {
        x_ = x;
        y_ = y;
        gridRef_ = gridRef;
        type_ = type;

    }

    //check if the piece is movable
    public bool IsMovable() { 
        return movableComponent_ != null;   
    }

    //check if the piece has color
    public bool IsColored()
    {
        return colorComponent_ != null;
    }
}

