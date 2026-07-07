using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgramToConvertXmlToJson.Services
{
    public interface IS3UploadService
    {
        void UploadFileToS3(string filePath, string bucketName, string region, string hashedLicenseFolder, string hashedAppTypeFolder);
        void UploadFileToS3Internal(string filePath, string bucketName, string region, string hashedLicenseFolder, string hashedAppTypeFolder);
    }
}
