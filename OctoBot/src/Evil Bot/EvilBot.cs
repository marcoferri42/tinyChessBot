using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.CodeAnalysis;
using System.Collections;


public class N // classetta nodo custom
{
    public int eval { get; set; }

    public Move move { get; set; }

    public Board board { get; set; }

    public N parent { get; set; }

    public N child { get; set; }


    public N() { }

    public N(N parent, int eval, Move move, Board b)
    {
        this.parent = parent;
        this.eval = eval;
        this.move = move;
        this.board = b;
    }
}


public class EvilBot : IChessBot
{
    private static string winPath = "..\\..\\..\\src\\My Bot\\pgn\\maps\\";
    private static string linuxPath = "src/My Bot/pgn/maps/";


    private static Dictionary<PieceType, (Dictionary<String, int>, Dictionary<String, int>)> positionalMaps =
        new Dictionary<PieceType, (Dictionary<String, int>, Dictionary<String, int>)>()
        {
            { PieceType.Queen,
                (ReadMapFromFile(winPath + "whiteQMap.txt"),
                 ReadMapFromFile(winPath + "blackQMap.txt")
                )
            },
            { PieceType.Bishop,
                (ReadMapFromFile(winPath + "whiteBMap.txt"),
                 ReadMapFromFile(winPath + "blackBMap.txt")
                )
            },
            { PieceType.Knight,
                (ReadMapFromFile(winPath + "whiteNMap.txt"),
                 ReadMapFromFile(winPath + "blackNMap.txt")
                )
            },
            { PieceType.Rook,
                (ReadMapFromFile(winPath + "whiteRMap.txt"),
                 ReadMapFromFile(winPath + "blackRMap.txt")
                )
            },
            { PieceType.King,
                (ReadMapFromFile(winPath + "whiteKMap.txt"),
                 ReadMapFromFile(winPath + "blackKMap.txt")
                )
            }
        };

    private Dictionary<PieceType, int> values = new Dictionary<PieceType, int>() { // pieces values
            { PieceType.Pawn, 100 },
            { PieceType.Bishop, 325 },
            { PieceType.Knight, 300 },
            { PieceType.Queen, 900 },
            { PieceType.Rook, 500 },
            { PieceType.King, 100000000 }
        };

    private Dictionary<ulong, int> seenPositions = new Dictionary<ulong, int>(); // positions table


    public void CliThink(string fen, string[] moves)
    {
        var list = moves.ToList();
        var Moves = new ArrayList();
        var start = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        fen = fen == "startpos" ? start : fen;

        Board board = Board.CreateBoardFromFEN(fen);

        foreach (string move in list)
        {
            Moves.Add(new Move(move, board));
        }

        foreach (Move mv in Moves)
        {
            board.MakeMove(mv);
            board.UndoMove(mv);
        }
    }


    public String Think(Board board, Move mv)
    {
        int depth = 4;
        N tree = new N();
        tree.move = mv;
        for (int i = 0; i < depth; i++) // iterative deepening
        {
            //AlphaB(int.MinValue, int.MaxValue, board, i, tree);
            Console.WriteLine("info depth " + i + " score cp " + tree.eval + " time 0 nodes 42 nps 69 pv " + tree.move.ToString());
        }

        return tree.child.move.ToString();
    }
    public Move Think(Board board, Timer tm)
    {
        if (board.GetLegalMoves().Count() == 1)
        {
            return board.GetLegalMoves()[0];
        }
        int depth = 4;
        N tree = new N();
        for (int i = 0; i < depth; i++) // iterative deepening
        {
            AlphaB(int.MinValue, int.MaxValue, board, i, tree);
            //Console.WriteLine("info depth " + i + " score cp " + tree.eval + " time 0 nodes 42 nps 69 pv " + tree.move.ToString());
        }
        //Console.WriteLine("EVIL " +tree.child.eval + "  " + tree.child.move);
        return tree.child.move;
    }

