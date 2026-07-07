using System.Text.Json;
using System.Xml.Linq;  
using Serilog;
using Microsoft.Data.SqlClient; 
using ProgramToConvertXmlToJson.Services;
using ProgramToConvertXmlToJson.Implementations;
using Microsoft.Extensions.DependencyInjection;
 
using System.Data; 
using System.Xml;
 
using System.Text.Json.Nodes;
using Serilog.Sinks.SystemConsole.Themes;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Configuration;
class Program
{

    static void Main()
    {

        var serviceProvider = new ServiceCollection()
                    .AddSingleton<IConfigurationService, ConfigurationService>()
                    .AddSingleton<IDatabaseService, DatabaseService>()
                    .AddSingleton<IHashingService>(sp => new HashingService(
                        sp.GetRequiredService<IConfigurationService>().LoadConfiguration()["HashSettings:PASalt"],
                        Convert.ToInt32(sp.GetRequiredService<IConfigurationService>().LoadConfiguration()["HashSettings:PAMinHashLength"]),
                        sp.GetRequiredService<IConfigurationService>().LoadConfiguration()["HashSettings:PAAlphabet"]))
                    .AddSingleton<IFileService, FileService>()
                    .AddSingleton<IArrangeFields,ArrangeFields>()
                    .AddSingleton<IS3UploadService, S3UploadService>()
                    .BuildServiceProvider();


        var config = serviceProvider.GetRequiredService<IConfigurationService>().LoadConfiguration();
        string logsLocation = config["logsLocation"];

        var controlNames = new HashSet<string>();


        Log.Logger = new LoggerConfiguration()
           .WriteTo.Console(theme: AnsiConsoleTheme.Code) // Log to the console
           .WriteTo.File(logsLocation, rollingInterval: RollingInterval.Day) // Log to a file
           .CreateLogger();

        string Server = config["Sql:Server"];
        string Database = config["Sql:Database"];
        string UserId = config["Sql:User Id"];
        string Password = config["Sql:Password"];
        string TrustServerCertificate = config["Sql:TrustServerCertificate"];
        string xmlPath = config["Internal_config:xmlPath"];
        string jsonPath = config["Internal_config:jsonPath"];
        string repoPath = config["Internal_config:repoPath"];
        var skipUISections = File.ReadAllLines(config["Internal_config:skipsections"]).ToHashSet();
        //string ApptypeIdforInternal = config["Internal_config:ApptypeIdforInternal"];

        Console.WriteLine("Select conversion option:");
        Console.WriteLine("1. Convert FSM Config to JSON");
        Console.WriteLine("2. Convert Internal Config to JSON");
        Console.Write("Enter your choice 1 or 2: ");

         static string ShowApplicationTypeMenu()
         {
            var applicationTypes = new Dictionary<string, string>
                     {
                        { "1", "BuildingPermit" },
                        { "2", "Structural" },
                        { "3", "Inspection" },
                        { "4", "Planning" },
                        { "5", "NSWBuilding" },
                        { "6", "NSWPlanning" },
                        { "7", "Consultancy" },
                        { "8", "SABuilding" },
                        { "9", "QLDBuildingPermit" },
                        { "10", "TassieBuildingPermit" },
                        { "11", "WABuilding" },
                        { "12", "ACTBuilding" },
                        { "13", "NTBuilding" },
                        { "14", "EGPPermit" },
                        { "15", "SwimmingPool" },
                        { "16", "TemporaryBWP" },
                        { "17", "ContractInspections" },
                        { "18", "ComplaintRegister" },
                        { "19", "Engineering" },
                        { "20", "OccupationCertificate" },
                        { "21", "ConstructionCertificate" },
                        { "22", "PrincipalCertifierAppointment" },
                        { "23", "SubdivisionCertificate" },
                        { "24", "SubdivisionWorksCertificate" },
                        { "25", "ContractApprovals" },
                        { "26", "SBRC" },
                        { "27", "PlanningAdvice" },
                        { "28", "BuilderProject" },
                        { "29", "BillingApplications" },
                        { "30", "GRC" },
                        { "31", "IllegalWorks" },
                        { "32", "Quotation" },
                        { "33", "PerformanceSolution" },
                        { "34", "Drafting" },
                        { "35", "ProtectionWorks" },
                        { "36", "PreApplication" },
                        { "37", "Action" },
                        { "38", "SmallLivableHousing" },
                        { "39", "Architect" },
                        { "40", "ExemptDevelopment" }
                    };
            Console.WriteLine("\nSelect Application Type:");
            Console.WriteLine("=" + new string('=', 40));

            foreach (var kvp in applicationTypes)
            {
                Console.WriteLine($"{kvp.Key,2}. {kvp.Value}");
            }

            Console.WriteLine("=" + new string('=', 40));
            Console.Write($"Enter your choice (1-{applicationTypes.Count}): ");

            string choice = Console.ReadLine();

            if (applicationTypes.TryGetValue(choice, out string selectedType))
            {
                Log.Information($"\nYou selected: {selectedType}");
                return selectedType;
            }
            else
            {
                Log.Information($"Invalid choice. Please enter a number between 1 and {applicationTypes.Count}.");
                return null;
            }
 
         }

        string choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                try
                {
                    //string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "example.txt");
                    string filePath = config["FileSettings:XmlFilePath"];


                    string content = filePath;
                        
                        //File.ReadAllText(filePath);
                    if (File.Exists(filePath))
                    {
                        Log.Information("File content: " + filePath);
                    }
                    else
                    {
                        Log.Information("File not found."); Log.Information("Looking for file at: " + filePath);
                    }


                    string xmlFilePath = content;

                    if (string.IsNullOrEmpty(xmlFilePath))
                    {
                        Log.Error("XML file path is missing or null.");
                        return;
                    }
                    else
                    {
                        Log.Information("XML file path: " + xmlFilePath);
                    }


                    //string targetAppTypeName = config["ApplicationTypeName"];

                    int licenseId = Convert.ToInt32(config["LicenseId"]);
                    List<int> targetAppTypeIds = GetApplicationTypeId(licenseId, Server, Database, UserId, Password, TrustServerCertificate);

                    if (targetAppTypeIds.Count == 0)
                    {
                        Log.Error($"No ApplicationTypeId found for LicenceID: {licenseId}");
                        return;
                    }
                    Log.Information("ApplicationTypeId: " + targetAppTypeIds);



                    foreach (var targetAppTypeId in targetAppTypeIds)
                    {
                        Log.Information("Processing ApplicationTypeId: " + targetAppTypeId);

                        var hashids = serviceProvider.GetRequiredService<IHashingService>();
                        string hashedLicenseFolder = hashids.Encode(licenseId.GetHashCode());
                        string hashedAppTypeFolder = hashids.Encode(targetAppTypeId.GetHashCode());
                        if (licenseId == 0)
                        {
                            Log.Error("License ID is null or invalid.");
                            return;
                        }
                        if (targetAppTypeId == 0)
                        {
                            Log.Error("Application Type ID is null or invalid.");
                            continue;
                        }

                        Log.Information("Hashed License Folder: {0}", hashedLicenseFolder);
                        Log.Information("Hashed Application Type Folder: {0}", hashedAppTypeFolder);

                        if (string.IsNullOrEmpty(hashedLicenseFolder))
                        {
                            Log.Error("Hashed License Folder is empty or invalid.");
                            continue; // Skip this iteration if the hashed folder is invalid
                        }

                        if (string.IsNullOrEmpty(hashedAppTypeFolder))
                        {
                            Log.Error("Hashed Application Type Folder is empty or invalid.");
                            continue; // Skip this iteration if the hashed folder is invalid
                        }

                        string outputBaseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files");
                        Log.Information("Output Base Directory: {OutputBaseDirectory}", outputBaseDirectory);

                        string appDirectory = Path.Combine(outputBaseDirectory, hashedLicenseFolder, hashedAppTypeFolder, "WorkflowConfig");
                        Log.Information("Application Directory: {0}", appDirectory);

                        Directory.CreateDirectory(appDirectory);

                        XDocument xmlDoc = XDocument.Load(xmlFilePath);
                        if (xmlDoc.Root == null)
                        {
                            Log.Error("XML document root is null.");
                            return;
                        }


                        string targetAppTypeName = GetFsmTypeName(licenseId, Server, Database, UserId, Password, TrustServerCertificate, targetAppTypeId);

                        //XElement targetAppType = xmlDoc.Root.Elements($"{targetAppTypeName}").FirstOrDefault(app => (string)app.Attribute("name") == targetAppTypeName);

                        XElement targetAppType = xmlDoc.Root.Elements("applicationType").FirstOrDefault(app => (string)app.Attribute("name") == targetAppTypeName);

                        if (targetAppType == null)
                        {
                            Log.Error($"Error: Application type '{targetAppTypeName}' not found in XML.");
                            continue; // Skip this iteration if application type is not found
                        }

                        if (string.IsNullOrEmpty(targetAppTypeName))
                        {
                            Log.Error($"FSMTypeName is null or empty for ApplicationTypeId: {targetAppTypeId}");
                            return;
                        }

                        var jsonObject = ConvertXmlElementToDictionary(targetAppType);
                        var jsonText = System.Text.Json.JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });

                        string jsonFilePath = Path.Combine(appDirectory, "WorkflowConfig.json");

                        serviceProvider.GetRequiredService<IFileService>().SaveJsonToFile(jsonText, jsonFilePath);
                        //serviceProvider.GetRequiredService<IS3UploadService>().UploadFileToS3(jsonFilePath, config["AWS:BucketName"], config["AWS:Region"], hashedLicenseFolder, hashedAppTypeFolder);

                        //string s3BucketName = config["AWS:BucketName"];
                        //if (string.IsNullOrEmpty(s3BucketName))
                        //{
                        //    Log.Error("S3 Bucket Name is null or empty.");
                        //    return;
                        //}



                        //Log.Information("---------------------------------$\"File uploaded to S3 bucket '{bucketName}' under the path '{s3Key}' successfully!\"--------------------------------");
                    }


