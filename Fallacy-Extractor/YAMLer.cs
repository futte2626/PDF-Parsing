using System.Collections.Generic;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks.Dataflow;
namespace Fallacy_Extractor;
public static class YAMLer
{

    public static List<string> Validate(Root root)
    {
        var errors = new List<string>();
        string[] validVersions = ["1.0", "test, the llm would never write this"];

        if (string.IsNullOrWhiteSpace(root.Version) || validVersions.Contains(root.Version))
            errors.Add("Root.Version must be '1.0'");
        Console.WriteLine(root.Version);
        if (root.Document == null)
            errors.Add("Root.Document cannot be null");
        else
        {

            if (string.IsNullOrWhiteSpace(root.Document.ID))
                errors.Add("Document.ID is required");
            if (string.IsNullOrWhiteSpace(root.Document.Source))
                errors.Add("Document.Source is required");
            if (string.IsNullOrWhiteSpace(root.Document.Language))
                errors.Add("Document.Language is required");
            if (root.Document.PageCount < 0)
                errors.Add("Document.PageCount cannot be negative");
        }

        foreach (var node in root.Nodes)
        {
            ValidateNode(node, errors);
        }

        foreach (var edge in root.Edges)
        {
            ValidateEdge(edge, root.Nodes, errors);
        }

        foreach (var fallacy in root.Fallacies)
        {
            ValidateFallacy(fallacy, root.Nodes, errors);
        }
        
        //IsDAG(root.Nodes, root.Edges, errors);


        // if (root.Stats.NodeCount != root.Nodes.Count)
        //     errors.Add($"Stats.NodeCount ({root.Stats.NodeCount}) does not match actual number of nodes ({root.Nodes.Count})");
        // if (root.Stats.EdgeCount != root.Edges.Count)
        //     errors.Add($"Stats.EdgeCount ({root.Stats.EdgeCount}) does not match actual number of edges ({root.Edges.Count})");

        return errors;
    }

    private static void ValidateNode(Node node, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(node.ID))
            errors.Add("Node.ID is required");

        if (node.Confidence < 0 || node.Confidence > 1)
            errors.Add($"Node {node.ID}: confidence out of range");

        if (!Enum.IsDefined(node.Role))
            errors.Add($"Node {node.ID}: invalid role '{node.Role}'");

        if (!node.Explicit && (node.InferredFrom == null || node.InferredFrom.Count == 0))
            errors.Add($"Node {node.ID}: implicit nodes must have inferred_from");

        if (node.Explicit && node.Role != Role.Conclusion)
        {
            if (node.TextSpan == null)
                errors.Add($"Node {node.ID}: explicit nodes must have a TextSpan");

            if (node.InferredFrom != null && node.InferredFrom.Count > 0)
                errors.Add($"Node {node.ID}: explicit nodes cannot have InferredFrom; use edges instead");
        }
    }

    private static void ValidateEdge(Edge edge, List<Node> nodes, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(edge.ID))
            errors.Add("Edge.ID is required");
        if (!Enum.IsDefined(edge.Relation))
            errors.Add($"Edge {edge.ID}: invalid relation '{edge.Relation}'");

        if (!nodes.Exists(n => n.ID == edge.From))
            errors.Add($"Edge {edge.ID}: From node '{edge.From}' does not exist");
        if (!nodes.Exists(n => n.ID == edge.To))
            errors.Add($"Edge {edge.ID}: To node '{edge.To}' does not exist");

        if (edge.Confidence < 0 || edge.Confidence > 1)
            errors.Add($"Edge {edge.ID}: confidence out of range");
    }

    private static void ValidateFallacy(Fallacy fallacy, List<Node> nodes, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(fallacy.ID))
            errors.Add("Fallacy.ID is required");
        if (string.IsNullOrWhiteSpace(fallacy.Type))
            errors.Add($"Fallacy {fallacy.ID}: Type is required");
        if (fallacy.Confidence < 0 || fallacy.Confidence > 1)
            errors.Add($"Fallacy {fallacy.ID}: confidence out of range");

        foreach (var targetId in fallacy.TargetNodes)
        {
            if (!nodes.Exists(n => n.ID == targetId))
                errors.Add($"Fallacy {fallacy.ID}: target node '{targetId}' does not exist");
        }

        if (fallacy.TextSpans == null)
            errors.Add($"Fallacy {fallacy.ID}: TextSpans cannot be null");
    }
}


public class Root {
    public required string Version { get; set; }
    public Document Document { get; set; } = new();

    public List<Node> Nodes { get; set; } = [];
    public List<Edge> Edges { get; set; } = [];
    public List<Fallacy> Fallacies { get; set; } = [];
    public Meta Meta { get; set; } = new();
    //public Stats Stats { get; set; } = new();
}
public class Node {
    public string ID { get; set; } = "";
    public Role Role { get; set; }
    public bool Explicit { get; set; }
    public string Text { get; set; } = "";
    public double Confidence { get; set; }
    public TextSpan? TextSpan { get; set; }

    public List<string> InferredFrom { get; set; } = new();
}
public enum Role { 
    Premise,
    ImplicitPremise,
    Conclusion,
}
public class Edge {
    public required string ID { get; set; } = "";
    public required string From { get; set; } = "";
    public  required string To { get; set; } = "";
    public Relation Relation { get; set; }    // supports | attacks
    public double Confidence { get; set; }
}
public enum Relation {
    Supports,
    Attacks,
    Implies,
}


public class Document {
    public string ID { get; set; } = "";
    public string Source { get; set; } = "";
    public string Language { get; set; } = "en";
    public int PageCount { get; set; }
}
public class Meta {
    public List<string> Warnings = [];
    public Stats Stats = new();
}
public class Stats{
    public int NodeCount;
    public int EdgeCount;
    public int ImplicitPremisesAmount;
}



public class Fallacy
{
    public string ID { get; set; } = "";
    public string Type { get; set; } = "";        // ad_hominem, strawman, etc.
    public List<string> TargetNodes { get; set; } = [];
    public string Description { get; set; } = "";
    public double Confidence { get; set; }
    public List<TextSpan> TextSpans { get; set; } = [];
}

public class TextSpan
{
    public int Page { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
}






