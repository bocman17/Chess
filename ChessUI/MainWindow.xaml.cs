﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using ChessLogic;
using System.Media;

namespace ChessUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Image[,] pieceImages = new Image[8, 8];
        private readonly Rectangle[,] highlights = new Rectangle[8, 8];
        private readonly Rectangle[,] checkHighlights = new Rectangle[8, 8];
        private readonly Dictionary<Position, Move> moveCache = new Dictionary<Position, Move>();

        private SoundPlayer soundPlayer;

        private GameState gameState;
        private Position selectedPos = null;

        public MainWindow()
        {
            InitializeComponent();
            InitializeBoard();

            gameState = new GameState(Player.White, Board.Initial());
            DrawBoard(gameState.Board);
            SetCursor(gameState.CurrentPlayer);

            soundPlayer = new SoundPlayer(@"C:\repos\Chess\ChessUI\Sounds\game-start.wav");
            soundPlayer.Play();
        }

        private void InitializeBoard()
        {

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Image image = new Image();
                    pieceImages[r, c] = image;
                    PieceGrid.Children.Add(image);

                    Rectangle highlight = new Rectangle();
                    highlights[r, c] = highlight;
                    HighlightGrid.Children.Add(highlight);

                    Rectangle checkHighlight = new Rectangle();
                    checkHighlights[r, c] = checkHighlight;
                    CheckHighlightGrid.Children.Add(checkHighlight);
                }
            }
        }

        private void DrawBoard(Board board)
        {
            if (board.IsInCheck(gameState.CurrentPlayer))
            {
                ShowCheckHighlight(gameState.CurrentPlayer, board);

                soundPlayer = new SoundPlayer(@"C:\repos\Chess\ChessUI\Sounds\move-check.wav");
                soundPlayer.Play();
                soundPlayer.Dispose();
            }

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Piece piece = board[r, c];
                    pieceImages[r, c].Source = Images.GetImage(piece);
                }
            }
        }

        private void BoardGrid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsMenuOnScreen())
            {
                return;
            }

            Point point = e.GetPosition(BoardGrid);
            Position pos = ToSquarePosition(point);

            if (selectedPos == null)
            {
                OnFromPositionSelected(pos);
            }
            else
            {
                OnToPositionSelected(pos, gameState.Board);
            }
        }
        private void OnFromPositionSelected(Position pos)
        {
            IEnumerable<Move> moves = gameState.LegalMovesForPiece(pos);

            if (moves.Any())
            {
                selectedPos = pos;
                CacheMoves(moves);
                ShowHighlights();
            }
        }

        private void OnToPositionSelected(Position pos, Board board)
        {
            selectedPos = null;
            HideHighlights();

            if (moveCache.TryGetValue(pos, out Move move))
            {
                if (move.Type == MoveType.PawnPromotion)
                {
                    HandlePromotoion(move.FromPos, move.ToPos, board);
                }
                else
                {
                    HandleMove(move, board);
                }
            }
        }

        private void HandlePromotoion(Position fromPos, Position toPos, Board board)
        {
            pieceImages[toPos.Row, toPos.Column].Source = Images.GetImage(gameState.CurrentPlayer, PieceType.Pawn);
            pieceImages[fromPos.Row, fromPos.Column].Source = null;

            PromotionMenu promotionMenu = new PromotionMenu(gameState.CurrentPlayer);
            MenuContainer.Content = promotionMenu;

            promotionMenu.PieceSelected += type =>
            {
                MenuContainer.Content = null;
                Move promMove = new PawnPromotion(fromPos, toPos, type);
                HandleMove(promMove, board);
            };
        }

        private void HandleMove(Move move, Board board)
        {
            HideCheckHighlights(gameState.CurrentPlayer, gameState.Board);

            if (( move.Type == MoveType.Normal && board[move.ToPos] != null ) || move.Type == MoveType.EnPassant)
            {
                soundPlayer = new SoundPlayer(@"C:\repos\Chess\ChessUI\Sounds\capture.wav");
                soundPlayer.Play();
                soundPlayer.Dispose();
            }
            else if (( move.Type == MoveType.Normal || move.Type == MoveType.DoublePawn ) && board[move.ToPos] == null)
            {
                soundPlayer = new SoundPlayer(@"C:\repos\Chess\ChessUI\Sounds\move-self.wav");
                soundPlayer.Play();
                soundPlayer.Dispose();
            }

            gameState.MakeMove(move);
            DrawBoard(gameState.Board);

            SetCursor(gameState.CurrentPlayer);


            if (gameState.IsGameOver())
            {
                soundPlayer = new SoundPlayer(@"C:\repos\Chess\ChessUI\Sounds\game-win.wav");
                soundPlayer.Play();
                soundPlayer.Dispose();

                ShowGameOver();
            }
        }

        private Position ToSquarePosition(Point point)
        {
            double squareSize = BoardGrid.ActualWidth / 8;
            int row = (int)( point.Y / squareSize );
            int column = (int)( point.X / squareSize );

            return new Position(row, column);
        }

        private void CacheMoves(IEnumerable<Move> moves)
        {
            moveCache.Clear();

            foreach (Move move in moves)
            {
                moveCache[move.ToPos] = move;
            }
        }

        private void ShowHighlights()
        {
            Color color = Color.FromArgb(150, 125, 255, 125);

            foreach (Position to in moveCache.Keys)
            {
                highlights[to.Row, to.Column].Fill = new SolidColorBrush(color);
            }
        }

        private void HideHighlights()
        {
            foreach (Position to in moveCache.Keys)
            {
                highlights[to.Row, to.Column].Fill = Brushes.Transparent;
            }
        }

        private void ShowCheckHighlight(Player player, Board board)
        {
            Color color = Color.FromRgb(201, 42, 42);

            Position kingPos = board.FindPiece(player, PieceType.King);
            checkHighlights[kingPos.Row, kingPos.Column].Fill = new SolidColorBrush(color);
        }

        private void HideCheckHighlights(Player player, Board board)
        {
            Position kingPos = board.FindPiece(player, PieceType.King);

            checkHighlights[kingPos.Row, kingPos.Column].Fill = Brushes.Transparent;
        }

        private void SetCursor(Player player)
        {
            if (player == Player.White)
            {
                Cursor = ChessCursors.WhiteCursor;
            }
            else
            {
                Cursor = ChessCursors.BlackCursor;
            }
        }

        private bool IsMenuOnScreen()
        {
            return MenuContainer.Content != null;
        }

        private void ShowGameOver()
        {
            GameOverMenu gameOverMenu = new GameOverMenu(gameState);
            MenuContainer.Content = gameOverMenu;

            gameOverMenu.OptionSelected += option =>
            {
                if (option == Option.Restart)
                {
                    MenuContainer.Content = null;
                    RestartGame();
                }
                else
                {
                    Application.Current.Shutdown();
                }
            };
        }

        private void RestartGame()
        {
            selectedPos = null;
            HideHighlights();
            moveCache.Clear();
            gameState = new GameState(Player.White, Board.Initial());
            DrawBoard(gameState.Board);
            SetCursor(gameState.CurrentPlayer);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!IsMenuOnScreen() && e.Key == Key.Escape)
            {
                ShowPauseMenu();
            }
        }
        private void ShowPauseMenu()
        {
            PauseMenu pauseMenu = new PauseMenu();
            MenuContainer.Content = pauseMenu;

            pauseMenu.OptionSelected += option =>
            {
                MenuContainer.Content = null;

                if (option == Option.Restart)
                {
                    RestartGame();
                }
            };
        }
    }
}