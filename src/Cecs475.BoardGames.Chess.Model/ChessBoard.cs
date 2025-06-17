using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO.Pipes;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Cecs475.BoardGames.Chess.Model;
using Cecs475.BoardGames.Model;

#pragma warning disable 1591 // disable warning about missing XML documentation.

namespace Cecs475.BoardGames.Chess.Model
{
    /// <summary>
    /// Represents the board state of a game of chess. Tracks which squares of the 8x8 board are occupied
    /// by which player's pieces.
    /// </summary>
    public class ChessBoard : IGameBoard
    {
        #region Member fields.
        public long BoardWeight
        {
            get
            {
                int whiteScore = 0;
                int blackScore = 0;

                // Adding points for ownership of pieces using the Advantage from CurrentAdvantage
                if (CurrentAdvantage.Player == 1)
                {
                    whiteScore += CurrentAdvantage.Advantage;
                }
                else if (CurrentAdvantage.Player == 2)
                {
                    blackScore += CurrentAdvantage.Advantage;
                }

                // Calculate each square a pawn has moved forward for both players
                foreach (var pos in GetPositionsOfPiece(ChessPieceType.Pawn, 1))
                {
                    whiteScore += 6 - pos.Row; // White pawns start on row 6 of 
                }
                foreach (var pos in GetPositionsOfPiece(ChessPieceType.Pawn, 2))
                {
                    blackScore += pos.Row - 1; // Black pawns start on row 1
                }

                // Calculate points for friendly piece threatening 
                ChessPieceType[] protectedPieces = new ChessPieceType[] { ChessPieceType.Knight, ChessPieceType.Bishop };

                var whiteProtect = GetAttackedPositions(1);
                foreach (ChessPieceType piece in protectedPieces)
                {
                    foreach (var pos in GetPositionsOfPiece(piece, 1))
                    {
                        if (whiteProtect.Contains(pos)) // threatened square == protected by teammate
                        {
                            whiteScore++;
                        }
                    }
                }

                var blackProtect = GetAttackedPositions(2);
                foreach (ChessPieceType piece in protectedPieces)
                {
                    foreach (var pos in GetPositionsOfPiece(piece, 2))
                    {
                        if (blackProtect.Contains(pos))
                        {
                            blackScore++;
                        }
                    }
                }

                // Calculate current players threats against enemy pieces. Grabs certain piece type of enemy, then checks if the current player is threatening any of those posiitons of the pieces.
                foreach (var pos in GetPositionsOfPiece(ChessPieceType.Knight, 2))
                {
                    if (whiteProtect.Contains(pos))
                    {
                        whiteScore++;
                    }
                }
                foreach (var pos in GetPositionsOfPiece(ChessPieceType.Bishop, 2))
                {
                    if (whiteProtect.Contains(pos))
                    {
                        whiteScore++;
                    }
                }
                foreach (var pos in GetPositionsOfPiece(ChessPieceType.Rook, 2))
                {
                    if (whiteProtect.Contains(pos))
                    {
                        whiteScore += 2;
                    }
                }
                foreach (var pos in GetPositionsOfPiece(ChessPieceType.Queen, 2))
                {
                    if (whiteProtect.Contains(pos))
                    {
                        whiteScore += 5;
                    }
                }
                foreach (var pos in GetPositionsOfPiece(ChessPieceType.King, 2))
                {
                    if (whiteProtect.Contains(pos))
                    {
                        whiteScore += 4;
                    }
                }

                foreach (var pos in GetPositionsOfPiece(ChessPieceType.Knight, 1))
                {
                    if (blackProtect.Contains(pos))
                    {
                        blackScore++;
                    }
                }
                foreach (var pos in GetPositionsOfPiece(ChessPieceType.Bishop, 1))
                {
                    if (blackProtect.Contains(pos))
                    {
                        blackScore++;
                    }
                }
                foreach (var pos in GetPositionsOfPiece(ChessPieceType.Rook, 1))
                {
                    if (blackProtect.Contains(pos))
                    {
                        blackScore += 2;
                    }
                }
                foreach (var pos in GetPositionsOfPiece(ChessPieceType.Queen, 1))
                {
                    if (blackProtect.Contains(pos))
                    {
                        blackScore += 5;
                    }
                }
                foreach (var pos in GetPositionsOfPiece(ChessPieceType.King, 1))
                {
                    if (blackProtect.Contains(pos))
                    {
                        blackScore += 4;
                    }
                }

                return whiteScore - blackScore;
            }
        }
        
        public const int BoardSize = 8;
        // The history of moves applied to the board.
        private List<ChessMove> mMoveHistory = new List<ChessMove>();
        private List<ChessPiece> capturedPieces = new List<ChessPiece>();
        private List<BoardPosition?> mPreviousEnPassantTargetHistory = new List<BoardPosition?>();


        // Uses same logic: private bool capture = false;
        private List<bool> captureHistory = new List<bool>();

        public int DrawCounter = 0;
        private int CounterForUndo = 0;

        // TODO: Add fields to implement bitboards for the black and white pieces.
        private byte[,] chessBoard;


        // TODO: Add a means of tracking miscellaneous board state, like captured pieces and the 50-move rule.
        private bool hasWhiteKingMoved = false;
        private bool hasWhiteKingSideRookMoved = false;
        private bool hasWhiteQueenSideRookMoved = false;
        private bool hasBlackKingMoved = false;
        private bool hasBlackKingSideRookMoved = false;
        private bool hasBlackQueenSideRookMoved = false;

        private BoardPosition? enPassantTarget = null;

        public int whiteAdvantage = 0;
        public int blackAdvantage = 0;

        private static Dictionary<ChessPieceType, int> PieceValues = new Dictionary<ChessPieceType, int>
        {
            { ChessPieceType.Empty, 0},
            { ChessPieceType.Pawn, 1},
            { ChessPieceType.Knight, 3},
            { ChessPieceType.Bishop, 3},
            { ChessPieceType.Rook, 5},
            { ChessPieceType.Queen, 9},
            { ChessPieceType.King, 109090934 } // set to a high value
        };
        #endregion

        #region Auto properties.
        public int CurrentPlayer { get; private set; }

        public GameAdvantage CurrentAdvantage { get; set; }
        #endregion

        #region Computed properties
        public bool IsFinished
        {
            get
            {
                return IsCheckmate || IsStalemate || DrawCounter >= 100;
            }
        }

