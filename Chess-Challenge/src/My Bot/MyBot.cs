using ChessChallenge.API;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks.Dataflow;

public class Node // classetta nodo custom
{
    public bool isRoot = false;
    public int eval { get; set; }
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
}

class NodeCompare : IComparer<Node>
{
    public int Compare(Node? x, Node? y)
    {
        if (x == null || y == null)
        {
            Console.WriteLine("ERROR COMPARE NULL");
            return 0;
        }

        // Compare by the eval
        return x.eval.CompareTo(y.eval);
    }
}


public class MyBot : IChessBot
{
    private Dictionary<PieceType, int[,]> positional = new Dictionary<PieceType, int[,]>() {
        {PieceType.Pawn, new int[,] {
            {100, 100, 100, 100, 100, 100, 100, 100},
            {50,  50,  50,  50,  50,  50,  50,  50},
            {10,  20,  20,  30,  30,  20,  20,  10},
            {5,   10,  10,  25,  25,  10,  10,  5},
            {0,   0,   0,   20,  20,  0,   0,   0},
            {5,   -10, -10, 0,   0,   -10, -10, 5},
            {5,   10,  10,  -25, -25, 10,  10,  5},
            {0,   0,   0,   0,   0,   0,   0,   0}}},

        {PieceType.Bishop, new int[,] {
            {-20, -10, -10, -10, -10, -10, -10, -20},
            {-10, 20,  0,   0,   0,   0,   20,  -10},
            {-10, 0,   10,  10,  10,  10,  0,   -10},
            {-10, 5,   5,   10,  10,  5,   5,   -10},
            {-10, 0,   10,  10,  10,  10,  0,   -10},
            {-10, 5,   5,   10,  10,  5,   5,   -10},
            {-10, 0,   0,   0,   0,   0,   0,   -10},
            {-20, -10, -10, -10, -10, -10, -10, -20}}},

        {PieceType.Rook, new int[,] {
            {0, 0, 0, 0, 0, 0, 0, 0},
            {5, 10, 10, 10, 10, 10, 10, 5},
            {-5, 0, 0, 0, 0, 0, 0, -5},
            {-5, 0, 0, 0, 0, 0, 0, -5},
            {-5, 0, 0, 0, 0, 0, 0, -5},
            {-5, 0, 0, 0, 0, 0, 0, -5},
            {-5, 0, 0, 0, 0, 0, 0, -5},
            {0, 0, 0, 5, 5, 0, 0, 0}}},

        {PieceType.Queen, new int[,] {
            {-20, -10, -10, -5, -5, -10, -10, -20},
            {-10, 0, 5, 0, 0, 0, 0, -10},
            {-10, 5, 5, 5, 5, 5, 0, -10},
            {0, 0, 5, 5, 5, 5, 0, -5},
            {-5, 0, 5, 5, 5, 5, 0, -5},
            {-10, 0, 5, 5, 5, 5, 0, -10},
            {-10, 0, 0, 0, 0, 0, 0, -10},
            {-20, -10, -10, -5, -5, -10, -10, -20}}},

        {PieceType.Knight, new int[,] {
            {-50, -40, -30, -30, -30, -30, -40, -50},
            {-40, -20, 0, 0, 0, 0, -20, -40},
            {-30, 0, 10, 15, 15, 10, 0, -30},
            {-30, 5, 15, 20, 20, 15, 5, -30},
            {-30, 0, 15, 20, 20, 15, 0, -30},
            {-30, 5, 10, 15, 15, 10, 5, -30},
            {-40, -20, 0, 5, 5, 0, -20, -40},
            {-50, -40, -30, -30, -30, -30, -40, -50}}},

        {PieceType.King, new int[,] {
            {0, 10, 10, -10, -10, 0, 10, 0},
            {10, 10, 10, 0, 0, 10, 10, 10},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {10, 10, 10, 0, 0, 10, 10, 10},
            {0, 10, 10, -10, -10, 0, 10, 0}}}
    };

    private Dictionary<PieceType, int> values = new Dictionary<PieceType, int>() { // pieces values
        { PieceType.Pawn, 150 },
        { PieceType.Bishop, 350 },  
        { PieceType.Knight, 320 },
        { PieceType.Queen, 950 },
        { PieceType.King, 1000000 },
        { PieceType.Rook, 550 }
    };

    public Dictionary<ulong, int> seenPositions = new Dictionary<ulong, int>(); // positions table

    private int totMoves = 0;

