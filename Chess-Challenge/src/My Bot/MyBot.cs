using ChessChallenge.API;
using ChessChallenge.Application;
using System;
using System.Collections.Generic;
using System.Linq;

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
            {3, 3, 3, 3, 3, 3, 3, 3},
            {1, 1, 2, 2, 2, 2, 1, 1},
            {0, 1, 1, 2, 2, 1, 1, 0},
            {0, 1, 1, 2, 2, 1, 1, 0},
            {0, 1, 1, 2, 2, 1, 1, 0},
            {0, 1, 1, 1, 1, 1, 1, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0}}},

        {PieceType.Bishop, new int[,] {
            {1, 0, 0, 0, 0, 0, 0, 1},
            {0, 1, 0, 0, 0, 0, 1, 0},
            {0, 0, 1, 1, 1, 1, 0, 0},
            {0, 0, 1, 1, 1, 1, 0, 0},
            {0, 0, 1, 1, 1, 1, 0, 0},
            {0, 0, 1, 1, 1, 1, 0, 0},
            {0, 1, 0, 0, 0, 0, 1, 0},
            {1, 0, 0, 0, 0, 0, 0, 1}}},

        {PieceType.Knight, new int[,] {
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 1, 1, 0, 0, 0},
            {0, 0, 1, 1, 1, 1, 0, 0},
            {0, 0, 1, 2, 2, 1, 0, 0},
            {0, 0, 1, 2, 2, 1, 0, 0},
            {0, 0, 1, 1, 1, 1, 0, 0},
            {0, 0, 0, 1, 1, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0}}},

        {PieceType.Queen, new int[,] {
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 1, 1, 0, 0, 0},
            {0, 0, 1, 1, 1, 1, 0, 0},
            {0, 0, 1, 2, 2, 1, 0, 0},
            {0, 0, 1, 2, 2, 1, 0, 0},
            {0, 0, 1, 1, 1, 1, 0, 0},
            {0, 0, 0, 1, 1, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0}}},

        {PieceType.King, new int[,] {
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 1, 0, 0, 1, 1, 1}}},

        {PieceType.Rook, new int[,] {
            {1, 1, 1, 1, 1, 1, 1, 1},
            {1, 1, 1, 1, 1, 1, 1, 1},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 1, 1, 0, 0, 0}}},
    };

    public Dictionary<ulong, int> seenPositions = new Dictionary<ulong, int>(); // positions table

    public Move Think(Board board, Timer t)
    {

        Node tree = new Node();

        for (int i = 1; i < 4; i++) // itereative deepening  da studiare bene ! 
        {
            tree = CreateTree(board, i, new Node());
        }

        //Console.WriteLine(tree.Children[0].eval);             <---------- evaluation print
        return tree.Children[0].lastMove;
    }

    private int Evaluate(Board board)
    {
        int score = 0, turn = board.IsWhiteToMove ? -1 : 1;

        if (seenPositions.ContainsKey(board.ZobristKey))
        {
            return seenPositions[board.ZobristKey];
        }
        else
        {
            /* ONE MOVE RULE

            if (board.GameMoveHistory.Length > 3)
            {
                PieceType previousMove = board.GameMoveHistory[^3].MovePieceType;
                PieceType lastMove = board.GameMoveHistory[^1].MovePieceType;

                if (previousMove == lastMove)
                {
                    score -= 10 * turn;
                }

            }
            */

            foreach (PieceList list in board.GetAllPieceLists())
            {
                foreach (Piece piece in list)
                {
                    score += values[piece.PieceType] * (piece.IsWhite ? 1 : -1); // material value
                    
                    if (piece.IsWhite) // positional value
                    {
                        score += positional[piece.PieceType][piece.Square.Index % 8, piece.Square.Index / 8] * 10;
                    }
                    else
                    {
                        score -= positional[piece.PieceType][piece.Square.Index % 8, Math.Abs((piece.Square.Index / 8)-7)] * 10;
                    }
                }

            }

            // checkmate and draw score
            if (board.IsInCheckmate())
            {
                score += 100000 * turn;
            }

            // check check check check check mate
            if (board.IsInCheck())
            {
                score += 50 * turn;
            }


            // add position to table
            seenPositions.Add(board.ZobristKey, score);

            return score;

        }
    }

    private Node CreateTree(Board board, int depth, Node rootNode)
    {
        if (depth > 0)
        {
            Move[] moves = board.GetLegalMoves();

            moves = FilterMoves(moves, board);

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

}