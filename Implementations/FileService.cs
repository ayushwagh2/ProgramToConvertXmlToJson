using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProgramToConvertXmlToJson.Services;
using System.IO;

namespace ProgramToConvertXmlToJson.Implementations
{ 
    public class FileService : IFileService
    {
        public void SaveJsonToFile(string jsonContent, string filePath)
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, jsonContent);
        }
    }
}
