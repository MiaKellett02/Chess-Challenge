using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using ChessChallenge.Application;

public class MyBot : IChessBot {
	private Board m_board;
	private bool isWhite;

	public Move Think(Board board, Timer timer) {
		//Cache the state of the board.
		m_board = board;

		//Get all the legal moves.
		Move[] moves = m_board.GetLegalMoves();

		//Get the colour of MyBot.
		isWhite = m_board.IsWhiteToMove;

		//Evalulate each move and choose best one.
		int highestValue = int.MinValue;
		Move bestMove = moves[0];
		foreach (Move move in moves) {
			//Evaluate the move.
			int value = Evaluate(move, 1);
			if (value > highestValue) {
				bestMove = move;
				highestValue = value;
			}
		}

		ConsoleHelper.Log("HighestValue: " + highestValue.ToString());
		return bestMove;
	}

	public int Evaluate(Move a_move, int a_deepness) {
		//Initalise return value.
		int boardValueAfterMove = 0;

		//Make move then get the score of the state of the board afterwards.
		m_board.MakeMove(a_move);

		if (m_board.IsInCheckmate()) {
			//Always do checkmate.
			m_board.UndoMove(a_move);
			return int.MaxValue;
		}

		int CHECK_VALUE = 100;
		if (m_board.IsInCheck()) {
			boardValueAfterMove += CHECK_VALUE;
		}

		int STALEMATE_VALUE = -100;
		if (m_board.IsInStalemate()) {
			boardValueAfterMove -= STALEMATE_VALUE;
		}

		int BLACK_MULTIPLIER;
		int WHITE_MULTIPLIER;
		if (isWhite) {
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

		PieceList[] allPieces = m_board.GetAllPieceLists();
		foreach (PieceList pieces in allPieces) {
			bool isWhitePieceList = pieces.IsWhitePieceList;
			if (isWhitePieceList) {
				boardValueAfterMove += (WHITE_MULTIPLIER * pieces.Count);
			} else {
				boardValueAfterMove += (BLACK_MULTIPLIER * pieces.Count);
			}
		}

		//Return board to original state.
		m_board.UndoMove(a_move);

		//Return the value.
		return boardValueAfterMove;
	}
}