using UnityEngine;

public class MovablePiece : MonoBehaviour
{
    private GamePiece piece;

    private void Awake() {
        piece = GetComponent<GamePiece>();

    }




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Move(int newX, int newY) {
        piece.X = newX; 
        piece.Y = newY;
        piece.transform.localPosition = piece.GridRef.GetWorldPos(newX, newY); //the piece is a child of the grid, that's why we use local position

    }
}
