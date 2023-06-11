using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InitialArrange
{
    public class Line
    {
        IPoint p1;
        IPoint p2;
        float a, b, c;//ax+by+c = 0 


        public Line(IPoint p1, IPoint p2)
        {
            this.p1 = p1;
            this.p2 = p2;
            if (p1.p - p2.p != 0)
            {
                a = (p1.q - p2.q) / (p1.p - p2.p);
                b = -1;
                c = p1.q - a * p1.p;
            }
            else {
                a = -1;
                b = 0;
                c = p1.p;

            }
        }
        public Line(float x1,float y1,float x2,float y2 )
        {

            this.p1 = new IPoint(x1,y1);
            this.p2 = new IPoint(x2,y2);
            if (p1.p - p2.p != 0)
            {
                a = (p1.q - p2.q) / (p1.p - p2.p);
                b = -1;
                c = p1.q - a * p1.p;
            }
            else
            {
                a = 1;
                b = 0;
                c = -p1.p;

            }
        }

        public float X1 { get => p1.p; }
        public float X2 { get => p2.p; }
        public float Y1 { get => p1.q; }
        public float Y2 { get => p2.q; }
        public float A { get => a; set => a = value; }
        public float B { get => b; set => b = value; }
        public float C { get => c; set => c = value; }
        public Domain Xrange
        {
            get
            {
                var Xrange = new Domain(p1.p, p2.p);
                return Xrange;
            }
        }
        public Domain Yrange
        {
            get
            {
                var Yrange = new Domain(p1.q, p2.q);
                return Yrange;
            }
        }


        //判断点在线上
        public bool OnLine(IPoint poi,float tolerance)
        {
            bool onLine=false;
            if (Math.Abs(a * poi.p +b*poi.q+c)<=tolerance)
                onLine = true;

            return onLine;
        }
    }
}
