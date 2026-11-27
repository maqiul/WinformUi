# 集合与数据控件参考

本文档覆盖 WinformUi 中集合/数据展示相关的五个控件:`FluentListBox`、`FluentCheckedListBox`、`FluentListView`、`FluentTreeView`(含节点类 `FluentTreeNode`)、`FluentDataGridView`。全部位于命名空间 `WinformUi.Controls`,随 `ApplicationThemeManager` 的主题自动换肤。属性、事件与默认值均以源码为准,示例片段可直接放入窗体或页面代码中(需自行引入 `System`、`System.Drawing`、`System.Windows.Forms`、`WinformUi.Controls`、`WinformUi.Theming` 等命名空间)。

## FluentListBox

Fluent 风格列表框:32 像素行高、微妙悬停高亮、选中行左侧强调色竖条,支持单选/多选与滚轮滚动。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Items | IList | 空的 List&lt;object&gt; | 数据项集合;可直接 Add,也可整体替换(替换会清空选择并复位滚动)。设置 DataSource 后指向绑定列表,此时赋值 Items 抛 InvalidOperationException。 |
| DataSource | object | null | 数据源:IList、IListSource、IEnumerable、DataTable、DataSet、BindingSource 等;设计器智能标记与属性窗格(Data 分类)均提供数据源下拉。 |
| DataMember | string | "" | 绑定 DataSet 时指定表名;无效成员绑定空列表,不抛异常。 |
| DisplayMember | string | "" | 用作显示文本的项属性名;为空时 ToString();TextSelector 优先于它。 |
| ValueMember | string | "" | 用作 SelectedValue 的项属性名;为空时 SelectedValue 即项本身。 |
| SelectedValue | object | null | 选中项的 ValueMember 值;赋值按值定位选中项。 |
| TextSelector | Func&lt;object, string&gt; | null | 自定义每项显示文本的提取函数;为 null 时调用项的 ToString()。 |
| MultiSelect | bool | false | 是否允许多选;开启后按住 Ctrl 点击可增删选择。关闭时只保留当前单选行。 |
| SelectedIndex | int | -1 | 当前单选/焦点行索引;赋值会清空多选集合并滚动到该行,触发 SelectedIndexChanged。 |
| SelectedItem | object | null | 当前选中项;赋值等价于按 IndexOf 设置 SelectedIndex。 |
| SelectedIndices | IEnumerable&lt;int&gt; | 空集合 | 只读,当前所有被选中的行索引(多选时包含 Ctrl 点选的行)。 |
| ItemHeight | int | 32 | 只读虚属性,行高;派生类可重写(如 FluentListView 详情模式为 30)。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| SelectedIndexChanged | 选择变化时触发:鼠标点选、键盘导航或代码设置 SelectedIndex。 |
| ItemClicked | 鼠标左键点击某一行时触发(在 SelectedIndexChanged 之后)。 |

### 示例

```csharp
var list = new FluentListBox { Size = new Size(240, 190) };
foreach (string s in new[] { "Azure", "Bing", "Copilot", "Edge", "GitHub", "Windows" })
{
    list.Items.Add(s);
}
list.SelectedIndex = 0;
var state = new FluentLabel { Text = "Azure", AutoSize = true };
list.SelectedIndexChanged += (s, e) =>
    state.Text = list.SelectedItem != null ? list.SelectedItem.ToString() : "(none)";

// 多选:Ctrl + 点击
var multi = new FluentListBox { Size = new Size(240, 190), MultiSelect = true };
foreach (string s in new[] { "Red", "Green", "Blue", "Cyan" })
{
    multi.Items.Add(s);
}
multi.SelectedIndexChanged += (s, e) =>
{
    foreach (int i in multi.SelectedIndices)
        Console.WriteLine(multi.Items[i]);
};

// 自定义对象 + 文本提取
var dates = new FluentListBox { Width = 240 };
dates.TextSelector = item => ((DateTime)item).ToString("d");
dates.Items.Add(new DateTime(2026, 11, 1));
dates.Items.Add(new DateTime(2026, 12, 25));

// 标准数据绑定(与原生 ListBox 同语义)
var bound = new FluentListBox { Width = 240, DisplayMember = "Name", ValueMember = "Id" };
bound.DataSource = users;                          // List<T>/BindingList<T>/BindingSource/DataTable…
bound.SelectedIndexChanged += (s, e) => Console.WriteLine(bound.SelectedValue);

Controls.Add(list);
Controls.Add(multi);
Controls.Add(dates);
Controls.Add(bound);
```

