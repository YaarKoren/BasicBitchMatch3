using UnityEngine;
using System.Collections;
using System.Collections.Generic; //to get access to dictionary class

public class ColorPiece : MonoBehaviour
{

    public enum ColorType { 
        BROWN,
        GREEN,
        RED,
        PURPLE,
        BLUE,
        ORANGE,
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