                    static List<int> GetApplicationTypeId(int licenceId, string Server, string Database, string UserId, string Password, string TrustServerCertificate)
                    {
                        // Set your connection string here (make sure to replace with actual values)



                        string connectionString = $"Server={Server};Database={Database};User Id={UserId};Password={Password};TrustServerCertificate={TrustServerCertificate};";
                        var applicationTypeIds = new List<int>();
                        try
                        {
                            using (var connection = new SqlConnection(connectionString))
                            {
                                connection.Open();
                                string query = "SELECT ApplicationTypeId FROM ApplicationTypeSetting WHERE LicenceID = @LicenceID";

                                using (var command = new SqlCommand(query, connection))
                                {
                                    // Add the parameter to the command
                                    command.Parameters.Add(new SqlParameter("@LicenceID", SqlDbType.Int) { Value = licenceId });

                                    // Execute the query and read the result
                                    using (var reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            applicationTypeIds.Add(reader.GetInt32(0));
                                        }
                                    }

                                    if (applicationTypeIds.Count == 0)
                                    {
                                        Log.Error("No ApplicationTypeId found for LicenceID: " + licenceId);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Error fetching ApplicationTypeIds: " + ex.Message);
                        }

                        return applicationTypeIds;

                    }


                    static string GetFsmTypeName(int licenceId, string Server, string Database, string UserId, string Password, string TrustServerCertificate, int targetAppTypeId)
                    {
                        string connectionString = $"Server={Server};Database={Database};User Id={UserId};Password={Password};TrustServerCertificate={TrustServerCertificate};";
                        string fsmTypeName = string.Empty;

                        try
                        {
                            using (var connection = new SqlConnection(connectionString))
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
                            Log.Error("Error fetching FSMTypeName: " + ex.Message);
                        }
                        return fsmTypeName;
                    }
                    Console.WriteLine("Press Enter to exit...");
                    Console.ReadLine();


                }
                catch (Exception ex)
                {
                    Console.WriteLine("Press Enter to exit..." + ex);
                    Console.ReadLine();

                    Log.Error(ex, "An error occurred.");
                }

                break;
            case "2":

                var selectedApplicationType = ShowApplicationTypeMenu();
                if (!string.IsNullOrEmpty(selectedApplicationType))
                {
                    FilterUISectionListByXml(controlNames, selectedApplicationType);
                    //SortApplication(xmlPath,jsonPath,repoPath);
                    

                    serviceProvider.GetRequiredService<IArrangeFields>().SortApplicationEdit();
                    serviceProvider.GetRequiredService<IArrangeFields>().SortApplicationView();
                    serviceProvider.GetRequiredService<IArrangeFields>().SortPropertyEdit();
                    serviceProvider.GetRequiredService<IArrangeFields>().AdjoiningPropertyEdit();
                    serviceProvider.GetRequiredService<IArrangeFields>().AdjoiningPropertyNew();
                    serviceProvider.GetRequiredService<IArrangeFields>().ApplicationContactNew("New");
                    serviceProvider.GetRequiredService<IArrangeFields>().ForEdit("Edit", "edit","ApplicationContact");
                    serviceProvider.GetRequiredService<IArrangeFields>().ForEditTabs("Edit", "edit", "Inspection");





                    Console.WriteLine("Do you want to upload it to S3 Bucket? ");
                    Console.WriteLine("1. Yes");
                    Console.WriteLine("2. No");
                    Console.Write("Enter your choice 1 or 2: ");

                    string exitChoice = Console.ReadLine()?.Trim();
                    if (exitChoice == "1") { UploadInternalS3(); }
                    if(exitChoice == "2") { 
                        Log.Information("Skipping S3 upload."); 
                        Console.WriteLine("Press any key to EXIT: ");
                        Console.ReadKey(); // Wait for user input before exiting
                        Environment.Exit(0); // Terminates the program
                        
                    }


                }


                break;
            default:
                Log.Information("Invalid choice. Please enter 1 or 2.");
                break;
        }
         

