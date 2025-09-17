using System.Collections;
using UnityEngine;

public class MovablePiece : MonoBehaviour
{
    private GamePiece piece;

    private IEnumerator moveCoroutine;

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

    //public void Move(int newX, int newY) {
        //piece.transform.localPosition = piece.GridRef.GetWorldPos(newX, newY); //the piece is a child of the grid, that's why we use local position
    //}
    
    //wrapper function to start and stip the Coroutine move function
    public void Move(int newX, int newY, /*hoe many the animation should wait*/ float time)
    {
       if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        //get a ref to a new Corouting
        moveCoroutine = MoveCoroutine(newX, newY, time);
        StartCoroutine(moveCoroutine);
    }

    private IEnumerator MoveCoroutine(int newX, int newY, float time)
    {
        piece.X = newX;
        piece.Y = newY; 

        //---amimation---
        //interpolate between the starting and ending pos of the piece, moving it a tiny bit each frame
        Vector3 startPos = transform.position;
        Vector3 endPos = piece.GridRef.GetWorldPos(newX, newY);

        //interpolation is getting a value between 2 values, using a parameter t
        //loop through t values from 0 to how long how animation should take (the varibale time) - to make our animation exactly time seconds to execute
        //Time.deltaTime is the amount time the lastframe took
        for (float t=0; t <= 1 * time; t += Time.deltaTime)
        {
            piece.transform.position = Vector3.Lerp(startPos, endPos, t / time); //divide t by time, to get a value between 0 and 1
            yield return 0; //wait for 1 frame
        }

        piece.transform.position = endPos; //just in case didn't get to the endPos

    }
}

   /*
    //-----------------------------------------------------------//

    using System.Collections;
using UnityEngine;

/// <summary>
/// Tiny helper that moves a piece to a target position over time.
/// Grid.fillTime controls the duration (you already have that in your Grid).
/// You can later replace with DOTween/LeanTween; the API stays the same.
/// </summary>
public class MovablePiece : MonoBehaviour
{
    /// <summary>
    /// Smoothly moves the GameObject to a world-space target in 'duration' seconds.
    /// Designed so Grid coroutines can 'yield return' it.
    /// </summary>
    public IEnumerator MoveTo(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float t = 0f;
        duration = Mathf.Max(0.0001f, duration);

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, target, Mathf.Clamp01(t));
            yield return null;
        }

        transform.position = target; // snap at end
    }

    /// <summary>
    /// Immediately snaps to a world-space position (no animation).
    /// </summary>
    public void SnapTo(Vector3 target)
    {
        transform.position = target;
    }
}
   */