using UnityEngine;
using System.Collections;

public static class FalloffGenerator
{
	public static float[,] Generate(Vector2Int size, float falloffStart, float falloffEnd)
    {
        float[,] heightMap = new float[size.x, size.y];

        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                Vector2 position = new Vector2((float)x / size.x * 2 - 1, (float)y / size.y * 2 - 1);
                float t = Mathf.Max(Mathf.Abs(position.x), Mathf.Abs(position.y));

                if (t < falloffStart)
                {
                    heightMap[x, y] = 1;
                } else if (t > falloffEnd)
                {
                    heightMap[x, y] = 0;
                } else
                {
                    heightMap[x, y] = Mathf.SmoothStep(1, 0, Mathf.InverseLerp(falloffStart, falloffEnd, t));
                }
            }
        }

        return heightMap;
    }
}