             void FilterUISectionListByXml(HashSet<string> controlNames, string applicationType)
            {


                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlPath);

                //getting the list of controls from the XMl
                XmlNodeList applicationNodes = xmlDoc.GetElementsByTagName("Application");
                bool applicationTypeFound = false;
                foreach (XmlNode application in applicationNodes)
                    {
                        if (application.Attributes["type"]?.Value == applicationType)
                        {
                            applicationTypeFound = true;

                            XmlNodeList controlNodes = application.SelectNodes(".//control");
                            foreach (XmlNode control in controlNodes)
                            {
                                if (control.Attributes?["name"] != null)
                                {
                                    controlNames.Add(control.Attributes["name"].Value);
                                }
                            }
                            break; // Exit the loop once found
                        }
                    }

                    // If not found, log and exit
                    if (!applicationTypeFound)
                    {
                        Log.Information($"Application type '{applicationType}' not found in XML.");
                        Environment.Exit(1); // Exits the program with an error code
                    }



            //loading and iterating through each of the fields

            string json = File.ReadAllText(jsonPath);
                JsonNode rootNode = JsonNode.Parse(json);

                //To remove the item from UISectionList
                if (rootNode is JsonObject rootObj && rootObj["UISectionList"] is JsonArray sectionList)
                {
                    for (int i = sectionList.Count - 1; i >= 0; i--)
                    {
                        var item = sectionList[i];
                        var sectionName = item?.ToString();
                        if (checkUiSectionExist(item?.ToString(), controlNames) || skipUISections.Contains(sectionName))
                        {
                            Log.Information($"Key: {item?.ToString()}");
                        }
                        else
                        {
                            sectionList.RemoveAt(i);

                            Log.Information($"Removed: {item?.ToString()}");
                        }
                    }
                }

