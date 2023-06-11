using System;
using System.Collections.Generic;
using System.Text;

namespace IndexCalculate
{
    public class District
    {
        string name;
        protected double site_area;
        BuildingList buildings;
        int index;

        public District(int index, string name)
        {
            this.index = index;

            this.name = name;
            buildings = new BuildingList();
        }


        //没有面积的区
        public District(int index,string name, double site_area)
        {
            this.index = index;
            this.name = name;
            this.site_area = site_area;
        }

        public double Site_area(List<string>strs)
        {
            if (buildings != null)
            {
                //site_area = buildings.Site_area_all();

                    for (int i = 0; i < buildings.Count; i++)
                    {
                        strs.Add("  " + (i + 1) + "." + buildings[i].Name + "  用地面积 ：" + buildings[i].Site_area);
                    }
                    //strs.Add("  总用地面积 ：" + site_area + "\r\n");

                return site_area;
            }
            else
            {
                strs.Add(" 总用地面积" + site_area);
                return site_area;
            }
        }
        public double Site_area()
        {
            if (buildings != null)
            {
                site_area = buildings.Site_area_all();
                return site_area;
            }
            else
            {
                return site_area;
            }
        }

        public string Name { get => name; set => name = value; }
        public BuildingList Buildings { get => buildings; set => buildings = value; }

        public double buildingArea()
        {
            if (buildings == null)
            {
                return 0;
            }
                return buildings.Area_all();

        }

        public double Density
        {
            get=> Buildings.FloorArea_all() / site_area;
        }

        public double PlotRatio
        {
            get=> buildingArea()/ site_area;
        }
        public int Index { get => index; }
    }
}
