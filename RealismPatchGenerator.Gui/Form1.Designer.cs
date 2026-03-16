namespace RealismPatchGenerator.Gui;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;
    private Label titleLabel;
    private Label pathLabel;
    private Label outputPathLabel;
    private Label outputHintLabel;
    private TextBox basePathTextBox;
    private TextBox outputPathTextBox;
    private Button browseButton;
    private Button saveAllButton;
    private Button reloadButton;
    private Button generateButton;
    private Button auditButton;
    private Label languageLabel;
    private ComboBox languageComboBox;
    private Label searchLabel;
    private TextBox searchTextBox;
    private SplitContainer mainSplitContainer;
    private GroupBox navigationGroupBox;
    private TreeView ruleTreeView;
    private SplitContainer editorSplitContainer;
    private GroupBox gridGroupBox;
    private DataGridView ruleGridView;
    private DataGridViewTextBoxColumn fieldColumn;
    private DataGridViewTextBoxColumn minColumn;
    private DataGridViewTextBoxColumn maxColumn;
    private DataGridViewCheckBoxColumn preferIntColumn;
    private DataGridViewTextBoxColumn sourceColumn;
    private TabControl detailTabControl;
    private TabPage explanationTabPage;
    private TabPage logTabPage;
    private TextBox explanationTextBox;
    private TextBox logTextBox;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel totalItemsStatusLabel;
    private ToolStripStatusLabel visibleItemsStatusLabel;
    private ToolStripStatusLabel dirtyItemsStatusLabel;
    private ToolStripStatusLabel stateStatusLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        var headerPanel = new Panel();
        var toolbarPanel = new Panel();
        titleLabel = new Label();
        pathLabel = new Label();
        outputPathLabel = new Label();
        outputHintLabel = new Label();
        basePathTextBox = new TextBox();
        outputPathTextBox = new TextBox();
        browseButton = new Button();
        saveAllButton = new Button();
        reloadButton = new Button();
        generateButton = new Button();
        auditButton = new Button();
        languageLabel = new Label();
        languageComboBox = new ComboBox();
        searchLabel = new Label();
        searchTextBox = new TextBox();
        mainSplitContainer = new SplitContainer();
        navigationGroupBox = new GroupBox();
        ruleTreeView = new TreeView();
        editorSplitContainer = new SplitContainer();
        gridGroupBox = new GroupBox();
        ruleGridView = new DataGridView();
        fieldColumn = new DataGridViewTextBoxColumn();
        minColumn = new DataGridViewTextBoxColumn();
        maxColumn = new DataGridViewTextBoxColumn();
        preferIntColumn = new DataGridViewCheckBoxColumn();
        sourceColumn = new DataGridViewTextBoxColumn();
        detailTabControl = new TabControl();
        explanationTabPage = new TabPage();
        explanationTextBox = new TextBox();
        logTabPage = new TabPage();
        logTextBox = new TextBox();
        statusStrip = new StatusStrip();
        totalItemsStatusLabel = new ToolStripStatusLabel();
        visibleItemsStatusLabel = new ToolStripStatusLabel();
        dirtyItemsStatusLabel = new ToolStripStatusLabel();
        stateStatusLabel = new ToolStripStatusLabel();
        headerPanel.SuspendLayout();
        toolbarPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
        mainSplitContainer.Panel1.SuspendLayout();
        mainSplitContainer.Panel2.SuspendLayout();
        mainSplitContainer.SuspendLayout();
        navigationGroupBox.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)editorSplitContainer).BeginInit();
        editorSplitContainer.Panel1.SuspendLayout();
        editorSplitContainer.Panel2.SuspendLayout();
        editorSplitContainer.SuspendLayout();
        gridGroupBox.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)ruleGridView).BeginInit();
        detailTabControl.SuspendLayout();
        explanationTabPage.SuspendLayout();
        logTabPage.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1380, 860);
        Controls.Add(mainSplitContainer);
        Controls.Add(toolbarPanel);
        Controls.Add(headerPanel);
        Controls.Add(statusStrip);
        MinimumSize = new Size(1200, 760);
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "SPT现实主义数值范围编辑生成器 v0.9";

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Height = 54;
        headerPanel.Padding = new Padding(16, 12, 16, 0);

        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Microsoft YaHei UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 134);
        titleLabel.Location = new Point(16, 12);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(520, 28);
        titleLabel.Text = "SPT现实主义数值范围编辑生成器 v0.9";

        toolbarPanel.Controls.Add(searchTextBox);
        toolbarPanel.Controls.Add(searchLabel);
        toolbarPanel.Controls.Add(languageComboBox);
        toolbarPanel.Controls.Add(languageLabel);
        toolbarPanel.Controls.Add(auditButton);
        toolbarPanel.Controls.Add(generateButton);
        toolbarPanel.Controls.Add(reloadButton);
        toolbarPanel.Controls.Add(saveAllButton);
        toolbarPanel.Controls.Add(browseButton);
        toolbarPanel.Controls.Add(outputHintLabel);
        toolbarPanel.Controls.Add(outputPathTextBox);
        toolbarPanel.Controls.Add(outputPathLabel);
        toolbarPanel.Controls.Add(basePathTextBox);
        toolbarPanel.Controls.Add(pathLabel);
        toolbarPanel.Dock = DockStyle.Top;
        toolbarPanel.Height = 132;
        toolbarPanel.Padding = new Padding(16, 10, 16, 10);

        pathLabel.AutoSize = true;
        pathLabel.Location = new Point(18, 15);
        pathLabel.Name = "pathLabel";
        pathLabel.Size = new Size(79, 17);
        pathLabel.Text = "数据目录:";

        basePathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        basePathTextBox.Location = new Point(101, 12);
        basePathTextBox.Name = "basePathTextBox";
        basePathTextBox.Size = new Size(1060, 23);
        basePathTextBox.ReadOnly = true;
        basePathTextBox.TabIndex = 0;
        basePathTextBox.TabStop = false;

        outputPathLabel.AutoSize = true;
        outputPathLabel.Location = new Point(18, 43);
        outputPathLabel.Name = "outputPathLabel";
        outputPathLabel.Size = new Size(79, 17);
        outputPathLabel.Text = "输出路径:";

        outputPathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        outputPathTextBox.Location = new Point(101, 40);
        outputPathTextBox.Name = "outputPathTextBox";
        outputPathTextBox.Size = new Size(1060, 23);
        outputPathTextBox.TabIndex = 1;

        outputHintLabel.AutoSize = true;
        outputHintLabel.ForeColor = SystemColors.GrayText;
        outputHintLabel.Location = new Point(101, 66);
        outputHintLabel.Name = "outputHintLabel";
        outputHintLabel.Size = new Size(478, 17);
        outputHintLabel.Text = "建议定位到你的现实主义模版路径即-user\\mods\\SPT-Realism\\db\\templates";

        browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        browseButton.Location = new Point(1190, 39);
        browseButton.Name = "browseButton";
        browseButton.Size = new Size(96, 25);
        browseButton.TabIndex = 2;
        browseButton.Text = "选择输出路径";
        browseButton.UseVisualStyleBackColor = true;
        browseButton.Click += browseButton_Click;

        saveAllButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        saveAllButton.Location = new Point(18, 92);
        saveAllButton.Name = "saveAllButton";
        saveAllButton.Size = new Size(96, 25);
        saveAllButton.TabIndex = 3;
        saveAllButton.Text = "保存全部";
        saveAllButton.UseVisualStyleBackColor = true;
        saveAllButton.Click += saveAllButton_Click;

        reloadButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        reloadButton.Location = new Point(122, 92);
        reloadButton.Name = "reloadButton";
        reloadButton.Size = new Size(96, 25);
        reloadButton.TabIndex = 4;
        reloadButton.Text = "重新加载";
        reloadButton.UseVisualStyleBackColor = true;
        reloadButton.Click += reloadButton_Click;

        generateButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        generateButton.Location = new Point(226, 92);
        generateButton.Name = "generateButton";
        generateButton.Size = new Size(96, 25);
        generateButton.TabIndex = 5;
        generateButton.Text = "生成补丁";
        generateButton.UseVisualStyleBackColor = true;
        generateButton.Click += generateButton_Click;

        auditButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        auditButton.Location = new Point(330, 92);
        auditButton.Name = "auditButton";
        auditButton.Size = new Size(180, 25);
        auditButton.TabIndex = 6;
        auditButton.Text = "检查未遵循规则物品";
        auditButton.UseVisualStyleBackColor = true;
        auditButton.Click += auditButton_Click;

        languageLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        languageLabel.AutoSize = true;
        languageLabel.Location = new Point(838, 96);
        languageLabel.Name = "languageLabel";
        languageLabel.Size = new Size(43, 17);
        languageLabel.Text = "语言:";

        languageComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        languageComboBox.FormattingEnabled = true;
        languageComboBox.Location = new Point(884, 92);
        languageComboBox.Name = "languageComboBox";
        languageComboBox.Size = new Size(116, 25);
        languageComboBox.TabIndex = 7;
        languageComboBox.SelectedIndexChanged += languageComboBox_SelectedIndexChanged;

        searchLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        searchLabel.AutoSize = true;
        searchLabel.Location = new Point(1012, 96);
        searchLabel.Name = "searchLabel";
        searchLabel.Size = new Size(43, 17);
        searchLabel.Text = "搜索:";

        searchTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        searchTextBox.Location = new Point(1059, 92);
        searchTextBox.Name = "searchTextBox";
        searchTextBox.Size = new Size(227, 23);
        searchTextBox.TabIndex = 8;
        searchTextBox.TextChanged += searchTextBox_TextChanged;

        mainSplitContainer.Dock = DockStyle.Fill;
        mainSplitContainer.FixedPanel = FixedPanel.Panel1;
        mainSplitContainer.Location = new Point(0, 186);
        mainSplitContainer.Name = "mainSplitContainer";
        mainSplitContainer.Panel1.Controls.Add(navigationGroupBox);
        mainSplitContainer.Panel2.Controls.Add(editorSplitContainer);
        mainSplitContainer.Panel1MinSize = 420;
        mainSplitContainer.SplitterDistance = 510;
        mainSplitContainer.TabIndex = 7;

        navigationGroupBox.Controls.Add(ruleTreeView);
        navigationGroupBox.Dock = DockStyle.Fill;
        navigationGroupBox.Location = new Point(0, 0);
        navigationGroupBox.Name = "navigationGroupBox";
        navigationGroupBox.Padding = new Padding(8);
        navigationGroupBox.Size = new Size(510, 718);
        navigationGroupBox.TabIndex = 0;
        navigationGroupBox.TabStop = false;
        navigationGroupBox.Text = "规则分类";

        ruleTreeView.Dock = DockStyle.Fill;
        ruleTreeView.HideSelection = false;
        ruleTreeView.Location = new Point(8, 24);
        ruleTreeView.Name = "ruleTreeView";
        ruleTreeView.Size = new Size(494, 686);
        ruleTreeView.TabIndex = 0;
        ruleTreeView.AfterSelect += ruleTreeView_AfterSelect;

        editorSplitContainer.Dock = DockStyle.Fill;
        editorSplitContainer.Location = new Point(0, 0);
        editorSplitContainer.Name = "editorSplitContainer";
        editorSplitContainer.Orientation = Orientation.Horizontal;
        editorSplitContainer.Panel1.Controls.Add(gridGroupBox);
        editorSplitContainer.Panel2.Controls.Add(detailTabControl);
        editorSplitContainer.SplitterDistance = 470;
        editorSplitContainer.TabIndex = 0;

        gridGroupBox.Controls.Add(ruleGridView);
        gridGroupBox.Dock = DockStyle.Fill;
        gridGroupBox.Location = new Point(0, 0);
        gridGroupBox.Name = "gridGroupBox";
        gridGroupBox.Padding = new Padding(8);
        gridGroupBox.Size = new Size(1096, 470);
        gridGroupBox.TabIndex = 0;
        gridGroupBox.TabStop = false;
        gridGroupBox.Text = "规则范围";

        ruleGridView.AllowUserToAddRows = false;
        ruleGridView.AllowUserToDeleteRows = false;
        ruleGridView.AllowUserToResizeRows = false;
        ruleGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        ruleGridView.BackgroundColor = SystemColors.Window;
        ruleGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        ruleGridView.Columns.AddRange(new DataGridViewColumn[] { fieldColumn, minColumn, maxColumn, preferIntColumn, sourceColumn });
        ruleGridView.Dock = DockStyle.Fill;
        ruleGridView.EditMode = DataGridViewEditMode.EditOnEnter;
        ruleGridView.Location = new Point(8, 24);
        ruleGridView.MultiSelect = false;
        ruleGridView.Name = "ruleGridView";
        ruleGridView.RowHeadersVisible = false;
        ruleGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        ruleGridView.Size = new Size(1080, 438);
        ruleGridView.TabIndex = 0;
        ruleGridView.CellEndEdit += ruleGridView_CellEndEdit;
        ruleGridView.CellValidating += ruleGridView_CellValidating;
        ruleGridView.SelectionChanged += ruleGridView_SelectionChanged;

        fieldColumn.FillWeight = 220F;
        fieldColumn.HeaderText = "字段";
        fieldColumn.Name = "fieldColumn";
        fieldColumn.ReadOnly = true;

        minColumn.FillWeight = 110F;
        minColumn.HeaderText = "最小值";
        minColumn.Name = "minColumn";

        maxColumn.FillWeight = 110F;
        maxColumn.HeaderText = "最大值";
        maxColumn.Name = "maxColumn";

        preferIntColumn.FillWeight = 80F;
        preferIntColumn.HeaderText = "整数优先";
        preferIntColumn.Name = "preferIntColumn";
        preferIntColumn.ReadOnly = true;

        sourceColumn.FillWeight = 160F;
        sourceColumn.HeaderText = "来源";
        sourceColumn.Name = "sourceColumn";
        sourceColumn.ReadOnly = true;

        detailTabControl.Controls.Add(explanationTabPage);
        detailTabControl.Controls.Add(logTabPage);
        detailTabControl.Dock = DockStyle.Fill;
        detailTabControl.Location = new Point(0, 0);
        detailTabControl.Name = "detailTabControl";
        detailTabControl.SelectedIndex = 0;
        detailTabControl.Size = new Size(1096, 244);
        detailTabControl.TabIndex = 0;

        explanationTabPage.Controls.Add(explanationTextBox);
        explanationTabPage.Location = new Point(4, 26);
        explanationTabPage.Name = "explanationTabPage";
        explanationTabPage.Padding = new Padding(8);
        explanationTabPage.Size = new Size(1088, 214);
        explanationTabPage.TabIndex = 0;
        explanationTabPage.Text = "字段说明";
        explanationTabPage.UseVisualStyleBackColor = true;

        explanationTextBox.Dock = DockStyle.Fill;
        explanationTextBox.Location = new Point(8, 8);
        explanationTextBox.Multiline = true;
        explanationTextBox.Name = "explanationTextBox";
        explanationTextBox.ReadOnly = true;
        explanationTextBox.ScrollBars = ScrollBars.Vertical;
        explanationTextBox.Size = new Size(1072, 198);
        explanationTextBox.TabIndex = 0;

        logTabPage.Controls.Add(logTextBox);
        logTabPage.Location = new Point(4, 26);
        logTabPage.Name = "logTabPage";
        logTabPage.Padding = new Padding(8);
        logTabPage.Size = new Size(1088, 214);
        logTabPage.TabIndex = 1;
        logTabPage.Text = "运行日志";
        logTabPage.UseVisualStyleBackColor = true;

        logTextBox.Dock = DockStyle.Fill;
        logTextBox.Location = new Point(8, 8);
        logTextBox.Multiline = true;
        logTextBox.Name = "logTextBox";
        logTextBox.ReadOnly = true;
        logTextBox.ScrollBars = ScrollBars.Both;
        logTextBox.Size = new Size(1072, 198);
        logTextBox.TabIndex = 0;
        logTextBox.WordWrap = false;

        statusStrip.Items.AddRange(new ToolStripItem[] { totalItemsStatusLabel, visibleItemsStatusLabel, dirtyItemsStatusLabel, stateStatusLabel });
        statusStrip.Location = new Point(0, 838);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(1380, 22);
        statusStrip.TabIndex = 8;

        totalItemsStatusLabel.Name = "totalItemsStatusLabel";
        totalItemsStatusLabel.Size = new Size(68, 17);
        totalItemsStatusLabel.Text = "总范围项: 0";

        visibleItemsStatusLabel.Margin = new Padding(18, 3, 0, 2);
        visibleItemsStatusLabel.Name = "visibleItemsStatusLabel";
        visibleItemsStatusLabel.Size = new Size(68, 17);
        visibleItemsStatusLabel.Text = "当前显示: 0";

        dirtyItemsStatusLabel.Margin = new Padding(18, 3, 0, 2);
        dirtyItemsStatusLabel.Name = "dirtyItemsStatusLabel";
        dirtyItemsStatusLabel.Size = new Size(80, 17);
        dirtyItemsStatusLabel.Text = "未保存修改: 0";

        stateStatusLabel.Margin = new Padding(18, 3, 0, 2);
        stateStatusLabel.Name = "stateStatusLabel";
        stateStatusLabel.Size = new Size(32, 17);
        stateStatusLabel.Text = "就绪";

        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        toolbarPanel.ResumeLayout(false);
        toolbarPanel.PerformLayout();
        mainSplitContainer.Panel1.ResumeLayout(false);
        mainSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
        mainSplitContainer.ResumeLayout(false);
        navigationGroupBox.ResumeLayout(false);
        editorSplitContainer.Panel1.ResumeLayout(false);
        editorSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)editorSplitContainer).EndInit();
        editorSplitContainer.ResumeLayout(false);
        gridGroupBox.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)ruleGridView).EndInit();
        detailTabControl.ResumeLayout(false);
        explanationTabPage.ResumeLayout(false);
        explanationTabPage.PerformLayout();
        logTabPage.ResumeLayout(false);
        logTabPage.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
