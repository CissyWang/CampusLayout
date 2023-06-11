using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InitialArrange
{
    public class Road: Line
    {
        string name;
        float width;
        Line insectPart;
        int upDown;//-1保留上方，1保留下方,2穿越，0不相交

        public Road(IPoint p1,IPoint p2,float width) : base(p1, p2)
        {
            this.width = width;

        }
        public Road(float x1, float y1, float x2, float y2) : base(x1, y1, x2, y2) { }
    
        public Road(float x1, float y1, float x2, float y2, float width) : base(x1, y1, x2, y2)
        {
            this.width = width;
        }
        public Road(float x1, float y1, float x2, float y2, int upDown) : base(x1, y1, x2, y2)
        {
            this.upDown = upDown;
        }

        public string Name { get => name; set => name = value; }
        public float Width { get => width; set => width = value; }

        public Line InsectPart { get => insectPart; set => insectPart = value; }
        public int UpDown { get => upDown; set => upDown = value; }
    }
}
