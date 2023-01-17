using UnityEngine;
using System;

public class UICustomContainerHorizontalLayout : UICustomContainerLayout
{
    public override Vector3 CalcPosition(int cellIndex)
    {
        return new Vector3(_cellWidth * cellIndex, 0, 0);
    }

}
