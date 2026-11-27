# Collection and Data Controls Reference

This document covers the five collection/data-display controls in WinformUi: `FluentListBox`, `FluentCheckedListBox`, `FluentListView`, `FluentTreeView` (including the node class `FluentTreeNode`) and `FluentDataGridView`. All of them live in the `WinformUi.Controls` namespace and automatically reskin with the theme of `ApplicationThemeManager`. Properties, events and default values all follow the source code; the example snippets can be placed directly in form or page code (you need to import namespaces such as `System`, `System.Drawing`, `System.Windows.Forms`, `WinformUi.Controls`, `WinformUi.Theming`).

## FluentListBox

Fluent-styled list box: 32-pixel row height, subtle hover highlight, an accent-color vertical bar on the left of the selected row, with single/multi selection and mouse-wheel scrolling.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Items | IList | empty List&lt;object&gt; | Collection of data items; you can Add directly or replace the whole collection (replacing clears the selection and resets scrolling). Once DataSource is set this points at the bound list, and assigning to Items then throws InvalidOperationException. |
| DataSource | object | null | Data source: IList, IListSource, IEnumerable, DataTable, DataSet, BindingSource, etc.; both the designer smart tag and the property window (Data category) offer a data-source drop-down. |
| DataMember | string | "" | Table name to bind when the data source is a DataSet; an invalid member binds an empty list instead of throwing. |
| DisplayMember | string | "" | Name of the item property used as display text; when empty, ToString() is used; TextSelector takes precedence over it. |
| ValueMember | string | "" | Name of the item property used for SelectedValue; when empty, SelectedValue is the item itself. |
| SelectedValue | object | null | The ValueMember value of the selected item; assigning locates and selects the item by value. |
| TextSelector | Func&lt;object, string&gt; | null | Custom function extracting the display text of each item; when null, the item's ToString() is called. |
| MultiSelect | bool | false | Whether multi selection is allowed; when enabled, Ctrl + click adds to or removes from the selection. When disabled only the current single-selection row is kept. |
| SelectedIndex | int | -1 | Index of the current single-selection/focus row; assigning clears the multi-selection set, scrolls to that row and raises SelectedIndexChanged. |
| SelectedItem | object | null | The currently selected item; assigning is equivalent to setting SelectedIndex via IndexOf. |
| SelectedIndices | IEnumerable&lt;int&gt; | empty collection | Read-only; the indices of all currently selected rows (in multi selection this includes Ctrl-clicked rows). |
| ItemHeight | int | 32 | Read-only virtual property, the row height; derived classes may override it (FluentListView detail mode uses 30). |

### Events

| Event | Description |
| --- | --- |
| SelectedIndexChanged | Raised when the selection changes: mouse click, keyboard navigation, or code setting SelectedIndex. |
| ItemClicked | Raised when the left mouse button clicks a row (after SelectedIndexChanged). |

### Example

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

// Multi selection: Ctrl + click
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

// Custom objects + text extraction
var dates = new FluentListBox { Width = 240 };
dates.TextSelector = item => ((DateTime)item).ToString("d");
dates.Items.Add(new DateTime(2026, 11, 1));
dates.Items.Add(new DateTime(2026, 12, 25));

// Standard data binding (same semantics as the native ListBox)
var bound = new FluentListBox { Width = 240, DisplayMember = "Name", ValueMember = "Id" };
bound.DataSource = users;                          // List<T>/BindingList<T>/BindingSource/DataTable…
bound.SelectedIndexChanged += (s, e) => Console.WriteLine(bound.SelectedValue);

