using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IndexCalculate
{
    //用于创建一类建筑
    public class BuildingList : IEnumerable<Building>
    {
        private List<Building> buildings;
        private readonly Campus campus;

        //构造方法1：从表格
        internal BuildingList(Campus owner, string fileName)
        {
            this.buildings = new List<Building>();
            this.campus = owner;
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs,Encoding.UTF8);
            string strLine ;
            strLine = sr.ReadLine();
            int index=0;

            while ((strLine = sr.ReadLine()) != null)
            {
                string[] str = strLine.Split(',');
                string name = str[0];

                string zone_name = str[str.Length - 4];
            
                double[] area1 = new double[str.Length - 5];
                for (int i = 0; i < area1.Length; i++)
                {
                    area1[i] = double.Parse(str[i + 1]);
                }
                int layer1 = int.Parse(str[str.Length-3]);
                double density1 = double.Parse(str[str.Length-2]);

                this.AddBuilding(index,name, layer1, area1, density1,zone_name);
                index++;
            }
        }

        //构造方法2：空+设置
        internal BuildingList()
        {
            this.buildings = new List<Building>();

        }

        //加入建筑，根据人数规模选择对应的人均面积要求
        internal int AddBuilding(int index,string building_name, int building_layer, double[] building_area_pers, double density, string zone_name)
        {
            int k = (int)campus.scType;
            int pop = campus.population;
            double building_per = 0;

            if (building_area_pers.Length > 1)
            {
                if (pop <= Campus.popClass[k, 0])//小于最低
                {
                    building_per = building_area_pers[0];
                }
                else if (pop > Campus.popClass[k, 2]) //大于最高人数
                {
                    building_per = building_area_pers[2];
                }
                else
                {
                    //在中间，插值
                    for (int i = 0; i < 2; i++)
                    {
                        if (pop > Campus.popClass[k, i] && pop <= Campus.popClass[k, i + 1])
                        {
                            building_per = building_area_pers[i] + (pop - Campus.popClass[k, i]) * (building_area_pers[i + 1]
                                - building_area_pers[i]) / (Campus.popClass[k, i + 1] - Campus.popClass[k, i]);

                            break;
                        }
                    }
                }
            }
            else if (building_area_pers.Length == 1)
            {
                building_per = building_area_pers[0];
            }

            buildings.Add(new Building(index,building_name, building_layer, 
                building_per * campus.population, density,zone_name));
            return this.Count;
        }

        //另外单独添加建筑和对应的面积要求
        internal int AddBuilding(Building addBuilding)
        {
            buildings.Add(addBuilding);
            return this.Count;
        }

        /// <summary>
        /// Get Attribute
        /// </summary>


        //返回某一种建筑的用地面积
        public double SiteArea(int i)
        {
            Console.WriteLine("  " + i + "." + this[i].Name + " _用地面积 ：" + this[i].Site_area);
            return (this[i].Site_area);
        }

        //返回该建筑列表的总面积
        public double Area_all(List<string> strs)
        {
            double area_all = 0;
            for (int i = 0; i < this.Count; i++)
                {
                    area_all += this[i].Area;
                }
           
                strs.Add(" 各项建筑面积 ：");
                for (int i = 0; i < this.Count; i++)
                {
                strs.Add($"  { (i + 1) }. { this[i].Name}  建筑面积 ：{this[i].Area}    用地面积：{Math.Round(this[i].Site_area),2}");
                }
            strs.Add($"  总计：{ area_all}\r\n");
            return area_all;
        }
        public double Area_all()
        {
            double area_all = 0;
            for (int i = 0; i < this.Count; i++)
            {
                area_all += this[i].Area;
            }
            return area_all;
        }
        public double FloorArea_all()
        {
            double floor_area_all=0;
            for (int i = 0; i < this.Count; i++)
            {
                floor_area_all += this[i].Floor_area;
            }
            return floor_area_all;
        }
        //返回该建筑列表的总用地面积
        public double Site_area_all()
        {
            double site_area_all = 0;
            for (int i = 0; i < this.Count; i++)
            {
                site_area_all += this[i].Site_area;
            }
            return site_area_all;
        }

        public double averageLayer()
        {
            double part=0;
            foreach (Building b in buildings)
            {
                part += b.Layer * b.Floor_area;
            }
            return  part / this.FloorArea_all();
        }

        //像数组一样返回元素
        public Building this[int index]
        {
            get
            {
                return this.buildings[index];
            }
            internal set
            {
                this.buildings[index] = value;
            }
        }

        //返回数量
        public int Count
        {
            get
            {
                return this.buildings.Count;
            }
        }

        public IEnumerator<Building> GetEnumerator()
        {
            return this.buildings.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.buildings.GetEnumerator();
        }
    }
}