        private HashSet<BoardPosition> checkedPositions = new HashSet<BoardPosition>();
        public IReadOnlyList<ChessMove> MoveHistory => mMoveHistory;
        public bool PositionIsAttacked(BoardPosition pos, int player)
        {
            if (checkedPositions.Contains(pos)) return false;
            checkedPositions.Add(pos);

            bool result = GetAttackedPositions(player).Contains(pos);
            checkedPositions.Remove(pos);
            return result;
        }

        private int checkDepth = 0;
        private const int maxCheckDepth = 5;

        public bool IsCheck
        {
            get
            {
                if (checkDepth > maxCheckDepth) return true;
                checkDepth++;

                BoardPosition king = FindKing(CurrentPlayer);

                bool isInCheck = IsPositionAttacked(king, CurrentPlayer);

                checkDepth--;

                return isInCheck;
            }
        }

        public bool IsCheckmate
        {
            get
            {
                if (!IsCheck) return false;
                var possibleMoves = GetPossibleMoves();
                foreach (var move in possibleMoves)
                {
                    if (IsMoveLegal(move))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool IsMoveLegal(ChessMove move)
        {
            // Get the piece to move and the captured piece
            ChessPiece pieceToMove = GetPieceAtPosition(move.StartPosition);
            ChessPiece capturedPiece = GetPieceAtPosition(move.EndPosition);

            // Temporarily make the move
            SetPieceAtPosition(move.EndPosition, pieceToMove);
            SetPieceAtPosition(move.StartPosition, ChessPiece.Empty);

            // Check if the current player's king is in check after the move
            bool isKingInCheck = IsCheck;

            // Undo the move (restore the board to its original state)
            SetPieceAtPosition(move.StartPosition, pieceToMove);
            SetPieceAtPosition(move.EndPosition, capturedPiece);

            // If the king is in check, the move is not legal
            return !isKingInCheck;
        }

        public bool IsStalemate
        {
            get
            {
                if (!IsCheck && !GetPossibleMoves().Any() || DrawCounter >= 100)
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsThreefoldRepetition()
        {
            // Group the moves by their representation and count their occurrences
            var repeatedMoves = MoveHistory
                .GroupBy(move => move)
                .Where(group => group.Count() >= 3)
                .Select(group => new { Move = group.Key, Count = group.Count() })
                .ToList();

            return repeatedMoves.Any();
        }


        public bool IsFiftyMoveRule()
        {
            if (DrawCounter >= 100)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsInsufficientMaterial()
        {
            // Count the number of pieces for each player (assuming GetPieceAtPosition returns a piece object)
            int whiteKings = 0, whiteKnights = 0, whiteBishops = 0;
            int blackKings = 0, blackKnights = 0, blackBishops = 0;

            // Iterate over all positions on the board
            foreach (BoardPosition pos in BoardPosition.GetRectangularPositions(BoardSize, BoardSize))
            {
                ChessPiece piece = GetPieceAtPosition(pos);

                if (piece.PieceType != ChessPieceType.Empty)
                {
                    if (CurrentPlayer == 1)
                    {
                        if (piece.PieceType == ChessPieceType.King) whiteKings++;
                        if (piece.PieceType == ChessPieceType.Knight) whiteKnights++;
                        if (piece.PieceType == ChessPieceType.Bishop) whiteBishops++;
                    }
                    else if (CurrentPlayer == 2)
                    {
                        if (piece.PieceType == ChessPieceType.King) blackKings++;
                        if (piece.PieceType == ChessPieceType.Knight) blackKnights++;
                        if (piece.PieceType == ChessPieceType.Bishop) blackBishops++;
                    }
                }
            }

            bool isWhiteInsufficient = (whiteKings == 1 && (whiteKnights + whiteBishops) <= 1);
            bool isBlackInsufficient = (blackKings == 1 && (blackKnights + blackBishops) <= 1);

            return isWhiteInsufficient && isBlackInsufficient;
        }

        public bool IsDraw
        {
            get
            {
                if (IsStalemate || IsFiftyMoveRule())
                {
                    return true;
                }
                return false;
            }
        }

        public void GetAdvantage()
        {
            if (whiteAdvantage == blackAdvantage)
            {
                CurrentAdvantage = new GameAdvantage(0, 0);
            }
            else if (whiteAdvantage > blackAdvantage)
            {
                CurrentAdvantage = new GameAdvantage(1, whiteAdvantage - blackAdvantage);
            }
            else
            {
                CurrentAdvantage = new GameAdvantage(2, blackAdvantage - whiteAdvantage);
            }

        }
        #endregion


        #region Constructors.


        public ChessBoard()
        {
            CurrentPlayer = 1;
            chessBoard = new byte[BoardSize, 4];
            byte[,] board = {
                {0b10101011, 0b11001101, 0b11101100, 0b10111010}, // Black starting pieces
				{0b10011001, 0b10011001, 0b10011001, 0b10011001}, // Black row of pawns
				{0b00000000, 0b00000000, 0b00000000, 0b00000000},
                {0b00000000, 0b00000000, 0b00000000, 0b00000000},
                {0b00000000, 0b00000000, 0b00000000, 0b00000000},
                {0b00000000, 0b00000000, 0b00000000, 0b00000000},
                {0b00010001, 0b00010001, 0b00010001, 0b00010001}, // White row of pawns
				{0b00100011, 0b01000101, 0b01100100, 0b00110010}  // White starting pieces
			};

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    chessBoard[row, col] = board[row, col];
                }
            }
        }

        /// <summary>
        /// To make copy of current chessboard for testing 
        /// </summary>
        /// <param name="original"></param>
        public ChessBoard(ChessBoard original)
        {
            // Copy basic properties
            this.CurrentPlayer = 3 - original.CurrentPlayer;

            // Deep copy the board
            this.chessBoard = new byte[BoardSize, 4];
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    this.chessBoard[row, col] = original.chessBoard[row, col]; // Copy bytes
                }
            }
            this.mMoveHistory = new List<ChessMove>(original.mMoveHistory);

        }

        // Cloning method for AI to test on and not modify actual board state
        public ChessBoard AIChessBoard()
        {
            var copy = new ChessBoard();

            copy.CurrentPlayer = CurrentPlayer;
            copy.whiteAdvantage = whiteAdvantage;
            copy.blackAdvantage = blackAdvantage;
            copy.DrawCounter = DrawCounter;
            copy.CounterForUndo = CounterForUndo;

            copy.hasWhiteKingMoved = hasWhiteKingMoved;
            copy.hasWhiteKingSideRookMoved = hasWhiteKingSideRookMoved;
            copy.hasWhiteQueenSideRookMoved = hasWhiteQueenSideRookMoved;
            copy.hasBlackKingMoved = hasBlackKingMoved;
            copy.hasBlackKingSideRookMoved = hasBlackKingSideRookMoved;
            copy.hasBlackQueenSideRookMoved = hasBlackQueenSideRookMoved;

            copy.enPassantTarget = enPassantTarget;

            // Copies current board state to copy ChessBoard
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    copy.chessBoard[row, col] = chessBoard[row, col];
                }
            }

            
            copy.mMoveHistory = mMoveHistory
                .Select(m => m is PawnPromotionChessMove pm
                    ? new PawnPromotionChessMove(pm.StartPosition, pm.EndPosition, pm.SelectedPromotion)
                    : new ChessMove(m.StartPosition, m.EndPosition, m.MoveType))
                .ToList();

            
            copy.capturedPieces = capturedPieces
                .Select(p => new ChessPiece(p.PieceType, p.Player))
                .ToList();

            copy.mPreviousEnPassantTargetHistory = new List<BoardPosition?>(mPreviousEnPassantTargetHistory);

            copy.captureHistory = new List<bool>(captureHistory);

            copy.CurrentAdvantage = new GameAdvantage(CurrentAdvantage.Player, CurrentAdvantage.Advantage);

            return copy;
        }

