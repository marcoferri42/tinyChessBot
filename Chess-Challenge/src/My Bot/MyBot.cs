using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Node // classetta nodo custom
{
    public bool isRoot = false;
    public int eval { get; set; }
    public Node root { get; set; }
    public Move bestMove { get; set; }

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
        this.bestMove = move;
        this.isRoot = false;
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
            Console.WriteLine(board.GameMoveHistory.Length + "- " + Evaluate(board, 0));


        for (int i = 1; i < 3; i++) // itereative deepening TODO da studiare bene ! 
        {
            tree = CreateTree(board, i, new Node());

            //Console.WriteLine("moves: " + totMoves + " depth: " + i);    // <----- totalmoves print
            totMoves = 0;
        }

        //Console.WriteLine(tree.Children[0].eval);             //<---------- evaluation print
        return tree.Children[0].bestMove;
    }

    private int Evaluate(Board board, int prevEval)
    {
        int score = 0, turn = board.IsWhiteToMove ? -1 : 1;
        Move[] prevMoves = board.GameMoveHistory;

        if (board.IsInCheckmate()){
            return 1000000 * turn;
        }

        if (seenPositions.ContainsKey(board.ZobristKey))
        {
            return seenPositions[board.ZobristKey];
        }

        // possible moves
        int currentPlayerMoves = board.GetLegalMoves().Length;
        int currentPlayerCaptures = board.GetLegalMoves(true).Length;

        if (board.TrySkipTurn()){
            int opponentMoves = board.GetLegalMoves().Length;
            
            // EVAL POSSIBLE MOVES
            score += (currentPlayerMoves > opponentMoves ? 1 : 0) * turn;  

            int opponentCaptures = board.GetLegalMoves(true).Length;

            // EVAL CAPTURES

            score += (currentPlayerCaptures > opponentCaptures ? 1 : 0) * turn;

            board.UndoSkipTurn();
        }

        // one move rule (piece weighted)
        if (prevMoves.Length >= 3)
        {
            // EVAL onemoverule
            if (prevMoves[^3].MovePieceType == prevMoves[^1].MovePieceType)
            {
                score += values[prevMoves[^1].MovePieceType]/5 * (-1*turn) ;
            }

        }

        // material and positional value
        foreach (PieceList list in board.GetAllPieceLists())
        {
            foreach (Piece piece in list)
            {
                score += values[piece.PieceType] * (piece.IsWhite ? 1 : -1); // material value

                (int x, int y) coords = getPieceCoords(piece);

                int [,] pos = positional[piece.PieceType];
                
                // flippa rows se nero
                if (!piece.IsWhite)
                {
                    pos = Reverse2DArray(pos);
                }

                score += pos[coords.x, coords.y] * (piece.IsWhite ? 1 : -1);
                
                //Console.WriteLine(pos[coords.x, coords.y] + " , " + (piece.IsWhite ? "w":"b"));
            }

        }


        // add position to table
        seenPositions.Add(board.ZobristKey, score);

        return score;
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
        if (depth > 0)
        {
            Move[] moves = board.GetLegalMoves();

            moves = FilterMoves(moves, board);

            totMoves += moves.Length;
            
            int eval = 0;

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                eval = Evaluate(board, rootNode.eval);

                rootNode.Children.Add(
                    CreateTree(board, depth - 1, new Node(rootNode, eval, move)) // recursive call for children
                );

                board.UndoMove(move);
            }

            if (moves.Length > 0 && rootNode.Children.Count == moves.Length)
            {
                rootNode.Children = SortEval(rootNode.Children, !board.IsWhiteToMove); // true -> ascending order >> black -> asc, white -> desc
                //Console.WriteLine(rootNode.Children[0].eval + " " + board.IsWhiteToMove);
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

            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return new Move[] { move }; ;
            }

            if (nMoves < 5) // mosse opening
            {
                if (move.MovePieceType == PieceType.Pawn){
                    Hpriority.Add(move);
                }
            }


            if (board.SquareIsAttackedByOpponent(move.TargetSquare)){
                Lpriority.Add(move);
            }

            if (move.IsPromotion || move.IsCastles)
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
    private int[,] Reverse2DArray(int[,] array)
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
        return array;
    }


    // tree eval sort
    static List<Node> SortEval(List<Node> nodes, bool ascending = true)
    {
        // compara
        Comparison<Node> comparison = (node1, node2) => ascending ? node1.eval.CompareTo(node2.eval) : node2.eval.CompareTo(node1.eval);

        // sorta
        nodes.Sort(comparison);
        return nodes;
    }

}