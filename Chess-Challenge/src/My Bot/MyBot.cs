using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


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
    private static Dictionary<PieceType, (Dictionary<String, int>, Dictionary<String, int>)> positionalMaps = 
        new Dictionary<PieceType, (Dictionary<String, int>, Dictionary<String, int>)>()
        {
            { PieceType.Queen, 
                (ReadMapFromFile("..\\..\\..\\src\\My Bot\\pgn\\maps\\whiteQMap.txt"),
                 ReadMapFromFile("..\\..\\..\\src\\My Bot\\pgn\\maps\\blackQMap.txt")
                )
            },
            { PieceType.Bishop,
                (ReadMapFromFile("..\\..\\..\\src\\My Bot\\pgn\\maps\\whiteBMap.txt"),
                 ReadMapFromFile("..\\..\\..\\src\\My Bot\\pgn\\maps\\blackBMap.txt")
                )
            },
            { PieceType.Knight,
                (ReadMapFromFile("..\\..\\..\\src\\My Bot\\pgn\\maps\\whiteNMap.txt"),
                 ReadMapFromFile("..\\..\\..\\src\\My Bot\\pgn\\maps\\blackNMap.txt")
                )
            },
            { PieceType.Rook,
                (ReadMapFromFile("..\\..\\..\\src\\My Bot\\pgn\\maps\\whiteRMap.txt"),
                 ReadMapFromFile("..\\..\\..\\src\\My Bot\\pgn\\maps\\blackRMap.txt")
                )
            },
            { PieceType.King,
                (ReadMapFromFile("..\\..\\..\\src\\My Bot\\pgn\\maps\\whiteKMap.txt"),
                 ReadMapFromFile("..\\..\\..\\src\\My Bot\\pgn\\maps\\blackKMap.txt")
                )
            }
        };

    private Dictionary<PieceType, int> values = new Dictionary<PieceType, int>() { // pieces values
            { PieceType.Pawn, 100 },
            { PieceType.Bishop, 325 },
            { PieceType.Knight, 300 },
            { PieceType.Queen, 900 },
            { PieceType.Rook, 500 },
            { PieceType.King, 1000000 }
        };

    private Dictionary<ulong, int> seenPositions = new Dictionary<ulong, int>(); // positions table

    public Move Think(Board board, Timer timer)
    {
        int depth = 4;
        Node tree = new Node();

        for (int i = 0; i < depth; i++) // iterative deepening
        {
            AlphaB(int.MinValue, int.MaxValue, board, i, tree);
        }

        /*
        Logging("responsetimelog.txt", timer.MillisecondsElapsedThisTurn +","+ board.GetLegalMoves().Count() + "\n");
        System.Console.WriteLine(tree.child.eval);
        System.Console.WriteLine(pruned);
        */
        System.Console.WriteLine(timer.MillisecondsElapsedThisTurn + " ms");
        System.Console.WriteLine(tree.child.move + "...." + tree.child.eval);

        return tree.child.move;
    }

    private int Evaluate(Board board)
    {
        int score = 0, turn = board.IsWhiteToMove ? -1 : 1;
        List<Move> gameHistory = board.GameMoveHistory.ToList<Move>();

        if (seenPositions.ContainsKey(board.ZobristKey))
        {
            return seenPositions[board.ZobristKey];
        }

        if (board.IsInCheckmate())
        {
            
            score = 1000000000 * turn;
            seenPositions.Add(board.ZobristKey, score);
            return score;
        }

        if (board.IsDraw())
        {
            score = 0;
            seenPositions.Add(board.ZobristKey, score);
            return score;
        }

        if (board.IsInCheck())
        {
            score += 5 * turn;
        }

        foreach (PieceType type in values.Keys)
        {
            score += BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(type, true)) * values[type];      // white
            score += BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(type, false)) * - values[type];  // 
        }

        var pieces = board.GetAllPieceLists().SelectMany(p => p).ToList();
        foreach (Piece piece in pieces)
        {
            var sum = BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, piece.IsWhite)); // valore materiale
            score += piece.IsWhite ? sum : -sum;
            
            
            if (piece.PieceType != PieceType.Pawn)
            {
                score += piece.IsWhite ?
                        positionalMaps[piece.PieceType].Item1[piece.Square.Name] :  // valore posizionale
                        positionalMaps[piece.PieceType].Item2[piece.Square.Name];
            }

        }

        if (board.TrySkipTurn())
        {
            int currentmoves = board.GetLegalMoves().Count();
            int opponentmoves = board.GetLegalMoves().Count();
            score += (currentmoves > opponentmoves) ? turn : 0; // incentivo a limitare mosse nemico
            board.UndoSkipTurn();
        }

        if (gameHistory.Count() >= 3)
        {
            if (gameHistory.Last().IsCastles) // incentivo castling
            {
                score -= 50 * turn;
            }
            
            if (gameHistory[^3].TargetSquare == gameHistory.Last().StartSquare) // one move rule
            {
                score -= 10 * turn;
            }

            var lastMoveStart = gameHistory.Last().StartSquare;
            var lastMoveTarget = gameHistory.Last().TargetSquare;
            var king = board.GetKingSquare(!board.IsWhiteToMove);
            if (Math.Abs(lastMoveStart.File - king.File) > Math.Abs(lastMoveTarget.File - king.File) || // incentivo se mi avvicino al re 
                Math.Abs(lastMoveTarget.Rank - king.Rank) > Math.Abs(lastMoveStart.Rank - king.Rank))
            {
                score += 100 * turn;
            }
        }


        // add position to table
        seenPositions.Add(board.ZobristKey, score);

        return score;
    }

    private Node AlphaB(int alpha, int beta, Board board, int depth, Node rootNode)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
        {
            return rootNode;
        }

        var moves = PrioritizeMoves(board.GetLegalMoves());

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
