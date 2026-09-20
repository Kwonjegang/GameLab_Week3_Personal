using UnityEditor;
using UnityEngine;

public class RaiseVisibleTerrainOnly
{
    [MenuItem("Tools/Terrain/Raise Visible Terrain Only")]
    public static void RaiseTerrain()
    {
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();

        if (terrain == null)
        {
            Debug.LogError("씬에서 Terrain을 찾지 못했습니다.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;

        int heightResolution = terrainData.heightmapResolution;
        int holeResolution = terrainData.holesResolution;

        float raiseAmountWorld = 15.5f;
        float normalizedRaiseAmount = raiseAmountWorld / terrainData.size.y;

        float[,] heights = terrainData.GetHeights(
            0,
            0,
            heightResolution,
            heightResolution
        );

        for (int y = 0; y < heightResolution; y++)
        {
            for (int x = 0; x < heightResolution; x++)
            {
                int holeX = Mathf.RoundToInt(
                    (float)x / (heightResolution - 1) * (holeResolution - 1)
                );

                int holeY = Mathf.RoundToInt(
                    (float)y / (heightResolution - 1) * (holeResolution - 1)
                );

                bool isHole = terrainData.IsHole(holeX, holeY);

                if (isHole)
                {
                    continue;
                }

                heights[y, x] += normalizedRaiseAmount;

                if (heights[y, x] > 1f)
                {
                    heights[y, x] = 1f;
                }
            }
        }

        terrainData.SetHeights(0, 0, heights);

        Debug.Log("Paint Holes로 뚫리지 않은 Terrain 부분만 전체적으로 상승시켰습니다.");
    }

    [MenuItem("Tools/Terrain/Lower Visible Terrain Only")]
    public static void LowerTerrain()
    {
        Terrain terrain = Object.FindFirstObjectByType<Terrain>();

        if (terrain == null)
        {
            Debug.LogError("씬에서 Terrain을 찾지 못했습니다.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;

        int heightResolution = terrainData.heightmapResolution;
        int holeResolution = terrainData.holesResolution;

        float lowerAmountWorld = 15.5f;
        float normalizedRaiseAmount = lowerAmountWorld / terrainData.size.y;

        float[,] heights = terrainData.GetHeights(
            0,
            0,
            heightResolution,
            heightResolution
        );

        for (int y = 0; y < heightResolution; y++)
        {
            for (int x = 0; x < heightResolution; x++)
            {
                int holeX = Mathf.RoundToInt(
                    (float)x / (heightResolution - 1) * (holeResolution - 1)
                );

                int holeY = Mathf.RoundToInt(
                    (float)y / (heightResolution - 1) * (holeResolution - 1)
                );

                bool isHole = terrainData.IsHole(holeX, holeY);

                if (isHole)
                {
                    continue;
                }

                heights[y, x] -= normalizedRaiseAmount;

                if (heights[y, x] > 1f)
                {
                    heights[y, x] = 1f;
                }
            }
        }

        terrainData.SetHeights(0, 0, heights);

        Debug.Log("Paint Holes로 뚫리지 않은 Terrain 부분만 전체적으로 하락시켰습니다.");
    }
}
