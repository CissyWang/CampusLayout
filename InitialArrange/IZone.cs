  using System;
using System.Collections.Generic;
using System.Text;
using IndexCalculate;
//using Calculate_Area.BuildingList;
//功能分区类


namespace InitialArrange
{
    public class IZone:ZoneBasic
    {
        internal ZoneVar[] zoneVars;
        int count=1;
        public string name;
        internal double building_area;
        double length_min;
        BuildingList buildings;
        internal Domain area_lim;
        public IZone(Zone d, int unit):base(unit)
        {
            this.index = d.Index;
            
            this.unit = unit;
            if (d.Buildings != null)
            {
                this.buildings = d.Buildings;
            }
            this.name = d.Name;
            this.site_area = d.Site_area();

            this.building_area = d.buildingArea();
        }
        public IZone(int index,string name, double area, double building_area,  int d_count, int unit):base(unit) 
        {
            this.index = index;
            this.unit = unit;
            this.name = name;
            this.site_area = area;
            this.building_area = building_area;
            this.count = d_count;
        }
        internal ZoneVar[] ZoneVars { get => zoneVars; set => zoneVars = value; }
        public int Count { get => count; set => count = value; }
        public double Building_area { get => building_area; set => building_area = value; }

        public double Length_min { get => length_min; set => length_min = value; }
        public BuildingList Buildings { get => buildings; set => buildings = value; }
        public double Result_area(int resultN)
        {
            double areaT = 0;
            foreach (ZoneVar dv in zoneVars)
            {
                areaT += dv.Area(resultN, unit);
            }
            return areaT;
        }

        public ZoneVar this[int index]
        {
            get
            {
                return this.zoneVars[index];
            }
        }

    }
}

