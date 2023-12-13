using ChessChallenge.API;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

public class Node // classetta nodo custom
{
    public int eval { get; set; }
    public Node root { get; set; }
    public Move move { get; set; }

    public Node child { get; set; }

    public Board board { get; set; }

    public Node() { }

    public Node(Node root, int eval, Move move, Board b)
    {
        this.root = root;
        this.eval = eval;
        this.move = move;
        this.board = b;
    }
}
public class MyBot : IChessBot
{
    private Dictionary<PieceType, int> values = new Dictionary<PieceType, int>() { // pieces values
            { PieceType.Pawn, 100 },
            { PieceType.Bishop, 320 },
            { PieceType.Knight, 300 },
            { PieceType.Queen, 900 },
            { PieceType.Rook, 500 },
            { PieceType.King, 1000000 }
        };

    public Dictionary<ulong, int> seenPositions = new Dictionary<ulong, int>(); // positions table

    public Move Think(Board board, Timer timer)
    {
        Node tree = new Node();

        AlphaB(int.MinValue, int.MaxValue, board, 5, tree);
        
        /*
        System.Console.WriteLine(tree.child.move);
        System.Console.WriteLine(tree.child.eval);
        System.Console.WriteLine(pruned);
        */

        return tree.child.move;
    }

    public int Evaluate(Board board)
    {
        int score = 0, turn = board.IsWhiteToMove ? -1 : 1;
        List<Move> gameHistory = board.GameMoveHistory.ToList<Move>();

        if (board.IsInCheckmate())
        {
            return int.MaxValue * turn;
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
            score += 50 * turn;
        }

        // material value
        foreach (PieceList list in board.GetAllPieceLists())
        {
            foreach (Piece piece in list)
            {
                score += values[piece.PieceType] * (piece.IsWhite ? 1 : -1); // material value
            }
        }


        int currentmoves = board.GetLegalMoves().Count();
        if (board.TrySkipTurn())
        {
            int opponentmoves = board.GetLegalMoves().Count();
            score += (currentmoves > opponentmoves) ? turn : 0; // incentivo a limitare mosse nemico
            board.UndoSkipTurn();
        }

        if (gameHistory.Count() >= 3)
        {
            if (gameHistory.Last().IsCastles) // incentivo castling
            {
                score += 50 * turn;
            }
            Square lastMoveStart = gameHistory.Last().StartSquare;
            Square lastMoveTarget = gameHistory.Last().TargetSquare;

            Square king = board.GetKingSquare(!board.IsWhiteToMove);

            // incentivo se mi avvicino al re 
            if (Math.Abs(lastMoveStart.File - king.File) > Math.Abs(lastMoveTarget.File - king.File) ||
                Math.Abs(lastMoveTarget.Rank - king.Rank) > Math.Abs(lastMoveStart.Rank - king.Rank))
            {
                score += 5 * turn;
            }
            
            // one move rule
            if (gameHistory[^3].TargetSquare == gameHistory.Last().StartSquare)
            {
                score -= 10 * turn;
            }
        }

        if (board.GameRepetitionHistory.Count() >= 1 && Math.Sign(score) == Math.Sign(turn*-1))
        {
            score -= 100 * turn;
        }

        // add position to table
        seenPositions.Add(board.ZobristKey, score);

        return score;
    }

    private Node AlphaB(int alpha, int beta, Board board, int depth, Node rootNode)
    {
        if (depth == 0)
        {
            return rootNode;
        }

        Move[] moves = board.GetLegalMoves();

        if (board.IsWhiteToMove) // maximizing
        {
            Node max = new Node(rootNode, int.MinValue, new Move(), board);
            
            foreach (Move move in moves)
            {
                board.MakeMove(move);

                Node child = AlphaB(alpha, beta, board, depth - 1, new Node(rootNode, Evaluate(board), move, board)); // recursive call for children

                if (max.eval < child.eval)
                {
                    max = child;
                }
                
                alpha = Math.Max(alpha, max.eval);

                if (beta <= alpha)
                {
                    board.UndoMove(move);
                    break;
                }

                board.UndoMove(move); 
            }

            rootNode.child = max;
            UpdateTreePath(rootNode.child);
            
            return rootNode;
        }
        else // minimizing
        {
            Node min = new Node(rootNode, int.MaxValue, new Move(), board);
            
            foreach (Move move in moves)
            {
                board.MakeMove(move);

                Node child = AlphaB(alpha, beta, board, depth - 1, new Node(rootNode, Evaluate(board), move, board)); // recursive call for children

                if (min.eval > child.eval)
                {
                    min = child;
                }

                beta = Math.Min(beta, min.eval);

                if (beta <= alpha)
                {
                    board.UndoMove(move); 
                    break;
                }

                board.UndoMove(move); 
            }

            rootNode.child = min;
            UpdateTreePath(rootNode.child);
            
            return rootNode;
        }
    }

    private void UpdateTreePath(Node node)
    {
        if (node.child != null)
        {
            node.eval = node.child.eval;
        }
        if (node.root != null)
        {
            UpdateTreePath(node.root); // Recursively update the parent node
        }
    }

}
