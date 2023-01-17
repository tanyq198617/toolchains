using System;
using System.Diagnostics;
using UnityEngine;

namespace ClientCore
{
    public class CostGcPrinter : System.IDisposable
    {
        private string _name = string.Empty;

        private long _startMemory;
        private long _endMemory;
        private long _endMemoryAfterGc;
        
        public CostGcPrinter(string name)
        {
            Start(name);
        }

        public void Dispose()
        {
            End();
        }

        [Conditional("UNITY_EDITOR"), Conditional("COST_GC_PRINTER")]
        private void Start(string name)
        {
            _name = name;
            
            GC.Collect();
            
            _startMemory = GC.GetTotalMemory(false);
        }

        [Conditional("UNITY_EDITOR"), Conditional("COST_GC_PRINTER")]
        private void End()
        {
            _endMemory = GC.GetTotalMemory(false);
            
            GC.Collect();
            _endMemoryAfterGc = GC.GetTotalMemory(false);

            var peakCostMemory = FormatMemory(_endMemory - _startMemory);
            var costMemory = FormatMemory(_endMemoryAfterGc - _startMemory);
            
            UnityEngine.Debug.Log($"[CostGcPrinter] [{_name}] 常驻消耗[{costMemory}]\t 峰值消耗[{peakCostMemory}]\t 当前总消耗[{FormatMemory(_endMemory)}]");
        }

        private string FormatMemory(long memory)
        {
            var kbValue = memory / 1024.0f;
            if (kbValue <= 1024)
            {
                return $"{kbValue:F2}k";
            }

            var mbValue = kbValue / 1024.0f;
            return $"{mbValue:F2}M";
        }
    }
}