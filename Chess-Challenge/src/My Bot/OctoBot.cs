using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.CodeAnalysis;
using System.Diagnostics;


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
    public string MoveHistoryToString()
    {
        var res = "";

        res += move.ToString() + " ";

        if(this.child != null)
        {
            res += this.child.MoveHistoryToString();
        }
        return res;
    }
}


public class OctoBot : IChessBot
{
    //private static Dictionary<(PieceType, string), int> positionalMaps = LoadPositionalMap(winPath);
    private Dictionary<PieceType, int> values = new Dictionary<PieceType, int>() { // pieces values
            { PieceType.Pawn, 120 },
            { PieceType.Bishop, 325 },
            { PieceType.Knight, 300 },
            { PieceType.Queen, 950 },
            { PieceType.Rook, 500 },
            { PieceType.King, 100000000 }
        };
    Dictionary<ulong, int> seenPositions = new Dictionary<ulong, int>(); // positions table
    private static string winPath = "..\\..\\..\\src\\My Bot\\pgn\\maps\\";
    public HashSet<ulong> hashPositions = new HashSet<ulong>();
    private static string linuxPath = "src/My Bot/pgn/maps/";
    private int nNodes = 0;




    //-------------------------------------------------------------------- START THINK
    public void CliThink(string fen, string[] moves)
    {
        var list = moves.ToList();
        var Moves = new List<Move>();
        var start = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        fen = fen =="startpos" ? start : fen;

        Board board = Board.CreateBoardFromFEN(fen);

        foreach(string move in list){
            Moves.Add(new Move(move, board));
        }

        foreach(Move mv in Moves)
        {
            board.MakeMove(mv);
        }
        Console.WriteLine("bestmove " + Think(board, Moves.Last()));
    }
    public string Think(Board board, Move mv)
    {
        Timer timer = new ChessChallenge.API.Timer(10000);
        Stopwatch stopWatch = new Stopwatch();
       
        if (board.GetLegalMoves().Count() == 1)
        {
            return board.GetLegalMoves()[0].ToString();
        }

        int depth = 4;
        Node tree = new Node();
        tree.move = mv;
        for (int i = 0; i < depth; i++) // iterative deepening
        {
            AlphaB(int.MinValue, int.MaxValue, board, i, tree);
            Console.WriteLine("info depth " + i + " score cp " + tree.eval + " time "+ timer.MillisecondsElapsedThisTurn + " nodes "+this.nNodes+" nps 69 pv "+ tree.MoveHistoryToString());
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
        Node tree = new Node();
        for (int i = 0; i < depth; i++) // iterative deepening
        {
            AlphaB(int.MinValue, int.MaxValue, board, i, tree);
            //Console.WriteLine("info depth " + i + " score cp " + tree.eval + " time 0 nodes 42 nps 69 pv " + tree.move.ToString());
        }
        //Console.WriteLine("GOOD " + tree.child.eval + "  " + tree.child.move);

        //Logging("logresponsetime4.txt", tm.MillisecondsElapsedThisTurn + "," + board.GetLegalMoves().Count() + "\n");

        return tree.child.move;
    }




    //-------------------------------------------------------------------- SEARCH
    private Node AlphaB(int alpha, int beta, Board board, int depth, Node rootNode)
    {
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
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

                if (hashPositions.Contains(board.ZobristKey))
                {
                    board.UndoMove(move);
                    continue;
                }

                var eval = Eval(board, depth - 1, move);
                var child = new Node(rootNode, eval, move, board);
                this.nNodes++;

                child = AlphaB(alpha, beta, board, depth - 1, child); // recursive call for children

                if(child.eval > max.eval)
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
            UpdateTreePath(rootNode, depth);
            return rootNode;
        }
        else // minimizing
        {
            Node min = new Node(rootNode, int.MaxValue, new Move(), board);

            foreach (Move move in moves)
            {
                board.MakeMove(move);

                if (hashPositions.Contains(board.ZobristKey))
                {
                    board.UndoMove(move);
                    continue;
                }

                var eval = Eval(board, depth - 1, move);
                var child = new Node(rootNode, eval, move, board);

                child = AlphaB(alpha, beta, board, depth - 1, child); // recursive call for children

                if (child.eval < min.eval)
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
            UpdateTreePath(rootNode, depth);
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
                                m.IsCapture ||
                                m.IsPromotion ||
                                values[board.GetPiece(m.StartSquare).PieceType] <= values[board.GetPiece(m.TargetSquare).PieceType]
                            ) ? 0 : 1);

        return MoveSet;
    }





