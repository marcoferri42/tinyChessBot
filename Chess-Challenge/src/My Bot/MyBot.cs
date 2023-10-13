using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Storage
{
    public List<List<Board>> boards = new List<List<Board>>();
    public List<List<int>> evals = new List<List<int>>();

    public Storage() {
        this.boards.Add(new List<Board>());
        this.boards.Add(new List<Board>());
        this.boards.Add(new List<Board>());
        this.boards.Add(new List<Board>());
        this.boards.Add(new List<Board>());
        this.evals.Add(new List<int>());
        this.evals.Add(new List<int>());
        this.evals.Add(new List<int>());
        this.evals.Add(new List<int>());
        this.evals.Add(new List<int>());

    }
}


public class MyBot : IChessBot
{
    private Dictionary<PieceType, int> values = new Dictionary<PieceType, int>() 
    { // pieces values
        { PieceType.Pawn, 150 },
        { PieceType.Bishop, 350 },  
        { PieceType.Knight, 320 },
        { PieceType.Queen, 950 },
        { PieceType.King, 1000000 },
        { PieceType.Rook, 550 }
    };

    public Dictionary<ulong, int> seenPositions = new Dictionary<ulong, int>(); // positions table

    public Storage moveVal = new Storage();
    public Move Think(Board board, Timer t)
    {
        int depth = 3;

        for (int i = 0; i < depth; i++)
        {

            FindAll(board, i);

            int minimax = board.IsWhiteToMove ? moveVal.evals[depth - 1].IndexOf(moveVal.evals[depth - 1].Max()) : moveVal.evals[depth - 1].IndexOf(moveVal.evals[depth - 1].Min());

        }
        Console.WriteLine(moveVal.evals[0].Count);
        Console.WriteLine(moveVal.evals[1].Count);
        Console.WriteLine(moveVal.evals[2].Count);

        return 
    }





    private void FindAll(Board b, int depth)
    {
        if (depth < 0) { return; }

        foreach (Move mv in b.GetLegalMoves())
        {
            b.MakeMove(mv);

            if (!seenPositions.ContainsKey(b.ZobristKey))
            {
                moveVal.evals[depth].Add(Evaluate(b));
                moveVal.boards[depth].Add(b);

                FindAll(b, depth - 1); //recurse
            }

            b.UndoMove(mv);
        }
    }

    private int Evaluate(Board board)
    {
        int score = 0, turn = board.IsWhiteToMove ? -1 : 1;
        Move[] prevMoves = board.GameMoveHistory;

        // checkmate
        if (board.IsInCheckmate()){
            return 1000000 * turn;
        }

        // draw
        if (board.IsDraw())
        {
            return 0;
        }

        // possible moves
        int currentPlayerMoves = board.GetLegalMoves().Length;
        int currentPlayerCaptures = board.GetLegalMoves(true).Length;

        if (board.TrySkipTurn()){
            int opponentMoves = board.GetLegalMoves().Length;
            
            score += (currentPlayerMoves > opponentMoves ? 1 : 0) * turn;  

            int opponentCaptures = board.GetLegalMoves(true).Length;

            score += (currentPlayerCaptures > opponentCaptures ? 1 : 0) * turn;

            board.UndoSkipTurn();
        }

        // one move rule
        if (prevMoves.Length > 2)
        {
            score += checkOneMoveRule(board, prevMoves) *turn;
        }


        // material value
        foreach (PieceList list in board.GetAllPieceLists())
        {
            foreach (Piece piece in list)
            {
                score += values[piece.PieceType] * (piece.IsWhite ? 1 : -1); // material value

            }

        }

        // add position to table
        seenPositions.Add(board.ZobristKey, score);

        return score;
    }

    private int checkOneMoveRule(Board board, Move[] prevMoves)
    {
        if (prevMoves[^1].MovePieceType == prevMoves[^3].MovePieceType)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

}
