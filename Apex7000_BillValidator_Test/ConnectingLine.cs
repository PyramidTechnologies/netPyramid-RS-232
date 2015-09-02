using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Apex7000_BillValidator_Test
{
    internal enum LineDirections
    {
        Up,
        Down,
        Left,
        Right
    }

    class ConnectingLine : UserControl
    {
        private LineDirections lineDirections;
        private Grid StateMachine;
        private Button btnPup;
        private Button btnDisabled;


        public ConnectingLine(LineDirections direction, Grid parent, Control start, Control end) 
            : base()
        {
            Direction = direction;
            Line = buildLine(direction, parent, start, end);
        }

        public LineDirections Direction { get; private set; }

        public Line Line { get; private set; }

        private static Line buildLine(LineDirections direction, Grid parent, Control start, Control end)
        {
            var line = new Line();
            line.Stroke = new SolidColorBrush(Colors.Black);
            line.StrokeThickness = 20.0;

            Point p1 = start.TransformToAncestor(parent).Transform(new Point(0, 0));
            Point p2 = end.TransformToAncestor(parent).Transform(new Point(0, 0));

            switch(direction)
            {
                    // end <- start
                case LineDirections.Left:
                    line.X1 = p1.X;
                    line.X2 = p2.X + end.ActualWidth;
                    line.Y1 = p1.Y + start.ActualHeight / 2;
                    line.Y2 = p2.Y + end.ActualHeight / 2;
                    break;

                    // start -> end
                case LineDirections.Right:
                    line.X1 = p1.X + start.ActualWidth;
                    line.X2 = p2.X;
                    line.Y1 = p1.Y + start.ActualHeight / 2;
                    line.Y2 = p2.Y + end.ActualHeight / 2;
                    break;

                    //  end
                    //   ^
                    //  start
                case LineDirections.Up:
                    line.X1 = p1.X + start.ActualWidth / 2;
                    line.X2 = p2.X + end.ActualWidth / 2;
                    line.Y1 = p1.Y;
                    line.Y2 = p2.Y + start.ActualHeight;
                    break;

                    // start
                    //   v
                    //  end
                case LineDirections.Down:
                    line.X1 = p1.X + start.ActualWidth / 2;
                    line.X2 = p2.X + end.ActualWidth / 2;
                    line.Y1 = p1.Y + start.ActualHeight;
                    line.Y2 = p2.Y;
                    break;
            }

            return line;

        }
    
    }
}