    private int MaterialEval(Board board, int depth)
    {
        int score = 0;
        foreach (PieceType type in values.Keys)
        {
            int whitePiecesValue = BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(type, true)) * values[type];
            int blackPiecesValue = BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(type, false)) * values[type];
            score += (whitePiecesValue - blackPiecesValue);
        }
        return score;
    }

    private int BoardEval(Board board, int depth)
    {
        int score = 0;
        int turn = board.IsWhiteToMove ? -1 : 1;
        List<Move> gameHistory = board.GameMoveHistory.ToList();
        int nlegalMoves = board.GetLegalMoves().Count();

        if (seenPositions.ContainsKey(board.ZobristKey))
        {
            return seenPositions[board.ZobristKey];
        }

        if (board.IsInCheckmate())
        {
            score = (100000000 + depth) * turn;
            seenPositions.TryAdd(board.ZobristKey, score);
            return score;
        }

        if (board.IsDraw())
        {
            seenPositions.TryAdd(board.ZobristKey, 0);
            return 0;
        }

        if (board.IsInCheck())
        {
            score += 20 * turn;
        }

        foreach (Piece piece in board.GetAllPieceLists().SelectMany(p => p))
        {
            int attackValue = BitboardHelper.GetNumberOfSetBits(
                BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, piece.IsWhite));

            score += attackValue * 2 * (piece.IsWhite ? 1 : -1);
        }

        if (board.TrySkipTurn())
        {
            score += (nlegalMoves - board.GetLegalMoves().Count()) / 5;
            board.UndoSkipTurn();
        }

        if (gameHistory.Count >= 3 && gameHistory[^3].TargetSquare == gameHistory.Last().StartSquare)
        {
            score -= 500 * turn;
        }

        seenPositions.TryAdd(board.ZobristKey, score);
        return score;
    }

    private int MoveEval(Board board, Move move, bool isWhitetoMove)
    {
        int turn = isWhitetoMove ? -1 : 1;
        int eval = 0;

        if (move.IsCastles)
        {
            eval += 100 * turn;  // Castling is generally positive, adjust value as needed
        }
        if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
        {
            eval -= 15 * turn;
        }
        var (xKing, yKing) = (board.GetKingSquare(!isWhitetoMove).Index, board.GetKingSquare(!isWhitetoMove).Rank);

        return eval;
    }

    private N AlphaB(int alpha, int beta, Board board, int depth, N rootNode)
    {
        if (depth == 0)
        {
            UpdateTreePath(rootNode, depth);
            return rootNode;
        }

        var moves = PrioritizeMoves(board.GetLegalMoves(), board);

        if (board.IsWhiteToMove) // maximizing
        {
            N max = new N(rootNode, int.MinValue, new Move(), board);


            foreach (Move move in moves)
            {
                board.MakeMove(move);

                var eval = BoardEval(board, depth - 1) + MoveEval(board, move, board.IsWhiteToMove) + MaterialEval(board, depth - 1);
                var child = new N(rootNode, eval, move, board);

                AlphaB(alpha, beta, board, depth - 1, child); // recursive call for children

                board.UndoMove(move);

                if (max.eval < child.eval)
                {
                    max = child;
                }

                alpha = Math.Max(alpha, max.eval);

                if (beta <= alpha)
                {
                    break;
                }

            }

            rootNode.child = max;
            UpdateTreePath(rootNode.child, depth);

            return rootNode;
        }
        else // minimizing
        {
            N min = new N(rootNode, int.MaxValue, new Move(), board);

            foreach (Move move in moves)
            {
                board.MakeMove(move);

                var eval = BoardEval(board, depth - 1) + MoveEval(board, move, board.IsWhiteToMove) + MaterialEval(board, depth - 1);
                var child = new N(rootNode, eval, move, board);
                //Console.WriteLine("info depth " + depth + " score cp " + eval + " time 0 nodes 42 nps 69 pv e2e4");

                AlphaB(alpha, beta, board, depth - 1, child); // recursive call for children

                board.UndoMove(move);

                if (min.eval > child.eval)
                {
                    min = child;
                }

                beta = Math.Min(beta, min.eval);

                if (beta <= alpha)
                {
                    break;
                }
            }

            rootNode.child = min;
            UpdateTreePath(rootNode.child, depth);

            return rootNode;
        }
    }

    private void UpdateTreePath(N node, int depth)
    {
        if (node.child != null)
        {
            node.eval = node.child.eval;
        }
        if (node.parent != null)
        {
            UpdateTreePath(node.parent, depth); // recursively update the parent node
        }
    }

    private HashSet<Move> PrioritizeMoves(Move[] PossibleMoves, Board board)
    {

        var MoveSet = PossibleMoves.ToHashSet();

        MoveSet.OrderBy(m => (m.IsCastles ||
                                (m.IsCapture && values[m.CapturePieceType] > values[m.MovePieceType]) ||
                                m.IsPromotion ||
                                m.PromotionPieceType == PieceType.Queen ||
                                m.IsEnPassant
                            ) ? 0 : 1);

        return MoveSet;
    }

    private void Logging(String filename, String log)
    {
        //File.AppendAllText("C:\\Users\\usr\\source\\repos\\tinyChessBot\\Chess-Challenge\\src\\My Bot\\" + filename, log); // finestre
        //File.AppendAllText("/home/hos/Desktop/proj/tinyChessBot/Chess-Challenge/src/My Bot/" + filename, log);    // linux
    }

    private static Dictionary<string, int> ReadMapFromFile(string fileName)
    {
        Dictionary<string, int> res = new Dictionary<string, int>();
        var lines = File.ReadLines(fileName);
        foreach (var line in lines)
        {
            var lineList = line.Split(':').ToList();
            lineList.Remove(":");

            if (lineList.Count() > 1)
            {
                res.Add(lineList[0], int.Parse(lineList[1]));
            }
        }
        return res;
    }

}
