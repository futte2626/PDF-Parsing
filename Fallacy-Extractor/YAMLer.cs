using System.Collections.Generic;
using System;
namespace Fallacy_Extractor;
public static class YAMLer
{
    public static List<string> Validate(Root root)
    {
        var errors = new List<string>();

        if (root.Version != "1.0")
            errors.Add("version must be \"1.0\"");

        foreach (var node in root.Nodes)
        {
            if (node.Confidence < 0 || node.Confidence > 1)
                errors.Add($"Node {node.Id}: confidence out of range");

            if (node.Role != Role.Premise && node.Role != Role.Conclusion)
                errors.Add($"Node {node.Id}: invalid role");

            if (!node.Explicit && (node.InferredFrom == null || node.InferredFrom.Count == 0))
                errors.Add($"Node {node.Id}: implicit nodes must have inferred_from");

            if (node.Explicit && node.TextSpan == null)
                errors.Add($"Node {node.Id}: explicit nodes must have text_span");
            if (node.Explicit && node.InferredFrom != null)
            {
                errors.Add($"Node {node.Id}: explicit nodes cannot be infered. use edges instead");
            }
        }

        return errors;
    }
}

public class Root
{
    public required string Version { get; set; }
    public Document Document { get; set; } = new();

    public List<Node> Nodes { get; set; } = new();
    public List<Edge> Edges { get; set; } = new();
    public List<Fallacy> Fallacies { get; set; } = new();
}
public class Node
{
    public string Id { get; set; } = "";
    public Role Role { get; set; }
    public bool Explicit { get; set; }
    public string Text { get; set; } = "";
    public double Confidence { get; set; }
    public TextSpan? TextSpan { get; set; }

    public List<string> InferredFrom { get; set; } = new();
}
public enum Role
{
    Premise,
    Conclusion,
}
public class Edge
{
    public string Id { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public Relation Relation { get; set; }    // supports | attacks
    public double Confidence { get; set; }
}
public enum Relation
{
    Supports,
    Attacks
}


public class Document
{
    public string Id { get; set; } = "";
    public string Source { get; set; } = "";
    public string Language { get; set; } = "EN";
    public int PageCount { get; set; }
}



public class Fallacy
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";        // ad_hominem, strawman, etc.
    public List<string> TargetNodes { get; set; } = new();
    public string Description { get; set; } = "";
    public double Confidence { get; set; }
    public List<TextSpan> TextSpans { get; set; } = new();
}

public class TextSpan
{
    public int Page { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
}






