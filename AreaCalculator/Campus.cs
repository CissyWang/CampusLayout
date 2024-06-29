using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Flowing;

namespace IndexCalculate
{
    public enum schoolType
    {
        综合一类 = 0,
        工业类 = 1,
        财经 = 2, 政法 = 2, 管理类 = 2,
        体育类 = 3,
        综合二类 = 4, 师范类 = 4,
        农林 = 5, 医药类 = 5,
        外语类 = 6,
        艺术类 = 7,
    }
    public class Campus
    {
        internal schoolType scType;
        internal int population;
        double area_per; //生均建筑面积
        double site_per;//额定生均用地面积
        double re_site_per;//实际生均用地面积
        double plotRatio; //总体容积率目标

        double area_total; //总建筑面积
        double area_total_target;//目标总建筑面积
        double site_area; // 校园面积

        double re_area_total; //实际总建筑面积

        double rest_buildingArea;
        double rest_buildingSiteArea;

        double building_siteArea;//校舍场地面积
        double sport_area;//户外体育活动场地面积
        double re_building_siteArea;//实际校舍场地面积
        double re_ratio;//实际容积率
        double re_density;//实际密度
        double floating;//需上调分区倍数
        double building_site_per;//额定
        double sport_area_per;//额定
        private BuildingList mustBuildings;//必配建筑
        private BuildingList optionalBuildings;//选配建筑
        private List<string> zoneNames = new List<string>();
        private List<Zone> zones = new List<Zone>();//

        string fileName1;
        string fileName2;
        string exportPath;

        List<string>[] strs = new List<string>[6];


        //规模等级和生均用地面积列表/改成表格输入
        internal static int[,] popClass = { { 5000, 8000, 10000 }, { 5000, 8000, 10000 }, { 5000, 8000, 10000 }, { 1000, 2000, 3000 }, { 5000, 8000, 10000 }, { 5000, 8000, 10000 }, { 5000, 8000, 10000 }, { 1000, 2000, 3000 } };
        static double[,] areaPerList = { { 24.56, 23.52, 22.49 }, { 26.38, 25, 24.29 }, { 21.85, 20.69, 20.07 }, { 31.28, 29.21, 28.08 }, { 25.68, 24.18, 23.40 }, { 26.15, 24.75, 24.04 }, { 22.63, 21.39, 20.74 }, { 35.25, 31.54, 29.27 } };
        static string siteAreaPerCsv = "生均用地指标-江苏省.csv";

        int[][] site_perList = new int[3][];//学校类型对应的 规模-人均用地
        int[] classify = new int[] { 5000, 10000 };//规模分级

