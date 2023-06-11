  using System;
using System.Collections.Generic;
using System.Text;
using IndexCalculate;
//using Calculate_Area.BuildingList;


namespace InitialArrange
{
    public class IDistrict
    {
        private int index;
        private double s; //缩小后的面积
        internal Domain area_lim;
        internal int grid;
        int count=1;
        internal DistrictVar[] districtVars;

        public string name;
        private double site_area;
        internal double building_area;
        double length_min;
        BuildingList buildings;

        public IDistrict(District d, int grid)
        {
            this.index = d.Index;
            
            this.grid = grid;
            if (d.Buildings != null)
            {
                this.buildings = d.Buildings;
            }
            this.name = d.Name;
            this.site_area = d.Site_area();

            this.building_area = d.buildingArea();
        }

        public IDistrict(int index,string name, double area, double building_area,  int d_count, int grid) : base()
        {
            this.index = index;
            this.grid = grid;
            this.name = name;
            this.site_area = area;
            this.building_area = building_area;
            this.count = d_count;
        }

        internal DistrictVar[] DistrictVars { get => districtVars; set => districtVars = value; }
        public int Count { get => count; set => count = value; }
        public double Building_area { get => building_area; set => building_area = value; }

        public double Site_area { get => site_area; set => site_area = value; }
        public double Length_min { get => length_min; set => length_min = value; }
        public int Index { get => index; }
        public BuildingList Buildings { get => buildings; set => buildings = value; }
            
        public double S { get => site_area / grid / grid; }

        public double Result_area(int resultN)
        {
            double areaT=0;
            foreach(DistrictVar dv in districtVars)
            {
                areaT += dv.Area(resultN,grid);
            }
            return areaT;
        }

        public DistrictVar this[int index]
        {
            get
            {
                return this.districtVars[index];
            }
        }


    }
}

