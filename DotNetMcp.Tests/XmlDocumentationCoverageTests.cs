using System.Reflection;
using System.Text;
using System.Xml.Linq;
using ModelContextProtocol.Server;
using Xunit;

namespace DotNetMcp.Tests;

public class XmlDocumentationCoverageTests
{
    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var slnxPath = Path.Combine(directory.FullName, "DotNetMcp.slnx");
            if (File.Exists(slnxPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Unable to locate repository root (DotNetMcp.slnx not found) starting from AppContext.BaseDirectory.");
    }

    private static string FindDotNetMcpXmlDocumentationPath()
    {
        var toolAssembly = typeof(DotNetMcp.DotNetCliTools).Assembly;

        if (!string.IsNullOrWhiteSpace(toolAssembly.Location))
        {
            var nextToAssembly = Path.ChangeExtension(toolAssembly.Location, ".xml");
            if (File.Exists(nextToAssembly))
            {
                return nextToAssembly;
            }
        }

        var repoRoot = FindRepoRoot();
        var binRoot = Path.Combine(repoRoot, "DotNetMcp", "bin");

        if (Directory.Exists(binRoot))
        {
            var candidates = Directory
                .EnumerateFiles(binRoot, "DotNetMcp.xml", SearchOption.AllDirectories)
                .OrderByDescending(path => path.Contains("\\Release\\", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(path => path.Contains("\\net10.0\\", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (candidates.Count > 0)
            {
                return candidates[0];
            }
        }

        throw new InvalidOperationException(
            "Unable to locate DotNetMcp.xml documentation file. Ensure the DotNetMcp project has GenerateDocumentationFile=true and that it has been built as part of the test run.");
    }

    [Fact]
    public void All_McpServerTool_Methods_ShouldHave_XmlSummary_AndParamDocs()
    {
        // Arrange
        var xmlPath = FindDotNetMcpXmlDocumentationPath();
        var doc = XDocument.Load(xmlPath);

        var memberElements = doc.Root?
            .Element("members")?
            .Elements("member")
            .ToDictionary(
                m => (string?)m.Attribute("name") ?? string.Empty,
                m => m,
                StringComparer.Ordinal);

        Assert.NotNull(memberElements);

        var toolAssembly = typeof(DotNetMcp.DotNetCliTools).Assembly;

        var toolTypes = toolAssembly
            .GetTypes()
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            .ToList();

        Assert.NotEmpty(toolTypes);

        var toolMethods = toolTypes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null)
            .ToList();

        Assert.NotEmpty(toolMethods);

        // Act
        var failures = new List<string>();

        foreach (var method in toolMethods)
        {
            var memberId = GetXmlDocumentationMemberId(method);

            if (!memberElements!.TryGetValue(memberId, out var memberElement))
            {
                failures.Add($"Missing XML documentation entry for {method.DeclaringType?.FullName}.{method.Name} (expected member id '{memberId}')");
                continue;
            }

            var summaryText = memberElement.Element("summary")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(summaryText))
            {
                failures.Add($"Missing or empty <summary> for {method.DeclaringType?.FullName}.{method.Name} (member id '{memberId}')");
            }

            foreach (var parameter in method.GetParameters())
            {
                var paramElement = memberElement
                    .Elements("param")
                    .FirstOrDefault(e => string.Equals((string?)e.Attribute("name"), parameter.Name, StringComparison.Ordinal));

                var paramText = paramElement?.Value?.Trim();
                if (string.IsNullOrWhiteSpace(paramText))
                {
                    failures.Add($"Missing or empty <param name=\"{parameter.Name}\"> for {method.DeclaringType?.FullName}.{method.Name} (member id '{memberId}')");
                }
            }
        }

        // Assert
        if (failures.Count > 0)
        {
            var message = new StringBuilder();
            message.AppendLine($"XML documentation coverage failures: {failures.Count}");

            foreach (var failure in failures.Take(50))
            {
                message.AppendLine("- " + failure);
            }

            if (failures.Count > 50)
            {
                message.AppendLine($"... and {failures.Count - 50} more");
            }

            Assert.Fail(message.ToString());
        }
    }

    private static string GetXmlDocumentationMemberId(MethodInfo method)
    {
        var declaringType = method.DeclaringType ?? throw new InvalidOperationException("Method has no declaring type.");
        var typeName = GetXmlDocTypeName(declaringType);

        var memberId = new StringBuilder();
        memberId.Append("M:");
        memberId.Append(typeName);
        memberId.Append('.');
        memberId.Append(method.Name);

        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return memberId.ToString();
        }

        memberId.Append('(');
        memberId.Append(string.Join(",", parameters.Select(p => GetXmlDocTypeName(p.ParameterType))));
        memberId.Append(')');

        return memberId.ToString();
    }

    private static string GetXmlDocTypeName(Type type)
    {
        if (type.IsByRef)
        {
            // XML doc IDs use '@' suffix for ref/out parameters
            return GetXmlDocTypeName(type.GetElementType()!) + "@";
        }

        if (type.IsArray)
        {
            return GetXmlDocTypeName(type.GetElementType()!) + "[]";
        }

        if (type.IsGenericParameter)
        {
            // Method generic parameters use ``0, type generic parameters use `0
            return (type.DeclaringMethod is not null ? "``" : "`") + type.GenericParameterPosition;
        }

        if (type.IsGenericType)
        {
            var genericDefinition = type.GetGenericTypeDefinition();
            var definitionName = (genericDefinition.FullName ?? genericDefinition.Name)
                .Replace('+', '.');

            var tickIndex = definitionName.IndexOf('`');
            if (tickIndex >= 0)
            {
                definitionName = definitionName.Substring(0, tickIndex);
            }

            var args = type.GetGenericArguments().Select(GetXmlDocTypeName);
            return definitionName + "{" + string.Join(",", args) + "}";
        }

        var fullName = type.FullName ?? type.Name;
        return fullName.Replace('+', '.');
    }
}