                File.WriteAllText(jsonPath, rootNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                Log.Information("Filtered UISectionList saved.");


                //To Remove the item from UISection

                if (rootNode is JsonObject rootObj1 && rootObj1["UISection"] is JsonArray sectionArray)
                {
                    for (int i = sectionArray.Count - 1; i >= 0; i--)
                    {
                        var item = sectionArray[i];

                        if (item is JsonObject section && section["UISectionName"] is JsonNode nameNode)
                        {
                            string sectionName = nameNode.ToString();

                            if (checkUiSectionExist(sectionName, controlNames) || skipUISections.Contains(sectionName))
                            {
                                Log.Information($"Keeping: {sectionName}");
                            }
                            else
                            {
                                sectionArray.RemoveAt(i);
                                Log.Information($"Removed: {sectionName}");
                            }
                        }
                    }
                }


                File.WriteAllText(jsonPath, rootNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                Log.Information("Filtered UISection saved.");


                //To remove fields that does not exits in the xml 
                HashSet<string> attributes = new HashSet<string>();
                HashSet<string> Uisecname = new HashSet<string> { "New", "Edit" };

                HashSet<string> Json_value = new HashSet<string>();


                XDocument doc = XDocument.Load(xmlPath);
                var query = from uiSection in doc.Descendants("UISection")
                            let uiSectionName = uiSection.Attribute("name")?.Value
                            from control in uiSection.Elements("control")
                            let controlName = control.Attribute("name")?.Value
                            from field in control.Elements("field")
                            let attributeName = field.Attribute("attribute")?.Value
                            where attributeName != null
                            select new
                            {
                                UISection = uiSectionName,
                                Control = controlName,
                                Attribute = attributeName
                            };


             foreach (var item in query)
            {
                string temp = checkFieldsExist(item.UISection, item.Attribute, item.Control, repoPath);                     /// this function is working for sure because it was returnign the value of PropertyTypeId = PropertyTypeID
                 
                if (temp != null)
                    {
                        Json_value.Add(temp);       //These are all the fields that exist in the JSON
                    }
                }


                //now  my goal is to get all the fieldis ifrom the json then remove the Id's that we got from the function and then just remove remaining ones
                string json1 = File.ReadAllText(jsonPath);
                JsonNode? root = JsonNode.Parse(json1);
                var allFieldIds = ExtractAllFieldIds(root);

                static HashSet<string> ExtractAllFieldIds(JsonNode? node)
                {
                    var result = new HashSet<string>();

                    void Traverse(JsonNode? current)
                    {
                        if (current is JsonObject obj)
                        {
                            foreach (var kvp in obj)
                            {
                                if (kvp.Key == "FieldId" && kvp.Value != null)
                                {
                                    result.Add(kvp.Value.ToString());
                                }
                                Traverse(kvp.Value);
                            }
                        }
                        else if (current is JsonArray array)
                        {
                            foreach (var item in array)
                            {
                                Traverse(item);
                            }
                        }
                    }

                    Traverse(node);
                    return result;
                }

                //Now we have all the fieldIds in the Json and also the ones that we got from the XML
                allFieldIds.ExceptWith(Json_value);              //Here we just minus the values from all the fields - the fields that are present in the XML
                var skipFieldIds = File.ReadAllLines(config["Internal_config:skipfields"]).ToList();


                foreach (var item in allFieldIds)
                {
                if (item.Contains("UrgentNote")) {
                    CleanJsonByFieldId(item,config); 
                }
               
                Log.Information($"Processing FieldId: {item}");

                    if (!skipFieldIds.Contains(item))
                    {
                        CleanJsonByFieldId(item, config);  
                    }
                    else
                    {
                        Log.Information($"Skipping FieldId: {item}");
                    }
                }


                static bool CleanJsonByFieldId(string targetFieldId, IConfiguration config)  // This is where we actually clean the json by removing the fieldId;
                {
                    string jsonPath = config["Internal_config:jsonPath"]; 

                    if (!File.Exists(jsonPath))
                    {
                        Log.Information("Input file not found.");
                        return false;
                    }

                    string json = File.ReadAllText(jsonPath);
                    JsonNode? root = JsonNode.Parse(json);

                    if (root == null)
                    {
                        Log.Information("Failed to parse JSON.");
                        return false;
                    }

                    JsonNode? cleanedJson = RemoveFieldId(root, targetFieldId);

                    File.WriteAllText(jsonPath, cleanedJson!.ToJsonString(new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));

                    return true;
                }

                static JsonNode? RemoveFieldId(JsonNode? node, string fieldId)
                {
                    if (node is JsonArray array)
                    {
                        var newArray = new JsonArray();
                        foreach (var item in array)
                        {
                            if (item is JsonObject obj && obj.TryGetPropertyValue("FieldId", out var val) && val?.ToString() == fieldId)
                                continue;

                            var cleaned = RemoveFieldId(item, fieldId);
                            if (cleaned != null)
                                newArray.Add(JsonNode.Parse(cleaned.ToJsonString())!);
                        }
                        return newArray;
                    }

                    if (node is JsonObject jsonObject)
                    {
                        var newObject = new JsonObject();
                        foreach (var kvp in jsonObject)
                        {
                            var cleaned = RemoveFieldId(kvp.Value, fieldId);
                            if (cleaned != null)
                                newObject[kvp.Key] = JsonNode.Parse(cleaned.ToJsonString())!;
                        }
                        return newObject;
                    }

                    return node;
                }

                
                //n to check if the passed name from the json exist in the list of controls that we got from xml
                static bool checkUiSectionExist(string name, HashSet<string> controlNames)
                {
                    return controlNames.Contains(name);
                }


                //this functin will check if the section name filed name and the control name that is passed to it does it exist
                //in the UISection-> action-> fieldid


                static string checkFieldsExist(string secname, string fieldname, string controlname, string repoPath )
                {


                   
                    string jsonContent = File.ReadAllText(repoPath);
                    var repository = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent);

                    //----------------------------------------here we have to compare the string found in the repository does it exists in the Json or not-----------------------------------


                    var valueFromJson = LookupJsonValue(secname, controlname, fieldname, repository);

                    if (valueFromJson != null)
                    {
                        Log.Information($"XML → Section: {secname}, Control: {controlname}, Field: {fieldname}");
                        Log.Information($"JSON Match → {valueFromJson}\n");
                        return valueFromJson;
                    }
                    else
                    {
                        Log.Information($"XML → Section: {secname}, Control: {controlname}, Field: {fieldname}");
                        Log.Information($"JSON Match → Not found\n");
                        return null;
                    }

                    string? LookupJsonValue(
                                  string uiSectionName,
                                  string controlName,
                                  string fieldAttribute,
                                  List<Dictionary<string, object>> repository)
                    {
                        var match = repository.FirstOrDefault(obj =>
                            obj.TryGetValue("controlName", out var cNameObj) &&
                            cNameObj?.ToString() == controlName &&
                            obj.TryGetValue(uiSectionName, out var sectionObj) &&
                            sectionObj is JsonElement section &&
                            section.ValueKind == JsonValueKind.Array
                        );

                        if (match != null)
                        {
                            var fieldsObject = ((JsonElement)match[uiSectionName])[0];

                            if (fieldsObject.TryGetProperty(fieldAttribute, out var valueElement))
                            {
                                return valueElement.GetString(); // Return the JSON value
                            }
                        }

                        return null; // Not found
                    }


                }

                void UpdateDisplayNamesFromXml(string xmlPath, string repoPath, string jsonPath)
                {
                    string xmlContent = File.ReadAllText(xmlPath);
                    string repoContent = File.ReadAllText(repoPath);
                    string jsonContent = File.ReadAllText(jsonPath);

                    var repository = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(repoContent);
                    JsonNode? root = JsonNode.Parse(jsonContent);

                    XDocument doc = XDocument.Parse(xmlContent);

                    var fields = from uiSection in doc.Descendants("UISection")
                                 let uiSectionName = uiSection.Attribute("name")?.Value
                                 from control in uiSection.Elements("control")
                                 let controlName = control.Attribute("name")?.Value
                                 from field in control.Elements("field")
                                 let attribute = field.Attribute("attribute")?.Value
                                 let displayName = field.Attribute("displayName")?.Value
                                 where uiSectionName != null && controlName != null && attribute != null && displayName != null
                                 select new
                                 {
                                     UISectionName = uiSectionName,
                                     ControlName = controlName,
                                     Attribute = attribute,
                                     DisplayName = displayName
                                 };

                    foreach (var f in fields)
                    {
                        var fieldId = LookupJsonValue(f.UISectionName, f.ControlName, f.Attribute, repository);

                        if (!string.IsNullOrWhiteSpace(fieldId))
                        {
                        if ((!f.DisplayName.Contains("&lt")) && (!f.DisplayName.Contains("<span")) ) { 
                            Log.Information($"Updating FieldId: {fieldId}  with DisplayName: {f.DisplayName}");
                            UpdateDisplayNameInJson(root, fieldId, f.DisplayName); 
                        }
                            
                        }
                    }

                    File.WriteAllText(jsonPath, root!.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));


                    // Helper to update DisplayName in main JSON
                    void UpdateDisplayNameInJson(JsonNode? node, string fieldId, string newDisplayName)
                    {
                        if (node is JsonObject obj)
                        {
                            if (obj.TryGetPropertyValue("FieldId", out var idNode) && idNode?.ToString() == fieldId)
                            {
                                obj["DisplayName"] = newDisplayName;
                            }

                            foreach (var kvp in obj)
                            {
                                UpdateDisplayNameInJson(kvp.Value, fieldId, newDisplayName);
                            }
                        }
                        else if (node is JsonArray array)
                        {
                            foreach (var item in array)
                            {
                                UpdateDisplayNameInJson(item, fieldId, newDisplayName);
                            }
                        }
                    }
                }

