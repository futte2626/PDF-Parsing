using System;
using System.Collections.Generic;
using System.Linq;

namespace Fallacy_Extractor;

public class CycleDetector
{
    public static void TestDetector()
    {
        // A (Adjacency) Matrix
        int[,] adjMatrix = {
            {0, 1, 0, 0, 1},
            {1, 0, 1, 0, 1},
            {0, 1, 0, 1, 0},
            {0, 0, 1, 0, 1},
            {1, 1, 0, 1, 0}
        };

        var cycles = FindCycles(adjMatrix);
        var edges = ExtractEdges(adjMatrix);
        
        PrintCycles(cycles, edges);
    }
    
    public static List<List<int>> FindCycles(int[,] adjMatrix)
    {
        // Step 1: Find edges fra A matrix
        var edges = ExtractEdges(adjMatrix);
        int n = adjMatrix.GetLength(0); // Antal nodes
        int m = edges.Count;            // Antal edges

        // Step 2: Laver incidence matrix B
        double[][] B = new double[n][];
        for (int i = 0; i < n; i++)
        {
            B[i] = new double[m];
        }

        //Sætter værdierne til +1/-1
        for (int j = 0; j < m; j++)
        {
            Tuple<int,int> edge = edges[j];
            B[edge.Item1][j] = 1;    // start = +1
            B[edge.Item2][j] = -1;   // slut = -1
        }

        // Step 3: Finder kernel af B
        List<double[]> kernelBasis = FindKernelBasis(B);

        // Step 4: Konverterere kernel vektorer til cycles 
        List<List<int>> cycles = new List<List<int>>();
        foreach (double[] basisVec in kernelBasis)
        {
            List<int> cycle = ExtractCycle(basisVec, edges);
            if (cycle.Any())
            {
                cycles.Add(cycle);
            }
        }

        return cycles;
    }

