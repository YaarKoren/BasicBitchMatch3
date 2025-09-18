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
        set
        {
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
    public bool IsMovable()
    {
        return movableComponent_ != null;
    }

    //check if the piece has color
    public bool IsColored()
    {
        return colorComponent_ != null;
    }

    //if on == true, make the scale 1.08 (≈8% bigger) or anything i decide
    //if on == false, reset the scale to 1.0 (normal size)
    public void SetSelected(bool on)
    {
        // simple pulse highlight; replace with outline if you want
        transform.localScale = on ? Vector3.one * 1.15f : Vector3.one;
    }

    // --------------------------------------------
    // detcet mouse clicks
    // --------------------------------------------

    // called when the mosue enters an element
    void OnMouseEnter()
    {
        //Debug.Log("OnMouseEnter on");
        gridRef_.EnterPiece(this); //this = a reference to self, this GamePiece (the one that was clicked on, in this case)
    }

    //called when the mouse is pressed inside an element         
    void OnMouseDown()
    {
        //Debug.Log("OnMouseDown on");
        gridRef_.PressPiece(this);
    }

    //called when the mouse is released     
    void OnMouseUp()
    {
        //Debug.Log("OnMouseUp on");
        gridRef_.ReleasePiece();
    }


}