                bool JsonContainsFieldId(JsonNode? node, string fieldId)
                {
                    if (node == null)
                        return false;

                    if (node is JsonObject obj)
                    {
                        foreach (var kvp in obj)
                        {
                            if (kvp.Key == "FieldId" && kvp.Value?.ToString() == fieldId)
                                return true;

                            if (JsonContainsFieldId(kvp.Value, fieldId))
                                return true;
                        }
                    }
                    else if (node is JsonArray array)
                    {
                        foreach (var item in array)
                        {
                            if (JsonContainsFieldId(item, fieldId))
                                return true;
                        }
                    }

                    return false;
                }


                string? LookupJsonValue(
                                   string uiSectionName,
                                   string controlName,
                                   string fieldAttribute,
                                   List<Dictionary<string, object>> repository)
                {
                    var match = repository.FirstOrDefault(obj =>
                        obj.TryGetValue("controlName", out var cNameObj) &&
                        cNameObj?.ToString() == controlName &&
                        obj.TryGetValue(uiSectionName, out var sectionObj) &&
                        sectionObj is JsonElement section &&
                        section.ValueKind == JsonValueKind.Array
                    );

                    if (match != null)
                    {
                        var fieldsObject = ((JsonElement)match[uiSectionName])[0];

                        if (fieldsObject.TryGetProperty(fieldAttribute, out var valueElement))
                        {
                            return valueElement.GetString(); // Return the JSON value
                        }
                    }

                    return null; // Not found
                }
                UpdateDisplayNamesFromXml(xmlPath, repoPath, jsonPath);
            }



        static Dictionary<string, object> ConvertXmlElementToDictionary(XElement element)
        {
            var dict = new Dictionary<string, object>();

            foreach (var attr in element.Attributes())
            {
                dict[$"_{attr.Name.LocalName}"] = attr.Value; // Prefix attributes with "_"
            }

            foreach (var child in element.Elements())
            {
                var childValue = ConvertXmlElementToDictionary(child);

                if (child.Name.LocalName == "actions")
                {
                    foreach (var action in child.Elements("action"))
                    {
                        var actionValue = ConvertXmlElementToDictionary(action);
                        if (!dict.ContainsKey("action")) dict["action"] = new List<object>();
                        ((List<object>)dict["action"]).Add(actionValue);
                    }
                }
                else if (child.Name.LocalName == "reminders")
                {
                    var reminderList = new List<object>();
                    foreach (var reminder in child.Elements("reminder"))
                    {
                        var reminderValue = ConvertXmlElementToDictionary(reminder);
                        reminderList.Add(reminderValue);
                    }
                    dict["reminder"] = reminderList;
                }
                else if (child.Name.LocalName == "letters")
                {
                    var templateList = new List<object>();
                    foreach (var template in child.Elements("template"))
                    {
                        var templateValue = ConvertXmlElementToDictionary(template);
                        templateList.Add(templateValue);
                    }
                    dict["letters"] = new Dictionary<string, object>
                        {
                            { "template", templateList }
                        };
                }
                else if (child.Name.LocalName == "transition")
                {
                    if (!dict.ContainsKey("transition")) dict["transition"] = new List<object>();
                    ((List<object>)dict["transition"]).Add(childValue);
                }
                else
                {
                    if (dict.ContainsKey(child.Name.LocalName))
                    {
                        if (dict[child.Name.LocalName] is List<object> list) list.Add(childValue);
                        else dict[child.Name.LocalName] = new List<object> { dict[child.Name.LocalName], childValue };
                    }
                    else
                    {
                        dict[child.Name.LocalName] = childValue;
                    }
                }
            }

            if (!element.HasElements && !string.IsNullOrWhiteSpace(element.Value))
            {
                dict["#text"] = element.Value;
            }

            return dict;
        }
        Console.WriteLine();

        void UploadInternalS3()
        {
            Log.Information($"Uploading Intenal File to S3");
            var hashids = serviceProvider.GetRequiredService<IHashingService>();
            int licenseId = Convert.ToInt32(config["Internal_config:LicenseId"]); 
            int ApptypeIdforInternal = Convert.ToInt32(config["Internal_config:ApptypeIdforInternal"]);


            string hashedLicenseFolder = hashids.Encode(licenseId.GetHashCode());
            string hashedAppTypeFolder = hashids.Encode(ApptypeIdforInternal.GetHashCode());


            string outputBaseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files");
            Log.Information("Output Base Directory: {OutputBaseDirectory}", outputBaseDirectory);

            string appDirectory = Path.Combine(outputBaseDirectory, hashedLicenseFolder, hashedAppTypeFolder, "InternalApplicationConfig");
            Log.Information("Application Directory: {0}", appDirectory);
            string jsonFilePath = Path.Combine(appDirectory, "Internal.json");

            string jsonText = File.ReadAllText(jsonPath);

            serviceProvider.GetRequiredService<IFileService>().SaveJsonToFile(jsonText, jsonFilePath);

            string s3BucketName = config["AWS:BucketName"];
            if (string.IsNullOrEmpty(s3BucketName))
            {
                Log.Error("S3 Bucket Name is null or empty.");
                return;
            }


            serviceProvider.GetRequiredService<IS3UploadService>().UploadFileToS3Internal(jsonFilePath, config["AWS:BucketName"], config["AWS:Region"], hashedLicenseFolder, hashedAppTypeFolder);


            Log.Information("Done Uploading On S3 bucket");

            Console.WriteLine("Do you want to exit the program? (Y/N):");
            string input = Console.ReadLine();
            if (input?.Trim().ToUpper() == "Y")
            {
                Environment.Exit(0); // Terminates the program
            }
        }

        

    }


    private static void SortApplication(string xmlPath, string jsonPath, string repository){
         
        // 🔑 Replace with your actual XML file path
        XDocument xmlDoc = XDocument.Load(xmlPath);

        List<string> attributeList = xmlDoc
            .Descendants("UISection")
            .Where(section => (string)section.Attribute("name") == "Edit")
            .Descendants("control")
            .Where(control => (string)control.Attribute("name") == "Application")
            .Descendants("field")
            .Select(field => (string)field.Attribute("attribute"))
            .Where(attr => !string.IsNullOrEmpty(attr))
            .ToList();

        // Output the list
        foreach (var attribute in attributeList)
        {
            Console.WriteLine(attribute);
        }




    }





}











































