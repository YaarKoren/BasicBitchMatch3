using UnityEngine;
using System.Collections;
using System.Collections.Generic; //to get access to dictionary class

public class Grid : MonoBehaviour
{
    
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

    public PiecePrefab[] piecePrefabs;


    private Dictionary<PieceType, GameObject> piecePrefabDict; //dictionaries can't be displayed in the inspectpr; we have the array of PiecePrefab for that
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
