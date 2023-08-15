using ChessChallenge.API;
using ChessChallenge.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

    public void Print()
    {
        Console.WriteLine(ToStringHelper(this, ""));
    }

    private string ToStringHelper(Node node, string indent)
    {
        string nodeString = $"{indent}+-- {node.eval}";

        for (int i = 0; i < node.Children.Count; i++)
        {
            nodeString += $"\n{ToStringHelper(node.Children[i], indent + (i == node.Children.Count - 1 ? "    " : "|   "))}";
        }

        return nodeString;
    }
}

class NodeCompare : IComparer<Node> // comparatore nodes
{
    public int Compare(Node x, Node y)
    {
        // Compare by the eval
        return x.eval.CompareTo(y.eval);
    }
}

public class MyBot : IChessBot
{
    private Dictionary<PieceType, int> values = new Dictionary<PieceType, int>() { // pieces values
        { PieceType.Pawn, 100 },
        { PieceType.Bishop, 300 },
        { PieceType.Knight, 300 },
        { PieceType.Queen, 900 },
        { PieceType.King, 10000 },
        { PieceType.Rook, 500 }
    };

    private Dictionary<PieceType, int[,]> positional = new Dictionary<PieceType, int[,]>() {
        {PieceType.Pawn, new int[,] { /* se nero reverse */
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 1, 1, 0, 0, 0},
            {0, 0, 1, 0, 0, 1, 0, 0},
            {1, 1, 1, 0, 0, 1, 1, 1},
            {0, 0, 0, 0, 0, 0, 0, 0}}},

        {PieceType.Bishop, new int[,] {
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 1, 0, 0, 0, 0, 1, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 1, 0, 0, 0, 0, 1, 0},
            {0, 0, 0, 0, 0, 0, 0, 0}}},

        {PieceType.Knight, new int[,] {
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 1, 0, 0, 1, 0, 0},
            {0, 0, 0, 1, 1, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0}}},

        {PieceType.Queen, new int[,] {
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 1, 1, 1, 1, 1, 1, 0},
            {0, 0, 1, 0, 0, 1, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0}}},

        {PieceType.King, new int[,] {
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 1, 1, 0, 0, 1, 1, 0}}},

        {PieceType.Rook, new int[,] {
            {0, 0, 0, 0, 0, 0, 0, 0},
            {1, 0, 0, 0, 0, 0, 0, 1},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 1, 1, 0, 0, 0}}},
    };

    public Dictionary<ulong, int> seenPositions = new Dictionary<ulong, int>(); // positions table
    private int totMoves, botColor;

    public Move Think(Board board, Timer t)
    {
        botColor = board.IsWhiteToMove ? -1 : 1;

        Node tree = new Node();

        for (int i = 0; i < 5; i++) // itereative deepening  da studiare bene ! 
        {
            tree = CreateTree(board, i, new Node());

            Console.WriteLine("moves: " + totMoves + " depth: " + i );    // <----- totalmoves print
            totMoves = 0;
        }

        Console.WriteLine("eval: " + tree.Children[0].eval);             //<---------- evaluation print
        return tree.Children[0].lastMove;
    }

    private int Evaluate(Board board)
    {
        int score = 0, turn = bot ? -1 : 1;

        if (seenPositions.ContainsKey(board.ZobristKey))
        {
            return seenPositions[board.ZobristKey];
        }
        else
        {
            /* ONE MOVE RULE

            */
            if (board.GameMoveHistory.Length > 3 && board.GameMoveHistory.Length < 30)
            {
                PieceType previousMove = board.GameMoveHistory[^3].MovePieceType;
                PieceType lastMove = board.GameMoveHistory[^1].MovePieceType;

                if (previousMove == lastMove)
                {
                    score += 10 * turn;
                }

            }


            foreach (PieceList list in board.GetAllPieceLists())
            {
                foreach (Piece piece in list)
                {
                    score += values[piece.PieceType] * (piece.IsWhite ? 1 : -1); // material value
                    
                    if (board.GameMoveHistory.Length < 10)
                    {
                        (int x, int y) coords = getPieceCoords(piece);

                        score += positional[piece.PieceType][coords.x, coords.y]* (piece.IsWhite ? 1 : -1);
                    }
                }

            }

            score -= board.IsRepeatedPosition() ? 500 *turn : 0;

            score += board.IsInCheckmate()  ? 100000 *turn : 0;
            
            score += board.IsInCheck()      ? 5 *turn : 0;

            // add position to table
            seenPositions.Add(board.ZobristKey, score);

            return score;

        }
    }

    private Node CreateTree(Board board, int depth, Node rootNode)
    {// TODO DIMINUISCI I RAMI DEL TREE AL LIVELLO 4 in su
        if (totMoves > 80000){
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
                int eval = Evaluate(board);


                rootNode.Children.Add(
                    CreateTree(board, depth - 1, new Node(rootNode, eval, move)) // recursive call for children
                );

                board.UndoMove(move);
            }

            if (rootNode.Children.Count > 0)
            {
                rootNode.Children.Sort(new NodeCompare()); // compares in ascending eval order (white best move first)

                if (board.IsWhiteToMove) // if white reverse (black best move first)
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

        foreach (Move move in moves)
        {

            board.MakeMove(move);

            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return new Move[] { move }; ;
            }

            if (move.IsPromotion || board.IsInCheck() || move.IsCastles || move.IsCapture)
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


    private (int, int) getPieceCoords(Piece piece){

    return (    
            (piece.IsWhite ? piece.Square.Index : Math.Abs(piece.Square.Index-8)) % 8, 
            (piece.IsWhite ? piece.Square.Index : Math.Abs(piece.Square.Index-8)) / 8
        );
    }
}