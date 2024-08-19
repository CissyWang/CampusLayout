## 校园基础布局程序
- 利用Gurobi求解二次规划问题，引用了gurobi95.dll，需要先在官网申请密钥
- 所有参数的调整都在config.xml 文件当中，Configuration文件夹下放了一些示例。
- 需要准备的文件包括
- - 1. export.csv和同一地址下的export.json(它们由指标计算器生成，参考AreaCalculator仓库)
    2. site.csv, 场地解析文件，目前是csv格式，之后可以改成json传输。它来自由gh读取并解析的场地模型的几何信息。
- 另外，指定输出的位置，输出的是存储了计算结果的location.csv以及在同一地址下会存储求解的log文件，前者由自主编写的gh电池读取后可以在Rhino中呈现布局（有时间也需要改成json传输）。
- 程序的可视化界面是基于OpenTK的Flowing，当然，可视化效果不如rhino，可用于简单查看生成结果。
