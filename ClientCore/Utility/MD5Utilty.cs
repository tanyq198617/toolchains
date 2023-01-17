using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Debug = UnityEngine.Debug;

namespace ClientCore
{
    public class MD5Utlity
    {
        public static bool CheckFileMd5(string filePath, string md5)
        {
            return ComputeFileMd5(filePath) == md5;
        }
        
        public static string ComputeFileMd5(string filePath)
        {
            if (!File.Exists(filePath))
            {
                D.Error($"MD5Utlity.ComputeFileMd5: can not find file: {filePath}");
                return string.Empty;
            }
            
            using (var md5Hash = MD5.Create())
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] data = md5Hash.ComputeHash(fileStream);
                
                    var sBuilder = new StringBuilder();
                    foreach (var b in data)
                    {
                        sBuilder.Append(b.ToString("x2"));
                    }
                    
                    return sBuilder.ToString();
                }
            }
        }
    }
}