### 备注

- 默认尺寸 220×160;键盘 Up/Down 移动选择,Home/End 跳到首/末行;设置 SelectedIndex 会自动滚动确保可见。
- 滚动仅靠鼠标滚轮,不绘制滚动条;自定义行绘制可重写受保护的 `PaintItem(Graphics, int, RectangleF)`。
- 数据绑定经 BindingContext/CurrencyManager 解析:绑定后默认选中第 0 项;同一数据源的多个列表控件选中位置互相同步;IBindingList/BindingSource 的增删实时刷新并钳制选择;DataSource = null 恢复未绑定 Items。FluentListView 继承全部绑定能力。
- 设计器智能标记与原生 ComboBox 同款:选中控件点右上角小箭头,"使用数据绑定项"勾选切换两种模式。未勾选为"未绑定模式",提供"编辑项..."打开字符串集合编辑器(Items 可在设计器编辑并随窗体序列化);勾选为"绑定模式",提供"选择数据源"下拉(列出本窗体上的 BindingSource/DataSet 等数据组件,.NET Framework 与 .NET 6/8/10 进程外设计器均支持)及 DataMember/DisplayMember/ValueMember 直填项;取消勾选清掉 DataSource 并恢复未绑定 Items。.NET Framework 项目绑定模式另含"添加项目数据源..."打开数据源窗口,.NET 项目请用 VS 菜单"视图→其他窗口→数据源"新建。FluentListView 继承同款智能标记。

## FluentCheckedListBox

带复选框的 Fluent 列表框。继承 `FluentListBox` 的列表显示、单选/多选、滚动、键盘、数据绑定和智能标记,每行左侧增加一个 17px 复选框。

适合权限、功能开关、任务清单等“同一列表既要选择又要勾选”的场景。如果只有几个独立选项,用多个 `FluentCheckBox` 更直观;如果需要树形层级勾选,当前库没有提供对应组件。

### 快速开始

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;