    //-------------------------------------------------------------------- EVALUATION
    private int Eval(Board board, int depth, Move move)
    {
        return MaterialEval(board, depth) + BoardEval(board, depth) + MoveEval(board, move, board.IsWhiteToMove);
    }

    private int MaterialEval(Board board, int depth)
    {
        int score = 0;
        foreach (PieceType type in values.Keys)
        {
            int whitePiecesValue = BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(type, true)) * values[type];    // pezzi bianchi
            int blackPiecesValue = BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(type, false)) * values[type];   // pezzi neri
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

        if (hashPositions.Contains(board.ZobristKey))
        {
            return seenPositions[board.ZobristKey];     // posizione gia vista
        }
        if (board.IsInCheckmate())
        {
            score = (100000000 + depth) * turn;
            seenPositions.TryAdd(board.ZobristKey, score);  // checkmate
            return score;       
        }

        if (board.IsDraw())
        {
            seenPositions.TryAdd(board.ZobristKey, 0);      // patta
            return 0;
        }

        if (board.IsInCheck())
        {
            score += 20 * turn;     //check
        }

        foreach (Piece piece in board.GetAllPieceLists().SelectMany(p => p))
        {

            if (piece.PieceType == PieceType.Pawn)
            {
                score += piece.IsWhite ? piece.Square.Rank : -piece.Square.Rank - 7;    // peso pedoni per rank
            }

            int attackValue = BitboardHelper.GetNumberOfSetBits(
                BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, piece.IsWhite));   // attacchi possibili pezzi

            score += attackValue * (piece.IsWhite ? 1 : -1);

        }

        if (board.TrySkipTurn())
        {
            score += (nlegalMoves > board.GetLegalMoves().Count()) ? 10 * turn : -10 * turn;    // maximize moves e minimizza moves nemico
            board.UndoSkipTurn();
        }

        if (gameHistory.Count >= 3 && 
            gameHistory[^3].TargetSquare.Equals(gameHistory.Last().StartSquare) && 
            Math.Sign(score) == Math.Sign(turn))
        {
            score -= 30 * turn;         // ripetizione se sei in vantaggio = male
        }

        seenPositions.TryAdd(board.ZobristKey, score);
        return score;
    }

    private int MoveEval(Board board, Move move, bool isWhitetoMove)
    {
        int turn = isWhitetoMove ? -1 : 1;
        int score = 0;

        if (move.IsCastles)
        {
            score += 100 * turn; // incentivo castle
        }
        if (board.GameMoveHistory.Count() > 3 && 
            board.GameMoveHistory[^3].TargetSquare.Equals(move.StartSquare)) // one move rule
        {
            score -= 50 * turn;
        }

        return score;
    }




    //-------------------------------------------------------------------- POSITIONAL MAPS TESTING
    private static Dictionary<(PieceType, string), int> LoadPositionalMap(string path)
    {
        var res = new Dictionary<(PieceType, string), int>();
        var colors = new List<string> { "black", "white" };
        var pieces = new Dictionary<PieceType, string>
        {
            { PieceType.Bishop , "B" },
            { PieceType.Queen, "Q" },
            { PieceType.Rook, "R" },
            { PieceType.King, "K" },
            { PieceType.Knight, "N" }
        };


        foreach (PieceType type in pieces.Keys)
        {
            ReadMapFromFile(res, type, path + "black" + pieces[type] + ".txt");
            ReadMapFromFile(res, type, path + "white" + pieces[type] + ".txt");
        }
        return res;
    }
    private static void ReadMapFromFile(Dictionary<(PieceType, string), int> res, PieceType type, string fileName)
    {
        var lines = File.ReadLines(fileName);
        
        foreach (var line in lines)
        {
            var lineList = line.Split(':').ToList();
            lineList.Remove(":");

            if (lineList.Count() > 1)
            {
                res.Add((type, lineList[0]), int.Parse(lineList[1]));
            }
        }
    }





    //-------------------------------------------------------------------- LOGGING
    private void Logging(string filename, string log)
    {
        File.AppendAllText("C:\\Users\\usr\\source\\repos\\tinyChessBot\\Chess-Challenge\\src\\My Bot\\logs\\" + filename, log); // finestre
        //File.AppendAllText("/home/hos/Desktop/proj/tinyChessBot/Chess-Challenge/src/My Bot/" + filename, log);    // linux
    }

}
