using ChessChallenge.API;
using ChessChallenge.Application.APIHelpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

public class Node // classetta nodo custom
{
    public bool isRoot = false;
    public int eval { get; set; }
    public Node root { get; set; }
    public Move move { get; set; }

    public Board board { get; set; }

    public List<Node> Children = new List<Node>();

    public Node()
    {
        this.eval = 0;
        this.isRoot = true;
    }

    public Node(Node root, int eval, Move move, Board b)
    {
        this.root = root;
        this.eval = eval;
        this.move = move;
        this.board = b;
        this.isRoot = false;
    }

    public List<Node> GetOrderedChildren()
    {
        this.Children = Children.OrderBy(child => child.eval).ToList();
        return this.Children;
    }
}
public class MyBot : IChessBot
{
    public int minimax;

    private Dictionary<PieceType, int> values = new Dictionary<PieceType, int>() { // pieces values
            { PieceType.Pawn, 100 },
            { PieceType.Bishop, 300 },
            { PieceType.Knight, 300 },
            { PieceType.Queen, 900 },
            { PieceType.Rook, 500 },
            { PieceType.King, 1000000 }
        };

    public Dictionary<ulong, int> seenPositions = new Dictionary<ulong, int>(); // positions table

    public Move Think(Board board, Timer timer)
    {
        Node tree = new Node();

        CreateTree(board, 4, tree);

        System.Console.WriteLine(board.GameMoveHistory.Count()/2 + " " + tree.Children[0].eval);

        return tree.Children[0].move;
    }

    public int Evaluate(Board board)
    {
        int score = 0, turn = board.IsWhiteToMove ? -1 : 1;

        if (board.IsInCheckmate())
        {
            return 1000000 * turn;
        }

        if (seenPositions.ContainsKey(board.ZobristKey))
        {
            return seenPositions[board.ZobristKey];
        }

        if (board.IsDraw())
        {
            return 0;
        }

        if (board.IsInCheck())
        {
            score += 5 * turn;
        }

        // material value
        foreach (PieceList list in board.GetAllPieceLists())
        {
            foreach (Piece piece in list)
            {
                score += values[piece.PieceType] * (piece.IsWhite ? 1 : -1); // material value
                if (board.SquareIsAttackedByOpponent(piece.Square))
                {
                    score -= 5 * (piece.IsWhite ? 1 : -1);
                }
            }

        }

        // possible moves
        int opponent = board.GetLegalMoves().Count();
        int opponentcap = board.GetLegalMoves(true).Count();

        if (board.TrySkipTurn())
        {
            int currentmoves = board.GetLegalMoves().Count();
            int currentcaptures = board.GetLegalMoves(true).Count();

            score += (currentmoves > currentcaptures ? 1 : 0) * turn;
            // EVAL CAPTURES

            score += (currentcaptures > opponentcap ? 1 : 0) * turn;

            board.UndoSkipTurn();
        }

        // add position to table
        seenPositions.Add(board.ZobristKey, score);

        return score;
    }

    private Node CreateTree(Board board, int depth, Node rootNode)
    {
        if (depth > 0)
        {
            Move[] moves = board.GetLegalMoves();

            int eval = 0;

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                eval = Evaluate(board);

                rootNode.Children.Add
                (
                    CreateTree(board, depth - 1, new Node(rootNode, eval, move, board)) // recursive call for children
                );

                board.UndoMove(move);
            }


            if (rootNode.Children.Count() > 0)
            {
                rootNode.Children = rootNode.GetOrderedChildren(); // orders children ascending order
                //System.Console.WriteLine(rootNode.Children[0].eval);
                if (board.IsWhiteToMove)
                {
                    rootNode.Children.Reverse();
                }
                rootNode.eval = rootNode.Children[0].eval;

                UpdateTreePath(rootNode);
            }
        }
        return rootNode;
    }

    private void UpdateTreePath(Node node)
    {
        // Assuming `eval` is a property that should be updated, and each node has a reference to its parent.
        if (node.Children != null && node.Children.Count > 0)
        {
            node.eval = node.Children.First().eval; // Update the current node's eval with the first child's eval.

            // Check if the node is not the root before making the recursive call
            if (node.root != null)
            {
                UpdateTreePath(node.root); // Recursively update the parent node
            }
        }
    }

}
