using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgramToConvertXmlToJson.Services
{
    public interface IConfigurationService
    {
        IConfigurationRoot LoadConfiguration();
    }
}