    public Move Think(Board board, Timer t)
    {
        Node tree = new Node();

        if (board.GameMoveHistory.Length > 0)
            Console.WriteLine(Evaluate(board, 0));

        for (int i = 1; i < 5; i++) // itereative deepening TODO da studiare bene ! 
        {
            tree = CreateTree(board, i, new Node());

            //Console.WriteLine("moves: " + totMoves + " depth: " + i);    // <----- totalmoves print
            totMoves = 0;
        }

        //Console.WriteLine(tree.Children[0].eval);             //<---------- evaluation print
        return tree.Children[0].lastMove;
    }

    private int Evaluate(Board board, int prevEval)
    {
        int score = 0, turn = board.IsWhiteToMove ? -1 : 1;

        if (seenPositions.ContainsKey(board.ZobristKey))
        {
            return seenPositions[board.ZobristKey];
        }
        else
        {
            // EVAL checkmate
            if (board.IsInCheckmate()){
                return score + 1000000 * turn;
            }

/*
            // EVAL draw
            if (board.IsDraw()){
                score += Math.Sign(prevEval) > 0 ? -10 : 10;
            }
*/

            // one move rule (piece weighted)
            if (board.GameMoveHistory.Length > 3)
            {
                // EVAL onemoverule
                PieceType previousMove = board.GameMoveHistory[board.GameMoveHistory.Length - 3].MovePieceType;
                PieceType lastMove = board.GameMoveHistory.Last().MovePieceType;

                if (previousMove == lastMove)
                {
                    score += 10 * turn ;
                }

            }

            // material and positional value
            foreach (PieceList list in board.GetAllPieceLists())
            {
                foreach (Piece piece in list)
                {
                    // EVAL material
                    score += values[piece.PieceType] * (piece.IsWhite ? 1 : -1); // material value

                    (int x, int y) coords = getPieceCoords(piece);
                    

                    // flippa rows se nero
                    int [,] pos = positional[piece.PieceType];
                    
                    if (piece.IsWhite)
                        Reverse2DArray(pos);

                    // EVAL positional
                    score += pos[coords.x, coords.y] * (piece.IsWhite ? 1 : -1) / 2;

                }

            }

/*
            // possible moves incentive
            int currentPlayerMoves = board.GetLegalMoves().Length;
            
            if (board.TrySkipTurn()){
                int opponentMoves = board.GetLegalMoves().Length;
                
                int moveCountDifference = currentPlayerMoves - opponentMoves;
                // EVAL POSSIBLE MOVES
                score += (moveCountDifference * turn)/4;

                board.UndoSkipTurn();
            }
*/



            // add position to table
            seenPositions.Add(board.ZobristKey, score);

            return score;

        }
    }

    private (int, int) getPieceCoords(Piece piece)
    {
        int x = piece.Square.Index % 8;
        int y = piece.Square.Index / 8;

        if (!piece.IsWhite)
        {
            x = 7 - x; // Adjust for black pieces
            y = 7 - y; // Adjust for black pieces
        }

        return (x, y);
    }

    private Node CreateTree(Board board, int depth, Node rootNode)
    {
        if (totMoves > 200000){
            return rootNode;
        }
        if (depth > 0)
        {
            Move[] moves = board.GetLegalMoves();

            moves = FilterMoves(moves, board);

            totMoves += moves.Length;

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int eval = Evaluate(board, rootNode.eval);

                rootNode.Children.Add(
                    CreateTree(board, depth - 1, new Node(rootNode, eval, move)) // recursive call for children
                );

                board.UndoMove(move);
            }

            if (rootNode.Children.Count > 0)
            {
                rootNode.Children.Sort(new NodeCompare()); // compares in ascending eval order  --> white best move first

                if (board.IsWhiteToMove) // black best move first
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

        int nMoves = board.GameMoveHistory.Length;

        foreach (Move move in moves)
        {

            board.MakeMove(move);

            if(nMoves < 4) // mosse opening ARBITRARIO A CAZZO TODO CAMBIA
            {
                if (move.MovePieceType == PieceType.Pawn){
                    Hpriority.Add(move);
                }
            }

            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return new Move[] { move }; ;
            }

            if (move.IsPromotion || move.IsCastles || move.IsCapture)
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

    // GPT :)
    static void Reverse2DArray(int[,] array)
    {
        int rows = array.GetLength(0);
        int cols = array.GetLength(1);

        for (int i = 0; i < rows / 2; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                int temp = array[i, j];
                array[i, j] = array[rows - i - 1, j];
                array[rows - i - 1, j] = temp;
            }
        }
    }
}
/*
TODO:

1. Piece Move Order
2. Clean Up EVERYTHING (varnames etc)









*/