using UnityEngine;
using System.Collections.Generic;

public class FlowField
{
    public Vector2[,] flowField; // 存储每个网格单元的流动方向向量
    public float[,] distanceField; // 存储每个网格单元到目标点的距离
    public int width; // 地图的宽度
    public int height; // 地图的高度
    public Vector2 target; // 目标点的位置
    public List<Vector2> obstacles; // 障碍物列表

    // 构造函数，初始化FlowField对象
    public FlowField(int width, int height, Vector2 target, List<Vector2> obstacles)
    {
        this.width = width;
        this.height = height;
        this.target = target;
        this.obstacles = obstacles;
        flowField = new Vector2[width, height];
        distanceField = new float[width, height];
        GenerateFlowField(); // 生成Flow Field
    }

    // 生成Flow Field的主函数
    void GenerateFlowField()
    {
        InitializeDistanceField(); // 初始化距离场
        FastMarchingMethod(); // 使用快速行进法计算距离场
        CalculateFlowField(); // 计算流动方向场
    }

    // 初始化距离场，将所有单元的距离设置为最大值，并将目标点和障碍物的距离设置为0
    void InitializeDistanceField()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                distanceField[x, y] = float.MaxValue;
            }
        }

        foreach (var obstacle in obstacles)
        {
            int x = Mathf.Clamp((int)obstacle.x, 0, width - 1);
            int y = Mathf.Clamp((int)obstacle.y, 0, height - 1);
            distanceField[x, y] = 0;
        }

        int targetX = Mathf.Clamp((int)target.x, 0, width - 1);
        int targetY = Mathf.Clamp((int)target.y, 0, height - 1);
        distanceField[targetX, targetY] = 0;
    }

    // 使用快速行进法（Fast Marching Method）计算距离场
    void FastMarchingMethod()
    {
        SortedList<float, Vector2> openList = new SortedList<float, Vector2>();
        int targetX = Mathf.Clamp((int)target.x, 0, width - 1);
        int targetY = Mathf.Clamp((int)target.y, 0, height - 1);
        openList.Add(0, new Vector2(targetX, targetY));

        while (openList.Count > 0)
        {
            var current = openList.Values[0];
            openList.RemoveAt(0);

            int x = (int)current.x;
            int y = (int)current.y;

            UpdateNeighbor(x + 1, y, distanceField[x, y], openList);
            UpdateNeighbor(x - 1, y, distanceField[x, y], openList);
            UpdateNeighbor(x, y + 1, distanceField[x, y], openList);
            UpdateNeighbor(x, y - 1, distanceField[x, y], openList);
        }
    }

    // 更新邻居单元的距离值，并将其添加到开放列表中
    void UpdateNeighbor(int x, int y, float currentDistance, SortedList<float, Vector2> openList)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        float newDistance = currentDistance + 1; // 假设每个单元的移动成本是均匀的
        if (newDistance < distanceField[x, y])
        {
            distanceField[x, y] = newDistance;
            openList.Add(newDistance, new Vector2(x, y));
        }
    }

    // 计算流动方向场
    void CalculateFlowField()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 gradient = CalculateGradient(x, y);
                flowField[x, y] = gradient.normalized;
            }
        }
    }

    // 计算某个单元的梯度（即流动方向）
    Vector2 CalculateGradient(int x, int y)
    {
        float left = (x > 0) ? distanceField[x - 1, y] : distanceField[x, y];
        float right = (x < width - 1) ? distanceField[x + 1, y] : distanceField[x, y];
        float down = (y > 0) ? distanceField[x, y - 1] : distanceField[x, y];
        float up = (y < height - 1) ? distanceField[x, y + 1] : distanceField[x, y];

        return new Vector2(right - left, up - down);
    }

    // 获取某个位置的流动方向
    public Vector2 GetFlowDirection(Vector2 position)
    {
        int x = Mathf.Clamp((int)position.x, 0, width - 1);
        int y = Mathf.Clamp((int)position.y, 0, height - 1);
        return flowField[x, y];
    }
}