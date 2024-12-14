using Nobi.UiRoundedCorners;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


//public struct ButtonColours
//{
//    public Color Foreground;
//    public Color Background;
//    public ButtonColours(string background, string foreground) { 
//        Foreground = ColorHelper.ColorFromRGB(foreground);
//        Background = ColorHelper.ColorFromRGB(background);
//    }
//}

public struct RainbowButtonState { 
    public Color Foreground;
    public Color Background;
    public Sprite sprite;

    private void invertColors()
    {
        Color tmp = Foreground;
        Foreground = Background;
        Background = tmp;
    }

    public RainbowButtonState( string fgColorRGBA, string bgColorRGBA, Sprite sprite, bool inverted = false )
    {
        Foreground = ColorHelper.ColorFromRGBA(fgColorRGBA);
        Background = ColorHelper.ColorFromRGBA(bgColorRGBA);       
        this.sprite = sprite;
        if (inverted)
        {
            invertColors();
        }
    }

    public RainbowButtonState(Color fgColor, Color bgColor, Sprite sprite, bool inverted = false)
    {
        Foreground = fgColor;
        Background = bgColor;
        this.sprite = sprite;
        if (inverted)
        {
            invertColors();
        }

    }

    public RainbowButtonState(Sprite sprite, bool inverted = false)
    {
        Foreground = RainbowGraphicsButton.ColorForegroundDefault;
        Background = RainbowGraphicsButton.ColorBackgroundDefault;
        this.sprite = sprite;
        if (inverted)
        {
            invertColors();
        }
    }
}

public class RainbowGraphicsButton 
{
    public Button button;
    public RectTransform buttonRectTransform;
    public RawImage  Background;
    private ImageWithRoundedCorners corners;
    private List<Sprite> sprites;
    private float deltaX = 6;
    private float deltaY = 6;  
    private bool round = false;
    private float innerMargins = 10;
    public List<RainbowButtonState> States = new();

    private int state;
    public static Color ColorBackgroundDefault = new Color(35 / 255.0f, 35 / 255.0f, 36 / 255.0f, 1);
    public static Color ColorForegroundDefault = Color.white;

    public Color BgColor;
    public Color FgColor;
    // public List<ButtonColours> ColorsForState = new();

    public float Radius
    {
        get => corners.radius;
        set {
            if(!round)
            {
                corners.radius = value;
            }
        }
    }

    public float InnerMargins
    {
        get => innerMargins;
        set
        {
            innerMargins = value;
            Vector2 parentRect = Background.rectTransform.sizeDelta;
            
            buttonRectTransform.sizeDelta = new Vector2(parentRect.x - innerMargins, parentRect.y - innerMargins); 
        }
    }
    public RainbowGraphicsButton AddState( RainbowButtonState state)
    {
        States.Add(state);
        return this;
    }

    public bool Round {  get => round; 
        set {
            round = value;
            if( corners != null && value )
            {
                corners.radius = Background.rectTransform.sizeDelta.x / 2;                
            }
        }
    }

    public RectTransform rectTransform
    {
        get => Background.rectTransform;
    }

    public int State
    {
        get => state;
        set {
            if (state >= States.Count)
            {
                return;
            }
            state = value;
            var image = button.GetComponent<Image>();
            image.useSpriteMesh = true;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            Sprite sprite = States[state].sprite;

            image.sprite = sprite;
            Background.color = States[state].Background;
            image.color = States[state].Foreground;
        }
    }

    public RainbowGraphicsButton() 
    {
        sprites = new();
        BgColor = ColorBackgroundDefault;
        FgColor = ColorForegroundDefault;
    }

    static public RainbowGraphicsButton Wrap(Button button, string[] sprites) 
    {
        RainbowGraphicsButton result = new RainbowGraphicsButton();
        result.button = button;
        foreach( var resource in sprites )
        {
            result.sprites.Add(Resources.Load<Sprite>(resource));
        }
        string name = $"Background {button.name}";
        result.Background = new GameObject(name).AddComponent<RawImage>();
        result.corners = result.Background.AddComponent<ImageWithRoundedCorners>();
        result.corners.radius = 8;
        // result.Background.transform.parent = button.transform.parent;
        result.Background.transform.SetParent(button.transform.parent, true);
        RectTransform buttonRectTransform = button.GetComponent<RectTransform>();
        result.Background.rectTransform.anchorMax = buttonRectTransform.anchorMax;
        result.Background.rectTransform.anchorMin = buttonRectTransform.anchorMin;
        result.Background.rectTransform.anchoredPosition = buttonRectTransform.anchoredPosition;
        result.Background.rectTransform.pivot = buttonRectTransform.pivot;
        button.transform.parent = result.Background.transform;
        return result;
    }

    public RainbowGraphicsButton(string name, float width, float height, Transform parent)
    {
        Background = new GameObject($"RainbowButton {name}").AddComponent<RawImage>();
        corners = Background.AddComponent<ImageWithRoundedCorners>();
        corners.radius = 8;
        corners.Refresh();
        Background.color = ColorBackgroundDefault;
        Background.rectTransform.sizeDelta = new Vector2(width, height);
        Background.rectTransform.SetParent(parent,true);
        
        button = new GameObject($"Btn {name}").AddComponent<Button>();
        button.transform.SetParent(Background.rectTransform, true);
        buttonRectTransform = button.AddComponent<RectTransform>();

        Object.Destroy(button.GetComponentInChildren<TMP_Text>());
        Image btnImage = button.AddComponent<Image>();        
        btnImage.type = Image.Type.Simple;
        btnImage.sprite = Resources.Load<Sprite>("Images/SVG/grow");
        buttonRectTransform.sizeDelta = new Vector2(btnImage.sprite.rect.width - deltaX, btnImage.sprite.rect.height - deltaY);        
        btnImage.preserveAspect = true;
        btnImage.useSpriteMesh = true;
        btnImage.color = Color.black;
    }

    static public RainbowGraphicsButton Make( string name, float width, float height, Transform parent)
    {
        return new RainbowGraphicsButton(name,width, height, parent);        
    }
}
