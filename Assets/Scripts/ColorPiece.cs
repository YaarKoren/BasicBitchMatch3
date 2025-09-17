using UnityEngine;
using System.Collections;
using System.Collections.Generic; //to get access to dictionary class

public class ColorPiece : MonoBehaviour
{

    public enum ColorType { 
        Color0,
        Color1,
        Color2,
        Color3,
        Color4,
        Color5,       
        ANY,
        COUNT
    }

    //an array to assign a sprite to each color
    [System.Serializable]   
    public struct ColorSprite {
        public ColorType color;
        public Sprite sprite;
    }
    public ColorSprite[] colorSprites;

    private ColorType color_;

    //Getter + Setter
    public ColorType Color { 
        get {  return color_; }
        set { SetColor(value); }
    }

    private SpriteRenderer sprite_;

    public int ColorsNum {
        get { return colorSprites.Length; }
    }

    private Dictionary<ColorType, Sprite> colorSpriteDict;

    private void Awake()
    {
        sprite_ = transform.Find("piece").GetComponent<SpriteRenderer>(); //to find the child GameObject

        colorSpriteDict = new Dictionary<ColorType, Sprite>();
        for (int i = 0; i < colorSprites.Length; i++)
        {
            if (!colorSpriteDict.ContainsKey(colorSprites[i].color))
            {
                colorSpriteDict.Add(colorSprites[i].color, colorSprites[i].sprite);

            }
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update() 
    {
        
    }

    public void SetColor(ColorType color)
    {
        color_ = color;

        //change sprite according to color
        if (colorSpriteDict.ContainsKey(color))
        {
            sprite_.sprite = colorSpriteDict[color];

        }
    }


}


/*
//-----------------------------------------------------------------------//

using UnityEngine;

/// <summary>
/// Handles the "color" of a piece. Your logic uses int color IDs;
/// we map them to an enum here and paint the SpriteRenderer accordingly.
/// Keep it simple now (tints); you can later swap sprites per color if you prefer.
/// </summary>
public class ColorPiece : MonoBehaviour
{
    // Enum that corresponds to your logic's color IDs (0..customColorsNum-1).
    // Make sure you have at least as many variants here as your "customColorsNum" in Grid.
    public enum ColorType
    {
        Color0 = 0,
        Color1 = 1,
        Color2 = 2,
        Color3 = 3,
        Color4 = 4,
        Color5 = 5,
        // add more if you increase customColorsNum
    }

    [Header("Rendering")]
    [Tooltip("SpriteRenderer to tint. If null, will auto-find on Start.")]
    public SpriteRenderer sr;

    [Header("Palette (matches ColorType order)")]
    [Tooltip("Color palette used to tint the sprite per enum value.")]
    public Color[] palette =
    {
        new Color(0.90f, 0.20f, 0.25f), // Color0
        new Color(0.20f, 0.70f, 0.30f), // Color1
        new Color(0.25f, 0.45f, 0.95f), // Color2
        new Color(0.95f, 0.80f, 0.25f), // Color3
        new Color(0.70f, 0.25f, 0.90f), // Color4
        new Color(0.20f, 0.80f, 0.80f), // Color5
    };

    // current color
    public ColorType Current { get; private set; } = ColorType.Color0;

    void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Assign visual color to match the logic board's color id (cast to ColorType).
    /// </summary>
    public void SetColor(ColorType color)
    {
        Current = color;
        if (sr != null)
        {
            int idx = (int)color;
            if (palette != null && idx >= 0 && idx < palette.Length)
                sr.color = palette[idx];
        }
    }
}
*/