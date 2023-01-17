using UnityEngine;
using System;
using System.Collections.Generic;
using ClientCore.Performance;
using Sirenix.OdinInspector;


[ExecuteInEditMode]
public class UICustomContainer : UIWidgetContainer
{
    public class CellData
    {
        public CellData(int index, GameObject template, Vector3 localPositin, Vector2 size, object userData)
        {
            _index = index;
            _template = template;
            _localPosition = localPositin;
            _size = size;
            _userData = userData;
        }

        private int _index = 0;

        public int Index
        {
            get { return _index; }
        }

        private GameObject _template;

        public GameObject Template
        {
            get { return _template; }
            set { _template = value; }
        }

        private Vector3 _localPosition;

        public Vector3 LocalPosition
        {
            get { return _localPosition; }
            set { _localPosition = value; }
        }

        private Vector2 _size;

        public Vector2 Size
        {
            get { return _size; }
        }

        private object _userData;

        public object UserData
        {
            get { return _userData; }
        }

        public bool ForceVisible;

        public GameObject CellInstance;
    }

    [SerializeField] private List<GameObject> _allTemplate = new List<GameObject>();
    public List<GameObject> AllTemplate => _allTemplate;

    public bool IgnoreCellVisible = false;
    public void SetTemplateByIndex(int index, GameObject obj, int maxLen)
    {
        if (_allTemplate.Capacity != maxLen)
        {
            _allTemplate = new List<GameObject>(maxLen);
            
            for (int i=0;i<maxLen;i++)
                _allTemplate.Add(null);
        }
        
        if (_allTemplate == null || index < 0 || index >= _allTemplate.Capacity) 
            return ;

        _allTemplate[index] = obj;
    }
    
    public GameObject GetTemplateByIndex(int index)
    {
        if (_allTemplate == null || index < 0 || index >= _allTemplate.Count) return null;

        return _allTemplate[index];
    }

    private Dictionary<GameObject, Stack<GameObject>> _allUnusedInstance =
        new Dictionary<GameObject, Stack<GameObject>>();

    private Dictionary<int, Bounds> _allTemplateBounds = new Dictionary<int, Bounds>();


    /// <summary>
    /// 代码动态设置模板，调用其它api之前，一定要先设置模板对象
    /// </summary>
    /// <param name="go"></param>
    /// <exception cref="SystemException"></exception>
    public void SetTemplate(GameObject go)
    {
        if (!go)
        {
            throw new SystemException(
                "template object CAN NOT be empty!!");
        }

        _allTemplate.Add(go);
    }

    public int TemplateCount
    {
        get { return _allTemplate.Count; }
    }

    public Bounds GetTemplateBounds(int templateIndex, bool considerInactive = true)
    {
        if (!_allTemplateBounds.ContainsKey(templateIndex))
        {
            var template = _allTemplate[templateIndex];

            Bounds bounds = NGUIMath.CalculateAbsoluteWidgetBounds(template.transform);

            //if (template.transform.parent != this.transform)
            {
                var min = bounds.min;
                var max = bounds.max;

                if (template.transform.parent != null)
                {
                    min = template.transform.InverseTransformPoint(min);
                    max = template.transform.InverseTransformPoint(max);
                }

                bounds.SetMinMax(min, max);
            }

            _allTemplateBounds.Add(templateIndex, bounds);
        }

        return _allTemplateBounds[templateIndex];
    }

    public void SetTemplateBounds(int templateIndex, Bounds bounds)
    {
        if (_allTemplateBounds.ContainsKey(templateIndex))
        {
            _allTemplateBounds.Remove(templateIndex);
        }

        _allTemplateBounds.Add(templateIndex, bounds);
    }

    private Bounds _containerBounds = new Bounds();
    private readonly List<CellData> _allCellDatas = new List<CellData>();

    public Bounds ContainerBounds
    {
        get { return _containerBounds; }
    }

    public delegate void RefreshCellFunc(int cellIndex, GameObject cell, object userData);

    public RefreshCellFunc OnRefreshCell;
    public Action OnAllLoadCallBack;

    private UIPanel FindParentPanel(Transform trans)
    {
        var parent = trans.parent;
        while (parent != null)
        {
            var panel = parent.GetComponent<UIPanel>();
            if (panel)
            {
                return panel;
            }

            parent = parent.parent;
        }

        return null;
    }

    private UIPanel _cachedPanel;