        public ChessBoard(IEnumerable<Tuple<BoardPosition, ChessPiece>> startingPositions)
            : this()
        {


            var king1 = startingPositions.Where(t => t.Item2.Player == 1 && t.Item2.PieceType == ChessPieceType.King);
            var king2 = startingPositions.Where(t => t.Item2.Player == 2 && t.Item2.PieceType == ChessPieceType.King);
            if (king1.Count() != 1 || king2.Count() != 1)
            {
                throw new ArgumentException("A chess board must have a single king for each player");
            }

            foreach (var position in BoardPosition.GetRectangularPositions(8, 8))
            {
                SetPieceAtPosition(position, ChessPiece.Empty);
            }

            foreach (var (position, piece) in startingPositions)
            {
                SetPieceAtPosition(position, piece);
            }

            // TODO:
            // Compute the current advantage of the board, and set the CurrentAdvantage property appropriately.

        }
        #endregion



        #region Public methods.
        /// <summary>
        /// Returns squares attacked by player.
        /// </summary>
        /// 
        public IEnumerable<BoardPosition> GetAttackedPositions(int player)
        {
            return BoardPosition.GetRectangularPositions(BoardSize, BoardSize)
                .SelectMany(x => {
                    ChessPiece piece = GetPieceAtPosition(x); // gets piece at position 'x'
                    return (player == piece.Player) // checks if given player is the player of the extracted piece
                        ? PieceAttackPositions(x, piece) // gets attacked squares sequence (if any)
                        : Enumerable.Empty<BoardPosition>(); // returns empty sequence of attacks if none
                });
        }

        public IEnumerable<BoardPosition> PieceAttackPositions(BoardPosition pos, ChessPiece piece)
        {
            if (piece.PieceType == ChessPieceType.Pawn)
            {
                return PawnAttacks(pos, piece);
            }
            else if (piece.PieceType == ChessPieceType.Rook || piece.PieceType == ChessPieceType.Queen || piece.PieceType == ChessPieceType.Bishop)
            {
                return DiagonalPiecesAttacks(pos, piece);
            }
            else if (piece.PieceType == ChessPieceType.King)
            {
                return KingAttacks(pos, piece);
            }
            else if (piece.PieceType == ChessPieceType.Knight)
            {
                return KnightAttacks(pos, piece);
            }
            else
            {
                return Enumerable.Empty<BoardPosition>();
            }
        }


        /// <summary>
        /// Returns every position where the given piece owned by the given player can be found.
        /// </summary>
        /// 
        public IEnumerable<BoardPosition> GetPositionsOfPiece(ChessPieceType pieceType, int forPlayer)
        {
            return BoardPosition.GetRectangularPositions(BoardSize, BoardSize)
                .Where(pos => {
                    var boardPiece = GetPieceAtPosition(pos);
                    return boardPiece.PieceType == pieceType && boardPiece.Player == forPlayer;
                });
        }

        public IEnumerable<ChessMove> GetPossibleMoves()
        {
            List<ChessMove> possibleMoves = new List<ChessMove>();

            foreach (BoardPosition pos in BoardPosition.GetRectangularPositions(BoardSize, BoardSize))
            {
                ChessPiece piece = GetPieceAtPosition(pos);
                // Only consider moves for the current player's pieces
                if (piece.Player != CurrentPlayer)
                {
                    continue;
                }

                // Get attackable positions (reused logic)
                IEnumerable<BoardPosition> attackablePositions = PieceAttackPositions(pos, piece);

                foreach (BoardPosition target in attackablePositions)
                {
                    ChessMove move = new ChessMove(pos, target);

                    // Simulate move and check if king remains safe
                    if (DoesMoveLeaveKingInCheck(move))
                    {
                        continue;
                    }
                    if (!PositionIsEmpty(target) && !PositionIsEnemy(target, CurrentPlayer))
                    {
                        continue;
                    }

                    // Special rules for pawns
                    if (piece.PieceType == ChessPieceType.Pawn)
                    {
                        // Handle Pawn Promotion
                        if (target.Row == 0 || target.Row == 7)
                        {
                            ChessPieceType[] promotionPieces = { ChessPieceType.Queen, ChessPieceType.Rook, ChessPieceType.Knight, ChessPieceType.Bishop };
                            foreach (var promotion in promotionPieces)
                            {
                                possibleMoves.Add(new PawnPromotionChessMove(pos, target, promotion));
                            }
                            continue;
                        }
                        if (enPassantTarget != null)
                        {
                            BoardPosition t = enPassantTarget.Value;

                            if (piece.PieceType == ChessPieceType.Pawn &&
                                Math.Abs(pos.Col - t.Col) == 1 &&
                                pos.Row == t.Row + ((piece.Player == 1) ? 1 : -1))
                            {
                                if (!possibleMoves.Contains(new ChessMove(pos, t, ChessMoveType.EnPassant)))
                                {
                                    possibleMoves.Add(new ChessMove(pos, t, ChessMoveType.EnPassant));
                                }
                            }
                        }
                    }

                    // King-Specific Move Rules
                    if (piece.PieceType == ChessPieceType.King)
                    {
                        // Castling Moves
                        BoardPosition kingSideTarget = new BoardPosition(pos.Row, pos.Col + 2);
                        if (CanKingSideCastle(pos, kingSideTarget) && !possibleMoves.Contains(new ChessMove(pos, kingSideTarget, ChessMoveType.CastleKingSide)))
                        {
                            possibleMoves.Add(new ChessMove(pos, kingSideTarget, ChessMoveType.CastleKingSide));

                        }

                        BoardPosition queenSideTarget = new BoardPosition(pos.Row, pos.Col - 2);
                        if (CanQueenSideCastle(pos, queenSideTarget) && !possibleMoves.Contains(new ChessMove(pos, queenSideTarget, ChessMoveType.CastleQueenSide)))
                        {
                            possibleMoves.Add(new ChessMove(pos, queenSideTarget, ChessMoveType.CastleQueenSide));
                        }

                    }

                    possibleMoves.Add(move);
                }
            }
            return possibleMoves;
        }