public sealed class CheckedListDemoForm : Form
{
    public CheckedListDemoForm()
    {
        Text = "CheckedListBox demo";
        ClientSize = new Size(420, 300);

        var list = new FluentCheckedListBox
        {
            Location = new Point(20, 20),
            Size = new Size(240, 200),
            ThreeState = true
        };
        foreach (string text in new[] { "设计器", "工具箱", "数据绑定", "深色模式" })
            list.Items.Add(text);
        list.SetItemChecked(0, true);
        list.SetItemCheckState(1, CheckState.Indeterminate);

        var summary = new FluentLabel
        {
            Location = new Point(280, 20),
            AutoSize = true,
            Text = "已选: 0"
        };
        list.ItemCheck += (s, e) =>
        {
            Console.WriteLine(e.Index + ": " + e.CurrentValue + " -> " + e.NewValue);
            list.BeginInvoke(new Action(() =>
            {
                summary.Text = "已选索引: " + string.Join(", ", list.CheckedIndices);
            }));
        };
        summary.Text = "已选索引: " + string.Join(", ", list.CheckedIndices);

        Controls.AddRange(new Control[] { list, summary });
    }
}
```

`ItemCheck` 在状态提交前触发。上面的 `BeginInvoke` 把摘要更新排到消息队列中,因此读到的是提交后的状态。如果业务允许使用事件中的 `e.NewValue`,也可以按它先计算新状态,但不要把旧集合当成最终结果。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| CheckOnClick | bool | `true` | 鼠标点击复选框区域时是否切换状态;false 时仍可用空格切换 |
| ThreeState | bool | `false` | 鼠标/空格是否按三态循环;代码始终可设置 Indeterminate |
| CheckedItems | IList | 空快照,只读属性 | 所有非 Unchecked 项的当前快照;Indeterminate 也计入 |
| CheckedIndices | IEnumerable&lt;int&gt; | 空快照,只读属性 | 所有非 Unchecked 项的当前索引快照 |
| Items | IList | 空的 `List<object>` | 列表项;整体赋值会清空全部勾选状态 |
| DataSource / DataMember / DisplayMember / ValueMember / SelectedValue | 继承 | 同 FluentListBox | 标准 WinForms 数据绑定 |
| MultiSelect / SelectedIndex / SelectedItem / SelectedIndices | 继承 | 同 FluentListBox | 行选择能力,与勾选状态相互独立 |
| ItemHeight | int | `32`,只读虚属性 | 行高;复选框和文字在该行内垂直居中 |

`CheckedItems` 与 `CheckedIndices` 每次读取都会生成新集合,不是实时视图。保存集合引用后,后续勾选变化不会自动反映到旧引用中。

### 方法

| 方法 | 说明 |
| --- | --- |
| `bool GetItemChecked(int index)` | 非 Unchecked 返回 true;Checked 与 Indeterminate 都返回 true |
| `void SetItemChecked(int index, bool value)` | true 设为 Checked,false 设为 Unchecked |
| `CheckState GetItemCheckState(int index)` | 读取精确三态;未记录的状态视为 Unchecked |
| `void SetItemCheckState(int index, CheckState value)` | 请求设置三态;与当前值不同才触发 ItemCheck |

索引必须在 `[0, Items.Count - 1]` 内,否则抛出 `ArgumentOutOfRangeException`。`SetItemCheckState` 只接受 `CheckState.Unchecked`、`Checked`、`Indeterminate`;事件处理器把 `e.NewValue` 改成非法枚举值也会抛异常。

### 事件

| 事件 | 说明 |
| --- | --- |
| ItemCheck | 请求的状态与当前状态不同时,在提交前触发;可取消或改写结果 |
| SelectedIndexChanged | 选中行变化时触发;勾选变化本身不一定改变选中行 |
| ItemClicked | 鼠标点击某一行时触发,与 CheckOnClick 是否开启无关 |

#### 取消或覆盖一次勾选

```csharp
list.ItemCheck += (s, e) =>
{
    // 禁止勾选第 2 项
    if (e.Index == 1)
        e.NewValue = e.CurrentValue;

    // 把所有“勾选”改成“不确定”
    if (e.Index == 2 && e.NewValue == CheckState.Checked)
        e.NewValue = CheckState.Indeterminate;
};
```

事件顺序为“当前状态 → 请求状态 → 事件处理 → 最终提交”。因此:

- `e.CurrentValue` 是修改前状态。
- `e.NewValue` 初始是请求状态,可改写。
- 把 `e.NewValue` 设为 `e.CurrentValue` 可取消。
- 相同状态重复赋值不会触发事件。
- 不建议在事件中递归调用 `SetItemCheckState` 或同时增删列表项。

如果需要在最终状态提交后更新业务模型,可以用 `BeginInvoke` 延迟读取,或由设置方法返回后自行同步:

```csharp
list.ItemCheck += (s, e) => list.BeginInvoke(new Action(() =>
{
    bool finalChecked = list.GetItemChecked(e.Index);
    UpdateBusinessState(e.Index, finalChecked);
}));
```

### 鼠标与键盘操作

默认不会在打开窗体时自动选中第一项,请先设置 `SelectedIndex` 或由用户点击一行,空格才会作用于当前选中项。

| 操作 | 结果 |
| --- | --- |
| 点击行左侧 x ∈ [8, 30) 的复选框区域 | CheckOnClick = true 时切换勾选 |
| 点击复选框以外的文本区域 | 只改变行选择,不切换勾选 |
| 空格 | 切换当前选中行的勾选状态,不受 CheckOnClick 限制 |
| 上下方向键 / Home / End | 移动选中行,不切换勾选 |
| Ctrl + 点击 | MultiSelect = true 时增删行选择,与勾选分别保存 |

`ThreeState = true` 时点击/空格循环为:未选 → 选中 → 不确定 → 未选。`ThreeState = false` 时只在未选/选中间切换,但代码仍可设置不确定态。

### 数据绑定示例

```csharp
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Controls;

