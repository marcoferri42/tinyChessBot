using ChessChallenge.API;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

public class Node // classetta nodo custom
{
    public bool isRoot = false;
    public int eval { get; set; }
    public int complexity { get; set; }
    public Node root { get; set; }
    public Move lastMove { get; set; }

    public List<Node> Children = new List<Node>();

    public Node()
    {
        this.eval = 0;
        this.isRoot = true;
    }

    public Node(Node root, int eval, Move move)
    {
        this.root = root;
        this.eval = eval;
        this.lastMove = move;
        this.isRoot = false;
    }

    public void Print()
    {
        Console.WriteLine(ToStringHelper(this, ""));
    }

    private string ToStringHelper(Node node, string indent)
    {
        string nodeString = $"{indent}+-- {node.eval}";

        for (int i = 0; i < node.Children.Count; i++)
        {
            nodeString += $"\n{ToStringHelper(node.Children[i], indent + (i == node.Children.Count - 1 ? "    " : "|   "))}";
        }

        return nodeString;
    }
}

class NodeCompare : IComparer<Node> // comparatore nodes
{
    public int Compare(Node x, Node y)
    {
        // Compare by the eval
        return x.eval.CompareTo(y.eval);
    }
}

public class MyBot : IChessBot
{
    // Generate heat distribution decreasing from the center with a specified radius
    static double HeatDistributionFunction(double x, double y, double maxHeat, double centerX, double centerY, double radius)
    {
        double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
        if (distance <= radius)
        {
            return maxHeat;
        }
        return maxHeat / (1 + distance - radius);
    }

    private Dictionary<PieceType, int> values = new Dictionary<PieceType, int>() { // pieces values
        { PieceType.Pawn, 1 },
        { PieceType.Bishop, 3 },
        { PieceType.Knight, 3 },
        { PieceType.Queen, 9 },
        { PieceType.King, 100 },
        { PieceType.Rook, 5 }
    };
    
    public Dictionary<ulong, int> seenPositions = new Dictionary<ulong, int>(); // positions table

    private int botColor = 1;

    private int[,] heatGrid;

    public Move Think(Board board, Timer t)
    {
        botColor = board.IsWhiteToMove ? 1 : -1;

        heatGrid = createHeatGrid(1.5/*max heat*/, 2.0/*max radius*/);

        Node tree = new Node();

        for (int i = 1; i < 4; i++) // itereative deepening TODO da studiare bene ! 
        {
            tree = CreateTree(board, i, new Node());
            
            if (t.MillisecondsElapsedThisTurn > 1000)
            {
                break;
            }
        }

        //Console.WriteLine(board.GetLegalMoves().Count());
        //Console.WriteLine(tree.Children.Count());

        //Console.WriteLine(tree.Children[0].eval);             <---------- evaluation print
        return tree.Children[0].lastMove;
    }

    private int Evaluate(Board board)
    {
        int score = 0, turn = board.IsWhiteToMove ? -1 : 1;

        if (seenPositions.ContainsKey(board.ZobristKey))
        {
            return seenPositions[board.ZobristKey];
        }
        else
        {            
            // one move rule
            if (board.GameMoveHistory.Length > 3)
            {
                PieceType previousMove = board.GameMoveHistory[^3].MovePieceType;
                PieceType lastMove = board.GameMoveHistory[^1].MovePieceType;

                //Console.WriteLine(previousMove +" "+lastMove);
                if (previousMove == lastMove)
                {
                    score -= 1 * turn;
                }

            }

            // material score
            foreach (PieceType type in values.Keys)
            {
                score += board.GetPieceList(type, true).Count() * values[type];
                score -= board.GetPieceList(type, false).Count() * values[type];
            }



            // checkmate and draw score
            if (board.IsInCheckmate())
            {
                score += 1000 * turn;
            }

            // check check check check check mate
            if (board.IsInCheck())
            {
                score += 1 * turn;
            }


            // add position to table
            seenPositions.Add(board.ZobristKey, score);

            return score;

        }
    }


    private Node CreateTree(Board board, int depth, Node rootNode)
    {
        if (depth > 0)
        {
            Move[] moves = board.GetLegalMoves();

            moves = FilterMoves(moves, board);

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int eval = Evaluate(board);


                rootNode.Children.Add(
                    CreateTree(board, depth - 1, new Node(rootNode, eval, move)) // recursive call for children
                );

                board.UndoMove(move);
            }

            if (rootNode.Children.Count > 0)
            {
                rootNode.Children.Sort(new NodeCompare()); // compares in ascending eval order (white best move first)

                if (board.IsWhiteToMove) // if white reverse (black best move first)
                {
                    rootNode.Children.Reverse();
                }
                rootNode.eval = rootNode.Children[0].eval;
            }

        }
        return rootNode;

    }

    private Move[] FilterMoves(Move[] moves, Board board)
    {
        List<Move> Hpriority = new List<Move>();
        List<Move> Lpriority = new List<Move>();

        foreach (Move move in moves)
        {
            
            board.MakeMove(move);

            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return new Move[] { move }; ;
            }

            if (move.IsPromotion || board.IsInCheck() || move.IsCastles || move.IsCapture )
            {
                Hpriority.Add(move);
            }
            else
            {
                Lpriority.Add(move);
            }

            board.UndoMove(move);
        }

        // concatenate high-priority moves and low-priority moves
        Move[] filteredMoves = Hpriority.Concat(Lpriority).ToArray();

        return filteredMoves;
    }

    private int[,] createHeatGrid(double maxHeat, double radius)
    {
        int[,] heatGrid = new int[8, 8];
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                double x = i;
                double y = j;
                heatGrid[i, j] = (int)HeatDistributionFunction(x, y, maxHeat, 4, 4, radius);
            }
        }

        return heatGrid;    
    }

}