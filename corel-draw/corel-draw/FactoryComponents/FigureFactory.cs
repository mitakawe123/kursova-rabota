﻿using corel_draw.Figures;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace corel_draw.FactoryComponents
{
    public abstract class FigureFactory
    {
        //Shift -> inser point
        //Ctrl -> move point
        protected readonly Pen _defaultPen = new Pen(Color.Black, 2f);

        public event Action<Figure> Finished;
        public abstract void BeginCreateFigure();
        public abstract void MouseDown(MouseEventArgs e);
        public abstract void MouseMove(MouseEventArgs e);
        public abstract void MouseUp(MouseEventArgs e);
        public abstract void Draw(Graphics g);

        public void OnFinished(Figure figure)
        {
            Finished?.Invoke(figure);
        }
    }
}
