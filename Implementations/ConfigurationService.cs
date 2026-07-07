using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ProgramToConvertXmlToJson.Services;

namespace ProgramToConvertXmlToJson.Implementations
{
    public class ConfigurationService : IConfigurationService
    {
        public IConfigurationRoot LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json");
            return builder.Build();
        }
    }
}