//private static void SortTheFields(string xmlPath, string jsonPath, string repository)
//{
//    XDocument xmlDoc = XDocument.Load(xmlPath);

//    // Create dictionary to store attribute and displayName
//    Dictionary<string, string> fieldMap = new Dictionary<string, string>();

//    // Query all <field> elements
//    foreach (var field in xmlDoc.Descendants("field"))
//    {
//        string attribute = field.Attribute("attribute")?.Value;
//        string displayName = field.Attribute("attribute")?.Value;

//        // Only add if attribute is not null or empty
//        if (!string.IsNullOrEmpty(attribute))
//        {
//            fieldMap[attribute] = displayName ?? "";
//        }
//    }

//    // Example: Print the hashmap
//    foreach (var kvp in fieldMap)
//    {
//        Console.WriteLine($"{kvp.Key} ➔ {kvp.Value}");
//    }

////Traversing the XML 
//XDocument xmlDoc1 = XDocument.Load(xmlPath);
//string jsonText = File.ReadAllText(repository);

//var repository1 = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonText, new JsonSerializerOptions
//{
//    PropertyNameCaseInsensitive = true
//});

//// Traverse UISections and also create the Jsonmap
//foreach (var uiSection in xmlDoc1.Descendants("UISection"))
//{
//    string sectionName = uiSection.Attribute("name")?.Value ?? "UnknownSection";
//    Console.WriteLine($"=== Section: {sectionName} ===");

