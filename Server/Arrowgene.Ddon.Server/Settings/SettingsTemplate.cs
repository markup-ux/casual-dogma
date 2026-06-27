using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Arrowgene.Ddon.Server.Settings
{
    public class SettingsTemplate
    {
        private static string FileHeader = @"/*
 * Settings file for Server customization.
 * This file supports hotloading.
 */
";

        public static void Generate(string outputPath, Type type)
        {
            string xmlDocPath = GetXmlDocumentationPath(type.Assembly);
            XmlDocument xmlDoc = LoadXmlDocumentation(xmlDocPath);

            using (StreamWriter writer = new StreamWriter(outputPath, false))
            {
                writer.WriteLine(FileHeader);

                if (xmlDoc != null)
                {
                    foreach (PropertyInfo prop in type.GetProperties())
                    {
                        PrintPropertySummary(writer, xmlDoc, prop, $"P:{type.FullName}.{prop.Name}");
                        writer.WriteLine("");
                    }
                }
                else
                {
                    MemberInfo[] members = type.GetMembers();
                    foreach (var memberInfo in members)
                    {
                        if (memberInfo.MemberType != MemberTypes.Property)
                        {
                            continue;
                        }

                        var typeName = GetFriendlyTypeName(((PropertyInfo)memberInfo).PropertyType);
                        writer.WriteLine($"// {typeName} {memberInfo.Name};");
                        writer.WriteLine("");
                    }
                }
            }
        }

        private static string GetFriendlyTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                // Handle generic types if needed
                // string genericName = type.Name.Substring(0, type.Name.IndexOf('`'));
                // string genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
                // return $"{genericName}<{genericArgs}>";
                return "var";
            }

            // Map common types to their C# keywords
            if (type == typeof(bool)) return "bool";
            if (type == typeof(uint)) return "uint";
            if (type == typeof(int)) return "int";
            if (type == typeof(string)) return "string";
            if (type == typeof(double)) return "double";
            if (type == typeof(float)) return "float";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(short)) return "short";
            if (type == typeof(ushort)) return "ushort";
            if (type == typeof(long)) return "long";
            if (type == typeof(ulong)) return "ulong";

            // Default to full name for other types
            return type.Name;
        }

        private static string GetXmlDocumentationPath(Assembly assembly)
        {
            return Path.ChangeExtension(assembly.Location, ".xml");
        }

        private static XmlDocument LoadXmlDocumentation(string xmlPath)
        {
            try
            {
                if (File.Exists(xmlPath))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(xmlPath);
                    return doc;
                }
                else
                {
                    Console.WriteLine("XML documentation file not found.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading XML documentation: {ex.Message}");
                return null;
            }
        }

        private static void PrintPropertySummary(StreamWriter writer, XmlDocument xmlDoc, PropertyInfo prop, string memberName)
        {
            XmlNode node = xmlDoc.SelectSingleNode($"//member[@name='{memberName}']");
            if (node != null)
            {
                XmlNode summary = node.SelectSingleNode("summary");
                if (summary != null)
                {
                    string reconstructedSummary = ReconstructSummary(summary);
                    writer.WriteLine(reconstructedSummary);
                }

                var defaultAttr = prop.GetCustomAttribute<DefaultValueAttribute>(false);

                var typeName = GetFriendlyTypeName(prop.PropertyType);
                if (defaultAttr != null)
                {
                    string defaultValue = defaultAttr.Value?.ToString() ?? "null";
                    if (typeName == "string")
                    {
                        defaultValue = $"\"{defaultValue}\"";
                    }
                    else if (typeName == "bool")
                    {
                        defaultValue = defaultValue.ToLower();
                    }

                    writer.WriteLine($"{typeName} {prop.Name} = {defaultValue};");
                }
                else
                {
                    writer.WriteLine($"/// {typeName} {prop.Name} = ;");
                }
            }
        }

        private static string ReconstructSummary(XmlNode summaryNode)
        {
            StringBuilder sb = new StringBuilder();

            string[] lines = ExtractText(summaryNode).Trim().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            sb.AppendLine("/// <summary>");
            foreach (string line in lines)
            {
                sb.AppendLine($"/// {line.Trim()}");
            }
            sb.AppendLine("/// </summary>");

            return sb.ToString().TrimEnd();
        }

        // Walks an XML node tree and extracts readable text, resolving <see cref="T:Ns.Name"/>
        // to just "Name" instead of silently dropping it as InnerText would.
        private static string ExtractText(XmlNode node)
        {
            var sb = new StringBuilder();
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Text)
                {
                    sb.Append(child.Value);
                }
                else if (child.Name is "see" or "seealso")
                {
                    var cref = child.Attributes?["cref"]?.Value;
                    if (cref != null)
                    {
                        // cref format: "T:Namespace.TypeName" or bare "TypeName"
                        var typePath = cref.Contains(':') ? cref[(cref.IndexOf(':') + 1)..] : cref;
                        sb.Append(typePath.Contains('.') ? typePath[(typePath.LastIndexOf('.') + 1)..] : typePath);
                    }
                }
                else
                {
                    sb.Append(ExtractText(child));
                }
            }
            return sb.ToString();
        }
    }
}
