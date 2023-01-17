using UnityEngine;

public class UICustomContainerGridLayout : UICustomContainerLayout
{
    [Range(1, 100)][SerializeField] private int _maxPerline = 5;
    public int MaxPerline { get { return _maxPerline; } } 

    public override Vector3 CalcPosition(int cellIndex)
    {
        // 0 1 
        // 2 3
        
        var rowNumber = cellIndex / _maxPerline;
        var columnNumber = cellIndex - rowNumber * _maxPerline;
        
        return new Vector3(columnNumber * _cellWidth, -rowNumber * _cellHeight);
    }
    
    public override Vector3 CalcCellPosition(int cellIndex, float offsetX, float offsetY)
    {
        Vector3 position = Vector3.zero;
        var rowNumber = cellIndex / _maxPerline;
        var columnNumber = cellIndex - rowNumber * _maxPerline;
        
        position.x = columnNumber * _cellWidth + offsetX;
        position.y = -rowNumber * _cellHeight + offsetY;
        
        return position;
    }
    
    public override void CalcWidgetOffset(int cellCount, out float offsetX, out float offsetY)
    {
        offsetX = 0;
        offsetY = 0;
        float widgetSize = CellWidth;
        float heightSize = CellHeight;
        
        long tmpCount = cellCount;
        if (cellCount > 0)
        {
            tmpCount = cellCount;
        }

        if (tmpCount <= 0)
            return;
        
        long col = 1;
        long row = 1;
        
        if (MaxPerline < 1)
        {
            _maxPerline = 1;
        }

        if (tmpCount >= MaxPerline)
        {
            widgetSize = (MaxPerline - 1) * widgetSize;
            
            row = tmpCount /MaxPerline;
            long tmpValue = tmpCount % MaxPerline;
            if (tmpValue > 0)
            {
                row++;
            }

            heightSize = (row - 1) * heightSize;

        }
        else
        {
            if (tmpCount > 1)
            {
                widgetSize = (tmpCount - 1) * widgetSize;
            }
            else
            {
                widgetSize = 0;
            }

            heightSize = 0;
        }
        
        Vector2 po = NGUIMath.GetPivotOffset(widgetPivot);
        offsetX = Mathf.Lerp( 0, widgetSize, po.x);
        offsetY = Mathf.Lerp(heightSize,0, po.y);
        offsetX = -offsetX;
    }

}