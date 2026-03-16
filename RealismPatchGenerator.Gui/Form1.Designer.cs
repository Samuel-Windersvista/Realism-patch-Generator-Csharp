namespace RealismPatchGenerator.Gui;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;
    private Label titleLabel;
    private TextBox basePathTextBox;
    private Button browseButton;
    private Button generateButton;
    private Button openOutputButton;
    private Button auditButton;
    private Button openAuditReportsButton;
    private TextBox logTextBox;
    private Label statusLabel;
    private TextBox phaseTextBox;
    private Label pathLabel;
    private Label phaseLabel;
    private Label logLabel;
    private Label rulesLabel;
    private ComboBox ruleFileComboBox;
    private Button reloadRuleButton;
    private Button saveRuleButton;
    private Button openRulesButton;
    private TextBox ruleEditorTextBox;

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
        titleLabel = new Label();
        basePathTextBox = new TextBox();
        browseButton = new Button();
        generateButton = new Button();
        openOutputButton = new Button();
        auditButton = new Button();
        openAuditReportsButton = new Button();
        logTextBox = new TextBox();
        statusLabel = new Label();
        phaseTextBox = new TextBox();
        pathLabel = new Label();
        phaseLabel = new Label();
        logLabel = new Label();
        rulesLabel = new Label();
        ruleFileComboBox = new ComboBox();
        reloadRuleButton = new Button();
        saveRuleButton = new Button();
        openRulesButton = new Button();
        ruleEditorTextBox = new TextBox();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1180, 760);
        Controls.Add(ruleEditorTextBox);
        Controls.Add(openRulesButton);
        Controls.Add(saveRuleButton);
        Controls.Add(reloadRuleButton);
        Controls.Add(ruleFileComboBox);
        Controls.Add(rulesLabel);
        Controls.Add(logLabel);
        Controls.Add(phaseLabel);
        Controls.Add(pathLabel);
        Controls.Add(phaseTextBox);
        Controls.Add(statusLabel);
        Controls.Add(logTextBox);
        Controls.Add(openAuditReportsButton);
        Controls.Add(auditButton);
        Controls.Add(openOutputButton);
        Controls.Add(generateButton);
        Controls.Add(browseButton);
        Controls.Add(basePathTextBox);
        Controls.Add(titleLabel);
        MinimumSize = new Size(1120, 720);
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "EFT 现实主义数值生成器 C# 版";

        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Microsoft YaHei UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 134);
        titleLabel.Location = new Point(24, 20);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(382, 28);
        titleLabel.Text = "EFT 现实主义数值生成器 C# 版";

        pathLabel.AutoSize = true;
        pathLabel.Location = new Point(27, 73);
        pathLabel.Name = "pathLabel";
        pathLabel.Size = new Size(103, 17);
        pathLabel.Text = "仓库根目录路径";

        basePathTextBox.Location = new Point(30, 95);
        basePathTextBox.Name = "basePathTextBox";
        basePathTextBox.Size = new Size(760, 23);
        basePathTextBox.TabIndex = 0;

        browseButton.Location = new Point(803, 94);
        browseButton.Name = "browseButton";
        browseButton.Size = new Size(102, 25);
        browseButton.TabIndex = 1;
        browseButton.Text = "选择目录";
        browseButton.UseVisualStyleBackColor = true;
        browseButton.Click += browseButton_Click;

        generateButton.Location = new Point(913, 94);
        generateButton.Name = "generateButton";
        generateButton.Size = new Size(109, 25);
        generateButton.TabIndex = 2;
        generateButton.Text = "生成补丁";
        generateButton.UseVisualStyleBackColor = true;
        generateButton.Click += generateButton_Click;

        auditButton.Location = new Point(1028, 94);
        auditButton.Name = "auditButton";
        auditButton.Size = new Size(124, 25);
        auditButton.TabIndex = 3;
        auditButton.Text = "审计 output";
        auditButton.UseVisualStyleBackColor = true;
        auditButton.Click += auditButton_Click;

        openOutputButton.Location = new Point(913, 129);
        openOutputButton.Name = "openOutputButton";
        openOutputButton.Size = new Size(109, 25);
        openOutputButton.TabIndex = 4;
        openOutputButton.Text = "打开 output";
        openOutputButton.UseVisualStyleBackColor = true;
        openOutputButton.Click += openOutputButton_Click;

        openAuditReportsButton.Location = new Point(1028, 129);
        openAuditReportsButton.Name = "openAuditReportsButton";
        openAuditReportsButton.Size = new Size(124, 25);
        openAuditReportsButton.TabIndex = 5;
        openAuditReportsButton.Text = "打开 audit_reports";
        openAuditReportsButton.UseVisualStyleBackColor = true;
        openAuditReportsButton.Click += openAuditReportsButton_Click;

        phaseLabel.AutoSize = true;
        phaseLabel.Location = new Point(27, 140);
        phaseLabel.Name = "phaseLabel";
        phaseLabel.Size = new Size(67, 17);
        phaseLabel.Text = "迁移阶段";

        phaseTextBox.Location = new Point(30, 164);
        phaseTextBox.Multiline = true;
        phaseTextBox.Name = "phaseTextBox";
        phaseTextBox.ReadOnly = true;
        phaseTextBox.ScrollBars = ScrollBars.Vertical;
        phaseTextBox.Size = new Size(520, 190);
        phaseTextBox.TabIndex = 4;

        rulesLabel.AutoSize = true;
        rulesLabel.Location = new Point(577, 140);
        rulesLabel.Name = "rulesLabel";
        rulesLabel.Size = new Size(67, 17);
        rulesLabel.Text = "规则编辑";

        ruleFileComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        ruleFileComboBox.FormattingEnabled = true;
        ruleFileComboBox.Location = new Point(580, 164);
        ruleFileComboBox.Name = "ruleFileComboBox";
        ruleFileComboBox.Size = new Size(285, 25);
        ruleFileComboBox.TabIndex = 6;
        ruleFileComboBox.SelectedIndexChanged += ruleFileComboBox_SelectedIndexChanged;

        reloadRuleButton.Location = new Point(875, 164);
        reloadRuleButton.Name = "reloadRuleButton";
        reloadRuleButton.Size = new Size(78, 25);
        reloadRuleButton.TabIndex = 7;
        reloadRuleButton.Text = "重载";
        reloadRuleButton.UseVisualStyleBackColor = true;
        reloadRuleButton.Click += reloadRuleButton_Click;

        saveRuleButton.Location = new Point(959, 164);
        saveRuleButton.Name = "saveRuleButton";
        saveRuleButton.Size = new Size(92, 25);
        saveRuleButton.TabIndex = 8;
        saveRuleButton.Text = "保存规则";
        saveRuleButton.UseVisualStyleBackColor = true;
        saveRuleButton.Click += saveRuleButton_Click;

        openRulesButton.Location = new Point(1057, 164);
        openRulesButton.Name = "openRulesButton";
        openRulesButton.Size = new Size(95, 25);
        openRulesButton.TabIndex = 9;
        openRulesButton.Text = "打开 rules";
        openRulesButton.UseVisualStyleBackColor = true;
        openRulesButton.Click += openRulesButton_Click;

        ruleEditorTextBox.AcceptsReturn = true;
        ruleEditorTextBox.AcceptsTab = true;
        ruleEditorTextBox.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        ruleEditorTextBox.Location = new Point(580, 198);
        ruleEditorTextBox.Multiline = true;
        ruleEditorTextBox.Name = "ruleEditorTextBox";
        ruleEditorTextBox.ScrollBars = ScrollBars.Both;
        ruleEditorTextBox.Size = new Size(572, 156);
        ruleEditorTextBox.TabIndex = 10;
        ruleEditorTextBox.WordWrap = false;

        logLabel.AutoSize = true;
        logLabel.Location = new Point(27, 376);
        logLabel.Name = "logLabel";
        logLabel.Size = new Size(55, 17);
        logLabel.Text = "运行日志";

        logTextBox.Location = new Point(30, 399);
        logTextBox.Multiline = true;
        logTextBox.Name = "logTextBox";
        logTextBox.ReadOnly = true;
        logTextBox.ScrollBars = ScrollBars.Both;
        logTextBox.Size = new Size(1122, 305);
        logTextBox.TabIndex = 11;
        logTextBox.WordWrap = false;

        statusLabel.AutoSize = true;
        statusLabel.Location = new Point(27, 719);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(32, 17);
        statusLabel.Text = "就绪";
    }

    #endregion
}
