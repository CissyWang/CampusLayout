using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace InitialArrange
{
    public struct IRectangle
    {

        private float x1;
        private float y1;
        private float x2;
        private float y2;
        private IPoint center;
        //private float area;
        float dx;
        float dy;
        

        public IRectangle(float x1, float y1, float x2, float y2)
        {
            this.x1 = x1;
            this.x2 = x2;
            this.y1 = y1;
            this.y2 = y2;
            this.center = new IPoint((x1 + x2) / 2, (y1 + y2) / 2);
            this.dx = Math.Abs(x2 - x1);
            this.dy = Math.Abs(y2 - y1);
        }
        public IRectangle(IPoint center, float dx, float dy)
        {
            this.center = center;
            this.dx = dx;
            this.dy = dy;
            this.x1 = center.p - dx / 2;
            this.x2 = center.p + dx / 2;
            this.y1 = center.q - dy / 2;
            this.y2 = center.q + dy / 2;
        }

        public float X1 { get => x1; }
        public float Y1 { get => y1;  }
        public float X2 { get => x2; }
        public float Y2 { get => y2; }

        
        public IPoint Center { get => center;  }
        public float Area { get => dx*dy; }
        public float Dx { get => dx; set => dx = value; }
        public float Dy { get => dy; set => dy = value; }
    }
}
