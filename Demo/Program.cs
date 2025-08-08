using BBCodeSharp;
using BBCodeSharp.Tags;

var parser = new Parser();
parser.AddTag(new BoldTag());

var input = "[b]Hello World![/b]";
var result = parser.Parse(input.AsSpan());

Console.WriteLine("AST:");
PrintNode(result, 0);

var html = parser.Render(result);

Console.WriteLine();

Console.WriteLine("Rendered HTML:");
Console.WriteLine(html);

return;

static void PrintNode(BBCodeNode node, int indent)
{
	var indentStr = new string(' ', indent * 2);
	Console.WriteLine($"{indentStr}{node.Tag}: {node.Parameter}");
        
	foreach (var child in node.Children)
	{
		PrintNode(child, indent + 1);
	}
}