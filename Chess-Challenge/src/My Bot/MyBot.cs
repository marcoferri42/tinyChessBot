using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Syntax;


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
    //private static string winPath = "..\\..\\..\\src\\My Bot\\pgn\\maps\\";
    private static string linuxPath = "src/My Bot/pgn/maps/";


    private static Dictionary<PieceType, (Dictionary<String, int>, Dictionary<String, int>)> positionalMaps = 
        new Dictionary<PieceType, (Dictionary<String, int>, Dictionary<String, int>)>()
        {
            { PieceType.Queen, 
                (ReadMapFromFile(linuxPath + "whiteQMap.txt"),
                 ReadMapFromFile(linuxPath + "blackQMap.txt")
                )
            },
            { PieceType.Bishop,
                (ReadMapFromFile(linuxPath + "whiteBMap.txt"),
                 ReadMapFromFile(linuxPath + "blackBMap.txt")
                )
            },
            { PieceType.Knight,
                (ReadMapFromFile(linuxPath + "whiteNMap.txt"),
                 ReadMapFromFile(linuxPath + "blackNMap.txt")
                )
            },
            { PieceType.Rook,
                (ReadMapFromFile(linuxPath + "whiteRMap.txt"),
                 ReadMapFromFile(linuxPath + "blackRMap.txt")
                )
            },
            { PieceType.King,
                (ReadMapFromFile(linuxPath + "whiteKMap.txt"),
                 ReadMapFromFile(linuxPath + "blackKMap.txt")
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
        System.Console.WriteLine(tree.child.move + "...." + tree.child.eval + "...." + timer.MillisecondsElapsedThisTurn + " ms");

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
            seenPositions.TryAdd(board.ZobristKey, score);
            return score;
        }

        if (board.IsDraw())
        {
            score = 0;
            seenPositions.TryAdd(board.ZobristKey, score);
            return score;
        }

        if (board.IsInCheck())
        {
            score += 10 * turn;
        }

        var seenTypes = new List<PieceType>();
        var pieces = board.GetAllPieceLists().SelectMany(p => p).ToList();
        foreach (Piece piece in pieces)
        {   
            var sum = BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, piece.IsWhite))/2; // valore attacchi
            score += piece.IsWhite ? sum : -sum;
            
            if (!seenTypes.Contains(piece.PieceType))
            {
                var w = BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(piece.PieceType, true)) * values[piece.PieceType];    // valore materiale
                var b = BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(piece.PieceType, false)) * values[piece.PieceType];
                score += w-b;
                seenTypes.Add(piece.PieceType);
            }
            
            if (piece.PieceType != PieceType.Pawn)
            {
                score += piece.IsWhite ?
                        positionalMaps[piece.PieceType].Item1[piece.Square.Name] :  // valore posizionale
                        positionalMaps[piece.PieceType].Item2[piece.Square.Name];
            }
        }

        int currentmoves = board.GetLegalMoves().Count();
        if (board.TrySkipTurn())
        {
            int opponentmoves = board.GetLegalMoves().Count();
            score += (currentmoves > opponentmoves) ? turn : 0; // incentivo a limitare mosse nemico
            score += currentmoves * turn; // incentivo masssimizzare numero mosse
            
            board.UndoSkipTurn();
        }

        if (gameHistory.Count() >= 3)
        {
            if (gameHistory.Last().IsCastles) // incentivo castling
            {
                score += 100 * turn;
            }
            
            if (gameHistory[^3].TargetSquare == gameHistory.Last().StartSquare) // one move rule
            {
                score -= 10 * turn;
            }

            /*
            var lastMoveStart = gameHistory.Last().StartSquare;
            var lastMoveTarget = gameHistory.Last().TargetSquare;
            var king = board.GetKingSquare(!board.IsWhiteToMove);
            if (Math.Abs(lastMoveStart.File - king.File) > Math.Abs(lastMoveTarget.File - king.File) || // incentivo se mi avvicino al re 
                Math.Abs(lastMoveTarget.Rank - king.Rank) > Math.Abs(lastMoveStart.Rank - king.Rank))
            {
                score += 25 * turn;
            }*/
        }


        // add position to table
        seenPositions.TryAdd(board.ZobristKey, score);

        return score;
    }

    private Node AlphaB(int alpha, int beta, Board board, int depth, Node rootNode)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
        {
            UpdateTreePath(rootNode);
            return rootNode;
        }

        var moves = PrioritizeMoves(board.GetLegalMoves(), board);

        if (board.IsWhiteToMove) // maximizing
        {
            Node max = new Node(rootNode, int.MinValue, new Move(), board);


            foreach (Move move in moves)
            {
                board.MakeMove(move);
                
                Node child = AlphaB(alpha, beta, board, depth - 1, new Node(rootNode, Evaluate(board), move, board)); // recursive call for children

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

    private HashSet<Move> PrioritizeMoves(Move[] PossibleMoves, Board board)
    {
        var check = new Dictionary<Move, bool>();
        foreach (Move move in PossibleMoves)
        {
            board.MakeMove(move);
            check[move] = board.IsInCheck() || board.IsInCheckmate();
            board.UndoMove(move);
        }

        var MoveSet = PossibleMoves.ToHashSet();

        MoveSet.OrderBy(m => (  check[m] ||
                                m.IsCastles ||
                                m.IsCapture ||
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
