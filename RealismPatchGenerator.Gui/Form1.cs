using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using RealismPatchGenerator.Core;
using PatchGenerator = RealismPatchGenerator.Core.RealismPatchGenerator;

namespace RealismPatchGenerator.Gui;

public partial class Form1 : Form
{
    private static readonly JsonSerializerOptions RuleJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private string? currentRepositoryRoot;
    private bool suppressRuleSelectionChanged;

    public Form1()
    {
        InitializeComponent();
        ResolveRepositoryRoot();
        InitializeRuleEditor();
        UpdateStatus();
    }

    private void ResolveRepositoryRoot()
    {
        currentRepositoryRoot = WorkspaceLocator.FindApplicationRoot(
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        basePathTextBox.Text = currentRepositoryRoot ?? string.Empty;
    }

    private void InitializeRuleEditor()
    {
        suppressRuleSelectionChanged = true;
        ruleFileComboBox.Items.Clear();
        foreach (var ruleFile in RuleWorkspace.RuleFileNames)
        {
            ruleFileComboBox.Items.Add(ruleFile);
        }

        if (ruleFileComboBox.Items.Count > 0)
        {
            ruleFileComboBox.SelectedIndex = 0;
        }

        suppressRuleSelectionChanged = false;
        LoadSelectedRuleFile();
    }

    private async void generateButton_Click(object sender, EventArgs e)
    {
        var basePath = basePathTextBox.Text.Trim();
        var repositoryRoot = WorkspaceLocator.FindDataRoot(basePath);
        if (repositoryRoot is null)
        {
            MessageBox.Show(this, "请选择包含 input 和 现实主义物品模板 的 C# 程序数据目录。", "目录无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        currentRepositoryRoot = repositoryRoot;
    EnsureRulesInitialized(repositoryRoot, appendToLog: false);
        ToggleBusy(true);
        logTextBox.Clear();
        AppendLog($"开始生成，数据目录: {repositoryRoot}");

        try
        {
            var result = await Task.Run(() => new PatchGenerator(repositoryRoot).Generate());
            foreach (var line in result.Logs)
            {
                AppendLog(line);
            }

            AppendLog(string.Empty);
            AppendLog($"总计: {result.Statistics.TotalCount} 个补丁");
            AppendLog($"输出目录: {result.OutputPath}");
            statusLabel.Text = "生成完成";
        }
        catch (Exception ex)
        {
            AppendLog($"生成失败: {ex.Message}");
            statusLabel.Text = "生成失败";
            MessageBox.Show(this, ex.Message, "生成失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private async void auditButton_Click(object sender, EventArgs e)
    {
        var repositoryRoot = WorkspaceLocator.FindDataRoot(basePathTextBox.Text.Trim());
        if (repositoryRoot is null)
        {
            MessageBox.Show(this, "请选择包含 input 和 现实主义物品模板 的 C# 程序数据目录。", "目录无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        currentRepositoryRoot = repositoryRoot;
        ToggleBusy(true, busyText: "正在审计");
        logTextBox.Clear();
        AppendLog($"开始审计，数据目录: {repositoryRoot}");

        try
        {
            var report = await Task.Run(() => new OutputRuleAuditor(repositoryRoot).Audit());
            var summary = OutputRuleAuditor.BuildConsoleSummary(report, 30);
            foreach (var line in summary.Split(Environment.NewLine))
            {
                AppendLog(line);
            }

            var reportsPath = Path.Combine(repositoryRoot, "audit_reports");
            Directory.CreateDirectory(reportsPath);
            var reportPath = Path.Combine(reportsPath, "output_rule_audit.json");
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, RuleJsonOptions));
            AppendLog(string.Empty);
            AppendLog($"审计报告: {reportPath}");
            statusLabel.Text = report.ViolationCount > 0 ? "审计完成，存在违规" : "审计完成";
        }
        catch (Exception ex)
        {
            AppendLog($"审计失败: {ex.Message}");
            statusLabel.Text = "审计失败";
            MessageBox.Show(this, ex.Message, "审计失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private void browseButton_Click(object sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择 C# 程序数据目录",
            UseDescriptionForTitle = true,
            InitialDirectory = Directory.Exists(basePathTextBox.Text) ? basePathTextBox.Text : Directory.GetCurrentDirectory(),
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            basePathTextBox.Text = dialog.SelectedPath;
            currentRepositoryRoot = WorkspaceLocator.FindDataRoot(dialog.SelectedPath) ?? WorkspaceLocator.FindApplicationRoot(dialog.SelectedPath);
            LoadSelectedRuleFile();
            UpdateStatus();
        }
    }

    private void openOutputButton_Click(object sender, EventArgs e)
    {
        var repositoryRoot = WorkspaceLocator.FindDataRoot(basePathTextBox.Text);
        if (repositoryRoot is null)
        {
            MessageBox.Show(this, "当前目录无效，无法打开 output。", "目录无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var outputPath = Path.Combine(repositoryRoot, "output");
        Directory.CreateDirectory(outputPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = outputPath,
            UseShellExecute = true,
        });
    }

    private void openAuditReportsButton_Click(object sender, EventArgs e)
    {
        var repositoryRoot = WorkspaceLocator.FindDataRoot(basePathTextBox.Text);
        if (repositoryRoot is null)
        {
            MessageBox.Show(this, "当前目录无效，无法打开 audit_reports。", "目录无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var reportsPath = Path.Combine(repositoryRoot, "audit_reports");
        Directory.CreateDirectory(reportsPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = reportsPath,
            UseShellExecute = true,
        });
    }

    private void ruleFileComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (suppressRuleSelectionChanged)
        {
            return;
        }

        LoadSelectedRuleFile();
    }

    private void reloadRuleButton_Click(object sender, EventArgs e)
    {
        LoadSelectedRuleFile();
    }

    private void saveRuleButton_Click(object sender, EventArgs e)
    {
        var repositoryRoot = ResolveDataRootForRules();
        if (repositoryRoot is null)
        {
            MessageBox.Show(this, "当前目录无效，无法保存规则。", "目录无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (ruleFileComboBox.SelectedItem is not string ruleFileName)
        {
            MessageBox.Show(this, "请选择规则文件。", "未选择规则", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            if (!RuleWorkspace.TryNormalizeRuleFile(ruleFileName, ruleEditorTextBox.Text, out var normalizedJson, out var errorMessage))
            {
                throw new InvalidOperationException(errorMessage);
            }

            var rulePath = RuleWorkspace.GetRuleFilePath(repositoryRoot, ruleFileName);
            File.WriteAllText(rulePath, normalizedJson);
            ruleEditorTextBox.Text = File.ReadAllText(rulePath);
            ruleEditorTextBox.SelectionStart = 0;
            ruleEditorTextBox.SelectionLength = 0;
            AppendLog($"规则已保存: {ruleFileName}");
            statusLabel.Text = $"已保存 {ruleFileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "保存规则失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = "保存规则失败";
        }
    }

    private void openRulesButton_Click(object sender, EventArgs e)
    {
        var repositoryRoot = ResolveDataRootForRules();
        if (repositoryRoot is null)
        {
            MessageBox.Show(this, "当前目录无效，无法打开 rules。", "目录无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        EnsureRulesInitialized(repositoryRoot, appendToLog: false);
        var rulesPath = RuleWorkspace.GetRulesDirectory(repositoryRoot);
        Process.Start(new ProcessStartInfo
        {
            FileName = rulesPath,
            UseShellExecute = true,
        });
    }

    private void LoadSelectedRuleFile()
    {
        var repositoryRoot = ResolveDataRootForRules();
        if (repositoryRoot is null || ruleFileComboBox.SelectedItem is not string ruleFileName)
        {
            ruleEditorTextBox.Text = string.Empty;
            return;
        }

        try
        {
            EnsureRulesInitialized(repositoryRoot, appendToLog: false);
            var rulePath = RuleWorkspace.GetRuleFilePath(repositoryRoot, ruleFileName);
            ruleEditorTextBox.Text = File.ReadAllText(rulePath);
            ruleEditorTextBox.SelectionStart = 0;
            ruleEditorTextBox.SelectionLength = 0;
            statusLabel.Text = $"已加载 {ruleFileName}";
        }
        catch (Exception ex)
        {
            ruleEditorTextBox.Text = string.Empty;
            statusLabel.Text = "加载规则失败";
            AppendLog($"加载规则失败: {ex.Message}");
        }
    }

    private string? ResolveDataRootForRules()
    {
        var repositoryRoot = WorkspaceLocator.FindDataRoot(basePathTextBox.Text.Trim());
        if (repositoryRoot is not null)
        {
            currentRepositoryRoot = repositoryRoot;
        }

        return repositoryRoot;
    }

    private void EnsureRulesInitialized(string repositoryRoot, bool appendToLog)
    {
        RuleWorkspace.EnsureInitialized(repositoryRoot, message =>
        {
            if (appendToLog)
            {
                AppendLog(message);
            }
        });
    }

    private void AppendLog(string message)
    {
        if (logTextBox.TextLength > 0)
        {
            logTextBox.AppendText(Environment.NewLine);
        }

        logTextBox.AppendText(message);
    }

    private void ToggleBusy(bool busy, string? busyText = null)
    {
        generateButton.Enabled = !busy;
        auditButton.Enabled = !busy;
        browseButton.Enabled = !busy;
        openOutputButton.Enabled = !busy;
        openAuditReportsButton.Enabled = !busy;
        reloadRuleButton.Enabled = !busy;
        saveRuleButton.Enabled = !busy;
        openRulesButton.Enabled = !busy;
        ruleFileComboBox.Enabled = !busy;
        ruleEditorTextBox.Enabled = !busy;
        basePathTextBox.Enabled = !busy;
        statusLabel.Text = busy ? (busyText ?? "处理中") : statusLabel.Text;
    }

    private void UpdateStatus()
    {
        if (string.IsNullOrWhiteSpace(currentRepositoryRoot))
        {
            statusLabel.Text = "未定位到仓库根目录";
            return;
        }

        statusLabel.Text = "就绪";
        phaseTextBox.Text = string.Join(Environment.NewLine, new[]
        {
            "已完成:",
            "- 模板加载与模板索引",
            "- 六种输入格式识别",
            "- 最小补丁重建与属性合并",
            "- 按源文件目录结构导出",
            "- GUI 直接调用共享核心生成",
            "- C# 原生输出审计",
            "- 规则外置到 rules/*.json",
            "- GUI 可直接加载/保存外置规则",
            string.Empty,
            "当前约束:",
            "- 数据目录独立于外部仓库",
            "- 默认使用 C# 工程自身的 input/模板/output",
            "- 审计报告输出到 audit_reports",
            string.Empty,
            "后续方向:",
            "- 规则字段级校验与版本化",
            "- GUI 结构化编辑器替代原始 JSON 文本框",
            "- 回归测试继续补齐",
        });
    }
}