//    // Traverse Controls
//    foreach (var control in uiSection.Elements("control"))
//    {
//        string controlName = control.Attribute("name")?.Value ?? "UnknownControl";
//        Console.WriteLine($"  ➔ Control: {controlName}");

//        // Traverse Fields
//        foreach (var field in control.Elements("field"))
//        {
//            string attribute = field.Attribute("attribute")?.Value ?? "";
//            string displayName = field.Attribute("displayName")?.Value ?? "";
//            string jsonText1 = File.ReadAllText(jsonPath);
//            var document = JsonDocument.Parse(jsonText1);
//            var root = document.RootElement;
//            var jsonfieldmap = new Dictionary<string, JsonElement>();
//            //go to the jSON where UISECTION name matces the UISECTION"
//            if (root.TryGetProperty("UISection", out JsonElement uiSections))
//            {
//                foreach (var section in uiSections.EnumerateArray())
//                {
//                    if (section.TryGetProperty("Actions", out JsonElement actions))
//                    {
//                        foreach (var action in actions.EnumerateArray())
//                        {
//                            if (action.TryGetProperty("FieldSets", out JsonElement fieldSets))
//                            {
//                                foreach (var fieldSet in fieldSets.EnumerateArray())
//                                {
//                                    if (fieldSet.TryGetProperty("Fields", out JsonElement fields))
//                                    {
//                                        foreach (var field1 in fields.EnumerateArray())
//                                        {
//                                            if (field1.TryGetProperty("FieldId", out JsonElement fieldIdElement))
//                                            {
//                                                string fieldId = fieldIdElement.GetString();

//                                                // Store entire field object against FieldId
//                                                if (!string.IsNullOrEmpty(fieldId) && !jsonfieldmap.ContainsKey(fieldId))
//                                                {
//                                                    jsonfieldmap[fieldId] = field1;
//                                                }
//                                            }
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//            Console.WriteLine($"      Field: {attribute} ➔ {displayName}");
//        }
//    }
//}

//    static string? LookupJsonValue(
//   string uiSectionName,
//   string controlName,
//   string fieldAttribute,
//   List<Dictionary<string, object>> repository)
//    {
//        var match = repository.FirstOrDefault(obj =>
//            obj.TryGetValue("controlName", out var cNameObj) &&
//            cNameObj?.ToString() == controlName &&
//            obj.TryGetValue(uiSectionName, out var sectionObj) &&
//            sectionObj is JsonElement section &&
//            section.ValueKind == JsonValueKind.Array
//        );

//        if (match != null)
//        {
//            var fieldsObject = ((JsonElement)match[uiSectionName])[0];

//            if (fieldsObject.TryGetProperty(fieldAttribute, out var valueElement))
//            {
//                return valueElement.GetString();
//            }
//        }

//        return null;
//    }



//}

