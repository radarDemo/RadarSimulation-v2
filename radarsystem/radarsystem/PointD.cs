using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace radarsystem
{
    public class PointD 
    {
        public double X;
        public double Y;
        public PointD(){

        }
        public PointD(double X,double Y)
        {
            this.X = X;
            this.Y = Y;
        }
        public static PointD operator +(PointD p1,PointD p2)
        {
            return new PointD(p1.X + p2.X, p1.Y + p2.Y);
         }

        public static PointD operator /(PointD p1, double d)
        {
            return new PointD(p1.X/d, p1.Y/d);
        }
    }
}
