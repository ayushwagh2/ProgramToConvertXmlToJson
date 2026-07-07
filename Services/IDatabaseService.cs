using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgramToConvertXmlToJson.Services
{
    public interface IDatabaseService
    {
        List<int> GetApplicationTypeIds(int licenseId);
        string GetFsmTypeName(int targetAppTypeId);
    }
}
