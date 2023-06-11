using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;

namespace InitialArrange
{
    class LinearVar
    {
        string name;
        internal int direction;
        internal GRBVar road;
        internal float width;
        List<Line> resultLine = new List<Line>();
        internal Domain[] range;
        GRBModel model;
        Site site;
        internal List<int> districtsLorB;
        internal List<int> districtsRorT;

        internal LinearVar(GRBModel model,int index, Site site,int direction,Domain[]range,float width)
        {
            this.model = model;
            this.site = site;
            this.direction = direction;
            this.width = width;
            this.range = range;
            if (direction == 0)
            {
                road = model.AddVar(site.Xmin, site.Xmax, 0, GRB.CONTINUOUS, "x");
                if (range[1].min < site.Ymin)
                    range[1].min = site.Ymin;
                if (range[1].max > site.Ymax)
                    range[1].max= site.Ymax;

            }
            else
            {
                road = model.AddVar(site.Ymin, site.Ymax, 0, GRB.CONTINUOUS, "y");
                if (range[1].min < site.Xmin)
                    range[1].min = site.Xmin;
                if (range[1].max > site.Xmax)
                    range[1].max = site.Xmax;
            }
            model.AddRange(road, range[0].min, range[0].max, " ");
        }

        internal List<Line> ResultLine { get => resultLine; set => resultLine = value; }


        internal void LineConstr(List<DistrictVar> districtVars)
        {

            int c = districtVars.Count;
            GRBVar[] road_bools = new GRBVar[ 4];

            switch (direction)
            {
                case 0:
                    //竖向道路
                    for (int d = 0; d < c; d++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            road_bools[i] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, " ");
                        }
                        model.AddConstr(districtVars[d].x - districtVars[d].dx / 2 + (1 - road_bools[0]) * site.W
                            >= road + width, "x-dx/2>=roadX ");//
                        model.AddConstr(districtVars[d].x + districtVars[d].dx / 2 - (1 - road_bools[1]) * site.W
                            <= road - width, "x+dx/2<=roadX ");//

                        model.AddConstr(districtVars[d].y + districtVars[d].dy / 2 - (1 - road_bools[2]) * site.H
                            <= range[1].min, "  ");//
                        model.AddConstr(districtVars[d].y - districtVars[d].dy / 2 + (1 - road_bools[3]) * site.H
                            >= range[1].max, "  ");//
                        model.AddConstr(road_bools[ 0] + road_bools[1] + road_bools[2] + road_bools[3] == 1, " ");

                        if (districtsLorB != null && districtsLorB.Contains(districtVars[d].District.Index))
                        {
                            model.AddConstr(road_bools[1] == 1, "");

                        }
                        if (districtsRorT != null && districtsRorT.Contains(districtVars[d].District.Index))
                        {
                            model.AddConstr(road_bools[0] == 1, "");

                        }
                    }
                        break;
                case 1:
                    //横向道路
                    for (int d = 0; d < c; d++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            road_bools[i] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, " ");
                        }
                        model.AddConstr(districtVars[d].y - districtVars[d].dy / 2 + (1 - road_bools[0]) * site.H >= road + width, "y-dy/2>=roadY ");
                        model.AddConstr(districtVars[d].y + districtVars[d].dy / 2 - (1 - road_bools[1]) * site.H <= road - width, "y+dy/2<=roadY ");
                        model.AddConstr(districtVars[d].x + districtVars[d].dx / 2 - (1 - road_bools[2]) * site.W <= range[1].min, "  ");//
                        model.AddConstr(districtVars[d].x - districtVars[d].dx / 2 + (1 - road_bools[3]) * site.W >= range[1].max, "  ");//

                        model.AddConstr(road_bools[0] + road_bools[1] + road_bools[2] + road_bools[3] == 1, " ");
                       
                        if (districtsLorB!=null&&districtsLorB.Contains(districtVars[d].District.Index))
                        {
                            
                            model.AddConstr(road_bools[1] == 1, "");
                        }
                        if (districtsRorT != null && districtsRorT.Contains(districtVars[d].District.Index))
                        {

                            model.AddConstr(road_bools[0] == 1, "");
                        }
                    }
                    break;
            }
        }

        internal void setResults(GRBModel model)
        {
            float roadLoc;
            roadLoc = (float)road.Xn;
            
            if (direction == 0)
               resultLine.Add(new Line(roadLoc, (float)range[1].min, roadLoc, (float)range[1].max));
            else
                resultLine.Add(new Line((float)range[1].min, roadLoc, (float)range[1].max, roadLoc));
        }

        internal void WriteResults(StreamWriter sw, int resultCount)
        {
            string dataStr;
            dataStr = width.ToString();
            for (int n = 0; n < resultCount; n++)
            {
                try
                {
                    dataStr += $",({resultLine[n].X1};{resultLine[n].Y1};" +
                        $"{ resultLine[n].X2 };{resultLine[n].Y2})" ;
                }
                catch { return; }
            }
            sw.WriteLine(dataStr);
        }
    }
}
