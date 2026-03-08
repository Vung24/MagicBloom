using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private TextAsset dataTube;
    [SerializeField] private Tube tubePrefab;
    [SerializeField] private ColorConfig colorConfig;
    [SerializeField] private LiquidSegment liquidSegmentPrefab;
    // Start is called before the first frame update
    void Start()
    {
        SpawnLevel(dataTube.text);
    }

    public void SpawnLevel(string data)
    {
        string[] layer = data.Split('|');
        int layerCount = layer.Length;
        for(int i = 0; i < layerCount; i++)
        {
            string[] tube = layer[i].Split(';');
             
            for(int j = 0; j < tube.Length; j++)
            {
                int visualLayer = layerCount - 1 - i;
                SpawnTube(tube[j], j, visualLayer);
            }
        }
    }

    public void SpawnTube(string dataTube, int index, int layer)
    {
        Tube tubeObj = Instantiate(
            tubePrefab,
            GameConstant.firstTubePos + new Vector3(0, 120, 0) + index * new Vector3(230, 0, 0) + layer * new Vector3(0, 700, 0),
            Quaternion.identity,
            transform
        );
        string[] keyString = dataTube.Split(',');
        for(int j = keyString.Length - 1; j >= 0; j--)  
        {
            string value = keyString[j].Trim();
            if(string.IsNullOrEmpty(value))
            {
                continue;
            }
            char key = value[0];
            LiquidSegment liquidSegment = SpawnLiquidSegment(key, j);
            tubeObj.liquidSegments.Add(liquidSegment);
            liquidSegment.transform.SetParent(tubeObj.container, false);
        }
    }

    public LiquidSegment SpawnLiquidSegment(char key, int index)
    {
        ColorData colorData = colorConfig.GetColorByKey(key);
        LiquidSegment liquidSegment = Instantiate(liquidSegmentPrefab, GameConstant.firstLiquidPos + GameConstant.liquidSegmentSize * index, Quaternion.identity);
        liquidSegment.SetColor(colorData);
        return liquidSegment;
    }
}
