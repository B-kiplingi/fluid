using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Math;

public class Util
{
    public static Vector2Int IdToCell(int id, Vector2Int gridDimensions){
        return new Vector2Int(id % gridDimensions.x, (int)Floor((float)id/gridDimensions.x));
    }

    public static int CellToId(Vector2Int cell, Vector2Int gridDimensions){
        return cell.x + cell.y * gridDimensions.x;
    }

        Vector2Int IdToCellInf(int id){
        int root = (int)Floor(Sqrt(id));
        root -= 1 - (root % 2);
        int edgeProgress = id - (int)Pow(root, 2);
        int sideLength = root + 1;
        int side = (int)Floor((float)edgeProgress / sideLength);
        int sideProgress = edgeProgress - (side * sideLength);
        int distance = (root + 1) / 2;

        if(side == 0)
            return new Vector2Int(distance - sideProgress, distance);
        else if(side == 1)
            return new Vector2Int(-distance, distance - sideProgress);
        else if(side == 2)
            return new Vector2Int(-distance + sideProgress, -distance);
        else if(side == 3)
            return new Vector2Int(distance, -distance + sideProgress);
        else return Vector2Int.zero;
    }

    int CellToIdInf(Vector2Int cell){
        if(cell == Vector2.zero) return 0;
        int distance = Max(Abs(cell.x), Abs(cell.y));
        int side = 2 * distance + 1;
        if(cell.y == distance && cell.x != -distance)
            return (int)Pow(side - 2, 2) - cell.x + distance;
        else if(cell.x == -distance && cell.y != -distance)
            return (int)Pow(side - 2, 2) - cell.y + side - 1 + distance;
        else if(cell.y == -distance && cell.x != distance)
            return (int)Pow(side - 2, 2) + cell.x + 2 * side - 2 + distance;
        else
            return (int)Pow(side - 2, 2) + cell.y + 3 * side - 3 + distance;
    }

    public static bool CellInBounds(Vector2Int cell, Vector2Int gridSize) {
        return 0 <= cell.x && cell.x < gridSize.x && 0 <= cell.y && cell.y < gridSize.y;
    }
}
