using ChessChallenge.API;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
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

    public Move Think(Board board, Timer t)
    {
        botColor = board.IsWhiteToMove ? 1 : -1;

        Node tree = new Node();

        for (int i = 1; i < 4; i++) // itereative deepening TODO da studiare bene ! 
        {
            tree = CreateTree(board, i, new Node());
        }

        //Console.WriteLine(board.GetLegalMoves().Count());
        //Console.WriteLine(tree.Children.Count());

        //Console.WriteLine(tree.Children[0].eval);             <---------- evaluation print
        return tree.Children[0].lastMove;
    }

    private (int, bool) Evaluate(Board board)
    {
        int score = 0, nPieces = 0;
        bool transposition = seenPositions.ContainsKey(board.ZobristKey);

        if (transposition)
        {
            return (seenPositions[board.ZobristKey], true);
        }
        else
        {
            // Material score
            foreach (PieceType type in values.Keys)
            {
                nPieces += board.GetPieceList(type, true).Count();
                nPieces += board.GetPieceList(type, false).Count();
                score += board.GetPieceList(type, true).Count() * values[type];
                score -= board.GetPieceList(type, false).Count() * values[type];
            }

            // checkmate and draw score
            if (board.IsInCheckmate())
            {
                score += 1000;
            }

            if (board.IsInStalemate() || board.IsRepeatedPosition() || board.IsFiftyMoveDraw())
            {
                score -= 2000;
            }

            // Is in check and few pieces on the board
            if (nPieces < 16 && board.IsInCheck())
            {
                score += 100;
            }

            // add position to table
            seenPositions.Add(board.ZobristKey, score);

            return (score, false);

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
                (int eval, bool isTransposition) = Evaluate(board);

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

            if (move.IsPromotion || move.IsEnPassant || board.IsInCheck() || board.IsInCheckmate()) // todo add minimization opponent moves
            {
                Hpriority.Add(move);
            }
            else
            {
                Lpriority.Add(move);
            }

            board.UndoMove(move);
        }

        // Concatenate high-priority moves and low-priority moves
        Move[] filteredMoves = Hpriority.Concat(Lpriority).ToArray();

        return filteredMoves;
    }

}