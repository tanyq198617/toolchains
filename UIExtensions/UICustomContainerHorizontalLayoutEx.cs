using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICustomContainerHorizontalLayoutEx : UICustomContainerLayout
{
    [Range(1, 100)] [SerializeField] private int _maxPerColumn = 1;

    public int MaxPerline
    {
        get => _maxPerColumn;
        set => _maxPerColumn = value;
    }

    public override Vector3 CalcPosition(int cellIndex)
    {
        // 0 2
        // 1 3
        
        
        var columnNumber = cellIndex / MaxPerline;
        var rowNumber = cellIndex - columnNumber * MaxPerline;
        
        return new Vector3(columnNumber * _cellWidth, -rowNumber * _cellHeight);
    }
}
