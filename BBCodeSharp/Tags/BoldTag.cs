using Cysharp.Text;

namespace BBCodeSharp.Tags;

public class BoldTag : BBCodeTag
{
	public override string Tag => "b";

	public override string Render(BBCodeNode node, Func<BBCodeNode, string> renderChild)
	{
		using var tag = ZString.CreateUtf8StringBuilder();
		tag.Append("<strong>");
		foreach (var child in node.Children)
		{
			if (child.Tag == TextTag)
			{
				tag.Append(renderChild(child));
			}
		}
		tag.Append("</strong>");
		return tag.ToString();
	}
}