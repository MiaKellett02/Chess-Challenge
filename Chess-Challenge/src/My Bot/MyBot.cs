using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using ChessChallenge.Application;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class MyBot : IChessBot {
	//Consts.
	private const int EVALUATION_RECURSIVE_DEPTH = 2;

	//Variables.
	private Board m_board;
	private bool isMyBotWhite;

	//Debug variables.
	private int highestValueLastTime;

	public Move Think(Board board, Timer timer) {
		//Cache the state of the board.
		m_board = board;

		//Get all the legal moves.
		Move[] moves = m_board.GetLegalMoves();

		//Get the colour of MyBot.
		isMyBotWhite = m_board.IsWhiteToMove;

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
			ConsoleHelper.Log("HighestValue: " + highestValue.ToString());
		}

		//Return the move to make.
		return bestMove;
	}

	public int Evaluate(Move a_move, int a_deepness) {
		int currentDepth = a_deepness - 1;
		//Initalise return value.
		int boardValueAfterMove = 0;

		//Get the current turn's colour.
		bool currentTurnIsWhite = m_board.IsWhiteToMove;

		//Make move then get the score of the state of the board afterwards.
		m_board.MakeMove(a_move);

		//Get the next turns colour.
		bool nextTurnIsWhite = m_board.IsWhiteToMove;

		int CHECKMATE_VALUE = 1000000;
		if (m_board.IsInCheckmate()) {
			//Always do checkmate.
			m_board.UndoMove(a_move);
			return CHECKMATE_VALUE;
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

		PieceList[] allPieces = m_board.GetAllPieceLists();
		foreach (PieceList pieces in allPieces) {
			bool isWhitePieceList = pieces.IsWhitePieceList;
			if (isWhitePieceList) {
				boardValueAfterMove += (WHITE_MULTIPLIER * pieces.Count);
			} else {
				boardValueAfterMove += (BLACK_MULTIPLIER * pieces.Count);
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

			//Set the main score to the total as we want to ignore the current state of the board and only think of the best state further on in the board.
			boardValueAfterMove = highestScore;
		}

		//Return board to original state.
		m_board.UndoMove(a_move);

		//Return the value.
		return boardValueAfterMove;
	}
}