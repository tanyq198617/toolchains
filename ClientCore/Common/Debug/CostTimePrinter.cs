using System.Diagnostics;
using UnityEngine;

namespace ClientCore
{
    /**
     * 调试计时器：辅助输出局部代码段，或某个阶段耗时, 不建议大面用来测试函数性能开销, 函数性能开销使用其他Profiler工。仅在编辑器下生效, 或宏定义COST_TIME_PRINTER
     * using(new CostTimePrinter("test"))
     * {
     *    any code fragment
     * }
     *
     * var printer = new CostTimePrinter("test")
     * any code fragment
     * printer.Dispose()
     */
    
    public class CostTimePrinter : System.IDisposable
    {
        private string _name = string.Empty;
        private Stopwatch _stopwatch = null;
        private int _assertMaxTime = -1;
        private bool _printLog = false;
        /**
         * @name: 活动标签名称
         * @assertMaxTime: 最大时间，当超出该时间时，error日志输出
         */
        public CostTimePrinter(string name, int assertMaxTime = -1, bool printLog = false)
        {
            _assertMaxTime = assertMaxTime;

            _printLog = printLog;
            
            Start(name);
        }

        public void Dispose()
        {
            End();
        }

        [Conditional("UNITY_EDITOR"), Conditional("COST_TIME_PRINTER")]
        private void Start(string name)
        {
            _name = name;
            
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        [Conditional("UNITY_EDITOR"), Conditional("COST_TIME_PRINTER")]
        private void End()
        {
            _stopwatch.Stop();

            if (_printLog)
            {
                UnityEngine.Debug.Log($"[CostTimePrinter] [{_name}] [{_stopwatch.ElapsedMilliseconds} ms]");
            }

            if (_assertMaxTime > 0)
            {
                AssertCostTimeLessThan(_assertMaxTime);
            }
        }
        
        [Conditional("UNITY_EDITOR"), Conditional("COST_TIME_PRINTER")]
        private void AssertCostTimeLessThan(int time)
        {
            if (_stopwatch.ElapsedMilliseconds > time)
            {
                UnityEngine.Debug.LogError($"[CostTimePrinter] [{_name}] [{_stopwatch.ElapsedMilliseconds} ms]");
            }
        }
    }
}