Controls.Add(list);
Controls.Add(multi);
Controls.Add(dates);
Controls.Add(bound);
```

### Notes

- Default size 220x160; the Up/Down keys move the selection, Home/End jump to the first/last row; setting SelectedIndex scrolls automatically to make the row visible.
- Scrolling works only through the mouse wheel; no scroll bar is drawn. For custom row drawing, override the protected `PaintItem(Graphics, int, RectangleF)`.
- Data binding is resolved through BindingContext/CurrencyManager: after binding, item 0 is selected by default; several list controls sharing the same data source keep their current-position in sync; additions and removals on an IBindingList/BindingSource refresh the list in real time and clamp the selection; `DataSource = null` restores the unbound Items. FluentListView inherits all binding capabilities.
- The designer smart tag matches the native ComboBox: select the control and click the small arrow at its top-right corner; the "Use Data Bound Items" checkbox switches between the two modes. Unchecked gives "unbound mode", offering "Edit Items..." which opens the string collection editor (Items can be edited in the designer and are serialized with the form); checked gives "bound mode", offering a "Choose Data Source" drop-down (listing the BindingSource/DataSet and other data components on this form; supported both by .NET Framework and by the out-of-process designer of .NET 6/8/10) plus direct DataMember/DisplayMember/ValueMember entries; unchecking again clears DataSource and restores the unbound Items. In .NET Framework projects, bound mode additionally offers "Add Project Data Source..." which opens the data source window; in .NET projects, create one via the VS menu "View → Other Windows → Data Sources". FluentListView inherits the same smart tag.

## FluentCheckedListBox

A Fluent list box with checkboxes. It inherits the list display, single/multi selection, scrolling, keyboard handling, data binding and smart tag of `FluentListBox`, and adds a 17px checkbox on the left of each row.

It suits scenarios such as permissions, feature switches and task lists, where "the same list must both select and check". For only a few independent options, several `FluentCheckBox` controls are more intuitive; for hierarchical checking in a tree, the library currently provides no matching component.

### Quick Start

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

`ItemCheck` is raised before the state is committed. The `BeginInvoke` above defers the summary update into the message queue, so it reads the committed state. If your business logic can use `e.NewValue` from the event, you may compute the new state from it, but do not treat the old collection as the final result.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| CheckOnClick | bool | `true` | Whether clicking the checkbox area with the mouse toggles the state; when false, the space bar still works |
| ThreeState | bool | `false` | Whether mouse/space cycle through the three states; code can always set Indeterminate |
| CheckedItems | IList | empty snapshot, read-only property | Snapshot of all items whose state is not Unchecked; Indeterminate counts as checked |
| CheckedIndices | IEnumerable&lt;int&gt; | empty snapshot, read-only property | Snapshot of the indices of all items whose state is not Unchecked |
| Items | IList | empty `List<object>` | List items; assigning a whole new collection clears all check states |
| DataSource / DataMember / DisplayMember / ValueMember / SelectedValue | inherited | same as FluentListBox | Standard WinForms data binding |
| MultiSelect / SelectedIndex / SelectedItem / SelectedIndices | inherited | same as FluentListBox | Row-selection capabilities, independent of the check states |
| ItemHeight | int | `32`, read-only virtual property | Row height; the checkbox and the text are vertically centered within the row |

`CheckedItems` and `CheckedIndices` build a new collection on every read; they are not live views. If you keep a reference to the returned collection, later check changes will not be reflected in that old reference.

### Methods

| Method | Description |
| --- | --- |
| `bool GetItemChecked(int index)` | Returns true for anything not Unchecked; both Checked and Indeterminate return true |
| `void SetItemChecked(int index, bool value)` | true sets Checked, false sets Unchecked |
| `CheckState GetItemCheckState(int index)` | Reads the exact three-state value; a state that was never recorded is treated as Unchecked |
| `void SetItemCheckState(int index, CheckState value)` | Requests a three-state value; ItemCheck is raised only when it differs from the current value |

The index must lie within `[0, Items.Count - 1]`, otherwise `ArgumentOutOfRangeException` is thrown. `SetItemCheckState` accepts only `CheckState.Unchecked`, `Checked` and `Indeterminate`; an event handler that sets `e.NewValue` to an invalid enum value also throws.

### Events

| Event | Description |
| --- | --- |
| ItemCheck | Raised before the commit when the requested state differs from the current state; the outcome can be cancelled or rewritten |
| SelectedIndexChanged | Raised when the selected row changes; a check change by itself does not necessarily change the selected row |
| ItemClicked | Raised when the mouse clicks a row, regardless of whether CheckOnClick is enabled |

#### Cancel or Override a Single Check

```csharp
list.ItemCheck += (s, e) =>
{
    // Forbid checking the 2nd item
    if (e.Index == 1)
        e.NewValue = e.CurrentValue;

    // Turn every "checked" into "indeterminate"
    if (e.Index == 2 && e.NewValue == CheckState.Checked)
        e.NewValue = CheckState.Indeterminate;
};
```

The event order is "current state → requested state → event handling → final commit". Therefore:

- `e.CurrentValue` is the state before the change.
- `e.NewValue` starts out as the requested state and may be rewritten.
- Setting `e.NewValue` to `e.CurrentValue` cancels the change.
- Assigning the same state again does not raise the event.
- Do not recursively call `SetItemCheckState` from within the event, and do not add or remove list items there.

To update your business model after the final state has been committed, either defer the read with `BeginInvoke` or synchronize yourself once the setter returns:

```csharp
list.ItemCheck += (s, e) => list.BeginInvoke(new Action(() =>
{
    bool finalChecked = list.GetItemChecked(e.Index);
    UpdateBusinessState(e.Index, finalChecked);
}));
```

### Mouse and Keyboard Interaction

By default the first item is not selected automatically when the form opens; set `SelectedIndex` first or let the user click a row, otherwise the space bar has no current row to act on.

| Action | Result |
| --- | --- |
| Click the checkbox area on the left of a row, x ∈ [8, 30) | Toggles the check when CheckOnClick = true |
| Click the text area outside the checkbox | Changes only the row selection, does not toggle the check |
| Space | Toggles the check of the currently selected row, not limited by CheckOnClick |
| Up/Down arrows / Home / End | Move the selected row, do not toggle the check |
| Ctrl + click | Adds to or removes from the row selection when MultiSelect = true, stored separately from the checks |

With `ThreeState = true`, clicks/space cycle through: unchecked → checked → indeterminate → unchecked. With `ThreeState = false` only unchecked/checked are toggled, but code can still set the indeterminate state.

### Data Binding Example

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

Item binding works exactly as in `FluentListBox`, but **the check states are not automatically bound to a bool property of the data object**. The internal check dictionary is keyed by index only, so business code must maintain this itself.

### Binding and Dynamic List Limitations

These are the most important limitations in practice:

- Check states are keyed by **index**, not by the data object.
- Inserting, removing or reordering items in the middle of the list does not migrate the check states attached to the existing indices.
- When the list shortens, out-of-range indices are cleaned up; states still within range may end up attached to a different item.
- Replacing `Items` wholesale, or replacing the bound list instance, clears all check states.
- Additions and removals on a `BindingList<T>` / `BindingSource` refresh the list but do not rebuild the correspondence between indices and data objects.

If a dynamic list must preserve its checks, keep the state under stable Ids yourself and reapply it after the list changes:

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

If the list gets reordered, save the Ids before sorting/inserting and restore them by Id once the list is stable, rather than relying long-term on the control's internal indices.

### Designer Usage

- It reuses `FluentListControlDesigner`, so the smart tag offers "Use Data Bound Items" and "Edit Items...". After leaving bound mode, "Edit Items..." opens the string collection editor, which suits plain string lists.
- Items, DataSource, DisplayMember, ValueMember and the like can be serialized by the designer.
- **The check states are currently not persisted with the form designer.** After reopening the form, call SetItemChecked/SetItemCheckState from the form constructor or the Load event.
- ThreeState and CheckOnClick can be set in the Behavior category of the property window.
- The first item is not selected by default; if the space bar should check immediately, set `SelectedIndex` first through code or let the user click a row.

### Common Pitfalls

- `GetItemChecked()` returns true for Indeterminate; use `GetItemCheckState()` when you need the exact three-state value.
- `ItemCheck` is not a "post-commit notification"; reading the control's state inside the handler may still give the old value.
- `CheckOnClick = false` disables only mouse checking, not the space bar.
- Row selection and checking are two separate states; reading CheckedIndices is not the same as reading SelectedIndices.
- The checkbox reserves no extra space for icons or custom content; when painting custom rows via PaintItem, keep the text starting at roughly 34px from the left.

## FluentListView

Fluent list view: derives from `FluentListBox`; in list mode the behavior is identical, and with DetailsView enabled detail rows are rendered as equal-width text columns beneath a header.

### Properties

Only the properties newly declared by this class are listed; the rest are inherited from FluentListBox (Items, SelectedIndex, MultiSelect, etc.).

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Columns | List&lt;string&gt; | empty list | Read-only collection of column headers; populate it directly with Add/AddRange. Each column gets an equal share of the control width. |
| SubItemSelector | Func&lt;object, string[]&gt; | null | Extracts the per-column cell texts from a data item; when null, detail rows fall back to single-text drawing. |
| DetailsView | bool | false | Whether detail (multi-column) mode is enabled; when enabled and column headers exist, a 30-pixel header is shown and the row height becomes 30. |
| ItemHeight | int | 30 in detail mode, otherwise 32 | Read-only, overrides FluentListBox. |

### Events

Inherits SelectedIndexChanged and ItemClicked from FluentListBox; this class adds no events.

### Example

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

// Detail mode can bind a DataTable as well; SubItemSelector reads the columns from the DataRowView
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

### Notes

- Column widths are an equal split of the control width; resizing columns by dragging and sorting by clicking a header are not supported.
- With DetailsView = false, or when no Columns have been added, drawing and interaction are exactly the same as FluentListBox.

## FluentTreeNode

The data node of `FluentTreeView` (a plain class, not a control).

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| Text | string | null | Node display text. |
| Icon | SymbolRegular | SymbolRegular.Empty | Node icon; when Empty no icon is drawn. |
| Tag | object | null | Arbitrary attached data. |
| IsExpanded | bool | false | Whether the child nodes are expanded; after changing it you must call the tree's Rebuild() for it to take effect. |
| Nodes | List&lt;FluentTreeNode&gt; | empty list | Read-only collection of child nodes; populate it directly with Add/AddRange. |

Constructors: `FluentTreeNode()`, `FluentTreeNode(string text)`, `FluentTreeNode(string text, params FluentTreeNode[] children)`.

## FluentTreeView

Fluent tree view: hierarchical indentation plus chevron expand/collapse, with subtle hover highlighting and an accent-color vertical bar on the selected row.

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| RootNodes | List&lt;FluentTreeNode&gt; | empty list | Read-only collection of root nodes; call Rebuild() after populating it to generate the visible rows. |
| SelectedNode | FluentTreeNode | null | The currently selected node; raising SelectedNodeChanged when it changes. |

### Events

| Event | Description |
| --- | --- |
| SelectedNodeChanged | Raised when the selected node changes: clicking a node, keyboard navigation, or code setting SelectedNode. |
| NodeExpandedChanged | Raised when a node's expansion state changes: clicking the chevron or pressing the left/right arrow key. |

Public methods: `void Rebuild()` — re-flattens the visible rows according to RootNodes and each node's IsExpanded, and repaints.

### Example

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

### Notes

- The row height is fixed at 30 pixels and each indentation level is 18 pixels; clicking the chevron area at the start of a row (about 18 pixels wide) toggles expansion, clicking anywhere else selects the node.
- After modifying RootNodes, child Nodes or IsExpanded from code you must call `Rebuild()`; only when there are no visible rows but root nodes exist does the paint stage rebuild automatically once.
- Keyboard: Up/Down move between visible rows, Right expands the selected node, Left collapses it; the mouse wheel scrolls and no scroll bar is drawn.

## FluentDataGridView

A Fluent-themed `DataGridView` derived class: the theme palette uniformly drives the headers, rows, alternating rows, grid lines and selection colors, and the native scroll bars are reskinned as well.

### Properties

This class declares no new public properties; it inherits the full set of standard `DataGridView` properties (Columns, Rows, DataSource, DefaultCellStyle, etc.). The constructor presets the following defaults:

| Inherited property | Value preset in the constructor |
| --- | --- |
| BorderStyle | BorderStyle.None |
| CellBorderStyle | SingleHorizontal (horizontal dividers only) |
| ColumnHeadersBorderStyle | Single |
| RowHeadersVisible | false |
| EnableHeadersVisualStyles | false |
| AllowUserToAddRows | false |
| AllowUserToResizeRows | false |
| SelectionMode | FullRowSelect |
| MultiSelect | false |
| AutoSizeColumnsMode | Fill |
| ColumnHeadersHeight | 36 (with DisableResizing) |
| RowTemplate.Height | 32 |
| DoubleBuffered | true |

### Events

This class declares no new events; it inherits the standard DataGridView events (CellClick, SelectionChanged, CellPainting, etc.).

### Example

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

### Notes

- Reskinning scope: the background/cell/alternating-row/header fill and text colors, the selection color and the grid-line colors are all computed from the current theme palette and reapplied automatically on a theme switch; the cell padding is fixed at 8 pixels on the left and right.
- Cell text is drawn by the control itself with GDI+ (by overriding OnCellPainting) so that transparent composition works over system backdrops such as Mica; when you handle CellPainting yourself, note that this default painting has already been taken over.
- The vertical/horizontal scroll bars are redrawn in the Fluent style via window subclassing; scroll bars that appear only when the content first overflows are hooked automatically as well.
- A native scrollbar child window moves its thumb by blitting inside `SetScrollInfo` and never sends `WM_PAINT` to the subclassed window procedure (`RedrawWindow(RDW_UPDATENOW)` does not deliver one either), so repaints must go through `InvalidateRect` + `UpdateWindow` (the root cause of the scrollbar ghosting fixed in 1.4.1).
