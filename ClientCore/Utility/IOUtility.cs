using System.IO;
using System;

namespace ClientCore
{
    public class IOUtility
    {
        public static void CreateDirectory(string directory, bool clearPrevious = false)
        {
            if (clearPrevious && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        
        public static bool CombineSubFileToLargeFile(string[] allSubFile, string filePath, bool deleteSubFile = true)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            
            var buffer = new byte[1024 * 1024];
            
            var tempFilePath = filePath + ".temp";

            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
            
            var fileStream = new FileStream(tempFilePath, FileMode.CreateNew);
            
            foreach (var subFile in allSubFile)
            {
                var subFileStream = new FileStream(subFile, FileMode.Open);
                
                int readCount = 0;
                while ((readCount = subFileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fileStream.Write(buffer, 0, readCount);
                }
                
                subFileStream.Close();

                if (deleteSubFile)
                {
                    File.Delete(subFile);
                }
            }
            
            fileStream.Close();
            
            File.Move(tempFilePath, filePath);

            return true;
        }
        
        public static bool SplitLargeFileToSubFile(string filePath, string subFileDirectory, int subFileCount)
        {
            var fileInfo = new FileInfo(filePath);

            var subFileLength = fileInfo.Length / subFileCount;
            
            CreateDirectory(subFileDirectory, true);
            
            var fileStream = new FileStream(filePath, FileMode.Open);

            var buffer = new byte[1024 * 1024];
            
            for (int i = 0; i < subFileCount; i++)
            {
                int leftReadLength = (int)(i < subFileCount - 1 ? subFileLength : fileInfo.Length - subFileLength * (subFileCount - 1));
                
                var subFileStream = new FileStream(Path.Combine(subFileDirectory, string.Format("{0}.part_{1}", fileInfo.Name, i)), FileMode.CreateNew);
                while (leftReadLength > 0)
                {
                    var readCount = fileStream.Read(buffer, 0, Math.Min(leftReadLength, buffer.Length));
            
                    subFileStream.Write(buffer, 0, readCount);
                    
                    leftReadLength -= readCount;
                }
                
                subFileStream.Close();
            }

            if (fileStream.Position != fileStream.Length)
            {
                throw new SystemException();
            }
            
            fileStream.Close();
            
            return true;
        }
    }
}