    private UIPanel CachedPanel
    {
        get
        {
            if (_cachedPanel == null)
            {
                _cachedPanel = FindParentPanel(transform);
                if (!IgnoreCellVisible) 
                    CachedPanel.GetComponent<UIPanel>().onClipMove += delegate(UIPanel panel) { UpdateCells(); };
            }

            return _cachedPanel;
        }
    }


    private UIWidget _containerWidget;

    public UIWidget ContainerWidget
    {
        get
        {
            if (!_containerWidget)
            {
                _containerWidget = NGUITools.AddChild<UIWidget>(this.gameObject);
            }

            return _containerWidget;
        }
    }

    private UICustomContainerLayout _layout = null;

    UICustomContainerLayout CachedLayout
    {
        get
        {
            if (_layout == null)
            {
                _layout = GetComponent<UICustomContainerLayout>();
            }

            return _layout;
        }
    }

    //是否使用分帧显示对象
    public bool EnableFraming = false;

    private void UpdateContainerWidget()
    {
        ContainerWidget.SetRect(_containerBounds.min.x, _containerBounds.min.y, _containerBounds.size.x,
            _containerBounds.size.y);
    }

    public CellData GetCellDataByUserData(object userData)
    {
        return _allCellDatas.Find(p => p.UserData == userData);
    }

    public CellData GetCellDataByUserData(Predicate<object> match)
    {
        foreach (var cellData in _allCellDatas)
        {
            if (match(cellData.UserData))
            {
                return cellData;
            }
        }

        return null;
    }

    public CellData GetCellDataByIndex(int index)
    {
        if (_allCellDatas == null || _allCellDatas.Count <= 0 || index > _allCellDatas.Count - 1) 
            return null;
        return _allCellDatas[index];
    }

    public void ClearAll()
    {
        foreach (var cellData in _allCellDatas)
        {
            if (cellData.CellInstance)
            {
                DestroyCellInstance(cellData);
            }
        }

        _allCellDatas.Clear();
        _containerBounds.SetMinMax(Vector3.zero, Vector3.zero);
        UpdateContainerWidget();

        _layoutDirty = true;
    }

    /// <summary>
    /// 新增添加列表数据CellList, 避免在每个子业务中for loop
    /// templateIndex 默认用模块列表中的第一个
    /// </summary>
    /// <param name="userDatas"></param>
    /// <param name="templateIndex"></param>
    public void AddCellList<T>(List<T> userDatas, int templateIndex = 0)
    {
        if(userDatas == null) return;
        ClearAll();
        for (int i = 0; i < userDatas.Count; ++i)
        {
            AddCell(templateIndex, userDatas[i]);
        }
    }

    /// <summary>
    /// 直接传列表数据大小, 避免在每个子业务中for loop
    /// templateIndex 默认用模块列表中的第一个
    /// </summary>
    /// <param name="userDataList"></param>
    /// <param name="count"></param>
    /// <param name="templateIndex"></param>
    public void AddCellList(long count, int templateIndex = 0)
    {
        ClearAll();
        for (int i = 0; i < count; ++i)
        {
            AddCell(templateIndex);
        }
    }

    public CellData AddCell(int templateIndex, object userData = null)
    {
        if (!CachedLayout)
        {
            throw new SystemException(
                "Can not add cell without layout. add cell with position or add layout component");
        }
        Vector3 localPosition =Vector3.zero;
        if (IsEnablePivot)
        {
            localPosition = CachedLayout.CalcCellPosition(_allCellDatas.Count, _perLineMaxWidget, _perLineMaxHeight);
        }
        else
        {
            localPosition = CachedLayout.CalcPosition(_allCellDatas.Count);
        }
        
        return AddCell(templateIndex, localPosition, userData);
        //return AddCell(templateIndex, CachedLayout.CalcPosition(_allCellDatas.Count), userData);
    }

    public CellData AddCell(int templateIndex, Vector3 localPosition, object userData = null)
    {
        using (PerfMan.Perf("UICustomContainer.AddCell"))
        {
            localPosition.z = 0.0f;

            var bounds = GetTemplateBounds(templateIndex);

            var cellData = new CellData(
                _allCellDatas.Count,
                _allTemplate[templateIndex],
                localPosition,
                new Vector2(bounds.size.x, bounds.size.y),
                userData);

            _allCellDatas.Add(cellData);

            _containerBounds.Encapsulate(localPosition + bounds.min);
            _containerBounds.Encapsulate(localPosition + bounds.max);

            UpdateContainerWidget();

            _layoutDirty = true;

            return cellData;
        }
    }

