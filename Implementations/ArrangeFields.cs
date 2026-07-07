using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ProgramToConvertXmlToJson.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProgramToConvertXmlToJson.Implementations
{
 
    public class ArrangeFields: IArrangeFields
    {
        private readonly IConfiguration _config;
        private readonly string xmlPath;
        private readonly string jsonFilePath;
        private readonly string repoPath;
        private readonly string outputFilePath;

        public ArrangeFields()
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IConfigurationService, ConfigurationService>()
                .BuildServiceProvider();

            _config = serviceProvider
           .GetRequiredService<IConfigurationService>()
           .LoadConfiguration();   // <-- your call

            // Ideally load from config:
            //_xmlPath = "D:\\SomeFinalFiles\\InternalUI.xml";
            //_jsonFilePath = "D:\\SomeFinalFiles\\ApplicationDetailsPageConfig.json";
            //_repoPath = "D:\\SomeFinalFiles\\Repository.json";
            //_outputFilePath = "D:\\SomeFinalFiles\\ApplicationDetailsPageConfig.json";

            xmlPath = _config["Internal_config:xmlPath"];
            outputFilePath = _config["Internal_config:jsonPath"];
            repoPath = _config["Internal_config:repoPath"];
            jsonFilePath = _config["Internal_config:jsonPath"];

        }

        public void SortApplicationEdit()
        {
            Console.WriteLine("Starting Field Reordering...");

            try
            {
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
                  // 🔑 Set your correct path here
                string jsonContent = File.ReadAllText(repoPath);
                var repository = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent);
                 // 🔑 Set your correct path here

                XDocument xmlDoc = XDocument.Load(xmlPath);

                List<string> desiredOrder = new List<string>();

                // populating the desiredOrder list based on the XML structure and repository 
                foreach (var uiSection in xmlDoc.Descendants("UISection"))
                {
                    var uiSectionName = uiSection.Attribute("name")?.Value;

                    if (uiSectionName != null && uiSectionName.Equals("Edit", StringComparison.OrdinalIgnoreCase))
                    {
                        // 2. Find all control nodes inside this UISection
                        foreach (var control in uiSection.Descendants("control"))
                        {
                            var controlName = control.Attribute("name")?.Value;

                            if (controlName != null && controlName.Equals("Application", StringComparison.OrdinalIgnoreCase))
                            {
                                // 3. Now find each field inside this control
                                foreach (var field in control.Descendants("field"))
                                {
                                    var attributeValue = field.Attribute("attribute")?.Value;

                                    if (!string.IsNullOrEmpty(attributeValue))
                                    {
                                        var atty = LookupJsonValue(
                                            uiSectionName,
                                            controlName,
                                            attributeValue,
                                            repository
                                        );
                                        desiredOrder.Add(atty); // Add the value or the attribute itself if not found
                                    }
                                }
                            }
                        }
                    }
                }

                //foreach (var attr in desiredOrder)
                //{
                //    Console.WriteLine(attr);
                //}

                // Step 1: Load the JSON
                 // 🔑 Set your correct path here
                var json = File.ReadAllText(jsonFilePath);

                // Step 2: Parse into JObject
                var jObject = JObject.Parse(json);

                // Step 3: Define your desired field order

                // Step 4: Traverse ➔ UISection ➔ Actions ➔ Tabs ➔ Actions ➔ FieldSets ➔ Fields
                var uiSections = jObject["UISection"] as JArray;
                if (uiSections != null)
                {
                    foreach (var uiSection in uiSections)
                    {
                        var uiSectionName = (string)uiSection["UISectionName"];
                        if (!string.Equals(uiSectionName, "Application", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var actions = uiSection["Actions"] as JArray;
                        if (actions == null) continue;

                        foreach (var action in actions)
                        {
                            var actionName = (string)action["ActionName"];
                            if (!string.Equals(actionName, "edit", StringComparison.OrdinalIgnoreCase))
                                continue;

                            var tabs = action["Tabs"] as JArray;
                            if (tabs == null) continue;

                            foreach (var tab in tabs)
                            {
                                var tabId = (int?)tab["TabId"];
                                if (tabId != 1)  // 🔑 Match TabId = 1
                                    continue;

                                var tabActions = tab["Actions"] as JArray;
                                if (tabActions == null) continue;

                                foreach (var tabAction in tabActions)
                                {
                                    var fieldSets = tabAction["FieldSets"] as JArray;
                                    if (fieldSets == null) continue;

                                    foreach (var fieldSet in fieldSets)
                                    {
                                        var fields = fieldSet["Fields"] as JArray;
                                        if (fields == null) continue;

                                        // Step 5: Sort fields based on desiredOrder
                                        var sortedFields = fields
                                            .OrderBy(f =>
                                            {
                                                var fieldId = ((string)f["FieldId"]) ?? "";
                                                var index = desiredOrder.FindIndex(x => x == fieldId);
                                                return index == -1 ? int.MaxValue : index;
                                            })
                                            .ToList();

                                        fields.Clear();
                                        foreach (var field in sortedFields)
                                        {
                                            fields.Add(field);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Step 6: Write updated JSON
                 
                File.WriteAllText(outputFilePath, jObject.ToString());

                Console.WriteLine($"✅ Field reordering completed.\nOutput saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }


        }


        public void SortApplicationView()
        {
            Console.WriteLine("Starting Field Reordering...");

            try
            {
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
                // 🔑 Set your correct path here
                string jsonContent = File.ReadAllText(repoPath);
                var repository = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent);
                // 🔑 Set your correct path here

                XDocument xmlDoc = XDocument.Load(xmlPath);

                List<string> desiredOrder = new List<string>();

                // populating the desiredOrder list based on the XML structure and repository 
                foreach (var uiSection in xmlDoc.Descendants("UISection"))
                {
                    var uiSectionName = uiSection.Attribute("name")?.Value;

                    if (uiSectionName != null && uiSectionName.Equals("View", StringComparison.OrdinalIgnoreCase))
                    {
                        // 2. Find all control nodes inside this UISection
                        foreach (var control in uiSection.Descendants("control"))
                        {
                            var controlName = control.Attribute("name")?.Value;

                            if (controlName != null && controlName.Equals("Application", StringComparison.OrdinalIgnoreCase))
                            {
                                // 3. Now find each field inside this control
                                foreach (var field in control.Descendants("field"))
                                {
                                    var attributeValue = field.Attribute("attribute")?.Value;

                                    if (!string.IsNullOrEmpty(attributeValue))
                                    {
                                        var atty = LookupJsonValue(
                                            uiSectionName,
                                            controlName,
                                            attributeValue,
                                            repository
                                        );
                                        desiredOrder.Add(atty); // Add the value or the attribute itself if not found
                                    }
                                }
                            }
                        }
                    }
                }

                //foreach (var attr in desiredOrder)
                //{
                //    Console.WriteLine(attr);
                //}

                // Step 1: Load the JSON
                // 🔑 Set your correct path here
                var json = File.ReadAllText(jsonFilePath);

                // Step 2: Parse into JObject
                var jObject = JObject.Parse(json);

                // Step 3: Define your desired field order

                // Step 4: Traverse ➔ UISection ➔ Actions ➔ Tabs ➔ Actions ➔ FieldSets ➔ Fields
                var uiSections = jObject["UISection"] as JArray;
                if (uiSections != null)
                {
                    foreach (var uiSection in uiSections)
                    {
                        var uiSectionName = (string)uiSection["UISectionName"];
                        if (!string.Equals(uiSectionName, "Application", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var actions = uiSection["Actions"] as JArray;
                        if (actions == null) continue;

                        foreach (var action in actions)
                        {
                            var actionName = (string)action["ActionName"];
                            if (!string.Equals(actionName, "View", StringComparison.OrdinalIgnoreCase))
                                continue;

                            var fieldSets = action["FieldSets"] as JArray;
                            if (fieldSets == null) continue;

                            foreach (var fieldSet in fieldSets)
                            {
                                var fields = fieldSet["Fields"] as JArray;
                                if (fields == null) continue;

                              

                                        // Step 5: Sort fields based on desiredOrder
                                        var sortedFields = fields
                                            .OrderBy(f =>
                                            {
                                                var fieldId = ((string)f["FieldId"]) ?? "";
                                                var index = desiredOrder.FindIndex(x => x == fieldId);
                                                return index == -1 ? int.MaxValue : index;
                                            })
                                            .ToList();

                                        fields.Clear();
                                        foreach (var field in sortedFields)
                                        {
                                            fields.Add(field);
                                        }
                                 
                                }
                            }
                        }
                    }
                

                // Step 6: Write updated JSON
                
                File.WriteAllText(outputFilePath, jObject.ToString());

                Console.WriteLine($"✅ Field reordering completed.\nOutput saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }


        }

        public void SortPropertyEdit()
        {
            Console.WriteLine("Starting Field Reordering...");

            try
            {
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
                // 🔑 Set your correct path here
                string jsonContent = File.ReadAllText(repoPath);
                var repository = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent);
                // 🔑 Set your correct path here

                XDocument xmlDoc = XDocument.Load(xmlPath);

                List<string> desiredOrder = new List<string>();

                // populating the desiredOrder list based on the XML structure and repository 
                foreach (var uiSection in xmlDoc.Descendants("UISection"))
                {
                    var uiSectionName = uiSection.Attribute("name")?.Value;

                    if (uiSectionName != null && uiSectionName.Equals("Edit", StringComparison.OrdinalIgnoreCase))
                    {
                        // 2. Find all control nodes inside this UISection
                        foreach (var control in uiSection.Descendants("control"))
                        {
                            var controlName = control.Attribute("name")?.Value;

                            if (controlName != null && controlName.Equals("Property", StringComparison.OrdinalIgnoreCase))
                            {
                                // 3. Now find each field inside this control
                                foreach (var field in control.Descendants("field"))
                                {
                                    var attributeValue = field.Attribute("attribute")?.Value;

                                    if (!string.IsNullOrEmpty(attributeValue))
                                    {
                                        var atty = LookupJsonValue(
                                            uiSectionName,
                                            controlName,
                                            attributeValue,
                                            repository
                                        );
                                        desiredOrder.Add(atty); // Add the value or the attribute itself if not found
                                    }
                                }
                            }
                        }
                    }
                }

                //foreach (var attr in desiredOrder)
                //{
                //    Console.WriteLine(attr);
                //}

                // Step 1: Load the JSON
                // 🔑 Set your correct path here
                var json = File.ReadAllText(jsonFilePath);

                // Step 2: Parse into JObject
                var jObject = JObject.Parse(json);

                // Step 3: Define your desired field order

                // Step 4: Traverse ➔ UISection ➔ Actions ➔ Tabs ➔ Actions ➔ FieldSets ➔ Fields
                var uiSections = jObject["UISection"] as JArray;
                if (uiSections != null)
                {
                    foreach (var uiSection in uiSections)
                    {
                        var uiSectionName = (string)uiSection["UISectionName"];
                        if (!string.Equals(uiSectionName, "Property", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var actions = uiSection["Actions"] as JArray;
                        if (actions == null) continue;

                        foreach (var action in actions)
                        {
                            var actionName = (string)action["ActionName"];
                            if (!string.Equals(actionName, "edit", StringComparison.OrdinalIgnoreCase))
                                continue;

                            var fieldSets = action["FieldSets"] as JArray;
                            if (fieldSets == null) continue;

                            foreach (var fieldSet in fieldSets)
                            {
                                var fields = fieldSet["Fields"] as JArray;
                                if (fields == null) continue;



                                // Step 5: Sort fields based on desiredOrder
                                var sortedFields = fields
                                    .OrderBy(f =>
                                    {
                                        var fieldId = ((string)f["FieldId"]) ?? "";
                                        var index = desiredOrder.FindIndex(x => x == fieldId);
                                        return index == -1 ? int.MaxValue : index;
                                    })
                                    .ToList();

                                fields.Clear();
                                foreach (var field in sortedFields)
                                {
                                    fields.Add(field);
                                }

                            }
                        }
                    }
                }


                // Step 6: Write updated JSON
                 
                File.WriteAllText(outputFilePath, jObject.ToString());

                Console.WriteLine($"✅ Field reordering completed.\nOutput saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }


        }


        public void AdjoiningPropertyEdit()
        {
            Console.WriteLine("Starting Field Reordering...");

            try
            {
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
                // 🔑 Set your correct path here
                string jsonContent = File.ReadAllText(repoPath);
                var repository = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent);
                // 🔑 Set your correct path here

                XDocument xmlDoc = XDocument.Load(xmlPath);

                List<string> desiredOrder = new List<string>();

                // populating the desiredOrder list based on the XML structure and repository 
                foreach (var uiSection in xmlDoc.Descendants("UISection"))
                {
                    var uiSectionName = uiSection.Attribute("name")?.Value;

                    if (uiSectionName != null && uiSectionName.Equals("Edit", StringComparison.OrdinalIgnoreCase))
                    {
                        // 2. Find all control nodes inside this UISection
                        foreach (var control in uiSection.Descendants("control"))
                        {
                            var controlName = control.Attribute("name")?.Value;

                            if (controlName != null && controlName.Equals("AdjoiningProperty", StringComparison.OrdinalIgnoreCase))
                            {
                                // 3. Now find each field inside this control
                                foreach (var field in control.Descendants("field"))
                                {
                                    var attributeValue = field.Attribute("attribute")?.Value;

                                    if (!string.IsNullOrEmpty(attributeValue))
                                    {
                                        var atty = LookupJsonValue(
                                            uiSectionName,
                                            controlName,
                                            attributeValue,
                                            repository
                                        );
                                        desiredOrder.Add(atty); // Add the value or the attribute itself if not found
                                    }
                                }
                            }
                        }
                    }
                }

                //foreach (var attr in desiredOrder)
                //{
                //    Console.WriteLine(attr);
                //}

                // Step 1: Load the JSON
                // 🔑 Set your correct path here
                var json = File.ReadAllText(jsonFilePath);

                // Step 2: Parse into JObject
                var jObject = JObject.Parse(json);

                // Step 3: Define your desired field order

                // Step 4: Traverse ➔ UISection ➔ Actions ➔ Tabs ➔ Actions ➔ FieldSets ➔ Fields
                var uiSections = jObject["UISection"] as JArray;
                if (uiSections != null)
                {
                    foreach (var uiSection in uiSections)
                    {
                        var uiSectionName = (string)uiSection["UISectionName"];
                        if (!string.Equals(uiSectionName, "AdjoiningProperty", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var actions = uiSection["Actions"] as JArray;
                        if (actions == null) continue;

                        foreach (var action in actions)
                        {
                            var actionName = (string)action["ActionName"];
                            if (!string.Equals(actionName, "edit", StringComparison.OrdinalIgnoreCase))
                                continue;

                            var fieldSets = action["FieldSets"] as JArray;
                            if (fieldSets == null) continue;

                            foreach (var fieldSet in fieldSets)
                            {
                                var fields = fieldSet["Fields"] as JArray;
                                if (fields == null) continue;



                                // Step 5: Sort fields based on desiredOrder
                                var sortedFields = fields
                                    .OrderBy(f =>
                                    {
                                        var fieldId = ((string)f["FieldId"]) ?? "";
                                        var index = desiredOrder.FindIndex(x => x == fieldId);
                                        return index == -1 ? int.MaxValue : index;
                                    })
                                    .ToList();

                                fields.Clear();
                                foreach (var field in sortedFields)
                                {
                                    fields.Add(field);
                                }

                            }
                        }
                    }
                }


                // Step 6: Write updated JSON
                 
                File.WriteAllText(outputFilePath, jObject.ToString());

                Console.WriteLine($"✅ Field reordering completed.\nOutput saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }


        }

        public void AdjoiningPropertyNew()
        {
            Console.WriteLine("Starting Field Reordering...");

            try
            {
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
                // 🔑 Set your correct path here
                string jsonContent = File.ReadAllText(repoPath);
                var repository = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent);
                // 🔑 Set your correct path here

                XDocument xmlDoc = XDocument.Load(xmlPath);

                List<string> desiredOrder = new List<string>();

                // populating the desiredOrder list based on the XML structure and repository 
                foreach (var uiSection in xmlDoc.Descendants("UISection"))
                {
                    var uiSectionName = uiSection.Attribute("name")?.Value;

                    if (uiSectionName != null && uiSectionName.Equals("New", StringComparison.OrdinalIgnoreCase))
                    {
                        // 2. Find all control nodes inside this UISection
                        foreach (var control in uiSection.Descendants("control"))
                        {
                            var controlName = control.Attribute("name")?.Value;

                            if (controlName != null && controlName.Equals("AdjoiningProperty", StringComparison.OrdinalIgnoreCase))
                            {
                                // 3. Now find each field inside this control
                                foreach (var field in control.Descendants("field"))
                                {
                                    var attributeValue = field.Attribute("attribute")?.Value;

                                    if (!string.IsNullOrEmpty(attributeValue))
                                    {
                                        var atty = LookupJsonValue(
                                            uiSectionName,
                                            controlName,
                                            attributeValue,
                                            repository
                                        );
                                        desiredOrder.Add(atty); // Add the value or the attribute itself if not found
                                    }
                                }
                            }
                        }
                    }
                }

                //foreach (var attr in desiredOrder)
                //{
                //    Console.WriteLine(attr);
                //}

                // Step 1: Load the JSON
                // 🔑 Set your correct path here
                var json = File.ReadAllText(jsonFilePath);

                // Step 2: Parse into JObject
                var jObject = JObject.Parse(json);

                // Step 3: Define your desired field order

                // Step 4: Traverse ➔ UISection ➔ Actions ➔ Tabs ➔ Actions ➔ FieldSets ➔ Fields
                var uiSections = jObject["UISection"] as JArray;
                if (uiSections != null)
                {
                    foreach (var uiSection in uiSections)
                    {
                        var uiSectionName = (string)uiSection["UISectionName"];
                        if (!string.Equals(uiSectionName, "AdjoiningProperty", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var actions = uiSection["Actions"] as JArray;
                        if (actions == null) continue;

                        foreach (var action in actions)
                        {
                            var actionName = (string)action["ActionName"];
                            if (!string.Equals(actionName, "View", StringComparison.OrdinalIgnoreCase))
                                continue;

                            var fieldSets = action["FieldSets"] as JArray;
                            if (fieldSets == null) continue;

                            foreach (var fieldSet in fieldSets)
                            {
                                var fields = fieldSet["Fields"] as JArray;
                                if (fields == null) continue;



                                // Step 5: Sort fields based on desiredOrder
                                var sortedFields = fields
                                    .OrderBy(f =>
                                    {
                                        var fieldId = ((string)f["FieldId"]) ?? "";
                                        var index = desiredOrder.FindIndex(x => x == fieldId);
                                        return index == -1 ? int.MaxValue : index;
                                    })
                                    .ToList();

                                fields.Clear();
                                foreach (var field in sortedFields)
                                {
                                    fields.Add(field);
                                }

                            }
                        }
                    }
                }


                // Step 6: Write updated JSON
                
                File.WriteAllText(outputFilePath, jObject.ToString());

                Console.WriteLine($"✅ Field reordering completed.\nOutput saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }


        }

        public void ApplicationContactNew(string U)
 
            
        {
            Console.WriteLine("Starting Field Reordering...");

            try
            {
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
                // 🔑 Set your correct path here
                string jsonContent = File.ReadAllText(repoPath);
                var repository = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent);
                // 🔑 Set your correct path here

                XDocument xmlDoc = XDocument.Load(xmlPath);

                List<string> desiredOrder = new List<string>();

                // populating the desiredOrder list based on the XML structure and repository 
                foreach (var uiSection in xmlDoc.Descendants("UISection"))
                {
                    var uiSectionName = uiSection.Attribute("name")?.Value;

                    if (uiSectionName != null && uiSectionName.Equals(U, StringComparison.OrdinalIgnoreCase))
                    {
                        // 2. Find all control nodes inside this UISection
                        foreach (var control in uiSection.Descendants("control"))
                        {
                            var controlName = control.Attribute("name")?.Value;

                            if (controlName != null && controlName.Equals("ApplicationContact", StringComparison.OrdinalIgnoreCase))
                            {
                                // 3. Now find each field inside this control
                                foreach (var field in control.Descendants("field"))
                                {
                                    var attributeValue = field.Attribute("attribute")?.Value;

                                    if (!string.IsNullOrEmpty(attributeValue))
                                    {
                                        var atty = LookupJsonValue(
                                            uiSectionName,
                                            controlName,
                                            attributeValue,
                                            repository
                                        );
                                        desiredOrder.Add(atty); // Add the value or the attribute itself if not found
                                    }
                                }
                            }
                        }
                    }
                }

                //foreach (var attr in desiredOrder)
                //{
                //    Console.WriteLine(attr);
                //}

                // Step 1: Load the JSON
                // 🔑 Set your correct path here
                var json = File.ReadAllText(jsonFilePath);

                // Step 2: Parse into JObject
                var jObject = JObject.Parse(json);

                // Step 3: Define your desired field order

                // Step 4: Traverse ➔ UISection ➔ Actions ➔ Tabs ➔ Actions ➔ FieldSets ➔ Fields
                var uiSections = jObject["UISection"] as JArray;
                if (uiSections != null)
                {
                    foreach (var uiSection in uiSections)
                    {
                        var uiSectionName = (string)uiSection["UISectionName"];
                        if (!string.Equals(uiSectionName, "ApplicationContact", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var actions = uiSection["Actions"] as JArray;
                        if (actions == null) continue;

                        foreach (var action in actions)
                        {
                            var actionName = (string)action["ActionName"];
                            if (!string.Equals(actionName, "new", StringComparison.OrdinalIgnoreCase))
                                continue;

                            var fieldSets = action["FieldSets"] as JArray;
                            if (fieldSets == null) continue;

                            foreach (var fieldSet in fieldSets)
                            {
                                var fields = fieldSet["Fields"] as JArray;
                                if (fields == null) continue;



                                // Step 5: Sort fields based on desiredOrder
                                var sortedFields = fields
                                    .OrderBy(f =>
                                    {
                                        var fieldId = ((string)f["FieldId"]) ?? "";
                                        var index = desiredOrder.FindIndex(x => x == fieldId);
                                        return index == -1 ? int.MaxValue : index;
                                    })
                                    .ToList();

                                fields.Clear();
                                foreach (var field in sortedFields)
                                {
                                    fields.Add(field);
                                }

                            }
                        }
                    }
                }
                // Step 6: Write updated JSON
          
                File.WriteAllText(outputFilePath, jObject.ToString());

                Console.WriteLine($"✅ Field reordering completed.\nOutput saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }


        }

        public void ForEdit(string XUiSec, string JUisec, string Xcon)


        {
            Console.WriteLine("Starting Field Reordering...");

            try
            {
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
                // 🔑 Set your correct path here
                string jsonContent = File.ReadAllText(repoPath);
                var repository = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent);
                // 🔑 Set your correct path here

                XDocument xmlDoc = XDocument.Load(xmlPath);

                List<string> desiredOrder = new List<string>();

                // populating the desiredOrder list based on the XML structure and repository 
                foreach (var uiSection in xmlDoc.Descendants("UISection"))
                {
                    var uiSectionName = uiSection.Attribute("name")?.Value;

                    if (uiSectionName != null && uiSectionName.Equals(XUiSec, StringComparison.OrdinalIgnoreCase))
                    {
                        // 2. Find all control nodes inside this UISection
                        foreach (var control in uiSection.Descendants("control"))
                        {
                            var controlName = control.Attribute("name")?.Value;

                            if (controlName != null && controlName.Equals(Xcon, StringComparison.OrdinalIgnoreCase))
                            {
                                // 3. Now find each field inside this control
                                foreach (var field in control.Descendants("field"))
                                {
                                    var attributeValue = field.Attribute("attribute")?.Value;

                                    if (!string.IsNullOrEmpty(attributeValue))
                                    {
                                        var atty = LookupJsonValue(
                                            uiSectionName,
                                            controlName,
                                            attributeValue,
                                            repository
                                        );
                                        desiredOrder.Add(atty); // Add the value or the attribute itself if not found
                                    }
                                }
                            }
                        }
                    }
                }

                //foreach (var attr in desiredOrder)
                //{
                //    Console.WriteLine(attr);
                //}

                // Step 1: Load the JSON
                // 🔑 Set your correct path here
                var json = File.ReadAllText(jsonFilePath);

                // Step 2: Parse into JObject
                var jObject = JObject.Parse(json);

                // Step 3: Define your desired field order

                // Step 4: Traverse ➔ UISection ➔ Actions ➔ Tabs ➔ Actions ➔ FieldSets ➔ Fields
                var uiSections = jObject["UISection"] as JArray;
                if (uiSections != null)
                {
                    foreach (var uiSection in uiSections)
                    {
                        var uiSectionName = (string)uiSection["UISectionName"];
                        if (!string.Equals(uiSectionName, Xcon, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var actions = uiSection["Actions"] as JArray;
                        if (actions == null) continue;

                        foreach (var action in actions)
                        {
                            var actionName = (string)action["ActionName"];
                            if (!string.Equals(actionName, JUisec, StringComparison.OrdinalIgnoreCase))
                                continue;

                            var fieldSets = action["FieldSets"] as JArray;
                            if (fieldSets == null) continue;

                            foreach (var fieldSet in fieldSets)
                            {
                                var fields = fieldSet["Fields"] as JArray;
                                if (fields == null) continue;



                                // Step 5: Sort fields based on desiredOrder
                                var sortedFields = fields
                                    .OrderBy(f =>
                                    {
                                        var fieldId = ((string)f["FieldId"]) ?? "";
                                        var index = desiredOrder.FindIndex(x => x == fieldId);
                                        return index == -1 ? int.MaxValue : index;
                                    })
                                    .ToList();

                                fields.Clear();
                                foreach (var field in sortedFields)
                                {
                                    fields.Add(field);
                                }

                            }
                        }
                    }
                }
                // Step 6: Write updated JSON

                File.WriteAllText(outputFilePath, jObject.ToString());

                Console.WriteLine($"✅ Field reordering completed.\nOutput saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }


        }

        public void ForEditTabs(string XUiSec, string JUisec, string Xcon)
        {
            Console.WriteLine("Starting Field Reordering...");

            try
            {
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
                // 🔑 Set your correct path here
                string jsonContent = File.ReadAllText(repoPath);
                var repository = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent);
                // 🔑 Set your correct path here

                XDocument xmlDoc = XDocument.Load(xmlPath);

                List<string> desiredOrder = new List<string>();

                // populating the desiredOrder list based on the XML structure and repository 
                foreach (var uiSection in xmlDoc.Descendants("UISection"))
                {
                    var uiSectionName = uiSection.Attribute("name")?.Value;

                    if (uiSectionName != null && uiSectionName.Equals(XUiSec, StringComparison.OrdinalIgnoreCase))
                    {
                        // 2. Find all control nodes inside this UISection
                        foreach (var control in uiSection.Descendants("control"))
                        {
                            var controlName = control.Attribute("name")?.Value;

                            if (controlName != null && controlName.Equals(Xcon, StringComparison.OrdinalIgnoreCase))
                            {
                                // 3. Now find each field inside this control
                                foreach (var field in control.Descendants("field"))
                                {
                                    var attributeValue = field.Attribute("attribute")?.Value;

                                    if (!string.IsNullOrEmpty(attributeValue))
                                    {
                                        var atty = LookupJsonValue(
                                            uiSectionName,
                                            controlName,
                                            attributeValue,
                                            repository
                                        );
                                        desiredOrder.Add(atty); // Add the value or the attribute itself if not found
                                    }
                                }
                            }
                        }
                    }
                }

                //foreach (var attr in desiredOrder)
                //{
                //    Console.WriteLine(attr);
                //}

                // Step 1: Load the JSON
                // 🔑 Set your correct path here
                var json = File.ReadAllText(jsonFilePath);

                // Step 2: Parse into JObject
                var jObject = JObject.Parse(json);

                // Step 3: Define your desired field order

                // Step 4: Traverse ➔ UISection ➔ Actions ➔ Tabs ➔ Actions ➔ FieldSets ➔ Fields
                var uiSections = jObject["UISection"] as JArray;
                if (uiSections != null)
                {
                    foreach (var uiSection in uiSections)
                    {
                        var uiSectionName = (string)uiSection["UISectionName"];
                        if (!string.Equals(uiSectionName, Xcon, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var actions = uiSection["Actions"] as JArray;
                        if (actions == null) continue;

                        foreach (var action in actions)
                        {
                            var actionName = (string)action["ActionName"];
                            if (!string.Equals(actionName, JUisec, StringComparison.OrdinalIgnoreCase))
                                continue;

                            var tabs = action["Tabs"] as JArray;
                            if (tabs == null) continue;

                            foreach (var tab in tabs)
                            {
                                var tabId = (int?)tab["TabId"];
                                if (tabId != 1)  // 🔑 Match TabId = 1
                                    continue;

                                var tabActions = tab["Actions"] as JArray;
                                if (tabActions == null) continue;

                                foreach (var tabAction in tabActions)
                                {
                                    var fieldSets = tabAction["FieldSets"] as JArray;
                                    if (fieldSets == null) continue;

                                    foreach (var fieldSet in fieldSets)
                                    {
                                        var fields = fieldSet["Fields"] as JArray;
                                        if (fields == null) continue;

                                        // Step 5: Sort fields based on desiredOrder
                                        var sortedFields = fields
                                            .OrderBy(f =>
                                            {
                                                var fieldId = ((string)f["FieldId"]) ?? "";
                                                var index = desiredOrder.FindIndex(x => x == fieldId);
                                                return index == -1 ? int.MaxValue : index;
                                            })
                                            .ToList();

                                        fields.Clear();
                                        foreach (var field in sortedFields)
                                        {
                                            fields.Add(field);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Step 6: Write updated JSON

                File.WriteAllText(outputFilePath, jObject.ToString());

                Console.WriteLine($"✅ Field reordering completed.\nOutput saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }


        }


    }
}
