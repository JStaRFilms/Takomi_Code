using System.Text.RegularExpressions;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace TakomiCode.UI.Controls;

public sealed partial class MarkdownMessageView : UserControl
{
    private static readonly Regex MarkdownLinkToAbsolutePathRegex = new(
        @"\[(?<label>[^\]]+)\]\((?<path>[A-Za-z]:[/\\][^)]+)\)",
        RegexOptions.Compiled);

    private static readonly Regex RelativePathWithAbsoluteRegex = new(
        @"(?<relative>(?:[A-Za-z0-9_.-]+/)+[A-Za-z0-9_.-]+)\((?<absolute>[A-Za-z]:[/\\][^)]+)\)",
        RegexOptions.Compiled);

    private static readonly Regex InlineTokenRegex = new(
        @"`[^`]+`|[A-Za-z]:[/\\][^\s)]+|(?<![\w`])(?:[A-Za-z0-9_.-]+/)+(?:[A-Za-z0-9_.-]+)(?![`])",
        RegexOptions.Compiled);

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(MarkdownMessageView),
        new PropertyMetadata(string.Empty, OnTextChanged));

    public MarkdownMessageView()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
    {
        if (dependencyObject is MarkdownMessageView view)
        {
            view.Render();
        }
    }

    private void Render()
    {
        BodyRoot.Children.Clear();

        var text = NormalizeMessage(Text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var lines = text.Replace("\r\n", "\n").Split('\n');
        var paragraphLines = new List<string>();
        var codeLines = new List<string>();
        var inCodeBlock = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                FlushParagraph(paragraphLines);

                if (inCodeBlock)
                {
                    AddCodeBlock(codeLines);
                    codeLines.Clear();
                }

                inCodeBlock = !inCodeBlock;
                continue;
            }

            if (inCodeBlock)
            {
                codeLines.Add(rawLine);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph(paragraphLines);
                continue;
            }

            if (TryAddSpecialLine(line))
            {
                FlushParagraph(paragraphLines);
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
            {
                FlushParagraph(paragraphLines);
                AddBullet(line[2..].Trim());
                continue;
            }

            paragraphLines.Add(line);
        }

        FlushParagraph(paragraphLines);

        if (codeLines.Count > 0)
        {
            AddCodeBlock(codeLines);
        }
    }

    private bool TryAddSpecialLine(string line)
    {
        if (line.StartsWith("Routing request to", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (line.StartsWith("Thinking through", StringComparison.OrdinalIgnoreCase))
        {
            AddMutedEventBlock("Thinking", line);
            return true;
        }

        if (line.StartsWith("Plan update:", StringComparison.OrdinalIgnoreCase))
        {
            AddStatusBlock("Plan update", line["Plan update:".Length..].Trim(), CreateBrush(255, 255, 185, 0));
            return true;
        }

        if (line.StartsWith("Changed files:", StringComparison.OrdinalIgnoreCase))
        {
            AddStatusBlock("Changed files", line["Changed files:".Length..].Trim(), CreateBrush(255, 16, 137, 62));
            return true;
        }

        if (line.StartsWith("Reading ", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("Inspecting ", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("Applying ", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("Building ", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("Checking ", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("Running package command", StringComparison.OrdinalIgnoreCase))
        {
            AddMutedEventBlock("Progress", line);
            return true;
        }

        return false;
    }

    private void FlushParagraph(List<string> paragraphLines)
    {
        if (paragraphLines.Count == 0)
        {
            return;
        }

        AddParagraph(string.Join(Environment.NewLine, paragraphLines));
        paragraphLines.Clear();
    }

    private void AddParagraph(string text)
    {
        var richTextBlock = CreateRichTextBlock();
        var paragraph = new Paragraph();
        AddInlineFragments(paragraph.Inlines, text);
        richTextBlock.Blocks.Add(paragraph);
        BodyRoot.Children.Add(richTextBlock);
    }

    private void AddBullet(string text)
    {
        var grid = new Grid
        {
            ColumnSpacing = 10
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var bullet = new Border
        {
            Width = 6,
            Height = 6,
            CornerRadius = new CornerRadius(3),
            Background = CreateBrush(255, 255, 185, 0),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var richTextBlock = CreateRichTextBlock();
        var paragraph = new Paragraph();
        AddInlineFragments(paragraph.Inlines, text);
        richTextBlock.Blocks.Add(paragraph);

        Grid.SetColumn(richTextBlock, 1);
        grid.Children.Add(bullet);
        grid.Children.Add(richTextBlock);
        BodyRoot.Children.Add(grid);
    }

    private void AddMutedEventBlock(string label, string body)
    {
        var border = new Border
        {
            Background = CreateBrush(255, 27, 27, 27),
            BorderBrush = CreateBrush(255, 58, 58, 58),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 10)
        };

        var stackPanel = new StackPanel
        {
            Spacing = 6
        };

        stackPanel.Children.Add(new TextBlock
        {
            Text = label.ToUpperInvariant(),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            CharacterSpacing = 80,
            Foreground = CreateBrush(255, 150, 150, 150)
        });

        var bodyText = new TextBlock
        {
            Text = body,
            TextWrapping = TextWrapping.WrapWholeWords,
            FontSize = 13,
            Foreground = CreateBrush(230, 230, 230, 230)
        };

        stackPanel.Children.Add(bodyText);
        border.Child = stackPanel;
        BodyRoot.Children.Add(border);
    }

    private void AddStatusBlock(string title, string body, SolidColorBrush accentBrush)
    {
        var border = new Border
        {
            Background = CreateBrush(76, accentBrush.Color.R, accentBrush.Color.G, accentBrush.Color.B),
            BorderBrush = accentBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 10)
        };

        var stackPanel = new StackPanel
        {
            Spacing = 6
        };

        stackPanel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = accentBrush,
            CharacterSpacing = 80
        });

        var richTextBlock = CreateRichTextBlock();
        var paragraph = new Paragraph();
        AddInlineFragments(paragraph.Inlines, body);
        richTextBlock.Blocks.Add(paragraph);
        stackPanel.Children.Add(richTextBlock);

        border.Child = stackPanel;
        BodyRoot.Children.Add(border);
    }

    private void AddCodeBlock(IEnumerable<string> lines)
    {
        var border = new Border
        {
            Background = CreateBrush(255, 20, 20, 20),
            BorderBrush = CreateBrush(255, 51, 51, 51),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12)
        };

        border.Child = new TextBlock
        {
            Text = string.Join(Environment.NewLine, lines),
            FontSize = 12,
            FontFamily = new FontFamily("Cascadia Code"),
            Foreground = CreateBrush(255, 242, 242, 242),
            TextWrapping = TextWrapping.WrapWholeWords,
            IsTextSelectionEnabled = true
        };

        BodyRoot.Children.Add(border);
    }

    private static RichTextBlock CreateRichTextBlock()
    {
        return new RichTextBlock
        {
            TextWrapping = TextWrapping.WrapWholeWords,
            FontSize = 14,
            Foreground = CreateBrush(230, 255, 255, 255),
            IsTextSelectionEnabled = true
        };
    }

    private static void AddInlineFragments(InlineCollection inlines, string text)
    {
        var currentIndex = 0;
        foreach (Match match in InlineTokenRegex.Matches(text))
        {
            if (match.Index > currentIndex)
            {
                inlines.Add(new Run
                {
                    Text = text[currentIndex..match.Index]
                });
            }

            AddInlineToken(inlines, match.Value);
            currentIndex = match.Index + match.Length;
        }

        if (currentIndex < text.Length)
        {
            inlines.Add(new Run
            {
                Text = text[currentIndex..]
            });
        }
    }

    private static void AddInlineToken(InlineCollection inlines, string token)
    {
        var isCode = token.StartsWith('`') && token.EndsWith('`');
        var isPathToken = !isCode && (token.Contains('/', StringComparison.Ordinal) || token.Contains('\\', StringComparison.Ordinal));
        var looksLikeFile = isPathToken && LooksLikeFilePath(token);
        var displayText = isCode
            ? token[1..^1]
            : token.Contains(':', StringComparison.Ordinal) || token.Contains('\\', StringComparison.Ordinal)
                ? ShortenWindowsPath(token)
                : token;

        var border = new Border
        {
            Background = isCode
                ? CreateBrush(255, 34, 34, 34)
                : looksLikeFile
                    ? CreateBrush(255, 40, 40, 40)
                    : CreateBrush(255, 41, 34, 13),
            BorderBrush = isCode
                ? CreateBrush(255, 64, 64, 64)
                : looksLikeFile
                    ? CreateBrush(255, 78, 78, 78)
                    : CreateBrush(255, 132, 102, 26),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(0, -1, 0, -1),
            Child = new TextBlock
            {
                Text = displayText,
                FontSize = 12,
                FontFamily = new FontFamily("Cascadia Code"),
                Foreground = isCode
                    ? CreateBrush(255, 235, 235, 235)
                    : looksLikeFile
                        ? CreateBrush(255, 214, 214, 214)
                        : CreateBrush(255, 255, 199, 75),
                TextWrapping = TextWrapping.NoWrap
            }
        };

        inlines.Add(new InlineUIContainer
        {
            Child = border
        });
    }

    private static string NormalizeMessage(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = MarkdownLinkToAbsolutePathRegex.Replace(
            input,
            match => $"`{NormalizePathForDisplay(match.Groups["label"].Value)}`");

        normalized = RelativePathWithAbsoluteRegex.Replace(
            normalized,
            match => $"`{NormalizePathForDisplay(match.Groups["relative"].Value)}`");

        normalized = normalized
            .Replace("Ã¢â‚¬â„¢", "'", StringComparison.Ordinal)
            .Replace("Ã¢â‚¬\"", "—", StringComparison.Ordinal)
            .Replace("Ã¢â‚¬â€œ", "–", StringComparison.Ordinal)
            .Replace("â€¦", "...", StringComparison.Ordinal);

        return normalized;
    }

    private static string ShortenWindowsPath(string path)
    {
        var normalized = NormalizePathForDisplay(path);
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length <= 3)
        {
            return normalized;
        }

        return $".../{segments[^2]}/{segments[^1]}";
    }

    private static string NormalizePathForDisplay(string path)
    {
        return path.Replace('\\', '/');
    }

    private static bool LooksLikeFilePath(string token)
    {
        var normalized = NormalizePathForDisplay(token);
        var lastSlashIndex = normalized.LastIndexOf('/');
        var lastSegment = lastSlashIndex >= 0 ? normalized[(lastSlashIndex + 1)..] : normalized;

        return lastSegment.Contains('.', StringComparison.Ordinal)
            && !lastSegment.EndsWith(".", StringComparison.Ordinal);
    }

    private static SolidColorBrush CreateBrush(byte alpha, byte red, byte green, byte blue)
    {
        return new SolidColorBrush(ColorHelper.FromArgb(alpha, red, green, blue));
    }
}