public sealed class Option
{
    public int Id { get; set; }
    public string Name { get; set; }
}

var options = new BindingList<Option>
{
    new Option { Id = 1, Name = "接收通知" },
    new Option { Id = 2, Name = "启用同步" }
};

var bound = new FluentCheckedListBox
{
    Size = new Size(280, 160),
    DataSource = options,
    DisplayMember = "Name",
    ValueMember = "Id"
};
bound.SetItemChecked(0, true);
```

列表项绑定与 `FluentListBox` 相同,但**勾选状态不会自动绑定到数据对象的 bool 属性**。当前勾选字典只保存索引,需要业务代码自行维护。

### 绑定与动态列表限制

这是使用时最重要的限制:

- 勾选状态以**索引**为键,不以数据对象为键。
- 在列表中间插入、删除或重新排序时,已有索引对应的勾选状态不会自动迁移。
- 列表缩短时,越界索引会被清理;仍在范围内的状态可能跟到另一项。
- 整体替换 `Items` 或替换绑定列表实例会清空全部勾选状态。
- `BindingList<T>` / `BindingSource` 的增删会刷新列表,但不会重建索引与数据对象的对应关系。

动态列表如需保持勾选,建议用稳定 Id 自己保存状态,列表变化后重新应用:

```csharp
var checkedIds = new HashSet<int>();
for (int i = 0; i < list.Items.Count; i++)
{
    var option = (Option)list.Items[i];
    if (checkedIds.Contains(option.Id)) list.SetItemChecked(i, true);
}

list.ItemCheck += (s, e) =>
{
    var option = (Option)list.Items[e.Index];
    if (e.NewValue != CheckState.Unchecked) checkedIds.Add(option.Id);
    else checkedIds.Remove(option.Id);
};
```

如果列表会重排,应在排序/插入前保存 Id,在列表稳定后按 Id 恢复,而不是长期依赖控件内部索引。

### 设计器使用

- 沿用 `FluentListControlDesigner`,智能标记提供“使用数据绑定项”和“编辑项...”。取消绑定模式后,“编辑项...”打开字符串集合编辑器,适合普通字符串列表。
- Items、DataSource、DisplayMember、ValueMember 等可设计器序列化。
- **勾选状态当前不随窗体设计器持久化**。重新打开窗体后,需在窗体构造或 Load 事件中调用 SetItemChecked/SetItemCheckState。
- ThreeState、CheckOnClick 可在属性窗口 Behavior 分类设置。
- 默认不选中第一项;需要空格键立即勾选时,先通过代码或用户点击设置 `SelectedIndex`。

### 常见坑

- `GetItemChecked()` 对 Indeterminate 返回 true;需要精确三态时用 `GetItemCheckState()`。
- `ItemCheck` 不是“提交后通知”,在事件内读取控件状态可能还是旧值。
- `CheckOnClick = false` 只关闭鼠标勾选,不会关闭空格键。
- 行选择与勾选是两套状态;读取 CheckedIndices 不等于 SelectedIndices。
- 复选框不会为图标或自定义内容预留额外空间;自定义 PaintItem 时应保留左侧约 34px 文字起点。

## FluentListView

Fluent 列表视图:继承 `FluentListBox`,列表模式行为完全一致;开启 DetailsView 后在表头下按等宽文本列渲染详情行。

### 属性

仅列出本类新声明的属性,其余继承自 FluentListBox(Items、SelectedIndex、MultiSelect 等)。

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Columns | List&lt;string&gt; | 空列表 | 只读集合,列标题;直接 Add/AddRange 填充。各列宽度均分控件宽度。 |
| SubItemSelector | Func&lt;object, string[]&gt; | null | 从数据项提取各列单元格文本;为 null 时详情行退回单文本绘制。 |
| DetailsView | bool | false | 是否启用详情(多列)模式;开启且有列标题时显示 30 像素表头,行高变为 30。 |
| ItemHeight | int | 详情模式 30,否则 32 | 只读,重写自 FluentListBox。 |

### 事件

继承 FluentListBox 的 SelectedIndexChanged 与 ItemClicked,本类未新增事件。

### 示例

```csharp
var listView = new FluentListView { Size = new Size(560, 200), DetailsView = true };
listView.Columns.AddRange(new[] { "Name", "Publisher", "Year" });
listView.SubItemSelector = item => (string[])item;
listView.Items.Add(new[] { "Windows Terminal", "Microsoft", "2019" });
listView.Items.Add(new[] { "PowerToys", "Microsoft", "2019" });
listView.Items.Add(new[] { "7-Zip", "Igor Pavlov", "1999" });
listView.SelectedIndex = 0;
listView.SelectedIndexChanged += (s, e) =>
{
    var row = (string[])listView.SelectedItem;
    if (row != null) Console.WriteLine(row[0]);
};

