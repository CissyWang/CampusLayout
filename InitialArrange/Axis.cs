using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InitialArrange
{
    public class Axis  : Line
    {
        internal double width;//轴线宽度

        internal int[] onZones;//在轴线上的分区id
        internal bool isCenter;//是否中心在轴线上

        internal int[] asideZones;//在轴线侧的分区id
        internal double buffer;//轴线影响范围

        public double Width { get => width; }

        public Axis(IPoint p1, IPoint p2) : base(p1, p2)
        {
        }

        public Axis(float x1, float y1, float x2, float y2) : base(x1, y1, x2, y2)
        {
        }
    }
}
