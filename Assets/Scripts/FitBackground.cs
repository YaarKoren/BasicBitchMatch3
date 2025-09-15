using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FitBackground : MonoBehaviour
{
    void Start()
    {
        var cam = Camera.main;
        var sr = GetComponent<SpriteRenderer>();

        // Get sprite bounds (in world units)
        float spriteWidth = sr.sprite.bounds.size.x;
        float spriteHeight = sr.sprite.bounds.size.y;

        // Camera height in world units
        float worldHeight = cam.orthographicSize * 2f;
        // Camera width in world units
        float worldWidth = worldHeight * cam.aspect;

        // Scale to fit
        transform.localScale = new Vector3(
            worldWidth / spriteWidth,
            worldHeight / spriteHeight,
            1f
        );
    }
}
