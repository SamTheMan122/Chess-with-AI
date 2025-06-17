using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cecs475.BoardGames.AvaloniaView;
using Cecs475.BoardGames.Chess.Model;

namespace Cecs475.BoardGames.Chess.AvaloniaView;

public partial class ChessView : UserControl, IAvaloniaGameView
{
    public ChessView()
    {
        InitializeComponent();

    }

    public ChessViewModel ChessViewModel => (ChessViewModel)Resources["vm"]!;

    public Control ViewControl => this;

    public IGameViewModel ViewModel => ChessViewModel;

    private ChessSquare? mSelectedSquare;

    private void Panel_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (sender is not Control b)
            return;

        var hoveredSquare = (ChessSquare)b.DataContext!;
        var vm = (ChessViewModel)Resources["vm"]!;

        if (mSelectedSquare != null)
        {
            var possibleMoves = vm.GetMovesFromPosition(mSelectedSquare.Position);
            if (possibleMoves.Any(m => m.EndPosition.Equals(hoveredSquare.Position)))
            {
                hoveredSquare.IsPossibleMoveHover = true;
            }
        }
        else
        {
            if (vm.GetMovesFromPosition(hoveredSquare.Position).Any())
            {
                hoveredSquare.IsPossibleMoveHover = true;
            }
        }
    }



    //private void Panel_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    //{
    //    if (sender is not Control b)
    //        return;

    //    var hoveredSquare = (ChessSquare)b.DataContext!;
    //    var vm = (ChessViewModel)Resources["vm"]!;

    //    if (mSelectedSquare != null)
    //    {
    //        // If a piece is selected, highlight valid destination squares
    //        var possibleMoves = vm.GetMovesFromPosition(mSelectedSquare.Position);
    //        if (possibleMoves.Any(m => m.EndPosition.Equals(hoveredSquare.Position)))
    //        {
    //            hoveredSquare.IsHighlighted = true;
    //            hoveredSquare.IsPossibleMoveHover = true;
    //        }
    //    }
    //    else
    //    {
    //        // If no piece is selected, highlight valid start squares
    //        if (vm.PossibleMoves.Contains(hoveredSquare.Position))
    //        {
    //            hoveredSquare.IsHighlighted = true;
    //        }
    //    }
    //}


    private void Panel_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (sender is not Control b)
            return;

        var square = (ChessSquare)b.DataContext!;
        square.IsHighlighted = false;
        square.IsPossibleMoveHover = false;
    }



    //private void Panel_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    //{
    //    if (sender is not Control b)
    //        return;

    //    var square = (ChessSquare)b.DataContext!;
    //    square.IsHighlighted = false;
    //    square.IsPossibleMoveHover = false;
    //}



    private async void Panel_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (sender is not Control b)
            throw new ArgumentException(nameof(sender));

        var clickedSquare = (ChessSquare)b.DataContext!;
        var vm = (ChessViewModel)Resources["vm"]!;

        // If a piece was already selected, and this click is a valid move destination:
        if (mSelectedSquare != null && vm.TryGetMove(mSelectedSquare.Position, clickedSquare.Position, out ChessMove? move))
        {
            if (move is PawnPromotionChessMove promotionMove)
            {
                var win = new PromotionWindow(vm.CurrentPlayer);
                var result = await win.ShowDialog<ChessPieceType>(this.VisualRoot as Window);

                var finalMove = new PawnPromotionChessMove(
                    promotionMove.StartPosition, promotionMove.EndPosition, result);
                vm.ApplyMove(finalMove);
            }
            else
            {
                vm.ApplyMove(move);
            }


            mSelectedSquare.IsSelected = false;
            clickedSquare.IsHighlighted = false;
            mSelectedSquare = null;
        }
        else if (clickedSquare.Player == vm.CurrentPlayer)
        {
            // clicked a piece the player owns
            if (mSelectedSquare != null)
                mSelectedSquare.IsSelected = false;

            mSelectedSquare = clickedSquare;
            mSelectedSquare.IsSelected = true;
        }
        else
        {
            // clear selection
            if (mSelectedSquare != null)
                mSelectedSquare.IsSelected = false;

            mSelectedSquare = null;
        }
    }




}