
# 校园基础布局程序
## Introduce
- 该程序将场地布局问题抽象为二次规划模型，输入场地中有指标要求的功能分区列表、和布局的要求，输出布局结果。
- 其中，每个功能分区可以划分为多个矩形地块，求解时作为基本单元，地块的坐标和长宽为模型的变量。
- 布局的要求包括：每个功能的面积、长宽比的形态要求，功能之间的拓扑关系，不同场地和功能对场地要素的响应，以及布局结构
- 参考文章：[Urban Site Adaptive Layout Generation: User- Configurable Algorithms Based on MQP](https://www.caadria2024.org/wp-content/uploads/2024/04/212-URBAN-SITE-ADAPTIVE-LAYOUT-GENERATION.pdf#:~:text=To%20accommodate%20real-world%20site%20conditions,%20this%20study%20explores%20site%20layout)
- 应用对象主要为大学校园，功能指标列表可由大学校园[指标计算器](https://github.com/CissyWang/IndexCalculator)生成
<img src="https://archialgo-com-sources.oss-cn-hangzhou.aliyuncs.com/images/Figure3.jpg" alt="示例图片" style="width: 500px; height: auto;">
<img src="https://archialgo-com-sources.oss-cn-hangzhou.aliyuncs.com/images/Fig5.jpg" alt="示例图片" style="width: 500px; height: auto;">

## 1.Reference & Configuration
- 利用商业求解器[Gurobi](https://www.gurobi.com/academia/academic-program-and-licenses/)求解二次规划问题，引用gurobi95，需要先在官网申请Token
- 程序内部的可视化依赖[Flowing](https://github.com/ds199895/Flowing)。另外，输出文件可由编写好的Grasshopper组件读取，在Rhino中呈现（见ghComponents/）。**待补充图片**
- 所有程序调试的参数config.xml文件当中，在Configuration文件夹下放置了一些测试示例。
- 引用的类库CampusClass.dll是与指标计算器共用的一些类，源代码见[指标计算器](https://github.com/CissyWang/IndexCalculator)
## 2.Preparation
- 2.1 准备分区指标文件：.csv、.json,可由[指标计算器](https://github.com/CissyWang/IndexCalculator)自动生成。
  - csv文件用于提供分区的名称、面积、划分数量信息，在自动生成的csv表格最后一栏可填写每个分区要划分的数量，默认为1。
  - json文件用于提供分区内的建筑信息（可选，如果没有在生成结果中仅呈现地块布局，不呈现三维体量）
- 2.2 准备场地文件：打开 /ghComponent/site.3dm，利用site组件读取几何图形并导出为.csv格式(建议改为json传输）。**待补充图片**
  - 注意：请先将将场地缩小unit倍数，默认为20（经测试，原尺寸运行程序耗时较长）
## 3.Config
调试程序请打开Solution/TestProject1.sln,修改主程序ProgramNew.cs中指向config文件，并运行
修改config.xml文件的参数含义：
- 3.1 程序运行基本参数<basic>：
  ```
	<basic>
        <unit>20</unit> <!--单元大小，即运行时整体缩放倍数，默认为20。-->
        <resultCount>5</resultCount> <!--单次求解数量-->
        <time>200.0</time> <!--求解时间限制，单位：s-->
        <searchMode>2</searchMode> <!--searchMode:  0专注最优解，1按数量搜索可行解并不保证质量，2系统搜索可行解-->
        <isInteger> 0 </isInteger> <!--是否为整数规划，0=false, 1=true-->
	</basic>
  ```
- 3.2 文件位置<filepath>：
  ```
  <filepath>
        <zoneFile> ../../南工职大/应用/export1.2-0.csv </zoneFile> <!--有分区信息表格，后续利用用户界面修改。-->
        <siteFile>../../南工职大/应用/site0.csv</siteFile> <!--场地信息表格，后续利用用户界面修改。-->
        <locationFile>../../location_newtest.csv</locationFile> <!--输出的是存储了计算结果的location.csv以及在同一地址下会存储求解的log文件，csv由自主编写的gh电池读取后可以在Rhino中呈现布局（建议改成json传输）。-->
  </filepath>
  ```
- 3.3 目标权重<weights>
    每个目标都归一化到0~100，默认为优化目标为最小化，最大化把权重改为负数。
    目前简单设置面积权重和距离权重，后续有用户界面，可深化为具体目标的权重。
    ```
    <weights>
        <areaWeight>0.5</areaWeight> <!--分区面积相关目标的权重-->
    	<distWeight>1</distWeight> <!--分区距离相关目标的权重-->
    </weights>
    ```
- 3.4 形状控制<Shape>
  形状控制部分包括长宽比、边长、间距、面积等的控制，可以指定zoneID，默认应用于整体。
    ```
    <Shape>
		<LenToWidth>2</LenToWidth> <!--长宽比-->
		<Spacing>12</Spacing> <!--间距-->
		<AreaFloats min="1" max="1.32"/> <!--面积浮动范围-->
		<Density>0.7</Density> <!--密度-->
		<AreaSep>0.7</AreaSep> <!--子分区 分配面积下限 应用于整体-->
		<AreaSep zoneID="19">0.5</AreaSep> <!--子分区 分配面积下限 指定第几个-->
		<SportArea> <!--运动场尺寸，体育场尺寸有规范要求，所以单拎出来-->
			<Width>150</Width>
			<Length>230</Length>
		</SportArea>
		<MinLength>60</MinLength> <!--总体最小边长-->
	</Shape>
    ```
- 3.5 拓扑关系<Topology>
  拓扑关系包括分区之间的拓扑关系<zoneLinks>和分区与场地要素（线、点）之间的关系，这些要素应在场地输入时定义。
  定义规则时，如果没有指定地块，则该分区下的所有地块都需要符合规则。
  应该采用用户界面点选输入这些需求。
  ```
  <Topology>
    <RoadLinks> <!--与道路的关系-->
        <RoadLink type="near" zoneID="{1}" num="1" roadNum="0"/> <!--靠近道路1-->
        <RoadLink type="align" zoneID="{6}" roadNum="2"/> <!--紧邻道路2-->
        <RoadLink type="oneSide" zoneID="{13,14,16,17}" side="1" roadNum="1"/> <!--在道路1的以南或以东-->
    </RoadLinks>
    <PointLinks> <!--与点（入口或者中心点）的关系-->
        <PointLink zoneID="{1}" poiNum="0"/> <!--靠近点0-->
        <PointLink zoneID="{6}" num="{1}" poiNum="2"/> <!--分区6的第一个地块靠近点2-->
    </PointLinks>
    <ZoneLinks>
        <ZoneLink type="near" zoneID="{4,19}" /> <!--分区互相靠近-->
        <ZoneLink type="subZoneAway" zoneID="{8}" /> <!--同一个分区的地块相互远离-->
    </ZoneLinks>
  </Topology>
  ```
- 3.6 定义结构<StructureElements>
 可选的结构有中心区、轴线、组团、网格四种。可以同时叠加。
 目前这些要素需要输入坐标，应该采用用户界面由用户绘制。
 *这里有一个bug，定义要素的坐标和长宽采用的是图形缩小20倍后的值。但是输入的stroke、buffer是实际值*
    1. 中心区Core
      中心区设置为变量，因为需要考虑实际的布局情况
	```
	<Core width="20">
	    <Domain xrange="{24,28}" yrange ="{21,24}" widthDomain ="{6,16}"	lengthDomain="{16,20}" stroke = "12"/> <!--指定中心区的坐标和长宽的范围-->
	    <!--或：<Domain xrange="{24,28}" yrange ="{21,24}"/> 只指定中心点坐标-->
	    <maximizeWeight>-1</maximizeWeight> <!--设定中心区最大化的权重，负数代表最小化-->
	    <insideZone isOnly="true" isAlign="true" zoneID="{4,7,9}"></insideZone> <!--设定中心区内部需要包含的分区，是否仅包含，以及是否需要近邻中心区边界-->
	    <outsideZone isAlign="1" zoneID="{1}"></outsideZone> <!--设定必须在中心区外部的分区，以及是否需要近邻中心区边界-->
	</Core>
	```
    2. 轴线 Axes
      轴线为固定的常量
	```
	<Axes>
		<Axis id="0" startPt="{41.6,26}" endPt ="{41.6,59}" width="16"> <!--轴线id，位置，宽度，均为常量-->
		<asRealAxis  zoneID="{3}"/> <!--作为实轴，呈现为道路的形式-->
		<asAbstractAxis onCenter="true" buffer="200" zoneID="{0,3,5,11}"/> <!--作为虚轴，即分区的中心点在轴线上，或中心区在轴线偏移一定范围内-->
		</Axis>
	</Axes>
	```
    3. 组团Groups
	```
	<Groups>
		<Group id="0" maximizeWeight="0.3" zoneID="{0,5,3,9}"/> <!--组团编号及包含的分区，最大化权重-->
		<Group id="1" maximizeWeight="0.3" zoneID="{10,11,12}"/>
	</Groups>
	```
    5. 网格Grids
       
	```
	<Grids>
	    <xGrid id="0"  stroke="10"> <!--横向的网格线-->
		    <Domain yrange= "{8,13}" xrange="{0,39}"/> <!--网格线y坐标的范围，端点x坐标值-->
		    <ControlZones zoneIDs="{10,11,12,13,14}" side="bottom"/> <!--网格线控制分区的方式-->
	    </xGrid>
	    <yGrid id="0"  stroke="0.5"> <!--纵向的网格线-->
		    <Domain yrange= "{8,13}" xrange="{0,39}"/> <!--网格线x坐标的范围，端点y坐标值-->
	    </yGrid>
	</Grids>
	```
 ## 4. 输出和呈现
 - 在程序内可以简单呈现布局结果，
   - key I ：显示指标信息
   - key B ：显示建筑体量
   - <- -> ：切换上一个/下一个结果
- 输出的location.csv由grasshopper电池读取，可以呈现在Rhino中，利用编写好的电池进一步处理（ghConponent/*.gh）

