using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Connect 4 AI.
/// Chooses a column with Minimax and alpha-beta pruning, limited by search depth and time limit.
/// </summary>
public class AIPlayer
{
    // Scoring for heuristic
    private const double WinScore = 1000000.0;
    private const int CenterColumn = 3;
    private const int CenterBonus = 6;
    private const int ParityBonus = 3;
    private const int ThreeFriendlyScore = 100;
    private const int TwoFriendlyScore = 10;
    private const int ThreeEnemyScore = -120;
    private const int TwoEnemyScore = -12;
    private const int SearchTimeLimitMs = 10000;

    // Columns, ordered from the center outwards, so alpha-beta prunes more branches.
    private readonly int[] columnOrder = { 3, 2, 4, 1, 5, 0, 6 };
    
    private readonly Stopwatch searchStopwatch = new Stopwatch();

    /// <summary>
    /// The piece value of the AI player: 1 or -1.
    /// </summary>
    public int Color { get; set; } = -1;

    /// <summary>
    /// Returns the column chosen by the AI.
    /// </summary>
    public int GetColumn(Board board, int depth)
    {
        searchStopwatch.Restart();
        return AlphaBeta(board, depth);
    }

    /// <summary>
    /// Root of the alpha beta search.
    /// Returns immediately if a winning move is found.
    /// </summary>
    private int AlphaBeta(Board board, int depth)
    {
        // Initialization
        double alpha = double.NegativeInfinity;
        double beta = double.PositiveInfinity;
        int bestColumn = CenterColumn;

        foreach (int column in GetOrderedPossibleColumns(board))
        {
            if (SearchTimedOut())
                return bestColumn;

            board.play(Color, column);
            if (board.checkWinner() == Color)
            {
                UndoMove(board, column);
                return column;
            }

            double value = Min(board, alpha, beta, 1, depth);
            UndoMove(board, column);

            if (value > alpha)
            {
                alpha = value;
                bestColumn = column;
            }
        }
        return bestColumn;
    }

    /// <summary>
    /// Max node: picks the move with the highest score.
    /// Returns a terminal score on win or loss.
    /// Or a static evaluation using our heuristic at the depth limit.
    /// </summary>
    private double Max(Board board, double alpha, double beta, int prof, int maxProf)
    {
        if (SearchTimedOut()) return Evaluate(board);

        //Checking end conditions
        int winner = board.checkWinner();
        if (winner == Color) return WinScore - prof;
        if (winner == -Color) return -WinScore + prof;
        if (prof >= maxProf || board.isFull()) return Evaluate(board);

        //Otherwise, try each move for the opponent’s min node
        foreach (int column in GetOrderedPossibleColumns(board))
        {
            board.play(Color, column);
            double value = Min(board, alpha, beta, prof + 1, maxProf);
            UndoMove(board, column);

            alpha = Math.Max(value, alpha);
            if (alpha >= beta)
                return beta;
        }
        return alpha;
    }

    /// <summary>
    /// Min node: picks the move with the lowest score.
    /// </summary>
    private double Min(Board board, double alpha, double beta, int prof, int maxProf)
    {
        if (SearchTimedOut()) return Evaluate(board);

        int winner = board.checkWinner();
        if (winner == Color) return WinScore - prof;
        if (winner == -Color) return -WinScore + prof;
        if (prof >= maxProf || board.isFull()) return Evaluate(board);

        foreach (int column in GetOrderedPossibleColumns(board))
        {
            board.play(-Color, column);
            double value = Max(board, alpha, beta, prof + 1, maxProf);
            UndoMove(board, column);

            beta = Math.Min(value, beta);
            if (beta <= alpha)
                return alpha;
        }
        return beta;
    }

    /// <summary>
    /// Yields the columns that are not full, in the order defined by columnOrder.
    /// </summary>
    private IEnumerable<int> GetOrderedPossibleColumns(Board board)
    {
        foreach (int col in columnOrder)
        {
            if (board.getHeight(col) < board.num_rows)
                yield return col;
        }
    }

    /// <summary>
    /// Removes the top piece of a column, reverting the last move played there.
    /// </summary>
    private void UndoMove(Board board, int col)
    {
        int row = board.getHeight(col);
        if (row > 0)
        {
            board.board[col][row - 1] = 0;
        }
    }

    /// <summary>
    /// Heuristic evaluation of the board from the AI point of view.
    /// Combines center control, row-parity and scoring of every 4 cell window.
    /// </summary>
    private double Evaluate(Board board)
    {
        double score = 0;
        int cols = board.num_cols;
        int rows = board.num_rows;

        // Center control and row parity
        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                int val = board.board[c][r];
                if (val == 0) continue;

                if (c == CenterColumn)
                {
                    score += (val == Color) ? CenterBonus : -CenterBonus;
                }

                // Player 1 (val == 1) targets even rows, player 2 (val == -1) targets odd rows.
                bool isTargetRow = (val == 1) ? (r % 2 == 0) : (r % 2 != 0);
                if (isTargetRow)
                {
                    score += (val == Color) ? ParityBonus : -ParityBonus;
                }
            }
        }
        // Checking every 4 cell window to find sequences of player or opponent pieces
        // Horizontal windows
        for (int c = 0; c < cols - 3; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                score += ScoreWindow(board.board[c][r], board.board[c + 1][r], board.board[c + 2][r], board.board[c + 3][r]);
            }
        }

        // Vertical windows
        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows - 3; r++)
            {
                score += ScoreWindow(board.board[c][r], board.board[c][r + 1], board.board[c][r + 2], board.board[c][r + 3]);
            }
        }

        // Rising diagonals (/)
        for (int c = 0; c < cols - 3; c++)
        {
            for (int r = 0; r < rows - 3; r++)
            {
                score += ScoreWindow(board.board[c][r], board.board[c + 1][r + 1], board.board[c + 2][r + 2], board.board[c + 3][r + 3]);
            }
        }

        // Falling diagonals (\)
        for (int c = 0; c < cols - 3; c++)
        {
            for (int r = 3; r < rows; r++)
            {
                score += ScoreWindow(board.board[c][r], board.board[c + 1][r - 1], board.board[c + 2][r - 2], board.board[c + 3][r - 3]);
            }
        }

        return score;
    }

    /// <summary>
    /// True when the time limit for the current search has been reached.
    /// When this happens, the search stops early and returns the best result found so far,
    /// which may be only partially evaluated.
    /// </summary>
    private bool SearchTimedOut()
    {
        return searchStopwatch.ElapsedMilliseconds >= SearchTimeLimitMs;
    }

    /// <summary>
    /// Scores a 4 cell window: 0 if it contains both players' pieces,
    /// a positive score if it favors the AI and a negative score otherwise.
    /// </summary>
    private int ScoreWindow(int a, int b, int c, int d)
    {
        int[] cells = { a, b, c, d };
        int friendly = 0;
        int enemy = 0;

        foreach (int cell in cells)
        {
            if (cell == Color) friendly++;
            else if (cell == -Color) enemy++;
        }

        if (friendly > 0 && enemy > 0) return 0;
        if (friendly == 3) return ThreeFriendlyScore;
        if (friendly == 2) return TwoFriendlyScore;
        if (enemy == 3) return ThreeEnemyScore;
        if (enemy == 2) return TwoEnemyScore;

        return 0;
    }
}