using ChessChallenge.API;
using System;
using System.Linq;

using System.Collections.Generic;


// remove when not debugging
using ChessChallenge.Application;

namespace ChessChallenge.Example {

	/// <summary>
	// JamesBot10.30-01-10-2023
	/// </summary>
	public class EvilBot : IChessBot {
    public static class Def // AKA - the magic-number zone
    {
        public static int[] pieceValues = { 0, 10, 90, 90, 250, 810, 810 }; // these values are squared to exponentially prioritise higher value pieces
        public static int moveBudget = 40000;
        public static int moveAvg = 1;
        public static int maxDepth = 2;
    }
    public struct MOVE // a Move with a score
    {
        int score;
        public Move move;
        public int interest;
        public int Budget;
        public MOVE(Move i, int j, int k)
        {
            move = i;
            score = j;
            interest = k;
            Budget = 0;

        }

        public void setScore(int i) => score = i;
        public int getScore() { return score; }
        public void setBudget(int totalInterest, int moveBudget)
        {
            float ratio = 0;
            if (totalInterest > 0) ratio = interest / (float)totalInterest;
            Budget = (int)(moveBudget * ratio);

        }
    }



    public class MinMaxFish
    {
        public int totalInterest = 0;
        MOVE[] getLegalMoves(Board board, int startingScore, int moveBudget)
        {
            var m = board.GetLegalMoves();
            var rng = new Random();
            m = m.OrderBy(e => rng.NextDouble()).ToArray();



            List<MOVE> moves = new List<MOVE>();
            foreach (Move i in m)
            {


                int localInterest = 1;
                int localScore = startingScore;

                //if (i.MovePieceType != PieceType.Pawn || i.MovePieceType != PieceType.King) // encourage using pieces besides pawns/kings
                //{

                //    localScore += 1;
                //}

                //if (i.TargetSquare.File != 0 && i.TargetSquare.File != 7) // discourage moving pieces to edge tiles
                //{
                //    localScore += 1;
                //}

                //if (i.TargetSquare.Rank >= 2 && i.TargetSquare.Rank <= 6) // Encourage developing pieces
                //{
                //    localScore += 1;
                //}

                //if (!board.IsInCheck()) // encourage moving the king while in check
                //{
                //    localScore += 1;
                //    // localInterest++;
                //}

                if (i.IsCastles) // encourage castling
                {
                    localScore += 50;
                    //      localInterest++;
                }
                if (i.IsPromotion) // encourage promotion!
                {
                    //     localScore += Def.pieceValues[(int)i.PromotionPieceType];
                }

                if (i.IsCapture) // encourage capture
                {
                    // localScore += Def.pieceValues[(int)i.CapturePieceType];
                    //   localInterest += 1;
                }

                if (board.SquareIsAttackedByOpponent(i.TargetSquare))
                {
                    // localInterest++;
                    // localScore -= Def.pieceValues[(int)i.MovePieceType] / 2 ;
                }


                board.MakeMove(i);

                if (board.IsInCheckmate())
                {
                    localScore = int.MaxValue;
                    localInterest = 0;
                }
                if (board.IsInStalemate() || board.IsInsufficientMaterial() || board.IsFiftyMoveDraw() || board.IsRepeatedPosition())
                {
                    localScore = (0);
                    localInterest = 0;
                }



                board.UndoMove(i);

                totalInterest += localInterest;
                moves.Add(new MOVE(i, localScore, localInterest));
            }

            List<MOVE> budgetMoves = new List<MOVE>();

            foreach (MOVE i in moves)
            {
                i.setBudget(totalInterest, moveBudget);
                budgetMoves.Add(i);
            }
            return budgetMoves.ToArray();
        }


        public MOVE Think(Board board, int moveBudget, int score, int depth)
        {
            Random rng = new Random();


            PieceList[] pieces = board.GetAllPieceLists();

            int whiteScore = 0;
            int blackScore = 0;

            for (int i = 0; i <= 5; i++)
            {
                foreach (Piece piece in pieces[i])
                {
                    whiteScore += Def.pieceValues[i + 1];
                    if (board.SquareIsAttackedByOpponent(piece.Square) && board.IsWhiteToMove)
                    {
                        //             whiteScore -= Def.pieceValues[i + 1] /2;
                    }
                }

                foreach (Piece piece in pieces[i + 6])
                {
                    blackScore += Def.pieceValues[i + 1];
                    if (board.SquareIsAttackedByOpponent(piece.Square) && !board.IsWhiteToMove)
                    {
                        //             blackScore -= Def.pieceValues[i + 1] /2;
                    }
                }
            }



            if (board.IsWhiteToMove) score = whiteScore - blackScore;
            else score = blackScore - whiteScore;

            if (moveBudget <= 0) score -= 1000;

            MOVE[] moves = getLegalMoves(board, score, moveBudget);

            List<MOVE> bestMoves = new List<MOVE>();

            for (int i = 0; i < Def.moveAvg; i++)
            {
                bestMoves.Add(moves[0]);
            }

            foreach (MOVE move in moves)
            {

                board.MakeMove(move.move);
                if (move.Budget >= 1 && !board.IsInCheckmate() && depth > 0)
                {

                    MinMaxFish i = new MinMaxFish();
                    MOVE a = i.Think(board, move.Budget - 1, (move.getScore()) * -1, depth - 1);

                    move.setScore(a.getScore() * -1);


                }
                board.UndoMove(move.move);


                for (int i = 0; i < Def.moveAvg; i++)
                {
                    if (move.getScore() > bestMoves[i].getScore())
                    {
                        bestMoves.Add(move);
                        bestMoves.Remove(bestMoves[i]);
                        i = Def.moveAvg;
                    }
                }

            }

            int totalScore = 0;
            MOVE bestMove = bestMoves[0];
            for (int i = 0; i < Def.moveAvg; i++)
            {
                totalScore += bestMoves[i].getScore();
                if (bestMoves[i].getScore() > bestMove.getScore()) bestMove = bestMoves[i];
            }


            bestMove.setScore(  (int) (((totalScore / Def.moveAvg) * 0.1f) + (bestMove.getScore() * 0.9f)) );

            return bestMove;

        }


    }


    public Move Think(Board board, Timer timer)
    {



        MinMaxFish fisher = new MinMaxFish();


        float x = (timer.MillisecondsRemaining / 60000.0f);
        x = (-4 * (float)Math.Pow(x, 2)) + (4 * x);
        x = (x * 0.75f) + 0.25f;
        int timeBudget = (int)(Def.moveBudget * x);
        MOVE bestMove;
        if (board.IsWhiteToMove) bestMove = fisher.Think(board, timeBudget, 0, Def.maxDepth);
        else bestMove = fisher.Think(board, timeBudget, 0, Def.maxDepth);

        Console.WriteLine($"My best move score: {bestMove.getScore()}");

        return bestMove.move;

    }

}
}