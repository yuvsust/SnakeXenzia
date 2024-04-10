using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SnakeXenzia
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int SnakeSquareSize = 20;
        const int SnakeStartLength = 3;
        const int SnakeStartSpeed = 400;
        const int SnakeSpeedThreshold = 100;

        private SolidColorBrush snakeBodyColor = Brushes.Green;
        private SolidColorBrush snakeHeadColor = Brushes.YellowGreen;
        private SolidColorBrush snakeFoodColor = Brushes.Red;
        private UIElement? snakeFood = null;
        private List<SnakePart> snakeParts = [];
        private System.Windows.Threading.DispatcherTimer gameTickTimer = new();

        public enum Direction { Left, Right, Up, Down };
        private Direction snakeDirection = Direction.Right;
        private int snakeLength;

        public MainWindow()
        {
            InitializeComponent();
            gameTickTimer.Tick += GameTickTimer_Tick;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();
        }

        private void DrawGameArea()
        {
            bool doneDrawingBackground = false;
            int nextX = 0, nextY = 0;
            int rowCounter = 0;
            bool nextIsOdd = false;

            while (!doneDrawingBackground)
            {
                var rect = new Rectangle
                {
                    Width = SnakeSquareSize,
                    Height = SnakeSquareSize,
                    Fill = nextIsOdd ? Brushes.White : Brushes.Black
                };
                GameArea.Children.Add(rect);
                Canvas.SetTop(rect, nextY);
                Canvas.SetLeft(rect, nextX);

                nextIsOdd = !nextIsOdd;
                nextX += SnakeSquareSize;
                if (nextX >= GameArea.ActualWidth)
                {
                    nextX = 0;
                    nextY += SnakeSquareSize;
                    rowCounter++;
                    nextIsOdd = (rowCounter % 2 != 0);
                }

                if (nextY >= GameArea.ActualHeight)
                    doneDrawingBackground = true;
            }
        }

        private void DrawSnake()
        {
            foreach (SnakePart snakePart in snakeParts)
            {
                if (snakePart.UiElement == null)
                {
                    snakePart.UiElement = new Rectangle()
                    {
                        Width = SnakeSquareSize,
                        Height = SnakeSquareSize,
                        Fill = (snakePart.IsHead ? snakeHeadColor : snakeBodyColor)
                    };
                    GameArea.Children.Add(snakePart.UiElement);
                    Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UiElement, snakePart.Position.X);
                }
            }
        }

        private void MoveSnake()
        {
            // In the SnakeParts, the last index snake part is the Head and the rests before it are body.
            // So, for the MoveSnake operation, the strategies are,
            // 1. Mark the current head as body.
            // 2. Add a new snake part infront of the current head and mark it as the new head with head color.
            //    To do so, first determine in which direction the snake is moving. We will add the new head in that direction.
            // 3. Remove the last snake part, which is the 0-th index snake part.


            // 1. we'll add a new element to the snake, which will be the (new) head  
            // Therefore, we mark the current head as non-head (body) elements and then  
            // The current head is at the last index of the snakeParts 
            var snakeHead = snakeParts[^1];
            (snakeHead.UiElement as Rectangle).Fill = snakeBodyColor;
            snakeHead.IsHead = false;

            // Determine in which direction to expand the snake, based on the current direction  
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;
            switch (snakeDirection)
            {
                case Direction.Left:
                    nextX -= SnakeSquareSize;
                    break;
                case Direction.Right:
                    nextX += SnakeSquareSize;
                    break;
                case Direction.Up:
                    nextY -= SnakeSquareSize;
                    break;
                case Direction.Down:
                    nextY += SnakeSquareSize;
                    break;
            }

            // 2. Now add the new head part to our list of snake parts.
            //    Note: We have maintained the head as of the last item of the snakeParts list.
            snakeParts.Add(new SnakePart()
            {
                Position = new Point(nextX, nextY),
                IsHead = true
            });

            // 3. Finally, remove the last part of the snake. 
            //    We are maintaing the loop here, because upto the initial snakeLength we will not remove any parts.
            //    Reminder: The last part is actually the 0-th part.
            while (snakeParts.Count > snakeLength)
            {
                GameArea.Children.Remove(snakeParts[0].UiElement);
                snakeParts.RemoveAt(0);
            }

            //... and then have it drawn!  
            DrawSnake();
        }

        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            MoveSnake();
        }

        private void StartNewGame()
        {
            GameArea.Children.Clear();
            DrawGameArea();

            snakeLength = SnakeStartLength;
            snakeDirection = Direction.Right;
            snakeParts.Add(new SnakePart() { Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5) });
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);

            // Draw the snake  
            DrawSnake();
            DrawSnakeFood();

            // Enable the timer        
            gameTickTimer.IsEnabled = true;
        }

        private void DrawSnakeFood()
        {
            Point foodPosition = GetNextFoodPosition();
            snakeFood = new Ellipse()
            {
                Width = SnakeSquareSize,
                Height = SnakeSquareSize,
                Fill = snakeFoodColor
            };

            GameArea.Children.Add(snakeFood);
            Canvas.SetTop(snakeFood, foodPosition.Y);
            Canvas.SetLeft(snakeFood, foodPosition.X);
        }

        private Point GetNextFoodPosition()
        {
            /*  Idea: we will generate random position not exceeding the GameArea. Also, the position should not be occupied by any of the snake parts.
             *  Also, the position should be the multiple of SnakeSquareSize(20). Means, it should be 0, 20, 40, 60,... and so on upto the GameArea limit.
             *  To do so, we will divide the GameArea height or width by the SnakeSquareSize(20). 
             *  And then will generate random number between the divided result.
             *  After that we will multiply the random number with SnakeSquareSize(20). In this way we can get any number from 0,20,40,60,... and so on.
             */

            var occupiedPosition_X = new HashSet<double>();
            var occupiedPosition_Y = new HashSet<double>();

            foreach(var snakePart in snakeParts)
            {
                occupiedPosition_X.Add(snakePart.Position.X);
                occupiedPosition_Y.Add(snakePart.Position.Y);
            }

            var rnd = new Random();
            double random_X = -1;
            double random_Y = -1;
            var maxValue = -1;

            // Keep finding position if the position is occupied by the snakeParts
            while((random_X == -1 || occupiedPosition_X.Contains(random_X))
                && (random_Y == -1 || occupiedPosition_Y.Contains(random_Y)))
            {
                maxValue = (int)(GameArea.ActualWidth / SnakeSquareSize);
                random_X = rnd.Next(maxValue) * SnakeSquareSize;

                maxValue = (int)(GameArea.ActualHeight / SnakeSquareSize);
                random_Y = rnd.Next(maxValue) * SnakeSquareSize;
            }

            
            return new Point()
            {
                X = random_X,
                Y = random_Y
            };

        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            var previousSnakeDirection = snakeDirection;
            switch(e.Key)
            {
                case Key.Up:
                    if(snakeDirection != Direction.Down)
                    {
                        snakeDirection = Direction.Up;
                    }
                    break;
                case Key.Down:
                    if (snakeDirection != Direction.Up)
                    {
                        snakeDirection = Direction.Down;
                    }
                    break;
                case Key.Left:
                    if (snakeDirection != Direction.Right)
                    {
                        snakeDirection = Direction.Left;
                    }
                    break;
                case Key.Right:
                    if (snakeDirection != Direction.Left)
                    {
                        snakeDirection = Direction.Right;
                    }
                    break;
                case Key.Space:
                    StartNewGame();
                    break;
            }

            if(previousSnakeDirection != snakeDirection)
            {
                MoveSnake();
            }
        }
    }

    public class SnakePart
    {
        // A single part of the snake body.
        // This could be a body part or,
        // This could be the head of the snake.
        public UIElement UiElement { get; set; }
        public Point Position { get; set; }
        public bool IsHead { get; set; }
    }
}