    // Finder edges fra adjacency matrix 
    private static List<Tuple<int, int>> ExtractEdges(int[,] adjMatrix)
    {
        // En liste af tupler (En anden måde at se edges som)
        List<Tuple<int, int>> edges = new List<Tuple<int, int>>();
        int n = adjMatrix.GetLength(0);
        
        // Lopper igennem matricens øverste trekant dette kan ses her fordi ellers vil vi få redundant edges
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                //Hvis den ikke er nul må der være en kant (kunne også skrives if(adjMatrix[i, j] == 1))
                if (adjMatrix[i, j] != 0)
                {
                    edges.Add(Tuple.Create(i, j));
                }
            }
        }
        return edges;
    }

    // Finder basis for kernel(B) med Gaussian elimination
    private static List<double[]> FindKernelBasis(double[][] B)
    {
        int n = B.Length;     // rækker
        int m = B[0].Length;  // kolloner

        // Laver er kopi
        double[][] R = new double[n][];
        for (int i = 0; i < n; i++)
        {
            R[i] = new double[m];
            Array.Copy(B[i], R[i], m);
        }

        // Dette bliver pivot kollonerne for RREF 
        var pivotCols = new List<int>();
        int currentRow = 0;

        for (int col = 0; col < m && currentRow < n; col++)
        {
            // Finder pivot i den nuværende kollone
            int pivotRow = -1;
            for (int row = currentRow; row < n; row++)
            {
                if (Math.Abs(R[row][col]) != 0)
                {
                    pivotRow = row;
                    break;
                }
            }

            if (pivotRow == -1)
            {
                continue; // Ingen pivot findes i denne kollone
            }

            // Bytter currentRow med pivotRow
            if (pivotRow != currentRow)
            {
                var temp = R[currentRow];
                R[currentRow] = R[pivotRow];
                R[pivotRow] = temp;
            }

            // Normalizere pivot række (gør pivot til 1 med division)
            double pivot = R[currentRow][col];
            for (int j = col; j < m; j++)
            {
                R[currentRow][j] /= pivot;
            }

            // Trækker denne kollone fra alle andre
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

        // Finder frie kolloner (hvor fri kolline svarer til en basis vektor for kernel(B))
        var freeCols = new List<int>();
        for (int col = 0; col < m; col++)
        {
            if (!pivotCols.Contains(col))
            {
                freeCols.Add(col);
            }
        }

        // Konstruerer kernel basis vectors
        var basis = new List<double[]>();

        foreach (int freeCol in freeCols)
        {
            double[] basisVec = new double[m];
            basisVec[freeCol] = 1; // Sætter variable til 1

            // Sætter pivot variabler baseret på vores RREF
            for (int i = 0; i < pivotCols.Count; i++)
            {
                int pivotCol = pivotCols[i];
                basisVec[pivotCol] = -R[i][freeCol];
            }

            basis.Add(basisVec);
        }

        return basis;
    }

    // Function til at finde cycles fra basis vectorerne (i kernel(B))
    private static List<int> ExtractCycle(double[] basisVec, List<Tuple<int, int>> edges)
    {
        int m = basisVec.Length;
        var cycle = new List<int>();

        // Step 1: Find kanter med ikke nul koefficienter
        var activeEdges = new List<int>();
        for (int j = 0; j < m; j++)
        {
            if (Math.Abs(basisVec[j]) > 1e-10)
            {
                activeEdges.Add(j);
            }
        }

        if (!activeEdges.Any())
        {
            return cycle; // En tom cyklus
        }

        // Step 2: Bygger rettet naboer fra kanterne
        var adj = new Dictionary<int, List<EdgeAdjacency>>();

        foreach (int edgeIdx in activeEdges)
        {
            var edge = edges[edgeIdx];
            int u = edge.Item1;
            int v = edge.Item2;
            double coeff = basisVec[edgeIdx];

            if (coeff > 0)
            {
                // Fremad: u->v
                if (!adj.ContainsKey(u)) adj[u] = new List<EdgeAdjacency>();
                adj[u].Add(new EdgeAdjacency(v, edgeIdx, 1));
                
                if (!adj.ContainsKey(v)) adj[v] = new List<EdgeAdjacency>();
                adj[v].Add(new EdgeAdjacency(u, edgeIdx, -1));
            }
            else
            {
                // Baggud: v → u
                if (!adj.ContainsKey(v)) adj[v] = new List<EdgeAdjacency>();
                adj[v].Add(new EdgeAdjacency(u, edgeIdx, 1));
                
                if (!adj.ContainsKey(u)) adj[u] = new List<EdgeAdjacency>();
                adj[u].Add(new EdgeAdjacency(v, edgeIdx, -1));
            }
        }

        // Step 3: Find Euler cyklus
        int startVertex = adj.Keys.First();
        int current = startVertex;
        var usedEdges = new HashSet<int>();

        while (true)
        {
            if (!adj.ContainsKey(current) || !adj[current].Any())
                break;

            // Find en ikke brugt udangående kant
            EdgeAdjacency next = null;
            foreach (var neighbor in adj[current])
            {
                if (!usedEdges.Contains(neighbor.EdgeIdx))
                {
                    next = neighbor;
                    break;
                }
            }

            if (next == null)
                break; // Ingen ikke bruge kanter

            usedEdges.Add(next.EdgeIdx);
            cycle.Add(next.EdgeIdx);
            current = next.Vertex;

            // Tjekker om vi er tilbage til start
            if (current == startVertex && usedEdges.Count == activeEdges.Count)
            {
                break;
            }
        }

        return cycle;
    }

    // Hjælper klasse
    private class EdgeAdjacency
    {
        public int Vertex { get; }
        public int EdgeIdx { get; }
        public int Direction { get; } // +1 for fremad, -1 for bagud

        public EdgeAdjacency(int vertex, int edgeIdx, int direction)
        {
            Vertex = vertex;
            EdgeIdx = edgeIdx;
            Direction = direction;
        }
    }

    // Hjælper klasse til at printe cycluser. Ved at få knuderne i cykluserne
   public static void PrintCycles(List<List<int>> cycles, List<Tuple<int, int>> edges)
{
    Console.WriteLine($"Found {cycles.Count} independent cycles:");
    
    for (int i = 0; i < cycles.Count; i++)
    {
        Console.Write($"Cycle {i + 1}: ");
        var cycle = cycles[i];
        
        if (!cycle.Any())
        {
            Console.WriteLine("Empty");
            continue;
        }

        var vertices = new List<int>();
        var firstEdge = edges[cycle[0]];
        vertices.Add(firstEdge.Item1);
        vertices.Add(firstEdge.Item2);
        
        for (int j = 1; j < cycle.Count; j++)
        {
            var edge = edges[cycle[j]];
            int lastVertex = vertices[vertices.Count - 1];
            if (edge.Item1 == lastVertex) vertices.Add(edge.Item2);
            else if (edge.Item2 == lastVertex) vertices.Add(edge.Item1);
            else {
                for (int k = 0; k < vertices.Count; k++) {
                    if (edge.Item1 == vertices[k]) {
                        vertices.Insert(k + 1, edge.Item2);
                        break;
                    }
                    if (edge.Item2 == vertices[k]) {
                        vertices.Insert(k + 1, edge.Item1);
                        break;
                    }
                }
            }
        }

        if (vertices.Count > 1 && vertices[0] == vertices[vertices.Count - 1])
        {
            vertices.RemoveAt(vertices.Count - 1);
        }
        
        foreach (int v in vertices)
        {
            Console.Write($"v{v + 1} ->    ");
        }
        // Close the cycle
        Console.WriteLine($"v{vertices[0] + 1}");
    }
}
}