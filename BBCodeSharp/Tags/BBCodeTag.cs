namespace BBCodeSharp.Tags;

public abstract class BBCodeTag
{
	public const string TextTag = "text";
	public const string RootTag = "root";
	
	public abstract string Tag { get; }
	public virtual bool IsSelfClosing => false;
	public abstract string Render(BBCodeNode node, Func<BBCodeNode, string> renderChild);
}