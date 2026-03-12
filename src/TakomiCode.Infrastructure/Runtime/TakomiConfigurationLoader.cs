using System.Text.RegularExpressions;
using TakomiCode.Application.Contracts.Runtime;
using TakomiCode.Domain.Entities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TakomiCode.Infrastructure.Runtime;

public class TakomiConfigurationLoader : ITakomiConfigurationLoader
{
    public async Task<ModeLoadResult> LoadModesAsync(string workspaceRoot, CancellationToken cancellationToken = default)
    {
        var modes = new List<TakomiMode>();
        var diagnostics = new List<LoadDiagnostic>();
        
        var modesDir = Path.Combine(workspaceRoot, ".agent", "Takomi-Agents");
        
        if (!Directory.Exists(modesDir))
        {
            // Missing directory is graceful degradation
            return new ModeLoadResult(modes, diagnostics);
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        foreach (var file in Directory.EnumerateFiles(modesDir, "*.yaml", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(modesDir, "*.yml", SearchOption.TopDirectoryOnly)))
        {
            try
            {
                var content = await File.ReadAllTextAsync(file, cancellationToken);
                var parsedContainer = deserializer.Deserialize<ModeContainerDto>(content);
                
                if (parsedContainer?.CustomModes != null)
                {
                    foreach (var m in parsedContainer.CustomModes)
                    {
                        modes.Add(new TakomiMode(
                            Slug: m.Slug ?? string.Empty,
                            Name: m.Name ?? string.Empty,
                            IconName: m.IconName ?? string.Empty,
                            Description: m.Description ?? string.Empty,
                            RoleDefinition: m.RoleDefinition ?? string.Empty,
                            WhenToUse: m.WhenToUse ?? string.Empty,
                            Groups: NormalizeGroups(m.Groups),
                            CustomInstructions: m.CustomInstructions ?? string.Empty,
                            Source: m.Source ?? string.Empty,
                            FilePath: file
                        ));
                    }
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add(new LoadDiagnostic(file, $"Failed to parse mode file: {ex.Message}", ex));
            }
        }

        return new ModeLoadResult(modes, diagnostics);
    }

    public async Task<WorkflowLoadResult> LoadWorkflowsAsync(string workspaceRoot, CancellationToken cancellationToken = default)
    {
        var workflows = new List<TakomiWorkflow>();
        var diagnostics = new List<LoadDiagnostic>();

        var workflowsDir = Path.Combine(workspaceRoot, ".agent", "workflows");
        
        if (!Directory.Exists(workflowsDir))
        {
            return new WorkflowLoadResult(workflows, diagnostics);
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var frontMatterRegex = new Regex(@"^\s*---\s*\n(.*?)\n\s*---\s*\n(.*)$", RegexOptions.Singleline | RegexOptions.Compiled);

        foreach (var file in Directory.EnumerateFiles(workflowsDir, "*.md", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var content = await File.ReadAllTextAsync(file, cancellationToken);
                content = content.Replace("\r\n", "\n");
                var match = frontMatterRegex.Match(content);

                string description = string.Empty;
                string body = content;

                if (match.Success)
                {
                    var frontMatter = match.Groups[1].Value;
                    body = match.Groups[2].Value;

                    var parsedFm = deserializer.Deserialize<WorkflowFrontmatterDto>(frontMatter);
                    if (parsedFm != null && !string.IsNullOrWhiteSpace(parsedFm.Description))
                    {
                        description = parsedFm.Description;
                    }
                }

                var slug = Path.GetFileNameWithoutExtension(file);

                workflows.Add(new TakomiWorkflow(
                    Slug: slug,
                    Description: description,
                    Body: body,
                    FilePath: file
                ));
            }
            catch (Exception ex)
            {
                diagnostics.Add(new LoadDiagnostic(file, $"Failed to parse workflow file: {ex.Message}", ex));
            }
        }

        return new WorkflowLoadResult(workflows, diagnostics);
    }

    private class ModeContainerDto
    {
        public List<ModeDto>? CustomModes { get; set; }
    }

    private class ModeDto
    {
        public string? Slug { get; set; }
        public string? Name { get; set; }
        public string? IconName { get; set; }
        public string? Description { get; set; }
        public string? RoleDefinition { get; set; }
        public string? WhenToUse { get; set; }
        public List<object>? Groups { get; set; }
        public string? CustomInstructions { get; set; }
        public string? Source { get; set; }
    }

    private class WorkflowFrontmatterDto
    {
        public string? Description { get; set; }
    }

    private static IReadOnlyList<TakomiToolGroup> NormalizeGroups(List<object>? groups)
    {
        if (groups is null || groups.Count == 0)
        {
            return Array.Empty<TakomiToolGroup>();
        }

        var normalized = new List<TakomiToolGroup>();

        foreach (var group in groups)
        {
            switch (group)
            {
                case string scalar when !string.IsNullOrWhiteSpace(scalar):
                    normalized.Add(new TakomiToolGroup(scalar));
                    break;

                case List<object> sequence when sequence.Count > 0:
                    var name = sequence[0]?.ToString();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    string? fileRegex = null;
                    string? description = null;

                    for (var index = 1; index < sequence.Count; index++)
                    {
                        if (sequence[index] is Dictionary<object, object> mapping)
                        {
                            if (mapping.TryGetValue("fileRegex", out var fileRegexValue))
                            {
                                fileRegex = fileRegexValue?.ToString();
                            }

                            if (mapping.TryGetValue("description", out var descriptionValue))
                            {
                                description = descriptionValue?.ToString();
                            }
                        }
                    }

                    normalized.Add(new TakomiToolGroup(name, fileRegex, description));
                    break;
            }
        }

        return normalized;
    }
}
