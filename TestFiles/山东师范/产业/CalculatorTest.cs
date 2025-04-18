﻿using Flowing;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IndexCalculate;

namespace User
{
    class CalculatorTest: IApp
    {
        static void Main(string[] args)
        {
            main();
        }

        public Campus campus;
        CamController cam;
        Font font;
        string fileName1 = "../../../山东师范职业技术/产业/mustBuilding1.csv";
        string fileName2 = "../../../山东师范职业技术/产业/optionalBuilding1.csv";
        string exportPath = "../../../山东师范职业技术/产业/export0.csv";

        override
        public void SetUp()
        {
            SetCampus();

            cam = new CamController(this);
            cam.FixZaxisRotation = true;
            ColorMode(HSB);
            font = CreateFont("微软雅黑", 24);
            TextFont(font);
            TextAlign(1, 1);
        }
        override
        public void Draw()
        {
            int bgColor = 255;
            Background(bgColor);

            //呈现初始结果
            campus.InitialResult(this);

        }
        public void SetCampus()
        {
            Console.WriteLine("请依次输入以下信息：");
            Console.WriteLine("（1）建设校园类型，" +
                "(0综合一类 , 1工业类， 2财经、政法、管理类, 3体育类，4综合二类、师范类， 5农林、医药类，6外语类，7艺术类)");
            Console.WriteLine("（2）建设用地（公顷）");
            Console.WriteLine("（3）学校规模（学生数：人）");
            Console.WriteLine("（4）目标容积率");


            int type = Convert.ToInt32(Console.ReadLine());
            while (!Enum.IsDefined(typeof(schoolType), type))
            {
                Console.WriteLine("不存在该学校类型，请重新输入建设校园类型");
                type = Convert.ToInt32(Console.ReadLine());
            }

            double area = Convert.ToDouble(Console.ReadLine());
            int pop = Convert.ToInt32(Console.ReadLine());
            double r = Convert.ToDouble(Console.ReadLine());

            campus = new Campus((schoolType)type, pop, area, r,fileName1,fileName2,exportPath);
            Console.WriteLine($"生均校舍用地面积为{campus.Building_site_per}修改或跳过");
            try { campus.Building_site_per = int.Parse(Console.ReadLine()); } catch { }
            Console.WriteLine($"生均体育用地面积为{campus.Sport_area_per}修改或跳过");
            try { campus.Sport_area_per = int.Parse(Console.ReadLine()); } catch { }
            campus.Run();
            campus.Export();

            ///(1,13000,62.4,1)
        }
    }
}
