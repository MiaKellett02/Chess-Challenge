using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot {
	//Consts.
	private const int EVALUATION_RECURSIVE_DEPTH = 3;//Keep this as an odd number so it always ends on MyBots move.

	//Variables.
	private Board m_board;
	private int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };// Piece values: null, pawn, knight, bishop, rook, queen, king


	//Debug variables.
	private int highestValueLastTime;

	public Move Think(Board board, Timer timer) {
		//Cache the state of the board.
		m_board = board;

		//Get all the legal moves.
		Move[] moves = m_board.GetLegalMoves();

		//Evalulate each move and choose best one.
		int highestValue = int.MinValue;
		Move bestMove = moves[0];
		foreach (Move move in moves) {
			//Evaluate the move.
			int value = Evaluate(move, EVALUATION_RECURSIVE_DEPTH);
			if (value > highestValue) {
				bestMove = move;
				highestValue = value;
			}
		}

		//DEBUG
		if (highestValueLastTime != highestValue) {
			highestValueLastTime = highestValue;
			ChessChallenge.Application.ConsoleHelper.Log("HighestValue: " + highestValue.ToString());
		}

		//Return the move to make.
		return bestMove;
	}

	public int Evaluate(Move a_move, int a_deepness) {
		int currentDepth = a_deepness - 1;
		//Initalise return value.
		int boardValueAfterMove = 0;

		// add capture piece value to return value.
		Piece capturedPiece = m_board.GetPiece(a_move.TargetSquare);
		int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
		boardValueAfterMove += capturedPieceValue;

		//Get the current turn's colour.
		bool currentTurnIsWhite = m_board.IsWhiteToMove;

		//Make move then get the score of the state of the board afterwards.
		m_board.MakeMove(a_move);

		int CHECKMATE_VALUE = 1000000000;
		if (m_board.IsInCheckmate()) {
			//Always do checkmate.
			m_board.UndoMove(a_move);
			return CHECKMATE_VALUE;
		}

		int CHECK_VALUE = 100000;
		if (m_board.IsInCheck()) {
			boardValueAfterMove += CHECK_VALUE;
		}

		int DRAW_VALUE = -100000;
		if(m_board.IsDraw()) { //Stalemate, draw, repetition, insuffcient material etc...
			boardValueAfterMove -= DRAW_VALUE;
		}

		int MOVING_KING_VALUE = -10000;
		int CASTLES_VALUE = 1000;
		if (a_move.IsCastles) {
			boardValueAfterMove += CASTLES_VALUE;
		}else if (a_move.StartSquare == m_board.GetKingSquare(currentTurnIsWhite)) {
			boardValueAfterMove += MOVING_KING_VALUE;
		}

		int ENPASSANT_VALUE = 1000;
		if (a_move.IsEnPassant) {
			boardValueAfterMove += ENPASSANT_VALUE;
		}

		int CAPTURE_VALUE = 500;
		if (a_move.IsCapture) {
			boardValueAfterMove += CAPTURE_VALUE;
		}

		int BLACK_MULTIPLIER;
		int WHITE_MULTIPLIER;
		if (currentTurnIsWhite) {
			//Count white pieces vs black pieces.
			//White pieces give positive value and black pieces give negative value.
			BLACK_MULTIPLIER = -1;
			WHITE_MULTIPLIER = 1;
		} else {
			//Count white pieces vs black pieces.
			//White pieces give negative value and black pieces give positive value.
			BLACK_MULTIPLIER = 1;
			WHITE_MULTIPLIER = -1;
		}

		//Get the value of the whole board.
		PieceList[] allPieces = m_board.GetAllPieceLists();
		foreach (PieceList pieces in allPieces) {
			bool isWhitePieceList = pieces.IsWhitePieceList;
			int piecesValuePlusCount = pieces.Count * pieceValues[(int)pieces.TypeOfPieceInList];
			if (isWhitePieceList) {
				boardValueAfterMove += (WHITE_MULTIPLIER * piecesValuePlusCount);
			} else {
				boardValueAfterMove += (BLACK_MULTIPLIER * piecesValuePlusCount);
			}
		}

		if (currentDepth > 0) {
			//Get list of next posisble moves.
			Move[] nextMoves = m_board.GetLegalMoves();

			//Evaluate each of those moves with 1 less depth than the previous call of evaluate.
			//When it reaches 0 then the recursive loop will exit with the best approximate move.
			int highestScore = int.MinValue;
			foreach (Move move in nextMoves) {
				int moveScore = -Evaluate(move, currentDepth); //Inverts the evaluation score as what's best for the next player won't be best for the current player.
				if (moveScore > highestScore) {
					highestScore = moveScore;
				}
			}

			//add the score to the moves score.
			boardValueAfterMove += highestScore;
		}

		//Return board to original state.
		m_board.UndoMove(a_move);

		//Return the value.
		return boardValueAfterMove;
	}
}