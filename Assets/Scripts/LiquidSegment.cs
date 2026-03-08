using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LiquidSegment : MonoBehaviour
{
    private ColorData dataLiquid;    
    [SerializeField] private Image image;
    public char ColorKey => dataLiquid != null ? dataLiquid.key : '\0';
    public Color Color => image != null ? image.color : Color.white;
    public ColorData Data => dataLiquid;

    public void SetColor(ColorData dataLiquid)
    {
        this.dataLiquid = dataLiquid;
        if(image != null && dataLiquid != null)
        {
            image.color = dataLiquid.color;
        }
    }
}
