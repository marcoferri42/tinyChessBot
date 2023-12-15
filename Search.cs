public class Node
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

public class Search
{
    private const int MARGIN = 50; // Example margin, should be tuned for the game

    public int Negamax(Node node, int alpha, int beta)
    {
        if (node == null) return 0;

        // Perform reverse futility pruning check
        int eval = Evaluate(node.board);
        node.eval = eval; // Store static evaluation in the node

        // RFP condition: if the evaluation minus the margin is greater than beta,
        // it's unlikely that the opponent would allow this position to occur.
        if (eval - MARGIN >= beta)
            return eval - MARGIN; // Fail-soft return

        // Here, you would normally generate legal moves for the position.
        // For the purposes of this example, we'll assume there's a method `GetLegalMoves(Node node)`
        // that returns a list of legal moves for the given node's board.
        List<Move> legalMoves = GetLegalMoves(node);

        int bestScore = int.MinValue;
        foreach (var move in legalMoves)
        {
            // Make the move on the board
            Board newBoard = MakeMove(node.board, move);
            Node childNode = new Node(node, eval, move, newBoard);

            // Continue search with child node
            int score = -Negamax(childNode, -beta, -alpha);

            // Unmake move
            UnmakeMove(newBoard, move);

            if (score > bestScore)
            {
                bestScore = score;
                if (score > alpha)
                {
                    alpha = score;
                    if (alpha >= beta)
                    {
                        break; // Beta cutoff
                    }
                }
            }
        }

        // If no moves were made, it's a terminal node (could be checkmate or stalemate)
        if (bestScore == int.MinValue)
        {
            bestScore = EvaluateTerminalPosition(node.board);
        }

        return bestScore;
    }

    private int Evaluate(Board b)
    {
        // Your static evaluation logic here
        return 0;
    }

    private List<Move> GetLegalMoves(Node node)
    {
        // Your move generation logic here
        return new List<Move>();
    }

    private Board MakeMove(Board board, Move move)
    {
        // Your logic to make a move on the board and return the new board state
        return board;
    }

    private void UnmakeMove(Board board, Move move)
    {
        // Your logic to unmake a move and revert the board state
    }

    private int EvaluateTerminalPosition(Board board)
    {
        // Your logic to evaluate terminal positions (e.g., checkmate or stalemate)
        return 0;
    }
}