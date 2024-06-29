using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InitialArrange
{ 
    public struct IPoint
    {
        public float p;
        public float q;
        //public string name;

        //public string Name { get => name; set => name = value; }

        public IPoint(float p, float q)
        {
            this.p = p;
            this.q = q;
        }

        //点到点的距离
        public double distToPoint(IPoint poi)
        {
            double temp = Math.Pow(poi.p - p,2) + Math.Pow(poi.q - q,2);
            return temp;
            //return Math.Sqrt(temp);
        }

        //点到区域的最短距离
        public double distToPoints_min(List<IPoint> pois)
        {
            double temp = Math.Pow(pois[0].p - p, 2) + Math.Pow(pois[0].q - q, 2);

            if (pois.Count > 1) {
                double temp1=0;
                
                for (int i=0;i<pois.Count;i++)
                {
                    temp1 = Math.Pow(pois[i].p - p, 2) + Math.Pow(pois[i].q - q, 2);
                    if (temp1 < temp)
                        temp = temp1;
                    
                }
            }
            return temp;
            //return Math.Sqrt(temp);
        }


        public double distToPoints_max(List<IPoint> pois)
        {
            double temp = Math.Pow(pois[0].p - p, 2) + Math.Pow(pois[0].q - q, 2);


            if (pois.Count > 1)
            {
                double temp1;
                foreach (IPoint poi in pois)
                {
                    temp1 = Math.Pow(poi.p - p, 2) + Math.Pow(poi.q - q, 2);
                    if (temp1 > temp)
                        temp = temp1;
                    
                }
            }
            //return Math.Sqrt(temp);
            return temp;
        }

        //点到直线的距离
        public double distToLine(IPoint point1,IPoint point2)
        {
            double k = (point2.q - point1.q) / (point2.p - point1.p);
            double b = point1.q - k * point1.p;
            double temp = Math.Pow(k * p + b - q,2)/ (k * k + 1);
            //double temp = Math.Abs(k * p + b - q) / Math.Sqrt(k * k + 1);
            return temp;
        }
    }
}
