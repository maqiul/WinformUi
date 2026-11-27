using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>A column definition for <see cref="FluentTreeGrid"/>.</summary>
    public class FluentTreeGridColumn
    {
        public FluentTreeGridColumn()
        {
        }

        public FluentTreeGridColumn(string title, int width = 0)
        {
            Title = title ?? string.Empty;
            Width = width;
        }

        [DefaultValue("")]
        public string Title { get; set; } = string.Empty;

        /// <summary>Column width in DIP; 0 shares the remaining width with other fill columns.</summary>
        [DefaultValue(0)]
        public int Width { get; set; }

        [DefaultValue(HorizontalAlignment.Left)]
        public HorizontalAlignment TextAlign { get; set; } = HorizontalAlignment.Left;
    }

    /// <summary>A row node for <see cref="FluentTreeGrid"/>.</summary>
    public class FluentTreeGridNode
    {
        public FluentTreeGridNode()
        {
        }

        public FluentTreeGridNode(string text, params FluentTreeGridNode[] children)
        {
            Text = text ?? string.Empty;
            if (children != null)
            {
                Nodes.AddRange(children);
            }
        }

        [DefaultValue("")]
        public string Text { get; set; } = string.Empty;

        [DefaultValue(SymbolRegular.Empty)]
        public SymbolRegular Icon { get; set; } = SymbolRegular.Empty;

        public object Tag { get; set; }

        /// <summary>Cell values for columns after the first; column 0 falls back to Text.</summary>
        public List<string> Cells { get; } = new List<string>();

        [DefaultValue(false)]
        public bool IsExpanded { get; set; }

        public List<FluentTreeGridNode> Nodes { get; } = new List<FluentTreeGridNode>();

        internal FluentTreeGridNode Parent;
    }

    /// <summary>
    /// Tree-shaped data grid: a header row over multi-column rows that expand and
    /// collapse like a tree view, with whole-row selection, chevrons, wheel scrolling
    /// and tree keyboard navigation. The reference component was a stub, so this is
    /// a complete design following the same header/column concept.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("SelectedNodeChanged")]
    public class FluentTreeGrid : FluentControlBase
    {
        private readonly List<FluentTreeGridNode> _rootNodes = new List<FluentTreeGridNode>();
        private readonly List<FluentTreeGridColumn> _columns = new List<FluentTreeGridColumn>();
        private readonly List<FluentTreeGridNode> _visibleRows = new List<FluentTreeGridNode>();
        private readonly Dictionary<FluentTreeGridNode, int> _depths = new Dictionary<FluentTreeGridNode, int>();
        private FluentTreeGridNode _selected;
        private int _hoveredRow = -1;
        private int _scrollOffset;

        private const int BaseRowHeight = 30;
        private const int BaseHeaderHeight = 36;
        private const int BaseIndent = 18;

        public FluentTreeGrid()
        {
            Size = new Size(480, 240);
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<FluentTreeGridColumn> Columns => _columns;

        [Category("Data")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<FluentTreeGridNode> RootNodes => _rootNodes;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FluentTreeGridNode SelectedNode
        {
            get { return _selected; }
            set
            {
                if (ReferenceEquals(_selected, value))
                {
                    return;
                }

                _selected = value;
                if (_selected != null)
                {
                    EnsureVisible(_selected);
                }

                Invalidate();
                SelectedNodeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler SelectedNodeChanged;
        public event EventHandler NodeExpandedChanged;

        protected virtual void OnNodeExpandedChanged()
        {
            NodeExpandedChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Re-flattens the visible rows after changing nodes or expansion state.</summary>
        public void Rebuild()
        {
            _visibleRows.Clear();
            _depths.Clear();
            foreach (FluentTreeGridNode root in _rootNodes)
            {
                root.Parent = null;
                AppendVisible(root, 0);
            }

            Invalidate();
        }

        private void AppendVisible(FluentTreeGridNode node, int depth)
        {
            _visibleRows.Add(node);
            _depths[node] = depth;
            if (!node.IsExpanded || node.Nodes.Count == 0)
            {
                return;
            }

            foreach (FluentTreeGridNode child in node.Nodes)
            {
                child.Parent = node;
                AppendVisible(child, depth + 1);
            }
        }

        private void EnsureVisible(FluentTreeGridNode node)
        {
            int index = _visibleRows.IndexOf(node);
            if (index < 0)
            {
                return;
            }

            int rowHeight = Scale(BaseRowHeight);
            int top = Scale(BaseHeaderHeight) + index * rowHeight - _scrollOffset;
            if (top < Scale(BaseHeaderHeight))
            {
                _scrollOffset = Scale(BaseHeaderHeight) + index * rowHeight - Scale(BaseHeaderHeight);
            }
            else if (top + rowHeight > Height)
            {
                _scrollOffset = top + rowHeight - Height;
            }

            ClampScroll();
        }

        private void ClampScroll()
        {
            int content = Scale(BaseHeaderHeight) + _visibleRows.Count * Scale(BaseRowHeight);
            _scrollOffset = Math.Max(0, Math.Min(_scrollOffset, Math.Max(0, content - Height)));
        }

        private int[] ColumnEdges()
        {
            int[] widths = new int[_columns.Count];
            int fixedTotal = 0;
            int fillCount = 0;
            for (int i = 0; i < _columns.Count; i++)
            {
                int w = Scale(_columns[i].Width);
                if (w <= 0)
                {
                    fillCount++;
                }
                else
                {
                    widths[i] = w;
                    fixedTotal += w;
                }
            }

            int remaining = Math.Max(0, Width - fixedTotal);
            int fillWidth = fillCount > 0 ? remaining / fillCount : 0;
            var edges = new int[_columns.Count + 1];
            for (int i = 0; i < _columns.Count; i++)
            {
                edges[i + 1] = edges[i] + (widths[i] > 0 ? widths[i] : fillWidth);
            }

            return edges;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            ControlHelper.PaintFullBackground(this, g);
            FluentGraphics.SetupGraphics(g);
            if (_visibleRows.Count == 0 && _rootNodes.Count > 0)
            {
                Rebuild();
            }

            ClampScroll();
            FluentPalette p = Palette;
            int headerHeight = Scale(BaseHeaderHeight);
            int rowHeight = Scale(BaseRowHeight);
            int[] edges = ColumnEdges();

            // Rows first; the opaque header band is painted on top afterwards,
            // so scrolled content slides under it (GDI text ignores GDI+ clipping).
            int first = Math.Max(0, _scrollOffset / Math.Max(1, rowHeight));
            int last = Math.Min(_visibleRows.Count - 1, first + Height / Math.Max(1, rowHeight) + 1);
            for (int r = first; r <= last; r++)
            {
                FluentTreeGridNode node = _visibleRows[r];
                float y = headerHeight + r * rowHeight - _scrollOffset;
                var rowRect = new RectangleF(0, y, Width, rowHeight);

                bool selectedRow = ReferenceEquals(node, _selected);
                if (selectedRow)
                {
                    using (var brush = new SolidBrush(ControlHelper.BlendOverParent(this, p.AccentFillSecondary)))
                    {
                        g.FillRectangle(brush, rowRect);
                    }

                    using (var brush = new SolidBrush(p.SystemAccentColorPrimary))
                    {
                        g.FillRectangle(brush, 0, y + 4, Scale(3), rowHeight - 8);
                    }
                }
                else if (r == _hoveredRow)
                {
                    using (var brush = new SolidBrush(ControlHelper.BlendOverParent(this, p.SubtleFillSecondary)))
                    {
                        g.FillRectangle(brush, rowRect);
                    }
                }

                int depth = _depths[node];
                float chevronX = Scale(6) + depth * Scale(BaseIndent);
                bool hasChildren = node.Nodes.Count > 0;
                if (hasChildren)
                {
                    int chevron = node.IsExpanded ? 0xE70D : 0xE76C;
                    FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(chevron), FluentTypography.Icon(7f),
                        p.TextFillSecondary, new RectangleF(chevronX, y + (rowHeight - Scale(14)) / 2f, Scale(14), Scale(14)));
                }

                float textX = chevronX + (hasChildren ? Scale(16) : Scale(2));
                if (node.Icon != SymbolRegular.Empty)
                {
                    FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph((int)node.Icon), FluentTypography.IconRegular(9f),
                        p.TextFillSecondary, new RectangleF(textX, y + (rowHeight - Scale(16)) / 2f, Scale(16), Scale(16)));
                    textX += Scale(18);
                }

                using (var format = new StringFormat(StringFormatFlags.NoWrap))
                {
                    format.LineAlignment = StringAlignment.Center;
                    format.Trimming = StringTrimming.EllipsisCharacter;
                    if (_columns.Count == 0)
                    {
                        var full = new RectangleF(textX, y, Width - textX - Scale(8), rowHeight);
                        FluentGraphics.DrawText(g, node.Text, Font,
                            Enabled ? p.TextFillPrimary : p.TextFillDisabled, full, format);
                    }

                    for (int c = 0; c < _columns.Count; c++)
                    {
                        format.Alignment = AlignOf(_columns[c].TextAlign);
                        string value = c == 0 ? node.Text :
                            (node.Cells.Count > c - 1 ? node.Cells[c - 1] : string.Empty);
                        float left = c == 0 ? textX : edges[c] + Scale(8);
                        float right = edges[Math.Min(1, edges.Length - 1)];
                        if (c > 0)
                        {
                            right = edges[c + 1];
                        }

                        var rect = new RectangleF(left, y, Math.Max(0, right - left - Scale(8)), rowHeight);
                        FluentGraphics.DrawText(g, value, Font,
                            Enabled ? p.TextFillPrimary : p.TextFillDisabled, rect, format);
                    }
                }

                using (var pen = new Pen(p.DividerStrokeDefault, 1f))
                {
                    float lineY = y + rowHeight - 1;
                    if (r < _visibleRows.Count - 1)
                    {
                        g.DrawLine(pen, 0, lineY, Width, lineY);
                    }
                }
            }

            g.ResetClip();

            // Header band, painted last so it covers scrolled rows.
            using (var brush = new SolidBrush(ControlHelper.BlendOverParent(this, p.ControlFillSecondary)))
            {
                g.FillRectangle(brush, 0, 0, Width, headerHeight);
            }

            using (var format = new StringFormat(StringFormatFlags.NoWrap))
            {
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;
                for (int i = 0; i < _columns.Count; i++)
                {
                    format.Alignment = AlignOf(_columns[i].TextAlign);
                    var rect = new RectangleF(edges[i] + Scale(8), 0, edges[i + 1] - edges[i] - Scale(16), headerHeight);
                    FluentGraphics.DrawText(g, _columns[i].Title, FluentTypography.Caption, p.TextFillSecondary, rect, format);
                }
            }

            using (var pen = new Pen(p.DividerStrokeDefault, 1f))
            {
                g.DrawLine(pen, 0, headerHeight - 1, Width, headerHeight - 1);
            }

            // Vertical column separators below the header.
            using (var pen = new Pen(p.DividerStrokeDefault, 1f))
            {
                for (int i = 1; i < _columns.Count; i++)
                {
                    g.DrawLine(pen, edges[i], headerHeight, edges[i], Height);
                }
            }
        }

        private static StringAlignment AlignOf(HorizontalAlignment align)
        {
            switch (align)
            {
                case HorizontalAlignment.Center: return StringAlignment.Center;
                case HorizontalAlignment.Right: return StringAlignment.Far;
                default: return StringAlignment.Near;
            }
        }

        private int RowAt(Point pt)
        {
            int headerHeight = Scale(BaseHeaderHeight);
            int rowHeight = Scale(BaseRowHeight);
            if (pt.Y < headerHeight)
            {
                return -1;
            }

            int index = (pt.Y + _scrollOffset - headerHeight) / Math.Max(1, rowHeight);
            return index >= 0 && index < _visibleRows.Count ? index : -1;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int row = RowAt(e.Location);
            if (row < 0)
            {
                return;
            }

            FluentTreeGridNode node = _visibleRows[row];
            int depth = _depths[node];
            int chevronX = Scale(6) + depth * Scale(BaseIndent);
            var chevronRect = new RectangleF(chevronX, 0, Scale(18), Height);
            if (node.Nodes.Count > 0 && chevronRect.Contains(e.Location))
            {
                node.IsExpanded = !node.IsExpanded;
                Rebuild();
                OnNodeExpandedChanged();
                return;
            }

            SelectedNode = node;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int row = RowAt(e.Location);
            if (row != _hoveredRow)
            {
                _hoveredRow = row;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredRow = -1;
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int rowHeight = Scale(BaseRowHeight);
            _scrollOffset += e.Delta > 0 ? -rowHeight : rowHeight;
            ClampScroll();
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                    return true;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            int index = _selected == null ? -1 : _visibleRows.IndexOf(_selected);
            switch (e.KeyCode)
            {
                case Keys.Down:
                    index = Math.Min(_visibleRows.Count - 1, index + 1);
                    break;
                case Keys.Up:
                    index = Math.Max(0, index - 1);
                    break;
                case Keys.Home:
                    index = 0;
                    break;
                case Keys.End:
                    index = Math.Max(0, _visibleRows.Count - 1);
                    break;
                case Keys.Right:
                    if (_selected != null && _selected.Nodes.Count > 0 && !_selected.IsExpanded)
                    {
                        _selected.IsExpanded = true;
                        Rebuild();
                        OnNodeExpandedChanged();
                    }

                    return;
                case Keys.Left:
                    if (_selected != null && _selected.IsExpanded)
                    {
                        _selected.IsExpanded = false;
                        Rebuild();
                        OnNodeExpandedChanged();
                    }

                    return;
                default:
                    return;
            }

            if (index >= 0 && index < _visibleRows.Count)
            {
                SelectedNode = _visibleRows[index];
                e.Handled = true;
            }
        }
    }
}
