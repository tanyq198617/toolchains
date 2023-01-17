#if UNITY_EDITOR && false
using UnityEngine;

public class UICustomContainerTester : MonoBehaviour
{
    [SerializeField]
    private UICustomContainer _customContainer = null;

    public void OnGUI()
    {
        GUILayout.BeginVertical();

        if (GUILayout.Button("Do Test"))
        {
            _customContainer.OnRefreshCell = OnRefreshCell;

            for (int i = 0; i < 10; i++)
            {
                var localPosition = new Vector3(0, 100 * i, 0);
                var templateIndex = i % 2;

                var labelText = "this is label " + i.ToString();
                var userData = labelText;
                
                _customContainer.AddCell(templateIndex, localPosition, userData);
            }
        }
        
        GUILayout.EndVertical();
    }

  

    void OnRefreshCell(int cellIndex, GameObject cell, object userData)
    {
        var labelText = (string)(userData);

        if (labelText != null)
        {
            var label = cell.GetComponent<UILabel>();
            label.text = labelText;
        }
    }
}
#endif