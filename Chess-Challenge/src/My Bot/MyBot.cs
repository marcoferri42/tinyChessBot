using ChessChallenge.API;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

public class Node // classetta nodo custom
{
    public int eval { get; set; }

    public Move move { get; set; }

    public Board board { get; set; }

    public Node parent { get; set; }

    public Node child { get; set; }


    public Node() { }

    public Node(Node parent, int eval, Move move, Board b)
    {
        this.parent = parent;
        this.eval = eval;
        this.move = move;
        this.board = b;
    }
}

public class MyBot : IChessBot
{

    private Dictionary<PieceType, int> values = new Dictionary<PieceType, int>() { // pieces values
            { PieceType.Pawn, 100 },
            { PieceType.Bishop, 325 },
            { PieceType.Knight, 300 },
            { PieceType.Queen, 900 },
            { PieceType.Rook, 500 },
            { PieceType.King, 1000000 }
        };

    public Dictionary<ulong, int> seenPositions = new Dictionary<ulong, int>(); // positions table

    public Move Think(Board board, Timer timer)
    {
        Node tree = new Node();

        AlphaB(int.MinValue, int.MaxValue, board, 3, tree);

        //Logging("responsetimelog.txt", timer.MillisecondsElapsedThisTurn+","+board.GetLegalMoves().Count()+",\n");
        Logging("boardevaluationlog.txt", board.GetHashCode() + "," + Evaluate(board) + ",\n");

        System.Console.WriteLine(timer.MillisecondsElapsedThisTurn + " ms");

        /*
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
            return 1000000000 * turn;
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

        if (board.HasKingsideCastleRight(board.IsWhiteToMove) ||
            board.HasQueensideCastleRight(board.IsWhiteToMove)) // incentivo a non rompere diritto arrocco
        {
            score += 5 * turn;
        }

        // material value
        Parallel.ForEach(board.GetAllPieceLists(), list =>
        {
            Parallel.ForEach(list, piece =>
            {
                score += values[piece.PieceType] * (piece.IsWhite ? 1 : -1); // material value
            });
        });

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
                score += 100 * turn;
            }
            
            // one move rule
            if (gameHistory[^3].TargetSquare == gameHistory.Last().StartSquare)
            {
                score += -5 * turn;
            }
        }

        if (board.GameRepetitionHistory.Count() > 1 && Math.Sign(score) == Math.Sign(turn))
        {
            score += -50 * turn;
        }
            
        // add position to table
        seenPositions.Add(board.ZobristKey, score);

        return score;
    }

    private Node AlphaB(int alpha, int beta, Board board, int depth, Node rootNode)
    {
        if (depth == 0 || board.IsInCheckmate())
        {
            return rootNode;
        }

        var moves = PrioritizeMoves(board.GetLegalMoves());

        if (board.IsWhiteToMove) // maximizing
        {
            Node max = new Node(rootNode, int.MinValue, new Move(), board);

            Parallel.ForEach(moves, (move, state) =>
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
                    state.Break();
                }

                board.UndoMove(move);
            });

            rootNode.child = max;
            UpdateTreePath(rootNode.child);
            
            return rootNode;
        }
        else // minimizing
        {
            Node min = new Node(rootNode, int.MaxValue, new Move(), board);

            Parallel.ForEach(moves, (move, state) =>
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
                    state.Break();
                }

                board.UndoMove(move);
            });

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
        if (node.parent != null)
        {
            UpdateTreePath(node.parent); // recursively update the parent node
        }
    }

    private HashSet<Move> PrioritizeMoves(Move[] PossibleMoves)
    {
        var MoveSet = PossibleMoves.ToHashSet();

        MoveSet.OrderBy(m => (m.IsCastles ||
                                 m.IsCapture ||
                                 m.IsPromotion ||
                                 m.PromotionPieceType == PieceType.Queen ||
                                 m.IsEnPassant) ? 0 : 1);

        return MoveSet;
    }

    private void Logging(String filename, String log)
    {
        File.AppendAllText("C:\\Users\\usr\\source\\repos\\tinyChessBot\\Chess-Challenge\\src\\My Bot\\logs\\" + filename, log);
    }

}
