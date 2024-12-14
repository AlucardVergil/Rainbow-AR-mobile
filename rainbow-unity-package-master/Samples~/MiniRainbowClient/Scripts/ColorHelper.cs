using UnityEngine;

public class ColorHelper
{

    public static Color ColorFromRGB( string  color )
    {
        int intValue = System.Convert.ToInt32(color, 16);
        Color result = new(
            (float)((intValue & 0xFF0000) >> 16) / 255.0f,
            (float)((intValue & 0xFF00) >> 8) / 255.0f,
            (float)(intValue & 0xFF) / 255.0f,
            1);
        return result;        
    }

    public static Color ColorFromRGBA(string color)
    {
        int intValue = System.Convert.ToInt32(color, 16);
        Color result = new(
            (float)((intValue & 0xFF000000) >> 24) / 255.0f,
            (float)((intValue & 0xFF0000) >> 16) / 255.0f,
            (float)((intValue & 0xFF00) >> 8) / 255.0f,
            (float)(intValue & 0xFF) / 255.0f);
        return result;
    }
 }
