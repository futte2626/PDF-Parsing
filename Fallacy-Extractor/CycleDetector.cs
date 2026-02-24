using System;
using System.Collections.Generic;
using System.Linq;

namespace Fallacy_Extractor;

public class CycleDetector
{ 
    public static List<List<Node>> GetCycles(List<Node> nodes, List<Edge> edges)
    {
        int size = nodes.Count;
        
        Dictionary<string, int> nodeIndexMap = new Dictionary<string, int>();
        for (int i = 0; i < size; i++)
        {
            nodeIndexMap[nodes[i].ID] = i;
        }
        
        int[,] adjMatrix = new int[size, size];
        
        foreach (var edge in edges)
        {
            if (nodeIndexMap.ContainsKey(edge.From) && nodeIndexMap.ContainsKey(edge.To))
            {
                int fromPos = nodeIndexMap[edge.From];
                int toPos = nodeIndexMap[edge.To];
                
                adjMatrix[fromPos, toPos] = 1;
                adjMatrix[toPos, fromPos] = 1;
            }
        }
        
        var cycles = FindCyclesViaIncidence(adjMatrix, nodes);
        
        return cycles;
    }

    private static List<List<Node>> FindCyclesViaIncidence(int[,] adjMatrix, List<Node> nodes)
    {
        int n = nodes.Count;
        
        var edges = new List<Tuple<int, int>>();
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                if (adjMatrix[i, j] == 1)
                {
                    edges.Add(Tuple.Create(i, j));
                }
            }
        }
        
        int m = edges.Count;
        
        if (m == 0) return new List<List<Node>>();
        double[][] B = new double[n][];
        for (int i = 0; i < n; i++)
        {
            B[i] = new double[m];
        }
        
        for (int j = 0; j < m; j++)
        {
            var edge = edges[j];
            B[edge.Item1][j] = 1;    // tail = +1
            B[edge.Item2][j] = -1;   // head = -1
        }
        
        var kernelBasis = FindKernelBasis(B);
        var cycles = new List<List<Node>>();
        
        foreach (var basisVec in kernelBasis)
        {
            var nodeCycle = ExtractNodeCycle(basisVec, edges, nodes);
            if (nodeCycle.Count > 0)
            {
                cycles.Add(nodeCycle);
            }
        }
        
        return cycles;
    }

    private static List<double[]> FindKernelBasis(double[][] B)
    {
        int n = B.Length;
        int m = B[0].Length;
        
        double[][] R = new double[n][];
        for (int i = 0; i < n; i++)
        {
            R[i] = new double[m];
            Array.Copy(B[i], R[i], m);
        }
        
        var pivotCols = new List<int>();
        int currentRow = 0;
        
        for (int col = 0; col < m && currentRow < n; col++)
        {
            int pivotRow = -1;
            for (int row = currentRow; row < n; row++)
            {
                if (Math.Abs(R[row][col]) > 1e-10)
                {
                    pivotRow = row;
                    break;
                }
            }
            
            if (pivotRow == -1)
            {
                continue;
            }
            
            if (pivotRow != currentRow)
            {
                var temp = R[currentRow];
                R[currentRow] = R[pivotRow];
                R[pivotRow] = temp;
            }
            
            double pivot = R[currentRow][col];
            for (int j = col; j < m; j++)
            {
                R[currentRow][j] /= pivot;
            }
            
            for (int i = 0; i < n; i++)
            {
                if (i != currentRow && Math.Abs(R[i][col]) > 1e-10)
                {
                    double factor = R[i][col];
                    for (int j = col; j < m; j++)
                    {
                        R[i][j] -= factor * R[currentRow][j];
                    }
                }
            }
            
            pivotCols.Add(col);
            currentRow++;
        }
        
        var freeCols = new List<int>();
        for (int col = 0; col < m; col++)
        {
            if (!pivotCols.Contains(col))
            {
                freeCols.Add(col);
            }
        }
        
        var basis = new List<double[]>();
        
        foreach (int freeCol in freeCols)
        {
            double[] basisVec = new double[m];
            basisVec[freeCol] = 1;
            
            for (int i = 0; i < pivotCols.Count; i++)
            {
                int pivotCol = pivotCols[i];
                basisVec[pivotCol] = -R[i][freeCol];
            }
            
            basis.Add(basisVec);
        }
        
        return basis;
    }

    private static List<Node> ExtractNodeCycle(double[] basisVec, List<Tuple<int, int>> edgeIndices, List<Node> nodes)
    {
        int m = basisVec.Length;

        var activeEdgeIndices = new List<int>();
        for (int j = 0; j < m; j++)
        {
            if (Math.Abs(basisVec[j]) > 1e-10)
            {
                activeEdgeIndices.Add(j);
            }
        }
        
        if (activeEdgeIndices.Count == 0)
        {
            return new List<Node>();
        }
        
        var adjacency = new Dictionary<int, List<Tuple<int, int>>>();
        
        foreach (int edgeIdx in activeEdgeIndices)
        {
            var edge = edgeIndices[edgeIdx];
            double coeff = basisVec[edgeIdx];
            
            if (coeff > 0)
            {
                // Fremad: u -> v
                if (!adjacency.ContainsKey(edge.Item1))
                    adjacency[edge.Item1] = new List<Tuple<int, int>>();
                adjacency[edge.Item1].Add(Tuple.Create(edge.Item2, edgeIdx));
                
                if (!adjacency.ContainsKey(edge.Item2))
                    adjacency[edge.Item2] = new List<Tuple<int, int>>();
                adjacency[edge.Item2].Add(Tuple.Create(edge.Item1, edgeIdx));
            }
            else
            {
                // bagud: v -> u
                if (!adjacency.ContainsKey(edge.Item2))
                    adjacency[edge.Item2] = new List<Tuple<int, int>>();
                adjacency[edge.Item2].Add(Tuple.Create(edge.Item1, edgeIdx));
                
                if (!adjacency.ContainsKey(edge.Item1))
                    adjacency[edge.Item1] = new List<Tuple<int, int>>();
                adjacency[edge.Item1].Add(Tuple.Create(edge.Item2, edgeIdx));
            }
        }
        
        var visited = new HashSet<int>();
        var nodeCycle = new List<Node>();

        int startVertex = edgeIndices[activeEdgeIndices[0]].Item1;
        
        var stack = new Stack<int>();
        var parent = new Dictionary<int, int>();
        var parentEdge = new Dictionary<int, int>();
        
        stack.Push(startVertex);
        visited.Add(startVertex);
        parent[startVertex] = -1;
        
        while (stack.Count > 0 && nodeCycle.Count == 0)
        {
            int current = stack.Pop();
            
            if (!adjacency.ContainsKey(current))
                continue;
            
            foreach (var neighbor in adjacency[current])
            {
                int nextVertex = neighbor.Item1;
                
                if (!visited.Contains(nextVertex))
                {
                    visited.Add(nextVertex);
                    parent[nextVertex] = current;
                    parentEdge[nextVertex] = neighbor.Item2;
                    stack.Push(nextVertex);
                }
                else if (nextVertex != parent[current] && parent.ContainsKey(current))
                {
                    var cycleVertices = new List<int>();
                    int cycleNode = current;
                    
                    while (cycleNode != nextVertex && cycleNode != -1)
                    {
                        cycleVertices.Add(cycleNode);
                        cycleNode = parent.ContainsKey(cycleNode) ? parent[cycleNode] : -1;
                    }
                    cycleVertices.Add(nextVertex);
                    cycleVertices.Reverse();
                    
                    foreach (int v in cycleVertices)
                    {
                        nodeCycle.Add(nodes[v]);
                    }
                    
                    break;
                }
            }
        }
        
        if (nodeCycle.Count > 0)
        {
            return nodeCycle;
        }
        
        var vertexSet = new HashSet<int>();
        foreach (int edgeIdx in activeEdgeIndices)
        {
            var edge = edgeIndices[edgeIdx];
            vertexSet.Add(edge.Item1);
            vertexSet.Add(edge.Item2);
        }
        
        foreach (int v in vertexSet)
        {
            nodeCycle.Add(nodes[v]);
        }
        
        return nodeCycle;
    }
    
    public static void PrintCycles(List<List<Node>> cycles)
    {
        Console.WriteLine($"Found {cycles.Count} cycles:");
        
        for (int i = 0; i < cycles.Count; i++)
        {
            Console.Write($"Cycle {i + 1}: ");
            var cycle = cycles[i];
            
            foreach (var node in cycle)
            {
                Console.Write($"{node.ID} -> ");
            }
        }
    }
}