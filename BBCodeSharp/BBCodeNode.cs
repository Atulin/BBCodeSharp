namespace BBCodeSharp;

public sealed class BBCodeNode(string tag)
{
	public string Tag { get; set; } = tag;
	public string? Parameter { get; set; }
	public List<BBCodeNode> Children { get; set; } = [];
}