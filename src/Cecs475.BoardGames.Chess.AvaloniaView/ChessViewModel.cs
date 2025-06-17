using Cecs475.BoardGames;
using Cecs475.BoardGames.AvaloniaView;
using Cecs475.BoardGames.Chess.Model;
using Cecs475.BoardGames.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Cecs475.BoardGames.Chess.AvaloniaView {

    /// <summary>
    /// Represents one square on the Chess board grid.
    /// </summary>
    public class ChessSquare : INotifyPropertyChanged
    {
        public ChessSquare Self => this;

        private int mPlayer;
        /// <summary>
        /// The player that has a piece in the given square, or 0 if empty.
        /// </summary>
        public int Player
        {
            get { return mPlayer; }
            set
            {
                if (value != mPlayer)
                {
                    mPlayer = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The position of the square.
        /// </summary>
        public BoardPosition Position
        {
            get; set;
        }


        private bool mIsHighlighted;
        /// <summary>
        /// Whether the square should be highlighted because of a user action.
        /// </summary>
        public bool IsHighlighted
        {
            get { return mIsHighlighted; }
            set
            {
                if (value != mIsHighlighted)
                {
                    mIsHighlighted = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool mIsSelected;
        /// <summary>
        /// Whether the square should be highlighted because of a user action.
        /// </summary>
        public bool IsSelected
        {
            get { return mIsSelected; }
            set
            {
                if (value != mIsSelected)
                {
                    mIsSelected = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool mIsKingInCheck;
        /// <summary>
        /// Whether the square should be highlighted because of a user action.
        /// </summary>
        public bool IsKingInCheck
        {
            get { return mIsKingInCheck; }
            set
            {
                if (value != mIsKingInCheck)
                {
                    mIsKingInCheck = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool mIsPossibleMoveHover;
        /// <summary>
        /// Whether the square should be highlighted because of a user action.
        /// </summary>
        public bool IsPossibleMoveHover
        {
            get { return mIsPossibleMoveHover; }
            set
            {
                if (value != mIsPossibleMoveHover)
                {
                    mIsPossibleMoveHover = value;
                    OnPropertyChanged();
                }
            }
        }


        private ChessPiece mPiece;
        public ChessPiece Piece
        {
            get => mPiece;
            set
            {
                mPiece = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public override string ToString()
        {
            return $"Square {Position}";
        }
    }

    /// <summary>
    /// Composes a 
    /// </summary>
    public class ChessViewModel : INotifyPropertyChanged, IGameViewModel {
        private readonly ChessBoard mBoard;
        private readonly ObservableCollection<ChessSquare> mSquares;

        public ChessViewModel() {
            mBoard = new ChessBoard();
            mSquares = new ObservableCollection<ChessSquare>(
                BoardPosition.GetRectangularPositions(8, 8)
                .Select(pos => new ChessSquare
                {
                    Position = pos,
                    Player = mBoard.GetPlayerAtPosition(pos),
                    Piece = mBoard.GetPieceAtPosition(pos)
                })
            );


            PossibleMoves = new HashSet<BoardPosition>(mBoard.GetPossibleMoves().Select(m => m.EndPosition));
        }
        /// <summary>
		/// Applies a move for the current player at the given position.
		/// </summary>
		public void ApplyMove(ChessMove move)
        {
            mBoard.ApplyMove(move);
            RebindState();

            if (mBoard.IsFinished)
            {
                GameFinished?.Invoke(this, EventArgs.Empty);
            }
        }

        public IGameBoard GetBoard() => mBoard;

        public bool CanApplyAIMove => !mBoard.IsFinished; // have to set the property to prevent race condition

        public void ApplyMove(IGameMove move)
        {
            mBoard.ApplyMove((ChessMove)move);
            RebindState();
        }

        private void RebindState()
        {
            // Rebind the possible moves.
            PossibleMoves = new HashSet<BoardPosition>(
                from ChessMove m in mBoard.GetPossibleMoves()
                select m.StartPosition
            );

            // Update the collection of squares by examining the new board state.
            var newSquares = BoardPosition.GetRectangularPositions(8, 8);
            int i = 0;
            foreach (var pos in newSquares)
            {
                var newPiece = mBoard.GetPieceAtPosition(pos);

                mSquares[i].Piece = new ChessPiece(ChessPieceType.Empty, 0); 
                mSquares[i].Piece = newPiece;
                mSquares[i].Player = newPiece.Player;

                mSquares[i].IsHighlighted = false;
                mSquares[i].IsSelected = false;
                mSquares[i].IsKingInCheck = false;
                mSquares[i].IsPossibleMoveHover = false;

                if (newPiece.PieceType == ChessPieceType.King &&
                    newPiece.Player == mBoard.CurrentPlayer &&
                    mBoard.IsCheck)
                {
                    mSquares[i].IsKingInCheck = true;
                }
                i++;
            }




            OnPropertyChanged(nameof(BoardAdvantage));
            OnPropertyChanged(nameof(CurrentPlayer));
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanApplyAIMove));
        }

        public ObservableCollection<ChessSquare> Squares
        {
            get { return mSquares; }
        }

        public HashSet<BoardPosition> PossibleMoves
        {
            get; private set;
        }

        public IEnumerable<ChessMove> GetMovesFromPosition(BoardPosition start)
        {
            return mBoard.GetPossibleMoves().Where(m => m.StartPosition.Equals(start));
        }
        public bool TryGetMove(BoardPosition start, BoardPosition end, out ChessMove? move)
        {
            move = mBoard.GetPossibleMoves()
                .OfType<ChessMove>()
                .FirstOrDefault(m => m.StartPosition.Equals(start) && m.EndPosition.Equals(end));
            return move != null;
        }



        public int CurrentPlayer
        {
            get { return mBoard.CurrentPlayer; }
        }

        public bool CanUndo => mBoard.MoveHistory.Any();

        public GameAdvantage BoardAdvantage => mBoard.CurrentAdvantage;


        public void UndoMove() {
            if (CanUndo)
            {
                mBoard.UndoLastMove();
                
                if (Players == NumberOfPlayers.One && CanUndo)
                {
                    mBoard.UndoLastMove();
                }
                RebindState();
            }
        }

		// Invoke this event after applying a move if the game is now finished.
		public event EventHandler? GameFinished;
        public NumberOfPlayers Players { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;

		private void OnPropertyChanged([CallerMemberName]string? name = null) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

	}
}
