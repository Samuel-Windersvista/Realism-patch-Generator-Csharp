namespace RealismPatchGenerator.Gui;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;
    private Label titleLabel;
    private Label pathLabel;
    private Label outputPathLabel;
    private Label seedLabel;
    private Label outputHintLabel;
    private TextBox basePathTextBox;
    private TextBox outputPathTextBox;
    private TextBox seedTextBox;
    private Button browseButton;
    private Button clearSeedButton;
    private Button useLastSeedButton;
    private Button saveAllButton;
    private Button reloadButton;
    private Button exceptionsButton;
    private Button generateButton;
    private Button auditButton;
    private CheckBox modifiedOnlyCheckBox;
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
    private TabPage exceptionsOverviewTabPage;
    private TabPage logTabPage;
    private TextBox explanationTextBox;
    private DataGridView exceptionsOverviewGridView;
    private DataGridViewCheckBoxColumn exceptionEnabledColumn;
    private DataGridViewTextBoxColumn exceptionItemIdColumn;
    private DataGridViewTextBoxColumn exceptionNameColumn;
    private DataGridViewTextBoxColumn exceptionFieldCountColumn;
    private DataGridViewTextBoxColumn exceptionSourceFileColumn;
    private DataGridViewTextBoxColumn exceptionNotesColumn;
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
        var toolbarTable = new TableLayoutPanel();
        var dataRootTable = new TableLayoutPanel();
        var outputTable = new TableLayoutPanel();
        var actionTable = new TableLayoutPanel();
        var rulesActionGroupBox = new GroupBox();
        var rulesActionPanel = new FlowLayoutPanel();
        var runActionGroupBox = new GroupBox();
        var runActionPanel = new FlowLayoutPanel();
        var filterActionGroupBox = new GroupBox();
        var filterActionPanel = new FlowLayoutPanel();
        var searchPanel = new FlowLayoutPanel();
        var languagePanel = new FlowLayoutPanel();
        titleLabel = new Label();
        pathLabel = new Label();
        outputPathLabel = new Label();
        seedLabel = new Label();
        outputHintLabel = new Label();
        basePathTextBox = new TextBox();
        outputPathTextBox = new TextBox();
        seedTextBox = new TextBox();
        browseButton = new Button();
        clearSeedButton = new Button();
        useLastSeedButton = new Button();
        saveAllButton = new Button();
        reloadButton = new Button();
        exceptionsButton = new Button();
        generateButton = new Button();
        auditButton = new Button();
        modifiedOnlyCheckBox = new CheckBox();
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
        exceptionsOverviewTabPage = new TabPage();
        exceptionsOverviewGridView = new DataGridView();
        exceptionEnabledColumn = new DataGridViewCheckBoxColumn();
        exceptionItemIdColumn = new DataGridViewTextBoxColumn();
        exceptionNameColumn = new DataGridViewTextBoxColumn();
        exceptionFieldCountColumn = new DataGridViewTextBoxColumn();
        exceptionSourceFileColumn = new DataGridViewTextBoxColumn();
        exceptionNotesColumn = new DataGridViewTextBoxColumn();
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
        exceptionsOverviewTabPage.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)exceptionsOverviewGridView).BeginInit();
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
        Text = "SPT现实主义数值范围编辑生成器 v1.2";

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Height = 54;
        headerPanel.Padding = new Padding(16, 12, 16, 0);

        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Microsoft YaHei UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 134);
        titleLabel.Location = new Point(16, 12);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(520, 28);
        titleLabel.Text = "SPT现实主义数值范围编辑生成器 v1.2";

        toolbarPanel.Controls.Add(toolbarTable);
        toolbarPanel.Dock = DockStyle.Top;
        toolbarPanel.Height = 146;
        toolbarPanel.Padding = new Padding(16, 10, 16, 10);

        toolbarTable.ColumnCount = 1;
        toolbarTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        toolbarTable.Controls.Add(dataRootTable, 0, 0);
        toolbarTable.Controls.Add(outputTable, 0, 1);
        toolbarTable.Controls.Add(outputHintLabel, 0, 2);
        toolbarTable.Controls.Add(actionTable, 0, 3);
        toolbarTable.Dock = DockStyle.Fill;
        toolbarTable.Location = new Point(16, 10);
        toolbarTable.Margin = new Padding(0);
        toolbarTable.Name = "toolbarTable";
        toolbarTable.RowCount = 4;
        toolbarTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        toolbarTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        toolbarTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 14F));
        toolbarTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        toolbarTable.Size = new Size(1348, 126);
        toolbarTable.TabIndex = 0;

        dataRootTable.ColumnCount = 2;
        dataRootTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
        dataRootTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        dataRootTable.Controls.Add(pathLabel, 0, 0);
        dataRootTable.Controls.Add(basePathTextBox, 1, 0);
        dataRootTable.Dock = DockStyle.Fill;
        dataRootTable.Location = new Point(0, 0);
        dataRootTable.Margin = new Padding(0);
        dataRootTable.Name = "dataRootTable";
        dataRootTable.RowCount = 1;
        dataRootTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        dataRootTable.Size = new Size(1348, 26);
        dataRootTable.TabIndex = 0;

        outputTable.ColumnCount = 7;
        outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
        outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72F));
        outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
        outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68F));
        outputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98F));
        outputTable.Controls.Add(outputPathLabel, 0, 0);
        outputTable.Controls.Add(outputPathTextBox, 1, 0);
        outputTable.Controls.Add(browseButton, 2, 0);
        outputTable.Controls.Add(seedLabel, 3, 0);
        outputTable.Controls.Add(seedTextBox, 4, 0);
        outputTable.Controls.Add(clearSeedButton, 5, 0);
        outputTable.Controls.Add(useLastSeedButton, 6, 0);
        outputTable.Dock = DockStyle.Fill;
        outputTable.Location = new Point(0, 26);
        outputTable.Margin = new Padding(0);
        outputTable.Name = "outputTable";
        outputTable.RowCount = 1;
        outputTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        outputTable.Size = new Size(1348, 26);
        outputTable.TabIndex = 1;

        actionTable.ColumnCount = 3;
        actionTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));
        actionTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26F));
        actionTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        actionTable.Controls.Add(rulesActionGroupBox, 0, 0);
        actionTable.Controls.Add(runActionGroupBox, 1, 0);
        actionTable.Controls.Add(filterActionGroupBox, 2, 0);
        actionTable.Dock = DockStyle.Fill;
        actionTable.Location = new Point(0, 66);
        actionTable.Margin = new Padding(0);
        actionTable.Name = "actionTable";
        actionTable.RowCount = 1;
        actionTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        actionTable.Size = new Size(1348, 60);
        actionTable.TabIndex = 3;

        rulesActionGroupBox.Controls.Add(rulesActionPanel);
        rulesActionGroupBox.Dock = DockStyle.Fill;
        rulesActionGroupBox.Location = new Point(0, 0);
        rulesActionGroupBox.Margin = new Padding(0, 0, 8, 0);
        rulesActionGroupBox.Name = "rulesActionGroupBox";
        rulesActionGroupBox.Padding = new Padding(8, 4, 8, 6);
        rulesActionGroupBox.Size = new Size(315, 60);
        rulesActionGroupBox.TabStop = false;
        rulesActionGroupBox.Text = "规则操作";

        rulesActionPanel.Controls.Add(saveAllButton);
        rulesActionPanel.Controls.Add(reloadButton);
        rulesActionPanel.Controls.Add(exceptionsButton);
        rulesActionPanel.Dock = DockStyle.Fill;
        rulesActionPanel.Location = new Point(8, 20);
        rulesActionPanel.Margin = new Padding(0);
        rulesActionPanel.Name = "rulesActionPanel";
        rulesActionPanel.Size = new Size(299, 34);
        rulesActionPanel.TabIndex = 0;
        rulesActionPanel.WrapContents = false;

        runActionGroupBox.Controls.Add(runActionPanel);
        runActionGroupBox.Dock = DockStyle.Fill;
        runActionGroupBox.Location = new Point(404, 0);
        runActionGroupBox.Margin = new Padding(0, 0, 8, 0);
        runActionGroupBox.Name = "runActionGroupBox";
        runActionGroupBox.Padding = new Padding(8, 4, 8, 6);
        runActionGroupBox.Size = new Size(342, 60);
        runActionGroupBox.TabStop = false;
        runActionGroupBox.Text = "生成检查";

        runActionPanel.Controls.Add(generateButton);
        runActionPanel.Controls.Add(auditButton);
        runActionPanel.Dock = DockStyle.Fill;
        runActionPanel.Location = new Point(8, 20);
        runActionPanel.Margin = new Padding(0);
        runActionPanel.Name = "runActionPanel";
        runActionPanel.Size = new Size(326, 34);
        runActionPanel.TabIndex = 0;
        runActionPanel.WrapContents = false;

        filterActionGroupBox.Controls.Add(filterActionPanel);
        filterActionGroupBox.Dock = DockStyle.Fill;
        filterActionGroupBox.Location = new Point(808, 0);
        filterActionGroupBox.Margin = new Padding(0);
        filterActionGroupBox.Name = "filterActionGroupBox";
        filterActionGroupBox.Padding = new Padding(8, 4, 8, 6);
        filterActionGroupBox.Size = new Size(674, 60);
        filterActionGroupBox.TabStop = false;
        filterActionGroupBox.Text = "筛选搜索";

        filterActionPanel.Controls.Add(modifiedOnlyCheckBox);
        filterActionPanel.Controls.Add(searchPanel);
        filterActionPanel.Controls.Add(languagePanel);
        filterActionPanel.Dock = DockStyle.Fill;
        filterActionPanel.Location = new Point(8, 20);
        filterActionPanel.Margin = new Padding(0);
        filterActionPanel.Name = "filterActionPanel";
        filterActionPanel.Size = new Size(658, 34);
        filterActionPanel.TabIndex = 0;
        filterActionPanel.WrapContents = false;

        searchPanel.AutoSize = true;
        searchPanel.Controls.Add(searchLabel);
        searchPanel.Controls.Add(searchTextBox);
        searchPanel.Location = new Point(111, 0);
        searchPanel.Margin = new Padding(0, 0, 12, 0);
        searchPanel.Name = "searchPanel";
        searchPanel.Size = new Size(229, 29);
        searchPanel.TabIndex = 1;
        searchPanel.WrapContents = false;

        languagePanel.AutoSize = true;
        languagePanel.Controls.Add(languageLabel);
        languagePanel.Controls.Add(languageComboBox);
        languagePanel.Location = new Point(352, 0);
        languagePanel.Margin = new Padding(0);
        languagePanel.Name = "languagePanel";
        languagePanel.Size = new Size(149, 29);
        languagePanel.TabIndex = 2;
        languagePanel.WrapContents = false;

        pathLabel.AutoSize = true;
        pathLabel.Anchor = AnchorStyles.Left;
        pathLabel.Location = new Point(3, 5);
        pathLabel.Margin = new Padding(0, 0, 6, 0);
        pathLabel.Name = "pathLabel";
        pathLabel.Size = new Size(79, 17);
        pathLabel.Text = "数据目录:";

        basePathTextBox.Dock = DockStyle.Fill;
        basePathTextBox.Location = new Point(83, 2);
        basePathTextBox.Margin = new Padding(0, 2, 0, 2);
        basePathTextBox.Name = "basePathTextBox";
        basePathTextBox.Size = new Size(1265, 23);
        basePathTextBox.ReadOnly = true;
        basePathTextBox.TabIndex = 0;
        basePathTextBox.TabStop = false;

        outputPathLabel.AutoSize = true;
        outputPathLabel.Anchor = AnchorStyles.Left;
        outputPathLabel.Location = new Point(3, 5);
        outputPathLabel.Margin = new Padding(0, 0, 6, 0);
        outputPathLabel.Name = "outputPathLabel";
        outputPathLabel.Size = new Size(79, 17);
        outputPathLabel.Text = "输出路径:";

        outputPathTextBox.Dock = DockStyle.Fill;
        outputPathTextBox.Location = new Point(83, 2);
        outputPathTextBox.Margin = new Padding(0, 2, 8, 2);
        outputPathTextBox.Name = "outputPathTextBox";
        outputPathTextBox.Size = new Size(783, 23);
        outputPathTextBox.TabIndex = 1;

        seedLabel.Anchor = AnchorStyles.Left;
        seedLabel.AutoSize = true;
        seedLabel.Location = new Point(1030, 5);
        seedLabel.Margin = new Padding(0, 0, 6, 0);
        seedLabel.Name = "seedLabel";
        seedLabel.Size = new Size(40, 17);
        seedLabel.Text = "Seed:";

        seedTextBox.Dock = DockStyle.Fill;
        seedTextBox.Location = new Point(1102, 2);
        seedTextBox.Margin = new Padding(0, 2, 0, 2);
        seedTextBox.Name = "seedTextBox";
        seedTextBox.PlaceholderText = "留空=每次随机";
        seedTextBox.Size = new Size(132, 23);
        seedTextBox.TabIndex = 3;

        clearSeedButton.Anchor = AnchorStyles.Right;
        clearSeedButton.Location = new Point(1237, 1);
        clearSeedButton.Margin = new Padding(0, 1, 0, 1);
        clearSeedButton.Name = "clearSeedButton";
        clearSeedButton.Size = new Size(64, 25);
        clearSeedButton.TabIndex = 4;
        clearSeedButton.Text = "清空";
        clearSeedButton.UseVisualStyleBackColor = true;
        clearSeedButton.Click += clearSeedButton_Click;

        useLastSeedButton.Anchor = AnchorStyles.Right;
        useLastSeedButton.Location = new Point(1250, 1);
        useLastSeedButton.Margin = new Padding(0, 1, 0, 1);
        useLastSeedButton.Name = "useLastSeedButton";
        useLastSeedButton.Size = new Size(98, 25);
        useLastSeedButton.TabIndex = 5;
        useLastSeedButton.Text = "填回上次";
        useLastSeedButton.UseVisualStyleBackColor = true;
        useLastSeedButton.Click += useLastSeedButton_Click;

        outputHintLabel.AutoSize = true;
        outputHintLabel.Dock = DockStyle.Fill;
        outputHintLabel.ForeColor = SystemColors.GrayText;
        outputHintLabel.Font = new Font("Microsoft YaHei UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 134);
        outputHintLabel.Location = new Point(3, 52);
        outputHintLabel.Margin = new Padding(80, -1, 0, 0);
        outputHintLabel.Name = "outputHintLabel";
        outputHintLabel.Size = new Size(1265, 14);
        outputHintLabel.TextAlign = ContentAlignment.MiddleLeft;
        outputHintLabel.Text = "建议定位到你的现实主义模版路径即-user\\mods\\SPT-Realism\\db\\templates";

        browseButton.Anchor = AnchorStyles.Right;
        browseButton.Location = new Point(1243, 1);
        browseButton.Margin = new Padding(0, 1, 0, 1);
        browseButton.Name = "browseButton";
        browseButton.Size = new Size(96, 25);
        browseButton.TabIndex = 2;
        browseButton.Text = "选择输出路径";
        browseButton.UseVisualStyleBackColor = true;
        browseButton.Click += browseButton_Click;

        saveAllButton.Location = new Point(0, 3);
        saveAllButton.Margin = new Padding(0, 1, 8, 1);
        saveAllButton.Name = "saveAllButton";
        saveAllButton.Size = new Size(88, 25);
        saveAllButton.TabIndex = 3;
        saveAllButton.Text = "保存全部";
        saveAllButton.UseVisualStyleBackColor = true;
        saveAllButton.Click += saveAllButton_Click;

        reloadButton.Location = new Point(96, 3);
        reloadButton.Margin = new Padding(0, 1, 8, 1);
        reloadButton.Name = "reloadButton";
        reloadButton.Size = new Size(88, 25);
        reloadButton.TabIndex = 4;
        reloadButton.Text = "重新加载";
        reloadButton.UseVisualStyleBackColor = true;
        reloadButton.Click += reloadButton_Click;

        exceptionsButton.Location = new Point(192, 3);
        exceptionsButton.Margin = new Padding(0, 1, 0, 1);
        exceptionsButton.Name = "exceptionsButton";
        exceptionsButton.Size = new Size(88, 25);
        exceptionsButton.TabIndex = 5;
        exceptionsButton.Text = "例外物品";
        exceptionsButton.UseVisualStyleBackColor = true;
        exceptionsButton.Click += exceptionsButton_Click;

        generateButton.Location = new Point(208, 3);
        generateButton.Margin = new Padding(0, 1, 8, 1);
        generateButton.Name = "generateButton";
        generateButton.Size = new Size(96, 25);
        generateButton.TabIndex = 6;
        generateButton.Text = "生成补丁";
        generateButton.UseVisualStyleBackColor = true;
        generateButton.Click += generateButton_Click;

        auditButton.Location = new Point(312, 3);
        auditButton.Margin = new Padding(0, 1, 8, 1);
        auditButton.Name = "auditButton";
        auditButton.Size = new Size(180, 25);
        auditButton.TabIndex = 7;
        auditButton.Text = "检查未遵循规则物品";
        auditButton.UseVisualStyleBackColor = true;
        auditButton.Click += auditButton_Click;

        modifiedOnlyCheckBox.AutoSize = true;
        modifiedOnlyCheckBox.Location = new Point(3, 4);
        modifiedOnlyCheckBox.Margin = new Padding(0, 4, 12, 0);
        modifiedOnlyCheckBox.Name = "modifiedOnlyCheckBox";
        modifiedOnlyCheckBox.Size = new Size(99, 21);
        modifiedOnlyCheckBox.TabIndex = 8;
        modifiedOnlyCheckBox.Text = "只看已修改项";
        modifiedOnlyCheckBox.UseVisualStyleBackColor = true;
        modifiedOnlyCheckBox.CheckedChanged += modifiedOnlyCheckBox_CheckedChanged;

        searchLabel.AutoSize = true;
        searchLabel.Location = new Point(3, 7);
        searchLabel.Margin = new Padding(0, 7, 6, 0);
        searchLabel.Name = "searchLabel";
        searchLabel.Size = new Size(43, 17);
        searchLabel.Text = "搜索:";

    searchTextBox.Location = new Point(52, 2);
    searchTextBox.Margin = new Padding(0, 2, 0, 2);
        searchTextBox.Name = "searchTextBox";
    searchTextBox.Size = new Size(170, 23);
        searchTextBox.TabIndex = 9;
        searchTextBox.TextChanged += searchTextBox_TextChanged;

        languageLabel.AutoSize = true;
    languageLabel.Location = new Point(3, 7);
    languageLabel.Margin = new Padding(0, 7, 6, 0);
        languageLabel.Name = "languageLabel";
        languageLabel.Size = new Size(43, 17);
        languageLabel.Text = "语言:";

        languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        languageComboBox.FormattingEnabled = true;
    languageComboBox.Location = new Point(52, 2);
    languageComboBox.Margin = new Padding(0, 2, 0, 2);
        languageComboBox.Name = "languageComboBox";
        languageComboBox.Size = new Size(100, 25);
        languageComboBox.TabIndex = 10;
        languageComboBox.SelectedIndexChanged += languageComboBox_SelectedIndexChanged;

        mainSplitContainer.Dock = DockStyle.Fill;
        mainSplitContainer.Location = new Point(0, 182);
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
        detailTabControl.Controls.Add(exceptionsOverviewTabPage);
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

        exceptionsOverviewTabPage.Controls.Add(exceptionsOverviewGridView);
        exceptionsOverviewTabPage.Location = new Point(4, 26);
        exceptionsOverviewTabPage.Name = "exceptionsOverviewTabPage";
        exceptionsOverviewTabPage.Padding = new Padding(8);
        exceptionsOverviewTabPage.Size = new Size(1088, 214);
        exceptionsOverviewTabPage.TabIndex = 1;
        exceptionsOverviewTabPage.Text = "例外总览";
        exceptionsOverviewTabPage.UseVisualStyleBackColor = true;

        exceptionsOverviewGridView.AllowUserToAddRows = false;
        exceptionsOverviewGridView.AllowUserToDeleteRows = false;
        exceptionsOverviewGridView.AllowUserToResizeRows = false;
        exceptionsOverviewGridView.BackgroundColor = SystemColors.Window;
        exceptionsOverviewGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        exceptionsOverviewGridView.Columns.AddRange(new DataGridViewColumn[] { exceptionEnabledColumn, exceptionItemIdColumn, exceptionNameColumn, exceptionFieldCountColumn, exceptionSourceFileColumn, exceptionNotesColumn });
        exceptionsOverviewGridView.Dock = DockStyle.Fill;
        exceptionsOverviewGridView.Location = new Point(8, 8);
        exceptionsOverviewGridView.MultiSelect = false;
        exceptionsOverviewGridView.Name = "exceptionsOverviewGridView";
        exceptionsOverviewGridView.ReadOnly = true;
        exceptionsOverviewGridView.RowHeadersVisible = false;
        exceptionsOverviewGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        exceptionsOverviewGridView.Size = new Size(1072, 198);
        exceptionsOverviewGridView.TabIndex = 0;

        exceptionEnabledColumn.HeaderText = "启用";
        exceptionEnabledColumn.Name = "exceptionEnabledColumn";
        exceptionEnabledColumn.ReadOnly = true;
        exceptionEnabledColumn.Width = 60;

        exceptionItemIdColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        exceptionItemIdColumn.FillWeight = 150F;
        exceptionItemIdColumn.HeaderText = "ItemID";
        exceptionItemIdColumn.Name = "exceptionItemIdColumn";
        exceptionItemIdColumn.ReadOnly = true;

        exceptionNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        exceptionNameColumn.FillWeight = 150F;
        exceptionNameColumn.HeaderText = "名称";
        exceptionNameColumn.Name = "exceptionNameColumn";
        exceptionNameColumn.ReadOnly = true;

        exceptionFieldCountColumn.HeaderText = "字段数";
        exceptionFieldCountColumn.Name = "exceptionFieldCountColumn";
        exceptionFieldCountColumn.ReadOnly = true;
        exceptionFieldCountColumn.Width = 70;

        exceptionSourceFileColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        exceptionSourceFileColumn.FillWeight = 170F;
        exceptionSourceFileColumn.HeaderText = "来源文件";
        exceptionSourceFileColumn.Name = "exceptionSourceFileColumn";
        exceptionSourceFileColumn.ReadOnly = true;

        exceptionNotesColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        exceptionNotesColumn.FillWeight = 160F;
        exceptionNotesColumn.HeaderText = "备注";
        exceptionNotesColumn.Name = "exceptionNotesColumn";
        exceptionNotesColumn.ReadOnly = true;

        logTabPage.Controls.Add(logTextBox);
        logTabPage.Location = new Point(4, 26);
        logTabPage.Name = "logTabPage";
        logTabPage.Padding = new Padding(8);
        logTabPage.Size = new Size(1088, 214);
        logTabPage.TabIndex = 2;
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
        exceptionsOverviewTabPage.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)exceptionsOverviewGridView).EndInit();
        logTabPage.ResumeLayout(false);
        logTabPage.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
