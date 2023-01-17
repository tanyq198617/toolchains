using UnityEngine;

public class UICustomContainerVerticalLayout : UICustomContainerLayout
{
    public override Vector3 CalcPosition(int cellIndex)
    {
        return new Vector3(0, -_cellHeight * cellIndex, 0);
    }
}