        public Campus(schoolType type_number, int population, double site_area, 
            double plotRatio, string mustFile,string optionalFile,string exportFile)
        {
            scType = type_number;

            #region 1. 用地和人数检测
            this.site_area = site_area * 10000; //单位：公顷
            this.population = population;
            this.SetSitePerList();
            var a = this.PopulationDetect(population);//检测学生数量是否在规定范围内
            while (!a)
            {
                Console.WriteLine("请重新输入学生人数（回车键跳过）");
                string s = Console.ReadLine();
                try{
                    int p = Convert.ToInt32(s);
                    population = Convert.ToInt32(s);
                    a = this.PopulationDetect(population);//检测学生数量是否在规定范围内
                }
                catch
                {
                    break;
                }
            }//重新输入或跳过
            
            re_site_per = this.site_area / population;//实际生均用地面积

            //分配额定用地
             building_site_per = PickNum(site_perList[1], classify);
            sport_area_per = PickNum(site_perList[2], classify);
            #endregion

            #region  2. 建设量和容积率检测
            //额定建设量
            this.SetAreaPer();//按照不同规模设置生均建筑面积
            this.area_total = area_per * this.population;

            //检测总建筑面积
            while (this.site_area * plotRatio < this.area_total)
            {
                Console.WriteLine($"容积率过低不满足生均建筑面积要求，容积率应大于: { this.area_total/ this.site_area}" );
                Console.WriteLine("请重新输入目标容积率");
                plotRatio = Convert.ToDouble(Console.ReadLine());
            }
            this.plotRatio = plotRatio;
            area_total_target = plotRatio * this.site_area;
            #endregion

            #region 3.总体指标及用地
            strs[0] = new List<string>();
            strs[0].Add($"学校类型：{scType}");
            strs[0].Add($"总用地面积：{ site_area}（公顷）");
            strs[0].Add($"总人数：{population}");

            strs[0].Add($"实际生均用地面积：{ Math.Round(re_site_per,2)}（平方米）");
            strs[0].Add($"额定总建筑面积：{ area_total }（平方米）");
            strs[0].Add($"目标总建筑面积： { area_total_target }（平方米）\r\n");
            foreach (string s in strs[0])
            {
                Console.WriteLine(s);
            }
            #endregion
              
            //设置文件路径
            this.exportPath = exportFile;
            this.fileName1 = mustFile;
            this.fileName2 = optionalFile;

        }
        public void Run()
        {
            #region 分配用地面积
            Console.WriteLine($"生均校舍用地面积为{Building_site_per}修改或跳过");
            try {building_site_per = int.Parse(Console.ReadLine()); } catch { }
            Console.WriteLine($"生均体育用地面积为{Sport_area_per}修改或跳过");
            try { sport_area_per = int.Parse(Console.ReadLine()); } catch { }

            if (building_siteArea == 0)
            {
                this.building_siteArea = building_site_per * population;
            }
            if (sport_area == 0)
            {
                this.sport_area = sport_area_per * population;
            }
            strs[5] = new List<string>();
            strs[5].Add("用地面积:");
            strs[5].Add($"  校舍用地面积：{building_siteArea }（平方米）");
            strs[5].Add($"  体育用地面积：{sport_area }（平方米）\r\n");

            foreach (string s in strs[5])
            {
                Console.WriteLine(s);
            }
            #endregion

            Console.WriteLine("\r\n" + ">>>  按任意键开始计算建筑");
            Console.ReadLine();
            #region 设置必配建筑
            strs[1] = new List<string>();
            
            mustBuildings = new BuildingList(this, fileName1);
            strs[1].Add($"必配建筑：{ mustBuildings.Count}");
            rest_buildingArea = this.area_total_target - mustBuildings.Area_all(strs[1]);
            rest_buildingSiteArea = building_siteArea - mustBuildings.Site_area_all(); //计算剩余的面积

            foreach (string s in strs[1])
            {
                Console.WriteLine(s);
            }//打印
            #endregion

            #region 设置选配建筑
            strs[2] = new List<string>();
            optionalBuildings = new BuildingList();
            if(rest_buildingArea <= 0)
            {
                re_area_total = this.area_total_target - rest_buildingArea;
                Console.WriteLine($"建筑面积{re_area_total}已到达建设量上限{this.area_total_target}");
                
            }
            else if (rest_buildingSiteArea <= 0)
            {
                strs[2].Add("已达到校舍用地面积");
            }
            else
            {
                this.addOptionalBuidingsOneByOne();
                strs[2].Add("选配建筑：" + optionalBuildings.Count);
                optionalBuildings.Area_all(strs[2]);
                rest_buildingSiteArea = building_siteArea - mustBuildings.Site_area_all() - optionalBuildings.Site_area_all();
                Console.WriteLine("富余校舍用地面积：" + Math.Round(rest_buildingSiteArea));
            }

            //打印
            foreach (string s in strs[2])
            {
                Console.WriteLine(s);
            }
            #endregion

            #region 计算分区
            Console.WriteLine("\r\n" + ">>>  按任意键开始计算分区");
            Console.ReadLine();
            //设置分区
            SetZone(mustBuildings);
            SetZone(optionalBuildings);
            zones.Add(new Zone(zones.Count,"户外体育区", sport_area));
            strs[3] = new List<string>();
            this.ExportZone(strs[3]);
            //foreach (Zone d in zones)
            //{
            //    //strs[3].Add("\r\n" + "(" + (zones.IndexOf(d) + 1) + ")" + d.Name);
            //    //d.Site_area(strs[3]);
            //}
            foreach (string s in strs[3])
            {
                Console.WriteLine(s);
            }
            #endregion

            #region 计算实际总指标
            re_area_total = area_total_target - rest_buildingArea;
            re_building_siteArea = building_siteArea - rest_buildingSiteArea;
            re_ratio = Math.Round(re_area_total / site_area, 2);
            double re_siteDensity = Math.Round(re_building_siteArea / site_area , 2);//分区用地率
            double re_building_floorArea = mustBuildings.FloorArea_all() + optionalBuildings.FloorArea_all();
            re_density = Math.Round(re_building_floorArea / site_area, 2);
            floating = Math.Round(building_siteArea / re_building_siteArea, 2);

            strs[4] = new List<string>();
            strs[4].Add("");
            strs[4].Add($"实际建设用地：校舍{Math.Round(re_building_siteArea,2)}，" +
                $"体育{sport_area}，其它{site_area - sport_area - re_building_siteArea}");
            strs[4].Add($"实际总建筑面积：{Math.Round(re_area_total,2)}");
            strs[4].Add($"实际容积率：{re_ratio}");
            strs[4].Add($"实际密度：{re_density}");
            strs[4].Add($"分区用地率：{re_siteDensity}");
            strs[4].Add($"分区面积上浮范围：{floating-1}");
            foreach (string s in strs[4])
            {
                Console.WriteLine(s);
            }
            #endregion
        }
        public void Export()
        {
            FileStream fs1 = new FileStream(exportPath, FileMode.Create, FileAccess.Write);
            StreamWriter sw1 = new StreamWriter(fs1, Encoding.UTF8);
            int[] index = new int[] {0,4,5,1,2,3};
            foreach(int i in index)
            {
                foreach (string s in strs[i])
                {
                    sw1.WriteLine(s);
                }
            }
            sw1.Close();
        }
        private void SetZone(BuildingList buildings)
        {
            int index = zones.Count;
            foreach (Building b in buildings)
            {
                if (!zoneNames.Contains(b.Zone_name))
                {
                    zoneNames.Add(b.Zone_name);
                    var d = new Zone(index,b.Zone_name);
                    d.Buildings.AddBuilding(b);
                    zones.Add(d);
                    index++;
                }
                else
                {
                    int i = zoneNames.IndexOf(b.Zone_name);
                    zones[i].Buildings.AddBuilding(b);
                }
            }
        }
        private void ExportZone(List<String>strs)
        {
            strs.Add("分区名, 建筑类型, 用地面积最小值, 建筑总面积, 分区容积率, 建设密度");
            foreach (Zone d in zones)
            {
                var str = $"{zones.IndexOf(d)}";
                str += d.Name + ",";
                var aa = d.Site_area();
                if (d.Buildings != null)
                {
                    foreach (Building b in d.Buildings)
                    {
                        str += b.Name + ";";
                    }
                    var a = d.Buildings.Area_all();
                    double a1 = a / aa;
                    double den = d.Buildings.FloorArea_all() / aa;
                    str += "," + Math.Round(aa) + "," + Math.Round(a) + "," + Math.Round(a1, 2) + "," + Math.Round(den, 2);
                }
                else
                {
                    str += "," + aa + "," + 0 + "," + 0 + "," + 0;
                }
                strs.Add(str);
            }

        }

