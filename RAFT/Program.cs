// See https://aka.ms/new-console-template for more information
using RAFT;

foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.log"))
{
    File.Delete(file);
}

List<RaftNode> nodes = new List<RaftNode>();
for (int i = 0; i < 3; i++)
{
    nodes.Add(new RaftNode(nodes));
}

foreach (var node in nodes)
{
    node.Initialize();
}

Console.WriteLine("Election running. Press key to stop election run.");
Console.ReadKey();
