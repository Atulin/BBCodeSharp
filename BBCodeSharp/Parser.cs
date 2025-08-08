using BBCodeSharp.Tags;
using Cysharp.Text;

namespace BBCodeSharp;

public class Parser
{
	private readonly Dictionary<string, BBCodeTag> _tags = new();

	public void AddTag(BBCodeTag tag)
	{
		_tags.Add(tag.Tag, tag);
	}

	public void RemoveTag(BBCodeTag tag)
	{
		_tags.Remove(tag.Tag);
	}

	public void ClearTags()
	{
		_tags.Clear();
	}

	public BBCodeNode Parse(ReadOnlySpan<char> text)
	{
		var root = new BBCodeNode(BBCodeTag.RootTag);
		ParseInternal(text, root);
		return root;
	}

	private void ParseInternal(ReadOnlySpan<char> text, BBCodeNode parent)
	{
		var i = 0;
		var currentText = ZString.CreateUtf8StringBuilder();

		while (i < text.Length)
		{
			var ch = text[i];

			if (ch == '[')
			{
				if (currentText.Length > 0)
				{
					parent.Children.Add(new BBCodeNode(BBCodeTag.TextTag)
					{
						Parameter = currentText.ToString(),
					});
					currentText.Clear();
				}

				var tagResult = ParseTag(text[i..]);
				if (tagResult.Success)
				{
					if (tagResult.IsClosing)
					{
						return;
					}
					if (_tags.TryGetValue(tagResult.TagName, out var value) && value.IsSelfClosing)
					{
						var node = new BBCodeNode(tagResult.TagName)
						{
							Parameter = tagResult.Parameter,
						};
						parent.Children.Add(node);
						i += tagResult.Length;
					}

					else
					{
						var node = new BBCodeNode(tagResult.TagName)
						{
							Parameter = tagResult.Parameter,
						};
						parent.Children.Add(node);
						i += tagResult.Length;

						var contentStart = i;
						var contentEnd = FindClosingTag(text[i..], tagResult.TagName);

						if (contentEnd > 0)
						{
							ParseInternal(text[contentStart..contentEnd], node);
							i += contentEnd;

							var closingTagResult = ParseTag(text[i..]);
							if (closingTagResult is { Success: true, IsClosing: true })
							{
								i += closingTagResult.Length;
							}
						}
						else
						{
							currentText.Append('[');
							i++;
						}
					}
				}
				else
				{
					currentText.Append(ch);
					i++;
				}
			}
			else
			{
				currentText.Append(ch);
				i++;
			}
		}

		if (currentText.Length > 0)
		{
			parent.Children.Add(new BBCodeNode(BBCodeTag.TextTag)
			{
				Parameter = currentText.ToString(),
			});
		}
		currentText.Clear();
	}

	private static TagParseResult ParseTag(ReadOnlySpan<char> text)
	{
		if (text.Length < 2 || text[0] != '[')
		{
			return new TagParseResult(false);
		}

		var i = 1;
		var isClosing = false;

		if (text[i] == '/')
		{
			isClosing = true;
			i++;
		}

		var tagStart = i;
		while (i < text.Length && text[i] != ']' && text[i] != '=')
		{
			i++;
		}

		if (i == tagStart || i >= text.Length)
		{
			return new TagParseResult(false);
		}

		var tagName = text[tagStart..(i - tagStart)].ToString();
		string? parameter = null;

		if (!isClosing && i < text.Length && text[i] == '=')
		{
			i++;
			var parameterStart = i;
			if (i < text.Length && text[i] != '"')
			{
				i++;
				parameterStart = i;
				while (i < text.Length && text[i] != '"')
				{
					i++;
				}
				if (i < text.Length)
				{
					parameter = text[parameterStart..(i - parameterStart)].ToString();
					i++;
				}
			}
			else
			{
				while (i < text.Length && text[i] != ']' && text[i] != ' ')
				{
					i++;
				}
				parameter = text[parameterStart..(i - parameterStart)].ToString();
			}
		}

		while (i < text.Length && text[i] == ']')
		{
			i++;
		}

		if (i >= text.Length)
		{
			return new TagParseResult(false);
		}

		i++;

		return new TagParseResult(true)
		{
			TagName = tagName,
			Parameter = parameter,
			IsClosing = isClosing,
			Length = i,
		};
	}

	private int FindClosingTag(ReadOnlySpan<char> text, string tagName)
	{
		var depth = 0;
		var i = 0;
		while (i < text.Length)
		{
			var ch = text[i];
			if (ch == '[')
			{
				var tagResult = ParseTag(text[i..]);
				if (tagResult.Success)
				{
					if (tagResult.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase))
					{
						if (tagResult.IsClosing)
						{
							if (depth == 0)
							{
								return i;
							}
							depth--;
						}
						else if (_tags.TryGetValue(tagResult.TagName, out var value) && !value.IsSelfClosing)
						{
							depth++;
						}
					}
					i += tagResult.Length;
				}
				else
				{
					i++;
				}
			}
			else
			{
				i++;
			}
		}

		return -1;
	}

	public string Render(BBCodeNode node)
	{
		return RenderNode(node);
	}

	private string RenderNode(BBCodeNode node)
	{
		if (node.Tag == "text")
		{
			return node.Parameter ?? ""; // Plain text content
		}

		if (node.Tag == "root")
		{
			// Root node - just render all children
			var content = ZString.CreateUtf8StringBuilder();
			foreach (var child in node.Children)
			{
				content.Append(RenderNode(child));
			}
			return content.ToString();
		}

		if (_tags.TryGetValue(node.Tag, out var tag))
		{
			// Known tag - use its render method
			return tag.Render(node, RenderNode);
		}

		// Unknown tag - render as plain text with brackets
		var fallback = ZString.CreateUtf8StringBuilder();
		fallback.Append('[');
		fallback.Append(node.Tag);
		if (!string.IsNullOrEmpty(node.Parameter))
		{
			fallback.Append('=');
			fallback.Append(node.Parameter);
		}
		fallback.Append(']');

		foreach (var child in node.Children)
		{
			fallback.Append(RenderNode(child));
		}

		if (_tags.TryGetValue(node.Tag, out var unknownTag) && unknownTag.IsSelfClosing)
		{
			return fallback.ToString();
		}
		
		fallback.Append('[');
		fallback.Append('/');
		fallback.Append(node.Tag);
		fallback.Append(']');

		return fallback.ToString();
	}

	private readonly struct TagParseResult(bool success)
	{
		public bool Success { get; } = success;
		public string TagName { get; init; }
		public string? Parameter { get; init; }
		public bool IsClosing { get; init; }
		public int Length { get; init; }
	}
}