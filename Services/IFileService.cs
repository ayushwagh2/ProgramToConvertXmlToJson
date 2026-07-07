using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgramToConvertXmlToJson.Services
{
    public interface IFileService
    {
        void SaveJsonToFile(string jsonContent, string filePath);


    }

}
