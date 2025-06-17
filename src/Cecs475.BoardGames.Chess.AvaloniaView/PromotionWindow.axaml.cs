using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Cecs475.BoardGames.Chess.Model;
using System;

namespace Cecs475.BoardGames.Chess.AvaloniaView;

public partial class PromotionWindow : Window
{
    public ChessPieceType SelectedPromotion { get; private set; } = ChessPieceType.Queen;

    public PromotionWindow(int player)
    {
        InitializeComponent();
        SetImages(player);
    }

    private void SetImages(int player)
    {
        string color = player == 1 ? "white" : "black";

        KnightImage.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://Cecs475.BoardGames.Chess.AvaloniaView/Resources/{color}-knight.png")));
        BishopImage.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://Cecs475.BoardGames.Chess.AvaloniaView/Resources/{color}-bishop.png")));
        RookImage.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://Cecs475.BoardGames.Chess.AvaloniaView/Resources/{color}-rook.png")));
        QueenImage.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://Cecs475.BoardGames.Chess.AvaloniaView/Resources/{color}-queen.png")));
    }


    private void OnPieceSelected(object? sender, PointerPressedEventArgs e)
    {
        if (sender == KnightImage) SelectedPromotion = ChessPieceType.Knight;
        else if (sender == BishopImage) SelectedPromotion = ChessPieceType.Bishop;
        else if (sender == RookImage) SelectedPromotion = ChessPieceType.Rook;
        else if (sender == QueenImage) SelectedPromotion = ChessPieceType.Queen;

        Close(SelectedPromotion);
    }
}
