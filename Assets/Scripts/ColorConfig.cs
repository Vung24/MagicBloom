using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class ColorData
{
    public char key;
    public Color color;
}

[CreateAssetMenu(fileName = "ColorConfig", menuName = "ScriptableObjects/ColorConfig", order = 1)]
public class ColorConfig : ScriptableObject
{
    public List<ColorData> colorDatas;
    public Dictionary<char, ColorData> dictColor;

    public ColorData GetColorByKey(char key)
    {
        if(dictColor == null)
        {
            dictColor = new Dictionary<char, ColorData>();
            for(int i = 0; i < colorDatas.Count; i++)
            {
                dictColor.Add(colorDatas[i].key, colorDatas[i]);
            }
        }
        return dictColor[key];
    }
}