// 详情模式同样可以绑定 DataTable,SubItemSelector 从 DataRowView 取列
var boundView = new FluentListView { Size = new Size(560, 200), DetailsView = true };
boundView.Columns.AddRange(new[] { "Id", "Name" });
boundView.SubItemSelector = item =>
{
    var row = (DataRowView)item;
    return new[] { row["Id"].ToString(), row["Name"].ToString() };
};
boundView.DataSource = usersTable;

Controls.Add(listView);
Controls.Add(boundView);
```

### 备注

- 列宽为控件宽度平均分配,不支持拖动调整列宽或点击表头排序。
- DetailsView = false 或未添加 Columns 时,绘制与交互和 FluentListBox 完全相同。

## FluentTreeNode

`FluentTreeView` 的数据节点(普通类,非控件)。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Text | string | null | 节点显示文本。 |
| Icon | SymbolRegular | SymbolRegular.Empty | 节点图标;为 Empty 时不绘制图标。 |
| Tag | object | null | 任意附加数据。 |
| IsExpanded | bool | false | 是否展开子节点;修改后需调用树的 Rebuild() 生效。 |
| Nodes | List&lt;FluentTreeNode&gt; | 空列表 | 只读集合,子节点;直接 Add/AddRange 填充。 |

构造函数:`FluentTreeNode()`、`FluentTreeNode(string text)`、`FluentTreeNode(string text, params FluentTreeNode[] children)`。

## FluentTreeView

Fluent 树视图:层级缩进 + 箭头(chevron)展开/折叠,微妙悬停与选中行强调色竖条。

### 属性

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| RootNodes | List&lt;FluentTreeNode&gt; | 空列表 | 只读集合,根节点;填充后调用 Rebuild() 生成可见行。 |
| SelectedNode | FluentTreeNode | null | 当前选中节点;变化时触发 SelectedNodeChanged。 |

### 事件

| 事件 | 说明 |
| --- | --- |
| SelectedNodeChanged | 选中节点变化时触发:点击节点、键盘导航或代码设置 SelectedNode。 |
| NodeExpandedChanged | 节点展开状态变化时触发:点击箭头或按左/右方向键。 |

公开方法:`void Rebuild()` —— 按 RootNodes 与各节点 IsExpanded 重新展平可见行并重绘。

### 示例

```csharp
var tree = new FluentTreeView { Size = new Size(300, 220) };
var work = new FluentTreeNode("Work documents") { Icon = SymbolRegular.Folder24, IsExpanded = true };
work.Nodes.Add(new FluentTreeNode("Quarterly report.docx") { Icon = SymbolRegular.Document24 });
work.Nodes.Add(new FluentTreeNode("Budget.xlsx") { Icon = SymbolRegular.Document24 });
var pictures = new FluentTreeNode("Pictures",
    new FluentTreeNode("Vacation 2026",
        new FluentTreeNode("beach.png") { Icon = SymbolRegular.Image24 }),
    new FluentTreeNode("profile.jpg") { Icon = SymbolRegular.Image24 })
{ Icon = SymbolRegular.Image24, IsExpanded = true };
tree.RootNodes.Add(work);
tree.RootNodes.Add(pictures);
tree.Rebuild();