    private long _cellCount = 0;

    public long CellCount
    {
        get { return _allCellDatas.Count; }
    }

    private bool _layoutDirty = false;

    private void Start()
    {
        foreach (var template in _allTemplate)
        {
            if( template )
                template.SetActive(false);
        }
    }

    public void UpdateImmediately()
    {
        using (PerfMan.Perf("UICustomContainer.UpdateImmediately"))
        {
            _layoutDirty = true;
            Update();
        }
    }

    private void Update()
    {
        using (PerfMan.Perf("UICustomContainer.Update"))
        if (_layoutDirty)
        {
            _layoutDirty = !UpdateCells();
            
            // after all visible cells created,do the callback then
            if (OnAllLoadCallBack != null && !_layoutDirty)
            {
                OnAllLoadCallBack();
            }
        }
    }
    
    private bool CalcCellVisible(CellData cell, Vector3 bottomLeftCorner, Vector3 topRightCorner)
    {
        if (cell.ForceVisible) return true;

        // rotation panel not supported currently.
        bool unvisible =
            cell.LocalPosition.x - cell.Size.x > topRightCorner.x ||
            cell.LocalPosition.x + cell.Size.x < bottomLeftCorner.x ||
            cell.LocalPosition.y + cell.Size.y < bottomLeftCorner.y ||
            cell.LocalPosition.y - cell.Size.y > topRightCorner.y;

        return !unvisible;
    }


