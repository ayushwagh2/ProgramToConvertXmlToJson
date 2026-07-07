using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Serilog;
using Microsoft.Extensions.Configuration;
using ProgramToConvertXmlToJson.Services;

namespace ProgramToConvertXmlToJson.Implementations
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IConfigurationService _configService;
        private readonly string _connectionString;

        public DatabaseService(IConfigurationService configService)
        {
            _configService = configService;
            var config = _configService.LoadConfiguration();

            _connectionString = $"Server={config["Sql:Server"]};" +
                                $"Database={config["Sql:Database"]};" +
                                $"User Id={config["Sql:User Id"]};" +
                                $"Password={config["Sql:Password"]};" +
                                $"TrustServerCertificate={config["Sql:TrustServerCertificate"]};";
        }

        public List<int> GetApplicationTypeIds(int licenseId)
        {
            var applicationTypeIds = new List<int>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT ApplicationTypeId FROM ApplicationTypeSetting WHERE LicenceID = @LicenceID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(new SqlParameter("@LicenceID", SqlDbType.Int) { Value = licenseId });

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                applicationTypeIds.Add(reader.GetInt32(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error fetching ApplicationTypeIds for LicenceID {licenseId}: {ex.Message}");
            }

            return applicationTypeIds;
        }

        public string GetFsmTypeName(int targetAppTypeId)
        {
            string fsmTypeName = string.Empty;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT FSMTypeName FROM ApplicationType WHERE ApplicationTypeID = @ApplicationTypeID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(new SqlParameter("@ApplicationTypeID", SqlDbType.Int) { Value = targetAppTypeId });

                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            fsmTypeName = result.ToString();
                        }
                        else
                        {
                            Log.Error($"FSMTypeName not found for ApplicationTypeID: {targetAppTypeId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error fetching FSMTypeName for ApplicationTypeID {targetAppTypeId}: {ex.Message}");
            }

            return fsmTypeName;
        }
    }
}