        //读取人均用地指标表（内置）
        private void SetSitePerList()
        {
            FileStream fs = new FileStream(siteAreaPerCsv, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);
            string strLine = sr.ReadLine();
            while (strLine != null && (strLine = sr.ReadLine()) != null)
            {
                string[] str = strLine.Split(',');
                string name = str[0];
                if (!name.Contains(scType.ToString()))
                {
                    continue;
                }
                string[] s;
                for (int j = 1;j<4;j++ ) {
                     s = str[j].Split('-', '{', '}');
                    site_perList[j-1] = new int[s.Length-2];
                    for (int i = 1; i < s.Length-1; i++)
                    {
                        site_perList[j-1][i-1] = int.Parse(s[i]);
                    }
                }  
            }
        }

        ///逐项增加选配建筑吧 
        private void addOptionalBuidingsOneByOne()
        {
            
            FileStream fs = new FileStream(fileName2, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, Encoding.UTF8);
            string strLine = sr.ReadLine();
            BuildingList outBuildings = new BuildingList();//
            int index = 0;
            while (strLine != null && (strLine = sr.ReadLine()) != null)
            {
                try
                {
                    string[] str = strLine.Split(',');
                    string name = str[0];
                    double area1 = double.Parse(str[1]);
                    int layer1 = int.Parse(str[3]);
                    double density1 = double.Parse(str[4]);
                    string district_name = str[2];

                    var add = new Building(index,name, layer1, area1, density1, district_name);
                    if (rest_buildingArea - add.Area > 0 && rest_buildingSiteArea - add.Site_area > 0)
                    {
                        rest_buildingArea -= add.Area;
                        rest_buildingSiteArea -= add.Site_area;
                        optionalBuildings.AddBuilding(add);
                    }
                    else
                    {
                        outBuildings.AddBuilding(add);//未添加的项目
                    }
                    index++;
                }
                catch
                {
                    continue;
                }
            }

            var strs1 = new List<string>();
            optionalBuildings.Area_all(strs1);
            foreach(string s in strs1)
            {
                Console.WriteLine(s);
            }
            
            Console.WriteLine("待分配建筑面积：" + rest_buildingArea);
            Console.WriteLine("富余校舍用地面积：" + Math.Round(rest_buildingSiteArea));
            //剩余面积分配给谁

            int toB;
            Console.WriteLine("剩余面积分配给(输入选配项目编号，默认最后一项)：");
            try{ toB = int.Parse(Console.ReadLine()); }catch { toB = index-1; }
            if (optionalBuildings.Count>0|| outBuildings.Count>0)
            {
                try
                {
                    optionalBuildings[toB].Area += rest_buildingArea;
                }
                catch
                {
                    optionalBuildings.AddBuilding(outBuildings[toB - optionalBuildings.Count]);
                }
            }
            ///实际总建筑面积       
        }