var state = new FluentLabel { Text = "Select a node", AutoSize = true };
tree.SelectedNodeChanged += (s, e) =>
    state.Text = tree.SelectedNode != null ? tree.SelectedNode.Text : "(none)";
tree.NodeExpandedChanged += (s, e) => Console.WriteLine("expand/collapse");

Controls.Add(tree);
Controls.Add(state);
```

### 备注

- 行高固定 30 像素,每层缩进 18 像素;点击行首箭头区域(约 18 像素宽)切换展开,点击其余区域选中节点。
- 通过代码修改 RootNodes、子 Nodes 或 IsExpanded 后必须调用 `Rebuild()`;仅当可见行为空且存在根节点时,绘制阶段会自动重建一次。
- 键盘:Up/Down 在可见行间移动,Right 展开选中节点,Left 折叠;滚轮滚动,不绘制滚动条。

## FluentDataGridView

Fluent 主题的 `DataGridView` 派生类:用主题调色板统一驱动表头、行、交替行、网格线、选中色,并对原生滚动条换肤。

### 属性

本类未声明新的公开属性,完整继承标准 `DataGridView` 的全部属性(Columns、Rows、DataSource、DefaultCellStyle 等)。构造函数预设了以下默认值:

| 继承属性 | 构造时预设值 |
| --- | --- |
| BorderStyle | BorderStyle.None |
| CellBorderStyle | SingleHorizontal(仅水平分隔线) |
| ColumnHeadersBorderStyle | Single |
| RowHeadersVisible | false |
| EnableHeadersVisualStyles | false |
| AllowUserToAddRows | false |
| AllowUserToResizeRows | false |
| SelectionMode | FullRowSelect |
| MultiSelect | false |
| AutoSizeColumnsMode | Fill |
| ColumnHeadersHeight | 36(且 DisableResizing) |
| RowTemplate.Height | 32 |
| DoubleBuffered | true |

### 事件

本类未声明新事件,继承标准 DataGridView 事件(CellClick、SelectionChanged、CellPainting 等)。

### 示例

```csharp
var grid = new FluentDataGridView { Size = new Size(640, 220) };
grid.Columns.Add("Product", "Product");
grid.Columns.Add("Category", "Category");
grid.Columns.Add("Price", "Price");
grid.Rows.Add("Surface Pro", "Hardware", "$999");
grid.Rows.Add("Surface Laptop", "Hardware", "$1299");
grid.Rows.Add("Arc Mouse", "Accessory", "$79");
grid.SelectionChanged += (s, e) =>
{
    if (grid.CurrentRow != null)
        Console.WriteLine(grid.CurrentRow.Cells[0].Value);
};

Controls.Add(grid);
```

### 备注

- 换肤范围:背景/单元格/交替行/表头的底色与文本色、选中色、网格线色均由当前主题调色板计算,主题切换时自动重新应用;单元格内边距固定为左右 8 像素。
- 单元格文本由控件用 GDI+ 自绘(重写 OnCellPainting),以兼容 Mica 等系统背景下的透明合成;自定义 CellPainting 事件处理时注意此默认绘制已被接管。
- 垂直/水平滚动条通过窗口子类化用 Fluent 样式重绘,内容首次溢出时才出现的滚动条也会被自动挂钩。
- 原生滚动条子窗口在 `SetScrollInfo` 内部直接搬移通道,不给子类化窗口过程发 `WM_PAINT`(`RedrawWindow(RDW_UPDATENOW)` 对这种子窗口同样不投递),因此重绘必须走 `InvalidateRect` + `UpdateWindow`(1.4.1 修复滚动条残影的根因)。