    //是否初始化完成
    private bool UpdateCells()
    {
        using (PerfMan.Perf("UpdateCells"))
        {
            if (!CachedPanel)
            {
                return true;
            }

            if (IgnoreCellVisible)
            {
                foreach (var cellData in _allCellDatas)
                {
                    using (PerfMan.Perf("cellIter"))
                    {
                        CreateCellInstance(cellData);
                    }
                }

                return true;
            }

            // World-space corners of the panel's clipping rectangle. The order is bottom-left, top-left, top-right, bottom-right.
            Vector3[] allWorldCorner = CachedPanel.worldCorners;
            var bottomLeftCorner = transform.InverseTransformPoint(allWorldCorner[0]);
            var topRightCorner = transform.InverseTransformPoint(allWorldCorner[2]);

            // var cellIter = _allCellData.GetEnumerator();
            // while (!_layoutDirty && cellIter.MoveNext())
            // {
            //     using (PerfMan.Perf("cellIter"))
            //     {
            //         var cellData = cellIter.Current;
            //         bool cellVisible = CalcCellVisible(cellData, bottomLeftCorner, topRightCorner);
            //         if (cellVisible && !cellData.CellInstance)
            //         {
            //             CreateCellInstance(cellData);
            //         }
            //
            //         if (!cellVisible && cellData.CellInstance)
            //         { 
            //             DestroyCellInstance(cellData);
            //         }                   
            //     }
            //     
            // }
            // cellIter.Dispose();

            FrameTimer.Reset();

            foreach (var cellData in _allCellDatas)
            {
                using (PerfMan.Perf("cellIter"))
                {
                    bool created = false;
                    bool cellVisible = CalcCellVisible(cellData, bottomLeftCorner, topRightCorner);
                    if (cellVisible && !cellData.CellInstance)
                    {
                        CreateCellInstance(cellData);
                        created = true;
                    }

                    if (!cellVisible && cellData.CellInstance)
                    {
                        DestroyCellInstance(cellData);
                    }

                    //如果运算超时，直接return false
                    if (EnableFraming && created && FrameTimer.IsExceedMs(10))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    private Stack<GameObject> GetAllUnusedCellInstance(GameObject template)
    {
        Stack<GameObject> allCellInstance = null;
        if (!_allUnusedInstance.ContainsKey(template))
        {
            allCellInstance = new Stack<GameObject>();
            _allUnusedInstance.Add(template, allCellInstance);
        }
        else
        {
            allCellInstance = _allUnusedInstance[template];
        }

        return allCellInstance;
    }

    private void CreateCellInstance(CellData cellData)
    {
        using (PerfMan.Perf("CreateCellInstance"))
        {
            var allUnusedInstance = GetAllUnusedCellInstance(cellData.Template);

            GameObject cellInstance = null;
            if (allUnusedInstance.Count > 0)
            {
                cellInstance = allUnusedInstance.Pop();
            }

            if (cellInstance == null)
            {
#if UI_PERF
                using (PerfMan.Perf("CreateCellInstance.Instantiate", cellData.Template.name))
#endif
                cellInstance = Instantiate(cellData.Template);
            }

            cellInstance.name = string.Format("{0}_{1}", cellData.Index, cellData.Template.name);
            cellInstance.transform.SetParent(transform);
            cellInstance.transform.localPosition = cellData.LocalPosition;
            cellInstance.transform.localScale = Vector3.one;
            cellInstance.SetActive(true);

            //分帧做渐现
            if (EnableFraming)
            {
                var tweenAlpha = cellInstance.GetComponent<TweenAlpha>();
                var widget = cellInstance.GetComponent<UIWidget>();

                if (tweenAlpha && widget)
                {
                    widget.alpha = 0;
                    tweenAlpha.PlayForward();
                }
            }

            cellData.CellInstance = cellInstance;

            if (null != OnRefreshCell)
                OnRefreshCell(cellData.Index, cellData.CellInstance, cellData.UserData);
        }
    }

    private void DestroyCellInstance(CellData cellData)
    {
        var cellInstance = cellData.CellInstance;
        if (cellInstance)
        {
            cellInstance.SetActive(false);
            GetAllUnusedCellInstance(cellData.Template).Push(cellInstance);
            cellInstance.name = string.Format("{0}_{1}", "unused", cellData.Template.name);
            cellData.CellInstance = null;
        }
    }

    public void ForeachInstance(Action<GameObject, object> func)
    {
        if (func == null) return;

        // var cellIter = _allCellData.GetEnumerator();
        // while (cellIter.MoveNext())
        // {
        //     var cellData = cellIter.Current;
        //     if (cellData.CellInstance != null)
        //     {
        //         func(cellData.CellInstance, cellData.UserData);
        //     }
        // }

        foreach (var cellData in _allCellDatas)
        {
            if (cellData.CellInstance != null)
            {
                func(cellData.CellInstance, cellData.UserData);
            }
        }
    }

    public void RefreshAllCell(Action<int, GameObject, object> func)
    {
        if (func == null) return;
        
        foreach (var cellData in _allCellDatas)
        {
            if (cellData.CellInstance != null)
            {
                func(cellData.Index, cellData.CellInstance, cellData.UserData);
            }
        }
    }
    
    public void RefreshAllCell()
    {
        if (_allCellDatas != null && OnRefreshCell != null)
        {
            foreach (var cellData in _allCellDatas)
            {
                if (cellData.CellInstance != null)
                {
                    OnRefreshCell(cellData.Index, cellData.CellInstance, cellData.UserData);
                }
            }
        }
    }
    
    public Vector3 FocusOnChild(UIScrollView sv, int cellIndex, int tmpIndex,
        float offset = 0f, float strength = 10f, bool instant = false, SpringPanel.OnFinished onFinished = null)
    {
        Vector3 targetPos = Vector3.zero;
        if (CalcPositionOnChild(out targetPos, sv, cellIndex, tmpIndex, offset))
        {
            SpringPanel.Begin(sv.gameObject, targetPos, strength).onFinished = () =>
            {
				if(null != onFinished)
				{
					onFinished();
				}
                if (!instant) return;
                sv.RestrictWithinBounds(true, sv.canMoveHorizontally, sv.canMoveVertically);
            };
        }

        return targetPos;
    }

    public bool CalcPositionOnChild(out Vector3 targetPos,
        UIScrollView sv, int cellIndex, int tmpIndex, float offset = 0)
    {
        if (CachedLayout == null || sv == null)
        {
            targetPos = Vector3.zero;
            return false;
        }

        GameObject cellObj = GetTemplateByIndex(tmpIndex);
        if (cellObj == null)
        {
            targetPos = Vector3.zero;
            return false;
        }

        Transform child0 = cellObj.transform.parent.GetChild(0);
        if (child0 != null)
        {
            var localPosition = sv.transform.localPosition;
            float deltax = localPosition.x;
            float deltay = localPosition.y;
            //UICustomContainerGridLayout
            UICustomContainerGridLayout gridLayout = CachedLayout as UICustomContainerGridLayout;
            if (gridLayout != null)
            {
                if (sv.canMoveHorizontally)
                {
                    int row = cellIndex / gridLayout.MaxPerline;
                    var col = cellIndex - row * gridLayout.MaxPerline;
                    deltax = -col * gridLayout.CellWidth + offset;
                }
                else
                {
                    int row = cellIndex / gridLayout.MaxPerline;
                    deltay = row * gridLayout.CellHeight + offset;
                }
            }
            else
            {
                if (sv.canMoveHorizontally)
                {
                    //UICustomContainerHorizontalLayout
                    deltax = -cellIndex * CachedLayout.CellWidth + offset;
                }
                else
                {
                    //UICustomContainerVerticalLayout
                    deltay = cellIndex * CachedLayout.CellHeight + offset;
                }
            }

            targetPos = new Vector3(deltax, deltay);
            return true;
        }

        targetPos = Vector3.zero;
        return false;
    }
    
        
    #region CustomContainer Pivot
    
    private float _perLineMaxWidget = 0;
    private float _perLineMaxHeight = 0;
    
    private bool IsEnablePivot
    {
        get { return CachedLayout != null && CachedLayout.EnablePivot; }
    }

    public void UpdateCellDatasCount(long count)
    {
        if (!IsEnablePivot)
            return;
        
        CalcPerLineWidgetOffset(count);
        
    }
    
    public void CalcPerLineWidgetOffset(long cellCount)
    {
        UICustomContainerGridLayout gridLayout = CachedLayout as UICustomContainerGridLayout;
        if (gridLayout == null)
            return ;

        float widgetSize = gridLayout.CellWidth;
        float heightSize = gridLayout.CellHeight;
        
        long tmpCount = cellCount;
        if (cellCount > 0)
        {
            tmpCount = cellCount;
        }

        if (tmpCount <= 0)
            return;
        
        long col = 1;
        long row = 1;

        if (tmpCount >= gridLayout.MaxPerline)
        {
            widgetSize = (gridLayout.MaxPerline - 1) * widgetSize;
            
            row = tmpCount / gridLayout.MaxPerline;
            long tmpValue = tmpCount % gridLayout.MaxPerline;
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
        
        Vector2 po = NGUIMath.GetPivotOffset(gridLayout.WidgetPivot);
        _perLineMaxWidget = Mathf.Lerp( 0, widgetSize, po.x);
        _perLineMaxHeight = Mathf.Lerp(heightSize,0, po.y);

        // if (Math.Abs(po.x) < 0.01f)
        // {
        //     _perLineMaxWidget -= gridLayout.CellWidth / 2;
        // }
        // else if (Math.Abs(po.x - 1) < 0.01f)
        // {
        //     _perLineMaxWidget += gridLayout.CellWidth / 2;
        // }
        _perLineMaxWidget = -_perLineMaxWidget;
        
        // if (Math.Abs(po.y) < 0.01f)
        // {
        //     _perLineMaxHeight += gridLayout.CellHeight / 2;
        // } else if (Math.Abs(po.y - 1) < 0.01f)
        // {
        //     _perLineMaxHeight -= gridLayout.CellHeight / 2;
        // }
        
    }
    #endregion

    #region CustomContainer测试
    public int CustomContainerCellCount = 3;
    
    [ContextMenu("ExcuteCustomContainer")] 
    public void ReAdjustCellPosition()
    {
        if (0 == _allTemplate.Count)
            return;
        
        if (!IsEnablePivot)
            return;
        
        int childCount = this.transform.childCount;
        List<GameObject> tmpList = new List<GameObject>();
        if (childCount > 0)
        {
            for (int i = 0; i < childCount; i++)
            {
                var trans = transform.GetChild(i);
                if (trans)
                {
                    if (trans.gameObject != _allTemplate[0])
                    {
                        tmpList.Add(trans.gameObject);
                        //Destroy(trans.gameObject);
                    }
                }
            }
            
            for (int i = 0, count =tmpList.Count; i < count; i++)
            {
                if (tmpList[i])
                { 
                    DestroyImmediate(tmpList[i]);
                }
            }
        }
        ClearAll();
        UpdateCellDatasCount(CustomContainerCellCount);
        
        for (int i = 0; i < CustomContainerCellCount; ++i)
        {
            AddCell(0);
        }
        UpdateImmediately();
    }
    
    #endregion

}
