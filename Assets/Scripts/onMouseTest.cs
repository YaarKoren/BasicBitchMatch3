using UnityEngine;

public class ClickProbe2D : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.OverlapPoint(wp);
            Debug.Log(hit ? $"2D HIT: {hit.name}" : "2D MISS");
        }
    }
}