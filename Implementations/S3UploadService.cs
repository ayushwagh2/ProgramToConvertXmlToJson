using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using ProgramToConvertXmlToJson.Services;
using Amazon;
using Serilog;


namespace ProgramToConvertXmlToJson.Implementations
{
    public class S3UploadService : IS3UploadService
    {
        private readonly IConfigurationService _configurationService;

        public S3UploadService(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public void UploadFileToS3(string filePath, string bucketName, string region, string hashedLicenseFolder, string hashedAppTypeFolder)
        {
            try
            {
                var config = _configurationService.LoadConfiguration();
                string awsRegion = config["AWS:Region"];
                var s3Client = new AmazonS3Client(
                    config["AWS:AccessKey"],
                    config["AWS:SecretKey"],
                    RegionEndpoint.GetBySystemName(awsRegion)
                );

                var fileTransferUtility = new TransferUtility(s3Client);
                string s3Key = $"{hashedLicenseFolder}/{hashedAppTypeFolder}/WorkflowConfig/WorkflowConfig.json";

                fileTransferUtility.Upload(filePath, bucketName, s3Key);
               Log.Information($"File uploaded to S3 bucket '{bucketName}' under the path '{s3Key}' successfully!");
            }
            catch (AmazonS3Exception e)
            {
               Log.Error($"Error: {e.Message}");
            }
            catch (Exception e)
            {
               Log.Error($"Error: {e.Message}");
            }
        }

        public void UploadFileToS3Internal(string filePath, string bucketName, string region, string hashedLicenseFolder, string hashedAppTypeFolder)
        {
            try
            {
                var config = _configurationService.LoadConfiguration();
                string awsRegion = config["AWS:Region"];
                var s3Client = new AmazonS3Client(
                    config["AWS:AccessKey"],
                    config["AWS:SecretKey"],
                    RegionEndpoint.GetBySystemName(awsRegion)
                );

                var fileTransferUtility = new TransferUtility(s3Client);
                string s3Key = $"{hashedLicenseFolder}/{hashedAppTypeFolder}/InternalApplicationConfig" +
                    $"/ApplicationDetailsPageConfig.json";

                fileTransferUtility.Upload(filePath, bucketName, s3Key);
                Log.Information($"File uploaded to S3 bucket '{bucketName}' under the path '{s3Key}' successfully!");
            }
            catch (AmazonS3Exception e)
            {
                Log.Error($"Error: {e.Message}");
            }
            catch (Exception e)
            {
                Log.Error($"Error: {e.Message}");
            }
        }




    }
}
