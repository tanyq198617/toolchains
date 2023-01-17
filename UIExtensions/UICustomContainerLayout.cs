using UnityEngine;

[RequireComponent(typeof(UICustomContainer))]
public abstract class UICustomContainerLayout : MonoBehaviour
{
    private const int PREVIEW_MAX_COUNT = 100;
    
    [SerializeField] protected int _cellWidth = 100;
    [SerializeField] protected int _cellHeight = 100;

    [SerializeField] private bool enablePivot = false;
    [SerializeField] protected UIWidget.Pivot widgetPivot = UIWidget.Pivot.TopLeft;

    public int CellWidth
    {
        get { return _cellWidth; }
        //set { _cellWidth = value; }
    }

    public int CellHeight
    {
        get { return _cellHeight; }
        //set { _cellHeight = value; }
    }
    public abstract Vector3 CalcPosition(int cellIndex);

    public virtual void CalcWidgetOffset(int cellData, out float offsetX, out float offsetY)
    {
        CalcWidgetOffset(cellData, out offsetX, out offsetY);
    }
    
    public UIWidget.Pivot WidgetPivot
    {
        get { return widgetPivot; }
    }
    public bool EnablePivot => enablePivot;

    public virtual Vector3 CalcCellPosition(int cellIndex, float offsetX, float offsetY)
    {
        return CalcPosition(cellIndex);
    }

    public void OnDrawGizmosSelected()
    {
        var previousColor = Gizmos.color;
        
        var localToWorldMatrix = transform.localToWorldMatrix;

        var localPosition = transform.localPosition;
        
        var worldCellSize = localToWorldMatrix.MultiplyVector(new Vector3(_cellWidth, _cellHeight, 0));
        
        Gizmos.color = Color.yellow;

        float offsetX = 0;
        float offsetY = 0;
        if (enablePivot)
        {
            CalcWidgetOffset(PREVIEW_MAX_COUNT,out offsetX, out offsetY);
        }
        
        for (int i = 0; i < PREVIEW_MAX_COUNT; i++)
        {
            if (enablePivot)
            {
                Gizmos.DrawWireCube(localToWorldMatrix.MultiplyPoint(CalcCellPosition(i, offsetX, offsetY)), worldCellSize);
            }
            else
            {
                Gizmos.DrawWireCube(localToWorldMatrix.MultiplyPoint(CalcPosition(i)), worldCellSize);
            }
        }
        
        Gizmos.color = previousColor;
    }
}
