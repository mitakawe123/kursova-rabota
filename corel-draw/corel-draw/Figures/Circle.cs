﻿using System;
using System.Drawing;

namespace corel_draw.Figures
{
    internal class Circle:Figure
    {
        public override double CalcArea()
        {
            double radius = Width / 2.0;
            return Math.PI * radius * radius;
        }
        public Circle(int x, int y, int width, int height) : base(x, y, width, height)
        {
        }
        public override void Draw(Graphics g)
        { 
           g.DrawEllipse(new Pen(Color, 5), Location.X, Location.Y, Width, Height);
        }
    }
}