        //根据生均面积要求，检测学生数量是否在规定范围内
        private bool PopulationDetect(int population)
        {
            site_per = PickNum(site_perList[0],classify);
            int pop_limit = (int)(this.site_area / site_per);
            if (pop_limit < population)
            {
                double perNew =Math.Round( this.site_area / population,2);
                Console.WriteLine($"注意：人均用地面积{perNew}<{site_per},总人数应限制在{ pop_limit}");
                return false;
            }
            else
            {
                return true;
            }
        }

        //插值法，按照不同规模设置生均建筑面积
        private void SetAreaPer()
        {
            if (population <= popClass[(int)scType, 0])//小于最低人数
            {
                area_per = areaPerList[(int)scType, 0];
                return;
            }
            else if (population > popClass[(int)scType, 2]) //大于最高人数
            {
                area_per = areaPerList[(int)scType, 2];
                return;
            }

            //在中间，插值
            for (int i = 0; i < 2; i++)
            {
                if (population > popClass[(int)scType, i] && population <= popClass[(int)scType, i + 1])
                {
                    area_per = areaPerList[(int)scType, i] + (population - popClass[(int)scType, i]) * 
                        (areaPerList[(int)scType, i + 1] - areaPerList[(int)scType, i]) / (popClass[(int)scType, i + 1] - popClass[(int)scType, i]);
                }
            }

        }

        //按照不同规模设置生均用地面积

        //非插值
        private double PickNum(int[] list,int[] classify)
        {
            double num;
            if (population <= classify[0])
            {
                num = list[2];
            }
            else if (population > classify[1])
            {
                num = list[0];
            }
            else
            {
                num = list[1];
            }
            return num;
        }