        private bool CanKingSideCastle(BoardPosition pos, BoardPosition target)
        {

            if (MeetsCastlingRequirements(GetPlayerAtPosition(pos)))
            {
                int row = pos.Row;
                if (GetPlayerAtPosition(pos) == 1)
                {
                    if (hasWhiteKingMoved || hasWhiteKingSideRookMoved) return false;
                }
                else
                {
                    if (hasBlackKingMoved || hasBlackKingSideRookMoved) return false;
                }
                ChessPiece rook = GetPieceAtPosition(new BoardPosition(row, 7));
                if (rook.PieceType != ChessPieceType.Rook || rook.Player != CurrentPlayer) return false;


                for (int col = 5; col <= 6; col++)
                {
                    if (GetPieceAtPosition(new BoardPosition(row, col)).PieceType != ChessPieceType.Empty)
                    {
                        return false;
                    }
                }

                for (int col = 4; col <= 6; col++)
                {
                    if (IsPositionAttacked(new BoardPosition(row, col), CurrentPlayer))
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CanQueenSideCastle(BoardPosition pos, BoardPosition target)
        {
            int player = GetPlayerAtPosition(pos);
            int row = pos.Row;

            if (player == 1)
            {
                if (hasWhiteKingMoved || hasWhiteQueenSideRookMoved) return false;
            }
            else
            {
                if (hasBlackKingMoved || hasBlackQueenSideRookMoved) return false;
            }

            ChessPiece rook = GetPieceAtPosition(new BoardPosition(row, 0)); // a1 or a8
            if (rook.PieceType != ChessPieceType.Rook || rook.Player != player) return false;

            for (int col = 1; col <= 3; col++)
            {
                if (GetPieceAtPosition(new BoardPosition(row, col)).PieceType != ChessPieceType.Empty)
                {
                    return false; // Path is blocked
                }
            }

            for (int col = 2; col <= 4; col++)
            {
                if (IsPositionAttacked(new BoardPosition(row, col), player))
                {
                    return false; // The king cannot castle through or into check
                }
            }
            return true;
        }

        // Checks to see if a king can castle
        private bool MeetsCastlingRequirements(int player)
        {
            if (player == 1) // White
            {
                return !hasWhiteKingMoved && (!hasWhiteKingSideRookMoved || !hasWhiteQueenSideRookMoved) && !IsCheck;
            }
            else // Black
            {
                return !hasBlackKingMoved && (!hasBlackKingSideRookMoved || !hasBlackQueenSideRookMoved) && !IsCheck;
            }
        }

        /// <summary>
        /// The following are methods to help verify a valid move, especially those for kings and pawns
        /// </summary>
        /// <param name="m"></param>

        private BoardPosition FindKing(int player)
        {
            foreach (BoardPosition pos in BoardPosition.GetRectangularPositions(BoardSize, BoardSize))
            {
                ChessPiece piece = GetPieceAtPosition(pos);
                if (piece.PieceType == ChessPieceType.King && piece.Player == player)
                {
                    return pos;
                }
            }
            // FOR TESTING: DO NOT REMOVE
            throw new Exception("King not found, which should never happen in a valid game.");
            
        }

        private bool DoesMoveLeaveKingInCheck(ChessMove move)
        {
            ChessBoard tempBoard = new ChessBoard(this);

            tempBoard.ApplyMove(move);

            return tempBoard.IsCheck;
        }

        private bool IsPositionAttacked(BoardPosition pos, int player)
        {
            foreach (BoardPosition opponentPos in BoardPosition.GetRectangularPositions(BoardSize, BoardSize))
            {
                ChessPiece opponentPiece = GetPieceAtPosition(opponentPos);
                if (opponentPiece.Player != player && opponentPiece.PieceType != ChessPieceType.Empty)
                {
                    IEnumerable<BoardPosition> attacks = PieceAttackPositions(opponentPos, opponentPiece);

                    if (attacks.Contains(pos))
                    {
                        return true;  // The position is under attack by this opponent piece
                    }
                }
            }
            return false;  // No opponent piece can attack the position
        }

        public void ApplyMove(ChessMove move)
        {
            BoardPosition start = move.StartPosition;
            BoardPosition end = move.EndPosition;
            ChessPiece movingPiece = GetPieceAtPosition(start);
            ChessPiece capturedPiece = GetPieceAtPosition(end);

            // Update draw counter based on move type
            if (movingPiece.PieceType == ChessPieceType.Pawn || move.MoveType == ChessMoveType.EnPassant || capturedPiece.PieceType != ChessPieceType.Empty)
            {
                CounterForUndo = DrawCounter;
                DrawCounter = 0;
            }
            else
            {
                DrawCounter++;
            }

            // BUGGY CODE HERE: AI moves bug out at En Passant

            //mPreviousEnPassantTargetHistory.Add(enPassantTarget);

            //// Track En Passant Possibility
            //if (movingPiece.PieceType == ChessPieceType.Pawn && Math.Abs(start.Row - end.Row) == 2)
            //{
            //    enPassantTarget = new BoardPosition((start.Row + end.Row) / 2, start.Col);
            //}
            //else
            //{
            //    enPassantTarget = null;
            //}

            // Handle Castling
            if (move.MoveType == ChessMoveType.CastleKingSide || move.MoveType == ChessMoveType.CastleQueenSide)
            {
                // Determine row and whether it's kingside or queenside castling
                int row = start.Row;
                bool isKingSide = move.MoveType == ChessMoveType.CastleKingSide;

                // Determine Rook positions
                BoardPosition rookStart = new BoardPosition(row, isKingSide ? 7 : 0);
                BoardPosition rookEnd = new BoardPosition(row, isKingSide ? 5 : 3);

                // Move the King
                SetPieceAtPosition(end, new ChessPiece(ChessPieceType.King, movingPiece.Player));

                ChessPiece piece = new ChessPiece(ChessPieceType.Empty, 0);
                SetPieceAtPosition(start, piece);

                // Move the Rook
                SetPieceAtPosition(rookEnd, new ChessPiece(ChessPieceType.Rook, movingPiece.Player));
                SetPieceAtPosition(rookStart, ChessPiece.Empty);

                // Update Castling Rights
                if (movingPiece.PieceType == ChessPieceType.King)
                {
                    if (movingPiece.Player == 1) hasWhiteKingMoved = true;
                    else hasBlackKingMoved = true;
                }
                if (movingPiece.PieceType == ChessPieceType.Rook)
                {
                    if (start.Col == 0) // Queenside
                    {
                        if (movingPiece.Player == 1) hasWhiteQueenSideRookMoved = true;
                        else hasBlackQueenSideRookMoved = true;
                    }
                    else if (start.Col == 7) // Kingside
                    {
                        if (movingPiece.Player == 1) hasWhiteKingSideRookMoved = true;
                        else hasBlackKingSideRookMoved = true;
                    }
                }
            }

            else if (move.MoveType == ChessMoveType.PawnPromote)
            {
                if (move is PawnPromotionChessMove promotionMove)
                {
                    SetPieceAtPosition(end, new ChessPiece(promotionMove.SelectedPromotion, movingPiece.Player));
                    SetPieceAtPosition(start, new ChessPiece(ChessPieceType.Empty, 1));

                    // Subtract the value of the pawn (1 point) from the advantage
                    int pawnValue = PieceValues[ChessPieceType.Pawn];
                    int pieceValue = PieceValues[GetPieceAtPosition(end).PieceType]; // Value of the promoted piece

                    if (capturedPiece.PieceType != ChessPieceType.Empty)
                    {
                        int capturedPieceValue = PieceValues[capturedPiece.PieceType];

                        if (movingPiece.Player == 1)  // White captured a piece
                        {
                            whiteAdvantage += capturedPieceValue;
                        }
                        else  // Black captured a piece
                        {

                            blackAdvantage += capturedPieceValue;
                        }
                        // mMoveHistory.Add(move);
                        //capturedPieces.Add(capturedPiece);
                    }

                    if (movingPiece.Player == 1) // White's turn
                    {
                        whiteAdvantage -= pawnValue; // Remove the pawn's value
                        whiteAdvantage += pieceValue; // Add the new piece's value
                    }
                    else // Black's turn
                    {
                        //blackAdvantage -= pawnValue; // Remove the pawn's value
                        blackAdvantage += pieceValue; // Add the new piece's value
                    }

                }
            }

            // Handle En Passant
            else if (move.MoveType == ChessMoveType.EnPassant)
            {
                int direction = (movingPiece.Player == 1) ? -1 : 1;  
                BoardPosition capturedPawnPos = new BoardPosition(end.Row - direction, end.Col); 

                // Correct the captured pawn's Player:
                ChessPiece capturedPawn = new ChessPiece(ChessPieceType.Pawn, (movingPiece.Player == 1) ? 0 : 1); 
                SetPieceAtPosition(capturedPawnPos, ChessPiece.Empty);  // Remove captured pawn from the board

                SetPieceAtPosition(end, movingPiece);  
                SetPieceAtPosition(start, ChessPiece.Empty); 

                capturedPieces.Add(capturedPawn);  

                // Update Advantage After En Passant Capture
                int pawnValue = PieceValues[ChessPieceType.Pawn];
                if (movingPiece.Player == 1) 
                {
                    whiteAdvantage += pawnValue; // Black loses a pawn
                }
                else 
                {
                    blackAdvantage += pawnValue; // White loses a pawn
                }
            }

            else
            {
                // Regular move handling
                SetPieceAtPosition(end, movingPiece);
                SetPieceAtPosition(start, ChessPiece.Empty);


                if (capturedPiece.PieceType != ChessPieceType.Empty)
                {
                    int capturedPieceValue = PieceValues[capturedPiece.PieceType];

                    if (movingPiece.Player == 1)  // White captured a piece
                    {
                        whiteAdvantage += capturedPieceValue;
                    }
                    else  // Black captured a piece
                    {

                        blackAdvantage += capturedPieceValue;
                    }

                }
                if (movingPiece.PieceType == ChessPieceType.King)
                {
                    if (movingPiece.Player == 1) hasWhiteKingMoved = true;
                    else hasBlackKingMoved = true;
                }

                if (movingPiece.PieceType == ChessPieceType.Rook)
                {
                    if (start.Col == 0) // Queenside
                    {
                        if (movingPiece.Player == 1) hasWhiteQueenSideRookMoved = true;
                        else hasBlackQueenSideRookMoved = true;
                    }
                    else if (start.Col == 7) // Kingside
                    {
                        if (movingPiece.Player == 1) hasWhiteKingSideRookMoved = true;
                        else hasBlackKingSideRookMoved = true;
                    }
                }
            }

            mMoveHistory.Add(move);
            capturedPieces.Add(capturedPiece);
            captureHistory.Add(capturedPiece.PieceType != ChessPieceType.Empty);

            GetAdvantage();

            CurrentPlayer = 3 - CurrentPlayer;
        }

        public void UndoLastMove()
        {
            if (mMoveHistory.Count == 0) return;

            ChessMove lastMove = mMoveHistory.Last();


            // Retrieve and remove the last captured piece from the list
            int mIdx = mMoveHistory.Count - 1; // Get the correct move index
            ChessPiece capturedPiece = capturedPieces[mIdx];


            // Move the piece back
            ChessPiece movedPiece = GetPieceAtPosition(lastMove.EndPosition);

            // Undo En Passant
            if (lastMove.MoveType == ChessMoveType.EnPassant)
            {
                int direction = (movedPiece.Player == 1) ? -1 : 1;
                BoardPosition capturedPawnPosition = new BoardPosition(lastMove.EndPosition.Row - direction, lastMove.EndPosition.Col);

                ChessPiece capturedPawnForUndo = new ChessPiece(ChessPieceType.Pawn, capturedPiece.Player);
                SetPieceAtPosition(capturedPawnPosition, capturedPawnForUndo);

                SetPieceAtPosition(lastMove.StartPosition, movedPiece);
                SetPieceAtPosition(lastMove.EndPosition, ChessPiece.Empty);

                //if (mPreviousEnPassantTargetHistory.Count > 0)
                //{
                //    enPassantTarget = mPreviousEnPassantTargetHistory.Last();
                //    mPreviousEnPassantTargetHistory.RemoveAt(mPreviousEnPassantTargetHistory.Count - 1);
                //}
                //else
                //{
                //    enPassantTarget = null;
                //}
                // Undo the advantage change for En Passant
                int pawnValue = PieceValues[ChessPieceType.Pawn];
                if (movedPiece.Player == 1) // White captured Black's pawn
                {
                    whiteAdvantage -= pawnValue;
                }
                else // Black captured White's pawn
                {
                    blackAdvantage -= pawnValue;
                }
            }
            // Undo Pawn Promotion
            else if (lastMove.MoveType == ChessMoveType.PawnPromote && lastMove is PawnPromotionChessMove promotionMove)
            {
                int promotedPieceValue = PieceValues[promotionMove.SelectedPromotion];
                int pawnValue = PieceValues[ChessPieceType.Pawn];

                // restore the empty square
                SetPieceAtPosition(lastMove.EndPosition, ChessPiece.Empty);

                if (movedPiece.Player == 1)
                {
                    whiteAdvantage -= promotedPieceValue;
                    whiteAdvantage += pawnValue;

                }
                else
                {
                    blackAdvantage -= promotedPieceValue;
                    blackAdvantage += pawnValue;
                }

                SetPieceAtPosition(lastMove.StartPosition, new ChessPiece(ChessPieceType.Pawn, movedPiece.Player));

                if (capturedPiece.PieceType != ChessPieceType.Empty)
                {
                    SetPieceAtPosition(lastMove.EndPosition, capturedPiece);
                    int capturedPieceValue = PieceValues[capturedPiece.PieceType];

                    if (movedPiece.Player == 1)
                    {
                        whiteAdvantage -= capturedPieceValue;
                    }
                    else
                    {
                        blackAdvantage -= capturedPieceValue;
                    }
                }

            }

            else if (lastMove.MoveType == ChessMoveType.CastleKingSide || lastMove.MoveType == ChessMoveType.CastleQueenSide)
            {
                int row = lastMove.StartPosition.Row;
                bool isKingSide = lastMove.MoveType == ChessMoveType.CastleKingSide;

                // King moves back
                BoardPosition kingStart = new BoardPosition(row, 4);
                SetPieceAtPosition(kingStart, new ChessPiece(ChessPieceType.King, movedPiece.Player));
                SetPieceAtPosition(lastMove.EndPosition, ChessPiece.Empty);

                // Rook moves back
                BoardPosition rookStart = new BoardPosition(row, isKingSide ? 7 : 0);
                BoardPosition rookEnd = new BoardPosition(row, isKingSide ? 5 : 3);
                SetPieceAtPosition(rookStart, new ChessPiece(ChessPieceType.Rook, movedPiece.Player));
                SetPieceAtPosition(rookEnd, ChessPiece.Empty);

                // Restore castling rights
                if (movedPiece.Player == 1)
                {
                    hasWhiteKingMoved = false;
                    if (isKingSide) hasWhiteKingSideRookMoved = false;
                    else hasWhiteQueenSideRookMoved = false;
                }
                else
                {
                    hasBlackKingMoved = false;
                    if (isKingSide) hasBlackKingSideRookMoved = false;
                    else hasBlackQueenSideRookMoved = false;
                }
            }
            else
            {
                int moveIndex = mMoveHistory.Count - 1;

                // Get the value of the captured piece
                int capturedPieceValue = PieceValues[capturedPiece.PieceType];

                if (capturedPiece.PieceType != ChessPieceType.Empty)
                {
                    if (movedPiece.Player == 1)
                    {
                        blackAdvantage += capturedPieceValue;
                    }
                    else
                    {
                        whiteAdvantage += capturedPieceValue;
                    }
                    SetPieceAtPosition(lastMove.StartPosition, movedPiece);
                    SetPieceAtPosition(lastMove.EndPosition, capturedPiece);
                }
                else
                {
                    SetPieceAtPosition(lastMove.StartPosition, movedPiece);
                    SetPieceAtPosition(lastMove.EndPosition, ChessPiece.Empty);
                }

                if (capturedPiece.PieceType != ChessPieceType.King)
                {
                    bool isKingSide = lastMove.MoveType == ChessMoveType.CastleKingSide;
                    if (movedPiece.Player == 1)
                    {
                        hasWhiteKingMoved = false;
                        if (isKingSide) hasWhiteKingSideRookMoved = false;
                        else hasWhiteQueenSideRookMoved = false;
                    }
                    else
                    {
                        hasBlackKingMoved = false;
                        if (isKingSide) hasBlackKingSideRookMoved = false;
                        else hasBlackQueenSideRookMoved = false;
                    }

                }
            }
            if (movedPiece.PieceType == ChessPieceType.Pawn || lastMove.MoveType == ChessMoveType.EnPassant || capturedPiece.PieceType != ChessPieceType.Empty)
            {
                DrawCounter = CounterForUndo;
            }
            else
            {
                DrawCounter--;
            }

            GetAdvantage();
            mMoveHistory.RemoveAt(mMoveHistory.Count - 1);
            capturedPieces.RemoveAt(capturedPieces.Count - 1);
            captureHistory.RemoveAt(captureHistory.Count - 1);
            CurrentPlayer = 3 - CurrentPlayer;

        }
        /// <summary>
        /// Returns whatever chess piece is occupying the given position.
        /// </summary>
        public ChessPiece GetPieceAtPosition(BoardPosition pos)
        {
            int col;
            int player;
            int byteRepOfPiece;
            ChessPieceType pieceType;

            if (pos.Row < 0 || pos.Row >= BoardSize || pos.Col < 0 || pos.Col >= BoardSize)
                return ChessPiece.Empty;

            if (pos.Col == 0 || pos.Col == 1)
            {
                col = 0;
            }
            else if (pos.Col == 2 || pos.Col == 3)
            {
                col = 1;
            }
            else if (pos.Col == 4 || pos.Col == 5)
            {
                col = 2;
            }
            else
            {
                col = 3;
            }

            byte pieceRepresentation = chessBoard[pos.Row, col];

            // Base Case: if byte is 00000000, that means position is empty
            if (pieceRepresentation == 0)
            {
                player = 0;
                byteRepOfPiece = 0;
            }
            // Given column is even (means that in the matrix, its the last 4 bits)
            else if (pos.Col % 2 == 0)
            {
                player = ((pieceRepresentation >> (pos.Col % 2 == 0 ? 7 : 3)) & 1) + 1;
                int mask = (1 << 3) - 1;
                byteRepOfPiece = (pieceRepresentation >> 4) & mask;

            }
            // Given column is odd (means that in the matrix, its the first 4 bits)
            else
            {
                player = ((pieceRepresentation >> (pos.Col % 2 == 0 ? 7 : 3)) & 1) + 1;
                int mask = (1 << 3) - 1;
                byteRepOfPiece = pieceRepresentation & mask;
            }



            // Checks the player bit extraction for what piece is there or if empty
            if (byteRepOfPiece == 0)
            {
                return new ChessPiece(ChessPieceType.Empty, 0);
            }
            else if (byteRepOfPiece == 1)
            {
                return new ChessPiece(ChessPieceType.Pawn, player);
            }
            else if (byteRepOfPiece == 2)
            {
                return new ChessPiece(ChessPieceType.Rook, player);
            }
            else if (byteRepOfPiece == 3)
            {
                return new ChessPiece(ChessPieceType.Knight, player);
            }
            else if (byteRepOfPiece == 4)
            {
                return new ChessPiece(ChessPieceType.Bishop, player);
            }
            else if (byteRepOfPiece == 5)
            {
                return new ChessPiece(ChessPieceType.Queen, player);
            }
            else if (byteRepOfPiece == 6)
            {
                return new ChessPiece(ChessPieceType.King, player);
            }
            else
            {
                throw new ArgumentException("Can only apply a ChessMove to a ChessBoard");
            }

        }

        /// <summary>
        /// Retruns whatever player is occupying the given position.
        /// </summary>
        public int GetPlayerAtPosition(BoardPosition pos)
        {
            return GetPieceAtPosition(pos).Player;
        }

        /// <summary>
        /// Returns all board positions where the given piece can be found.
        /// </summary>
        public IEnumerable<BoardPosition> GetPositionsOfPiece(ChessPiece piece)
        {
            return BoardPosition.GetRectangularPositions(8, 4)
                .Where(pos => {
                    var boardPiece = GetPieceAtPosition(pos);
                    return (boardPiece.PieceType == piece.PieceType) && (boardPiece.Player == piece.Player);
                });

        }

        /// <summary>
        /// 
        /// Returns true if the given position has no piece on it.
        /// </summary>
        public bool PositionIsEmpty(BoardPosition position)
        {
            return GetPieceAtPosition(position).PieceType == ChessPieceType.Empty;
        }

        /// <summary>
        /// Returns true if the given position contains a piece that is the enemy of the given player.
        /// </summary>
        /// <remarks>returns false if the position is not in bounds</remarks>
        public bool PositionIsEnemy(BoardPosition pos, int player)
        {
            ChessPiece piece = GetPieceAtPosition(pos);
            return (piece.Player != 0 && piece.PieceType != ChessPieceType.Empty) && piece.Player != player;
        }
        #endregion

        #region Private methods.
        /// <summary>
        /// Mutates the board state so that the given piece is at the given position.
        /// </summary>
        private void SetPieceAtPosition(BoardPosition pos, ChessPiece piece)
        {
            int col;
            int player = piece.Player;
            int byteRepOfPiece;
            // ChessPieceType pieceType;
            Dictionary<int, byte> chessPieceEncoding = new Dictionary<int, byte>
{
                { 0, 0b000 }, // Empty
				{ 1, 0b001 }, // Pawn
				{ 2, 0b010 }, // Rook
				{ 3, 0b011 }, // Knight
				{ 4, 0b100 }, // Bishop
				{ 5, 0b101 }, // Queen
				{ 6, 0b110 }  // King
			};

            // Checks corresponding column in matrix since C# matrix is 8 bit byte 
            if (pos.Col == 0 || pos.Col == 1)
            {
                col = 0;
            }
            else if (pos.Col == 2 || pos.Col == 3)
            {
                col = 1;
            }
            else if (pos.Col == 4 || pos.Col == 5)
            {
                col = 2;
            }
            else
            {
                col = 3;
            }

            byte pieceRepresentation = chessBoard[pos.Row, col];

            byte pieceBits = chessPieceEncoding[(int)piece.PieceType];

            // Set the player bit (MSB of nibble)
            byte playerBit = (byte)((piece.Player == 1) ? 0 : 1);

            if (piece.PieceType == ChessPieceType.Empty)
            {
                if (pos.Col % 2 == 0) // Even column
                {
                    // Clear the upper 4 bits (set to 0000)
                    pieceRepresentation &= 0b00001111;

                    pieceRepresentation &= 0b01111111;
                }
                else // Odd column
                {
                    // Clear the lower 4 bits (set to 0000)
                    pieceRepresentation &= 0b11110000;

                    pieceRepresentation &= 0b11110111;
                }

            }
            else
            {
                if (pos.Col % 2 == 0) // Even Column
                {
                    byte maskOff = 0b00001111; // Clears bits 7-4
                    pieceRepresentation &= maskOff; // Apply mask
                    byte shift = (byte)((playerBit << 7) | (pieceBits << 4)); // Set player bit + piece bits
                    pieceRepresentation |= shift; // Merge into byte
                }
                else // Odd Column
                {
                    byte maskOff = 0b11110000; // Clears bits 3-0
                    pieceRepresentation &= maskOff; // Apply mask
                    byte shift = (byte)((playerBit << 3) | (pieceBits << 0)); // Set player bit + piece bits
                    pieceRepresentation |= shift; // Merge into byte
                }
            }



            chessBoard[pos.Row, col] = pieceRepresentation;
        }
        #endregion

        /// <summary>
        /// Returns attackable positions of Pawn
        /// </summary>
        private IEnumerable<BoardPosition> PawnAttacks(BoardPosition pos, ChessPiece piece)
        {


            List<BoardPosition> attackablePositions = new List<BoardPosition>();
            int direction = (piece.Player == 1) ? -1 : 1;
            int startRow = (piece.Player == 1) ? 6 : 1; // Pawn starting rows

            int[] possibleColumns = { -1, 1 };

            // **Diagonal attack moves (if an enemy is there)**
            foreach (int i in possibleColumns)
            {
                BoardPosition diagonal = pos.Translate(direction, i);
                if (diagonal.Row >= 0 && diagonal.Row < BoardSize && diagonal.Col >= 0 && diagonal.Col < BoardSize)
                {
                    if (PositionIsEnemy(diagonal, piece.Player)) // Only attack if an enemy is there
                    {
                        attackablePositions.Add(diagonal);
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            // **One-square forward move**
            BoardPosition oneStep = pos.Translate(direction, 0);
            if (PositionIsEmpty(oneStep) && oneStep.Row >= 0 && oneStep.Row < BoardSize && oneStep.Col >= 0 && oneStep.Col < BoardSize)
            {
                attackablePositions.Add(oneStep);

                // Two-square forward move (if in starting position and path is clear)
                BoardPosition twoStep = oneStep.Translate(direction, 0);
                if (pos.Row == startRow && PositionIsEmpty(twoStep) && twoStep.Row >= 0 && twoStep.Row < BoardSize && twoStep.Col >= 0 && twoStep.Col < BoardSize)
                {
                    attackablePositions.Add(twoStep);
                }
            }

            return attackablePositions;

        }
        private IEnumerable<BoardPosition> DiagonalPiecesAttacks(BoardPosition pos, ChessPiece piece)
        {
            List<BoardPosition> attackablePositions = new List<BoardPosition>();

            List<BoardDirection> rookDirections = new List<BoardDirection>()
            {
                new BoardDirection(1, 0),
                new BoardDirection(-1, 0),
                new BoardDirection(0, 1),
                new BoardDirection(0, -1)
            };

            List<BoardDirection> bishopDirections = new List<BoardDirection>()
            {
                new BoardDirection(1, 1),
                new BoardDirection(1, -1),
                new BoardDirection(-1, 1),
                new BoardDirection(-1, -1)
            };

            List<BoardDirection> queenDirections = new List<BoardDirection>()
            {
                new BoardDirection(1, 0),
                new BoardDirection(-1, 0),
                new BoardDirection(0, 1),
                new BoardDirection(0, -1),
                new BoardDirection(1, 1),
                new BoardDirection(1, -1),
                new BoardDirection(-1, 1),
                new BoardDirection(-1, -1)
            };

            List<BoardDirection> pieceDirection = new List<BoardDirection>();

            if (piece.PieceType == ChessPieceType.Rook)
            {
                pieceDirection = rookDirections;

            }
            else if (piece.PieceType == ChessPieceType.Bishop)
            {
                pieceDirection = bishopDirections;
            }
            else if (piece.PieceType == ChessPieceType.Queen)
            {
                pieceDirection = queenDirections;
            }

            foreach (BoardDirection direction in pieceDirection)
            {

                BoardPosition currentPosition = pos.Translate(direction);
                while (currentPosition.Row >= 0 && currentPosition.Row < BoardSize && currentPosition.Col >= 0 && currentPosition.Col < BoardSize)
                {
                    if (!PositionIsEmpty(currentPosition))
                    {
                        if (PositionIsEnemy(currentPosition, piece.Player))
                        {
                            attackablePositions.Add(currentPosition);


                        }
                        break;
                    }
                    attackablePositions.Add(currentPosition);
                    currentPosition = currentPosition.Translate(direction);

                }
            }

            return attackablePositions;
        }

        private IEnumerable<BoardPosition> KingAttacks(BoardPosition pos, ChessPiece piece)
        {
            List<BoardPosition> attackablePositions = new List<BoardPosition>();

            List<BoardDirection> kingMoves = new List<BoardDirection> {
                new BoardDirection(1, 0),   // Down
                new BoardDirection(-1, 0),  // Up
                new BoardDirection(0, 1),   // Right
                new BoardDirection(0, -1),  // Left
                new BoardDirection(1, 1),   // Down-right
                new BoardDirection(1, -1),  // Down-left
                new BoardDirection(-1, 1),  // Up-right
                new BoardDirection(-1, -1)  // Up-left
            };

            foreach (BoardDirection direction in kingMoves)
            {
                BoardPosition currentPosition = pos.Translate(direction);

                if (currentPosition.Row >= 0 && currentPosition.Row < BoardSize &&
                    currentPosition.Col >= 0 && currentPosition.Col < BoardSize)
                {
                    attackablePositions.Add(currentPosition);
                }
            }

            return attackablePositions;
        }

        private IEnumerable<BoardPosition> KnightAttacks(BoardPosition pos, ChessPiece piece)
        {
            List<BoardPosition> attackablePositions = new List<BoardPosition>();

            List<BoardDirection> knightMoves = new List<BoardDirection>()
            {
                new BoardDirection(2, 1),
                new BoardDirection(2, -1),
                new BoardDirection(-2, 1),
                new BoardDirection(-2, -1),
                new BoardDirection(1, 2),
                new BoardDirection(1, -2),
                new BoardDirection(-1, 2),
                new BoardDirection(-1, -2)
            };

            foreach (BoardDirection direction in knightMoves)
            {
                BoardPosition currentPosition = pos.Translate(direction);
                if (currentPosition.Row >= 0 && currentPosition.Row < BoardSize && currentPosition.Col >= 0 && currentPosition.Col < BoardSize)
                {
                    if (PositionIsEmpty(currentPosition) || PositionIsEnemy(currentPosition, piece.Player))
                    {
                        attackablePositions.Add(currentPosition);
                    }
                }
            }

            return attackablePositions;
        }

        #region Explicit IGameBoard implementations.
        IEnumerable<IGameMove> IGameBoard.GetPossibleMoves()
        {
            return GetPossibleMoves();
        }
        void IGameBoard.ApplyMove(IGameMove m)
        {
            if (m is not ChessMove move)
            {
                throw new ArgumentException("Can only apply a ChessMove to a ChessBoard");
            }
            ApplyMove(move);
        }
        IReadOnlyList<IGameMove> IGameBoard.MoveHistory => mMoveHistory;
        #endregion
    }
}