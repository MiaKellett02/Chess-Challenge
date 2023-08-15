using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{

    /// <summary>
    // alfiebotv16.50-15-08-2023
    /// </summary>
    public class EvilBot : IChessBot
    {
        //Value of pieces for taking and protecting.
    public enum PieceValues {
        PAWN = 100,
        BISHOP = 350,
        KNIGHT = 275,
        ROOK = 350,
        QUEEN = 600,
        KING = 800
    }

    //Cost of moving a piece, to discourage moving high value pieces.
    public enum MoveCost {
        PAWN = 50,
        BISHOP = 100,
        KNIGHT = 90,
        ROOK = 100,
        QUEEN = 200,
        KING = 450
    }
    
    //Interest level of a move adds additional levels of recursion to a move
    public enum Interest {
        NONE = 0,
        LOW = 0,
        MEDIUM = 1,
        HIGH = 2,
    }

    //Scores awarded for various states of the game.
    const int checkValue = 100;
    const int checkMateValue = 10000000;
    const int drawValue = -20000000;

    //Bonuses given to special moves.
    const int promotionBonus = 400;
    const int enPassantBonus = 300;
    const int castleBonus = 200;

    //Base level of recursion to use when evaluating a move.
    const int baseMoveDepth = 3;

    //Whether its the players turn or not.
    static bool isMyTurn;
    //Multiplier that sets preference to protecting pieces on the players turn.
    const float myPieceMultiplier = 2f;

    //An evaluated move with a score, a level of interest and an assigned move
    public struct EvaluatedMove {
        //Score - How valuable this move is to the current player.
        public int score;
        //Intrest - How intresting a move is which can be used as a bonus to provide deeper levels of recursion.
        public Interest interest;
        //Move - The move that is made using the chess challenge API.
        public Move move;

        public EvaluatedMove(Move _move) {
            score = 0;
            interest = Interest.NONE;
            move = _move;
        }
    }

    public Move Think(Board board, Timer timer) {
        //Upon begining to think it should be the players turn.
        isMyTurn = true;

        //Get All Moves
        Move[] moves = board.GetLegalMoves();
        EvaluatedMove[] evaluatedMoves = EvaluateMoves(moves, board);

        //Get the highest scoring move from all the evaluated moves, this will (hopefully) be the optimal move
        EvaluatedMove bestMove = GetBestMove(evaluatedMoves);

        Console.WriteLine($"Best move score: {bestMove.score}");
        return bestMove.move;
    }

    public EvaluatedMove[] EvaluateMoves(Move[] moves, Board board, int currentDepth = 1, int recursiveDepth = baseMoveDepth) {
        //Creates an array of evaluated moves equal to the amount of possible legal moves
        EvaluatedMove[] evaluatedMoves = new EvaluatedMove[moves.Length];

        //Consider all legal moves, with current and max depth set, by default this is base depth
        for (int i = 0; i < evaluatedMoves.Length; i++) {
            evaluatedMoves[i] = Consider(moves[i], board, currentDepth, recursiveDepth);
        }

        return evaluatedMoves;
    }

    public static EvaluatedMove GetBestMove(EvaluatedMove[] evaluatedMoves) {
        //Chooses a random move, if no move is deemed better, this is the move that is made.
        Random rng = new();
        EvaluatedMove bestMove = evaluatedMoves[rng.Next(evaluatedMoves.Length)];

        //Gets the best move
        for (int i = 0; i < evaluatedMoves.Length; i++) {
            if (isMyTurn) {
                if (evaluatedMoves[i].score > bestMove.score)
                    bestMove = evaluatedMoves[i];
            }
            else {
                if (evaluatedMoves[i].score < bestMove.score)
                    bestMove = evaluatedMoves[i];
            }
        }

            return bestMove;
    }

    public EvaluatedMove Consider(Move move, Board board, int currentDepth, int recursiveDepth = baseMoveDepth) {
        EvaluatedMove evalMove = new(move);
        int score = 0;

        //================================================
        //If the move is a capture move, how valuable would the captured piece be?
        if (move.IsCapture) {
            switch (move.CapturePieceType) {

                case PieceType.Pawn:
                    evalMove.interest = Interest.LOW;
                    score += PieceMultipler((int)PieceValues.PAWN);
                    break;
                case PieceType.Bishop:
                    evalMove.interest = Interest.MEDIUM;
                    score += PieceMultipler((int)PieceValues.BISHOP);
                    break;
                case PieceType.Knight:
                    evalMove.interest = Interest.MEDIUM;
                    score += PieceMultipler((int)PieceValues.KNIGHT);
                    break;
                case PieceType.Rook:
                    evalMove.interest = Interest.MEDIUM;
                    score += PieceMultipler((int)PieceValues.ROOK);
                    break;
                case PieceType.Queen:
                    evalMove.interest = Interest.MEDIUM;
                    score += PieceMultipler((int)PieceValues.QUEEN);
                    break;
                default:
                    evalMove.interest = Interest.NONE;
                    break;
            }
        }
        //================================================

        //================================================
        //Each piece has a movement cost, to discourage throwing valuable pieces at the enemy
        switch (move.MovePieceType) {
            case (PieceType.Pawn):
                score -= (int)MoveCost.PAWN;
                break;
            case (PieceType.Bishop):
                score -= (int)MoveCost.BISHOP;
                break;
            case (PieceType.Knight):
                score -= (int)MoveCost.KNIGHT;
                break;
            case (PieceType.Rook):
                score -= (int)MoveCost.ROOK;
                break;
            case (PieceType.Queen):
                score -= (int)MoveCost.QUEEN;
                break;
            case (PieceType.King):
                score -= (int)MoveCost.KING;
                break;

        }
        //================================================

        //================================================
        //Checks next move to see if its advantagous
        board.MakeMove(move);

        if (board.IsInCheck()) {
            evalMove.interest = Interest.HIGH;
            score += checkValue;
        }

        if (board.IsInCheckmate()) {
           score += checkMateValue;
        }

        if (board.IsDraw())
            score += drawValue;

        board.UndoMove(move);
        //================================================

        //================================================
        //Bonus scores given to special moves
        if (move.IsPromotion)
            score += promotionBonus;

        if (move.IsEnPassant)
            score += enPassantBonus;

        if (move.IsCastles)
            score += castleBonus;
        //===============================================

        //Moves that are made from the other player are negative, and are retracted from a moves scoring
        evalMove.score += (score / currentDepth) * TurnMultipler();

        //Bonus depth is added depending on how interesting that move was
        int depthToExplore = currentDepth == 1 ? recursiveDepth + (int)evalMove.interest : recursiveDepth;

        //Moves are checked recursively to decide whether a move would be good or not, the moves alternate players
        if (currentDepth != recursiveDepth) {
            board.MakeMove(move);
            isMyTurn = !isMyTurn;

            Move[] legalMoves = board.GetLegalMoves();
            if (legalMoves.Length > 0) {
                EvaluatedMove nextBestMove = GetBestMove(EvaluateMoves(board.GetLegalMoves(), board, currentDepth + 1, depthToExplore));
                evalMove.score += nextBestMove.score;
            }

            board.UndoMove(move);
            isMyTurn = !isMyTurn;
        }

        return evalMove;
    }

    //When its the enemies turn the scores are flipped.
    static int TurnMultipler() {
        if (isMyTurn)
            return 1;
        else
            return -2;
    }

    //Multplier that prioritises protecting high value pieces over taking pieces.
    static int PieceMultipler(int pieceValue) {
        if (isMyTurn)
            return 1;
        else
            return (int)(myPieceMultiplier * pieceValue);
    }
    }
}