        //呈现
        public void InitialResult(IApp app)
        {
            float width;
            float height;
            float height1;
            float x = 0;
            float y = 0;
               
            float k = 1.5f;
            int colorIndex = 0;
           // var colorH = new int[] {0,100,182,142,156,168,12,0,200,130,36,44,72,28,84};
            var colorH = new int[20];
            for (int i = 0; i < 20; i++)
            {
                colorH[i] = 12 * i;
            }
            app.TextFont(new System.Drawing.Font("宋体",6));
            app.PushMatrix();
            app.Rotate(-45);
            foreach (Zone d in zones)
            {
                width = 60;
                height = (float)d.Site_area() / width;
                
                if (height > k * width)
                {
                    width = (float)Math.Sqrt(d.Site_area() / k);
                    height = k * width;
                }
                else if (k * height < width)
                {
                    height = (float)Math.Sqrt(d.Site_area() / k);
                    width = k * height;
                }
                height1 = (float)floating * height;
                app.Stroke(0);
                app.StrokeWeight(1.5f);
                //app.Fill(12*colorIndex, 120, 240,150);
                //if(colorIndex>1)
                    app.Fill(colorH[colorIndex%colorH.Length], 120, 240, 150);
                //else
                //    app.Fill(colorH[colorIndex], 100);

                app.PushMatrix();
                app.Translate(x,y,0);
                app.Rotate(20);
                app.BeginShape();
                app.Vertex(0,0, 0);
                app.Vertex(width,0 , 0);
                app.Vertex( width, height, 0);
                app.Vertex(0,  height, 0);
                app.EndShape();
                
                //绘制用地上限
                if (d.Buildings!=null)
                {
                    app.Fill(colorH[colorIndex % colorH.Length], 120, 240, 50);
                    app.BeginShape();
                    app.Vertex(0,0, -1);
                    app.Vertex(width, 0, -1);
                    app.Vertex(width, height1, -1);
                    app.Vertex(0, height1, -1);
                    app.EndShape();
                }
                

                //绘制建筑
                if (d.Buildings != null)
                {
                    ///app.Stroke(12 * colorIndex, 255, 180);
                    //if (colorIndex > 1)
                        app.Stroke(colorH[colorIndex % colorH.Length], 255, 180);
                    //else
                    //    app.Stroke(colorH[colorIndex], 100);
                    float y1 = 10;
                    foreach (Building b in d.Buildings)
                    {
                        app.Fill(255);
                        int z = 2;
                        float width1 = 0.8f * width;
                        float length = (float)b.Floor_area / width1;

                        if (length < 3)
                        {
                            length = 3;
                            width1 = (float)b.Floor_area / 3;
                        }
                        //绘制底线
                        app.BeginShape();
                        app.Vertex(width / 2-width1/2, y1, 0);
                        app.Vertex(width / 2 + width1 / 2, y1, 0);
                        app.Vertex(width / 2 + width1 / 2, y1+length, 0);
                        app.Vertex(width / 2 - width1 / 2, y1 + length, 0);
                        app.EndShape();

                        //绘制每层体块
                        for (int floor = 0; floor < b.Layer; floor++)
                        {
                            app.PushMatrix();
                            app.Translate( width / 2, y1 + length / 2, z);
                            app.Cube(width1, length, 4);

                            z += 4;
                            app.PopMatrix();
                        }

                        //if (colorIndex > 1)
                            app.Fill(colorH[colorIndex % colorH.Length], 255, 120,150);
                        //else
                        //    app.Fill(colorH[colorIndex],150);
                        //tab
                        app.TextSize(6.5f);
                        string bName = b.Name;
                        if (b.Name.Length > 8)
                        {
                            //dName = d.Name.Remove(6);
                            bName = b.Name.Insert(6, Environment.NewLine);
                        }
                        app.Text(bName,  width / 2, y1 + length / 2 + 30, z);
                        app.TextSize(6);
                        app.Text(b.Area.ToString() + "㎡", width / 2, y1 + length / 2+5, z);
                        app.Text(b.Layer.ToString()+ "F",  width / 2, y1 + length / 2-5, z);

                        y1 += length + 25;
                    }

                }
                app.PopMatrix();
                app.Fill(0,140);
                app.NoStroke();

                app.TextSize(8.5f);
                string dName = d.Name;
                if (d.Name.Length > 6)
                {
                    //dName = d.Name.Remove(6);
                    dName = d.Name.Insert(4,Environment.NewLine);
                }
                
                app.Text(colorIndex+dName,  x+width / 2,  y- 25, 0);
                app.Text(Math.Round(d.Site_area(), 0).ToString() + "㎡", x+width / 2, y- 50, 0);
                

                app.StrokeWeight(0);
                
                if (x>800)//colorIndex>0&&colorIndex % 7==0
                {
                    x = 0;
                    y += 550;
                }
                else
                {
                    x += width + 40;
                }
                colorIndex++;
            }
            app.PopMatrix();
        }



        #region 封装
        public double Area { get => re_area_total; }
        public double Site_area { get => site_area; }
        public BuildingList MustBuildings { get => mustBuildings; }
        public BuildingList OptiomalBuildings { get => optionalBuildings; set => optionalBuildings = value; }
        public List<Zone> Zones { get => zones; set => zones = value; }
        public double Building_siteArea {set => building_siteArea = value; }
        public double Sport_area {set => sport_area = value; }
        public double Building_site_per { get => building_site_per; set => building_site_per = value; }
        public double Sport_area_per { get => sport_area_per; set => sport_area_per = value; }

        #endregion
    }
}
