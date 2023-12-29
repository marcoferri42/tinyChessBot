using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.CodeAnalysis;
using System.Collections;


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


public class OctoBot
{
    private static string winPath = "..\\..\\..\\src\\My Bot\\pgn\\maps\\";
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


    public void CliThink(string fen, string[] moves)
    {
        var list = moves.ToList();
        var Moves = new ArrayList();
        var start = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        fen = fen =="startpos" ? start : fen;
        
        Board board = Board.CreateBoardFromFEN(fen);

        foreach(string move in list){
            Moves.Add(new Move(move, board));
            Console.WriteLine(new Move(move, board));
        }

        Console.WriteLine(Moves.Count);
        foreach(Move mv in Moves)
        {
            board.MakeMove(mv);
            Console.WriteLine(Think(board, mv));
            board.UndoMove(mv);
        }
    }


    public String Think(Board board, Move mv)
    {
        int depth = 4;
        Node tree = new Node();
        tree.move = mv;
        for (int i = 0; i < depth; i++) // iterative deepening
        {
            AlphaB(int.MinValue, int.MaxValue, board, i, tree);
            Console.WriteLine("info depth " + i + " score cp " + tree.eval + " time 0 nodes 42 nps 69 pv "+ tree.move.ToString());
        }

        /*
        Logging("responsetimelog.txt", timer.MillisecondsElapsedThisTurn +","+ board.GetLegalMoves().Count() + "\n");
        System.Console.WriteLine(tree.child.eval);
        System.Console.WriteLine(pruned);
        */
        //System.Console.WriteLine(tree.child.move + "....boardeval" + tree.child.eval + "....moveeval" + MoveEval(tree.child.move, board.IsWhiteToMove) + "....in" + timer.MillisecondsElapsedThisTurn + " ms");

        return tree.child.move.ToString();
    }

    private int BoardEval (Board board, int depth)
    {
        int score = 0, turn = board.IsWhiteToMove ? -1 : 1;
        List<Move> gameHistory = board.GameMoveHistory.ToList<Move>();

        if (seenPositions.ContainsKey(board.ZobristKey))
        {
            return seenPositions[board.ZobristKey];
        }

        if (board.IsInCheckmate())
        {
            
            score = (9999990+depth) * turn;
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
            score += 10 * turn;
        }

        var pieces = board.GetAllPieceLists().SelectMany(p => p).ToList();
        foreach (Piece piece in pieces)
        {   
            var sum = BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, piece.IsWhite)); // numero attacchi possibili pesati
            sum = sum * values[piece.PieceType]/10;
            score += sum * (piece.IsWhite ? 1 : -1);
            
            if (piece.PieceType != PieceType.Pawn)
            {
               // score += piece.IsWhite ?
                //        positionalMaps[piece.PieceType].Item1[piece.Square.Name]/2 :  // valore posizionale
                //        positionalMaps[piece.PieceType].Item2[piece.Square.Name]/2 ;
            }
        }

        foreach (PieceType type in values.Keys)
        {

            var w = BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(type, true)) * values[type];    // valore materiale
            var b = BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(type, false)) * values[type];

            score += w - b;
        }

        int currentmoves = board.GetLegalMoves().Count();
        if (board.TrySkipTurn())
        {
            int opponentmoves = board.GetLegalMoves().Count();
            score += (currentmoves > opponentmoves) ? turn : 0; // incentivo a limitare mosse nemico
            score += currentmoves/3 * turn; // incentivo masssimizzare numero mosse
            
            board.UndoSkipTurn();
        }

        if (gameHistory.Count() >= 3)
        {
            
            if (gameHistory[^3].TargetSquare == gameHistory.Last().StartSquare) // one move rule
            {
                score -= 10 * turn; // bilancia check
            }

        }

        // add position to table
        seenPositions.TryAdd(board.ZobristKey, score);

        return score;
    }


    private int MoveEval(Move move, bool isWhitetoMove)
    {
        var turn = isWhitetoMove ? -1 : 1;
        var eval = 0;

        if(move.IsCastles)
        {
            eval += 100;
        }
        if(move.IsCapture)
        { 
            eval -= values[move.MovePieceType] / 100;
            eval += (values[move.CapturePieceType] - values[move.MovePieceType])/5;
        }
        return eval * turn;
    }

    private Node AlphaB(int alpha, int beta, Board board, int depth, Node rootNode)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
        {
            UpdateTreePath(rootNode, depth);
            return rootNode;
        }

        var moves = PrioritizeMoves(board.GetLegalMoves(), board);

        if (board.IsWhiteToMove) // maximizing
        {
            Node max = new Node(rootNode, int.MinValue, new Move(), board);


            foreach (Move move in moves)
            {
                board.MakeMove(move);

                var eval = BoardEval(board, depth-1) + MoveEval(move, board.IsWhiteToMove);
                var child = new Node(rootNode, eval, move, board);
                
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
            rootNode.move = max.move;
            UpdateTreePath(rootNode.child, depth);

            return rootNode;
        }
        else // minimizing
        {
            Node min = new Node(rootNode, int.MaxValue, new Move(), board);

            foreach (Move move in moves)
            {
                board.MakeMove(move);

                var eval = BoardEval(board, depth-1) + MoveEval(move, board.IsWhiteToMove);
                var child = new Node(rootNode, eval, move, board);
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
            rootNode.move = min.move;
            UpdateTreePath(rootNode.child, depth);

            return rootNode;
        }
    }

    private void UpdateTreePath(Node node, int depth)
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

        MoveSet.OrderBy(m => (  m.IsCastles ||
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
