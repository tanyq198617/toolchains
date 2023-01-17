
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace ClientCore
{
    public static class HttpLogUtil
    {
        
        [Conditional("DEBUG_HTTP_MANAGER")]
        public static void Log(string message)
        {
            Debug.Log($"[HttpLogUtil] {message}");
        }
        
        [Conditional("DEBUG_HTTP_MANAGER")]
        public static void Error(string message)
        {
            Debug.LogError("[HttpLogUtil] " + message);
        }


    }    
}

