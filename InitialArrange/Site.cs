using System;
using System.Collections.Generic;
using System.IO;

namespace InitialArrange
{
    public class Site
    {
        //基础frame
        IRectangle oob;
        //减去的矩形
        List<IRectangle> minus;
        List<IRectangle> blocks;
        //frame和site之间区域的点集
        List<IPoint> outOfSite;
        List<IPoint> entrances;
        List<IPoint> boundry;
        List<Road> roads;
        List<Road> cutters;

        internal float w;
        internal float h;

        //父场地，完整的矩阵
        public Site(int width, int height, IPoint origin)
        {
            this.oob = new IRectangle(origin.p, origin.q, origin.p + width, origin.q + height);
            w = width;
            h = height;
        }

        //从列表中读取场地
        public Site(string fileName)
        {
            this.ReadFrameFromCSV(fileName);
            w = oob.Dx;
            h = oob.Dy;
        }


        public List<IPoint> All_points
        {
            get
            {
                var all_points1 = new List<IPoint>();
                for (int j = (int)Math.Ceiling(oob.Y1); j <= oob.Y2; j++)
                {
                    for (int i = (int)Math.Ceiling(oob.X1); i <= oob.X2; i++)
                    {
                        all_points1.Add(new IPoint(i, j));
                    }
                }
                return all_points1;
            }
        }

        //缩放后的可用面积
        public float Area()
        {
            float area = w * h;

            if (minus != null)
            {
                foreach (IRectangle rect in minus)
                {
                    area = area - rect.Area;
                }
            }
            if (outOfSite != null)
            {
                foreach (IPoint p in outOfSite)
                {
                    area = area - 1;
                }
            }
            return area;
        }

        ///从列表中读取场地
        public void ReadFrameFromCSV(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            string strLine = "";
            string[] str;

            while ((strLine = sr.ReadLine()) != null)
            {
                switch (strLine)
                {
                    case "Boundry":
                        boundry = new List<IPoint>();
                        this.ReadPoints(strLine, sr, boundry);

                        break;

                    case "Frame":
                        strLine = sr.ReadLine();
                        str = strLine.Split('(', ';', ')');
                        oob = new IRectangle(float.Parse(str[1]), float.Parse(str[2]), float.Parse(str[3]), float.Parse(str[4]));
                        break;

                    case "Minus":
                        minus = new List<IRectangle>();
                        this.ReadRects(strLine, sr, minus);
                        break;
                    case "Block":
                        blocks = new List<IRectangle>();
                        this.ReadRects(strLine, sr, blocks);
                        break;

                    case "Entrance":
                        entrances = new List<IPoint>();
                        this.ReadPoints(strLine, sr, entrances);
                        break;

                    case "Road":
                        roads = new List<Road>();
                        this.ReadLines(strLine, sr, roads);
                        break;

                    case "OutsidePoints":
                        outOfSite = new List<IPoint>();
                        this.ReadPoints(strLine, sr, outOfSite);
                        break;
                }
            }
        }

        /// 读取点、线、面的方法
        private void ReadPoints(string strLine, StreamReader sr, List<IPoint> points)
        {

            strLine = sr.ReadLine();
            string[] str = strLine.Split(',');
            foreach (string s in str)
            {
                if (s.Length > 0)
                {
                    string[] strs = s.Split('(', ';', ')');
                    points.Add(new IPoint(float.Parse(strs[1]), float.Parse(strs[2])));
                }
            }

        }
        private void ReadRects(string strLine, StreamReader sr, List<IRectangle> rects)
        {
            strLine = sr.ReadLine();
            string[] str = strLine.Split(',');
            foreach (string s in str)
            {
                if (s.Length > 0)
                {
                    string[] strs = s.Split('(', ';', ')');
                    rects.Add(new IRectangle(float.Parse(strs[1]), float.Parse(strs[2]), float.Parse(strs[3]), float.Parse(strs[4])));
                }
            }
        }
        private void ReadLines(string strLine, StreamReader sr, List<Road> roads)
        {
            strLine = sr.ReadLine();
            string[] str = strLine.Split(',');
            foreach (string s in str)
            {
                if (s.Length > 0)
                {
                    string[] strs = s.Split('(', ';', ')');
                    var r = new Road(float.Parse(strs[1]), float.Parse(strs[2]), float.Parse(strs[3]), float.Parse(strs[4]));

                    r.Width = float.Parse(strs[5]);
                    r.UpDown = int.Parse(strs[6]);
                    if (strs.Length > 7)
                        r.InsectPart = new Line(float.Parse(strs[7]), float.Parse(strs[8]), float.Parse(strs[9]), float.Parse(strs[10]));
                    roads.Add(r);
                }
            }
        }

        internal double lineDistMax(Line l)
        {
            double dist = 0;
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    double dist1 = (i * l.A + j * l.B + l.C) * (i * l.A + j * l.B + l.C) / (l.A * l.A + l.B * l.B);
                    if (dist1 > dist)
                        dist = dist1;
                }
            }

            return dist;
        }

        /// <summary>
        /// Attributes
        /// </summary>
        public float W { get => w; }
        public float H { get => h; }
        public float Xmin { get => oob.X1; }
        public float Ymin { get => oob.Y1; }
        public float Xmax { get => oob.X2; }
        public float Ymax { get => oob.Y2; }

        public List<IPoint> OutOfSite { get => outOfSite; }
        public IRectangle Oob { get => oob; }
        public List<IRectangle> Minus { get => minus; }
        public List<IPoint> Entrances { get => entrances; }
        public List<Road> Roads { get => roads; }
        public List<IPoint> Boundry { get => boundry; set => boundry = value; }
        public List<IRectangle> Blocks { get => blocks; set => blocks = value; }


        ///</end>
    }
}
