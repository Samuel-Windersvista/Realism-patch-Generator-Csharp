using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using RealismPatchGenerator.Core;
using PatchGenerator = RealismPatchGenerator.Core.RealismPatchGenerator;

namespace RealismPatchGenerator.Gui;

public partial class Form1 : Form
{
    private const string GlobalProfileKey = "__global__";

    private static readonly JsonSerializerOptions RuleJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly Dictionary<string, RuleDocument> ruleDocuments = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RuleRangeEntry> allEntries = [];
    private readonly List<RuleRangeEntry> visibleEntries = [];

    private string? currentRepositoryRoot;
    private UiLanguage currentLanguage = UiLanguage.Chinese;
    private bool isBusy;
    private bool suppressTreeSelection;
    private bool suppressLanguageSelection;
    private string currentStateKey = "State.Ready";
    private RuleEditorSectionDefinition? selectedSection;
    private string? selectedProfileKey;

    public Form1()
    {
        InitializeComponent();
        InitializeLanguageSelector();
        ResolveRepositoryRoot();
        ApplyLanguage();
        LoadRuleWorkspace(selectFirstNode: true, appendReloadLog: false);
    }

    private void InitializeLanguageSelector()
    {
        suppressLanguageSelection = true;
        languageComboBox.Items.Clear();
        languageComboBox.Items.Add(string.Empty);
        languageComboBox.Items.Add(string.Empty);
        languageComboBox.SelectedIndex = 0;
        suppressLanguageSelection = false;
    }

    private void ResolveRepositoryRoot()
    {
        currentRepositoryRoot = WorkspaceLocator.FindApplicationRoot(
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        basePathTextBox.Text = currentRepositoryRoot ?? string.Empty;
        outputPathTextBox.Text = currentRepositoryRoot is not null
            ? Path.Combine(currentRepositoryRoot, "output")
            : string.Empty;
    }

    private void ApplyLanguage()
    {
        Text = T("App.Title");
        titleLabel.Text = T("App.Title");
        pathLabel.Text = T("Label.DataRoot");
        outputPathLabel.Text = T("Label.OutputPath");
        outputHintLabel.Text = T("Message.OutputPathHint");
        browseButton.Text = T("Button.Browse");
        saveAllButton.Text = T("Button.SaveAll");
        reloadButton.Text = T("Button.Reload");
        generateButton.Text = T("Button.Generate");
        auditButton.Text = T("Button.Audit");
        languageLabel.Text = T("Label.Language");
        searchLabel.Text = T("Label.Search");
        explanationTabPage.Text = T("Tab.Explanation");
        logTabPage.Text = T("Tab.Log");
        fieldColumn.HeaderText = T("Column.Field");
        minColumn.HeaderText = T("Column.Min");
        maxColumn.HeaderText = T("Column.Max");
        preferIntColumn.HeaderText = T("Column.PreferInt");
        sourceColumn.HeaderText = T("Column.Source");

        suppressLanguageSelection = true;
        languageComboBox.Items[0] = T("Language.Chinese");
        languageComboBox.Items[1] = T("Language.English");
        languageComboBox.SelectedIndex = currentLanguage == UiLanguage.English ? 1 : 0;
        suppressLanguageSelection = false;

        UpdateGroupTitles();
        RebuildTree(preserveSelection: true, selectFirstNode: false);
        RefreshGrid(preserveSelection: true);
        UpdateExplanation();
        RefreshStatus();
    }

    private string T(string key) => UiTextCatalog.Get(key, currentLanguage);

    private string Tf(string key, params object[] args) => UiTextCatalog.Format(key, currentLanguage, args);

    private async void saveAllButton_Click(object sender, EventArgs e)
    {
        if (!ruleDocuments.Values.Any(document => document.IsDirty))
        {
            SetState("State.Ready");
            RefreshStatus();
            return;
        }

        await SaveAllAsync();
    }

    private void reloadButton_Click(object sender, EventArgs e)
    {
        if (ruleDocuments.Values.Any(document => document.IsDirty))
        {
            var result = MessageBox.Show(
                this,
                T("Message.ReloadConfirm"),
                T("Message.ReloadConfirmTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }
        }

        LoadRuleWorkspace(selectFirstNode: false, appendReloadLog: true);
    }

    private async void generateButton_Click(object sender, EventArgs e)
    {
        var repositoryRoot = ResolveDataRoot();
        if (repositoryRoot is null)
        {
            ShowInvalidDataRootMessage();
            return;
        }

        if (!await ConfirmSaveBeforeRunAsync())
        {
            return;
        }

        ToggleBusy(true, "State.Generating");
        logTextBox.Clear();
        detailTabControl.SelectedTab = logTabPage;
        var outputPath = ResolveOutputPath();
        AppendLog(Tf("Log.StartGenerate", outputPath));

        try
        {
            EnsureRulesInitialized(repositoryRoot, appendToLog: false);
            var result = await Task.Run(() => new PatchGenerator(repositoryRoot).Generate(outputPath));
            foreach (var line in result.Logs)
            {
                AppendLog(line);
            }

            AppendLog(string.Empty);
            AppendLog($"{T("Button.Generate")}: {result.Statistics.TotalCount}");
            AppendLog($"output: {result.OutputPath}");
            SetState("State.GenerateDone");
        }
        catch (Exception ex)
        {
            AppendLog(ex.Message);
            SetState("State.GenerateFailed");
            MessageBox.Show(this, ex.Message, T("Message.GenerateFailedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private async void auditButton_Click(object sender, EventArgs e)
    {
        var repositoryRoot = ResolveDataRoot();
        if (repositoryRoot is null)
        {
            ShowInvalidDataRootMessage();
            return;
        }

        if (!await ConfirmSaveBeforeRunAsync())
        {
            return;
        }

        ToggleBusy(true, "State.Auditing");
        logTextBox.Clear();
        detailTabControl.SelectedTab = logTabPage;
        var outputPath = ResolveOutputPath();
        AppendLog(Tf("Log.StartAudit", outputPath));

        try
        {
            var report = await Task.Run(() => new OutputRuleAuditor(
                repositoryRoot,
                new OutputAuditOptions { OutputDirectory = outputPath }).Audit());
            var summary = OutputRuleAuditor.BuildConsoleSummary(report, 30);
            foreach (var line in summary.Split(Environment.NewLine))
            {
                AppendLog(line);
            }

            var reportsPath = Path.Combine(repositoryRoot, "audit_reports");
            Directory.CreateDirectory(reportsPath);
            var reportPath = Path.Combine(reportsPath, "output_rule_audit.json");
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, RuleJsonOptions));
            AppendLog($"report: {reportPath}");
            SetState(report.ViolationCount > 0 ? "State.AuditDoneViolation" : "State.AuditDone");
        }
        catch (Exception ex)
        {
            AppendLog(ex.Message);
            SetState("State.AuditFailed");
            MessageBox.Show(this, ex.Message, T("Message.AuditFailedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            Description = T("Button.Browse"),
            UseDescriptionForTitle = true,
            InitialDirectory = Directory.Exists(outputPathTextBox.Text) ? outputPathTextBox.Text : Directory.GetCurrentDirectory(),
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        outputPathTextBox.Text = dialog.SelectedPath;
    }

    private void searchTextBox_TextChanged(object sender, EventArgs e)
    {
        RefreshGrid(preserveSelection: false);
    }

    private void ruleTreeView_AfterSelect(object sender, TreeViewEventArgs e)
    {
        if (suppressTreeSelection)
        {
            return;
        }

        if (e.Node?.Tag is not RuleTreeSelection selection)
        {
            selectedSection = null;
            selectedProfileKey = null;
        }
        else
        {
            selectedSection = selection.Section;
            selectedProfileKey = selection.ProfileKey;
        }

        RefreshGrid(preserveSelection: false);
        UpdateExplanation();
    }

    private void ruleGridView_SelectionChanged(object sender, EventArgs e)
    {
        UpdateExplanation();
    }

    private void ruleGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
    {
        if (e.RowIndex < 0 || (e.ColumnIndex != minColumn.Index && e.ColumnIndex != maxColumn.Index))
        {
            return;
        }

        if (!TryParseDoubleFlexible(Convert.ToString(e.FormattedValue, CultureInfo.CurrentCulture), out var editedValue))
        {
            ShowInfoMessage(T("Message.ParseNumber"), T("Message.SaveFailedTitle"));
            e.Cancel = true;
            return;
        }

        var row = ruleGridView.Rows[e.RowIndex];
        if (row.Tag is not RuleRangeEntry entry)
        {
            return;
        }

        var currentMin = e.ColumnIndex == minColumn.Index ? editedValue : entry.MinValue;
        var currentMax = e.ColumnIndex == maxColumn.Index ? editedValue : entry.MaxValue;
        if (currentMin > currentMax)
        {
            ShowInfoMessage(T("Message.MinGreaterThanMax"), T("Message.SaveFailedTitle"));
            e.Cancel = true;
        }
    }

    private void ruleGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || (e.ColumnIndex != minColumn.Index && e.ColumnIndex != maxColumn.Index))
        {
            return;
        }

        var row = ruleGridView.Rows[e.RowIndex];
        if (row.Tag is not RuleRangeEntry entry)
        {
            return;
        }

        if (!TryParseDoubleFlexible(Convert.ToString(row.Cells[minColumn.Index].Value, CultureInfo.CurrentCulture), out var minValue)
            || !TryParseDoubleFlexible(Convert.ToString(row.Cells[maxColumn.Index].Value, CultureInfo.CurrentCulture), out var maxValue))
        {
            row.Cells[minColumn.Index].Value = RuleEditorCatalog.FormatNumber(entry.MinValue, entry.PreferInt);
            row.Cells[maxColumn.Index].Value = RuleEditorCatalog.FormatNumber(entry.MaxValue, entry.PreferInt);
            return;
        }

        if (Math.Abs(minValue - entry.MinValue) < 1e-12 && Math.Abs(maxValue - entry.MaxValue) < 1e-12)
        {
            return;
        }

        entry.Update(minValue, maxValue);
        ApplyRowValues(row, entry);
        RefreshStatus();
        UpdateExplanation();
    }

    private void languageComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (suppressLanguageSelection)
        {
            return;
        }

        currentLanguage = languageComboBox.SelectedIndex == 1 ? UiLanguage.English : UiLanguage.Chinese;
        ApplyLanguage();
    }

    private async Task<bool> ConfirmSaveBeforeRunAsync()
    {
        if (!ruleDocuments.Values.Any(document => document.IsDirty))
        {
            return true;
        }

        var message = currentLanguage == UiLanguage.English
            ? "You have unsaved rule changes. Select Yes to save before running, No to run with rule files currently on disk, or Cancel to abort."
            : "当前有未保存的规则修改。选择“是”先保存再执行，选择“否”将直接使用磁盘上现有规则执行，选择“取消”中止。";
        var title = currentLanguage == UiLanguage.English ? "Unsaved Changes" : "未保存修改";
        var result = MessageBox.Show(this, message, title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

        return result switch
        {
            DialogResult.Yes => await SaveAllAsync(),
            DialogResult.No => true,
            _ => false,
        };
    }

    private async Task<bool> SaveAllAsync()
    {
        var repositoryRoot = ResolveDataRoot();
        if (repositoryRoot is null)
        {
            ShowInvalidDataRootMessage();
            return false;
        }

        try
        {
            ToggleBusy(true, "State.Saving");
            ruleGridView.EndEdit();
            await Task.Run(SaveAllDocuments);
            LoadRuleWorkspace(selectFirstNode: false, appendReloadLog: false);
            SetState("State.Saved");
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, T("Message.SaveFailedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private void SaveAllDocuments()
    {
        foreach (var document in ruleDocuments.Values.Where(document => document.IsDirty))
        {
            if (!RuleWorkspace.TryNormalizeRuleFile(document.FileName, document.Root.ToJsonString(RuleJsonOptions), out var normalizedJson, out var errorMessage))
            {
                throw new InvalidOperationException($"{document.FileName}: {errorMessage}");
            }

            File.WriteAllText(document.FilePath, normalizedJson);
            BeginInvoke(() => AppendLog(Tf("Log.SaveSuccess", document.FileName)));
        }
    }

    private void LoadRuleWorkspace(bool selectFirstNode, bool appendReloadLog)
    {
        var repositoryRoot = ResolveDataRoot();
        if (repositoryRoot is null)
        {
            ClearLoadedRules();
            SetState("State.InvalidRoot");
            RefreshStatus();
            return;
        }

        try
        {
            EnsureRulesInitialized(repositoryRoot, appendToLog: false);
            var documents = LoadDocuments(repositoryRoot);
            var entries = BuildEntries(documents);

            ruleDocuments.Clear();
            foreach (var pair in documents)
            {
                ruleDocuments[pair.Key] = pair.Value;
            }

            allEntries.Clear();
            allEntries.AddRange(entries);
            RebuildTree(preserveSelection: !selectFirstNode, selectFirstNode: selectFirstNode);
            RefreshGrid(preserveSelection: true);
            if (appendReloadLog)
            {
                AppendLog(T("Log.ReloadSuccess"));
            }

            SetState("State.Loaded");
        }
        catch (Exception ex)
        {
            ClearLoadedRules();
            SetState("State.InvalidRoot");
            MessageBox.Show(this, ex.Message, T("Message.LoadFailedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            RefreshStatus();
            UpdateExplanation();
        }
    }

    private Dictionary<string, RuleDocument> LoadDocuments(string repositoryRoot)
    {
        var documents = new Dictionary<string, RuleDocument>(StringComparer.OrdinalIgnoreCase);
        foreach (var fileName in RuleEditorCatalog.Sections.Select(section => section.FileName).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var filePath = RuleWorkspace.GetRuleFilePath(repositoryRoot, fileName);
            var root = JsonNode.Parse(File.ReadAllText(filePath))?.AsObject()
                ?? throw new InvalidOperationException($"{fileName} is not a valid JSON object.");
            documents[fileName] = new RuleDocument
            {
                FileName = fileName,
                FilePath = filePath,
                Root = root,
                IsDirty = false,
            };
        }

        return documents;
    }

    private static List<RuleRangeEntry> BuildEntries(IReadOnlyDictionary<string, RuleDocument> documents)
    {
        var entries = new List<RuleRangeEntry>();
        foreach (var section in RuleEditorCatalog.Sections)
        {
            if (!documents.TryGetValue(section.FileName, out var document))
            {
                continue;
            }

            if (document.Root[section.SectionProperty] is not JsonObject sectionObject)
            {
                continue;
            }

            if (section.IsFlatRangeMap)
            {
                foreach (var fieldPair in sectionObject.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (fieldPair.Value is not JsonObject rangeNode)
                    {
                        continue;
                    }

                    entries.Add(CreateEntry(document, section, GlobalProfileKey, fieldPair.Key, rangeNode));
                }

                continue;
            }

            foreach (var profilePair in sectionObject.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (profilePair.Value is not JsonObject profileObject)
                {
                    continue;
                }

                foreach (var fieldPair in profileObject.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (fieldPair.Value is not JsonObject rangeNode)
                    {
                        continue;
                    }

                    entries.Add(CreateEntry(document, section, profilePair.Key, fieldPair.Key, rangeNode));
                }
            }
        }

        return entries;
    }

    private static RuleRangeEntry CreateEntry(RuleDocument document, RuleEditorSectionDefinition section, string profileKey, string fieldKey, JsonObject rangeNode)
    {
        var minValue = rangeNode["min"]?.GetValue<double>() ?? 0.0;
        var maxValue = rangeNode["max"]?.GetValue<double>() ?? 0.0;
        var preferInt = rangeNode["preferInt"]?.GetValue<bool>() ?? false;

        var entry = new RuleRangeEntry
        {
            Document = document,
            Section = section,
            ProfileKey = profileKey,
            FieldKey = fieldKey,
            RangeNode = rangeNode,
            PreferInt = preferInt,
        };
        entry.Initialize(minValue, maxValue);
        return entry;
    }

    private void RebuildTree(bool preserveSelection, bool selectFirstNode)
    {
        var selected = preserveSelection && ruleTreeView.SelectedNode?.Tag is RuleTreeSelection selection
            ? selection
            : null;

        suppressTreeSelection = true;
        ruleTreeView.BeginUpdate();
        ruleTreeView.Nodes.Clear();

        foreach (var section in RuleEditorCatalog.Sections)
        {
            var sectionEntryCount = allEntries.Count(entry => entry.Section.Key == section.Key);
            var sectionNode = new TreeNode(section.DisplayName.Get(currentLanguage))
            {
                Tag = new RuleTreeSelection(section, null),
            };
            sectionNode.Text = Tf("Tree.SectionNode", section.DisplayName.Get(currentLanguage), sectionEntryCount);

            var profileKeys = allEntries
                .Where(entry => entry.Section.Key == section.Key)
                .Select(entry => entry.ProfileKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var profileKey in profileKeys)
            {
                sectionNode.Nodes.Add(new TreeNode(DisplayProfileKey(profileKey))
                {
                    Tag = new RuleTreeSelection(section, profileKey),
                });
            }

            ruleTreeView.Nodes.Add(sectionNode);
        }

        ruleTreeView.ExpandAll();
        ruleTreeView.EndUpdate();
        UpdateGroupTitles();

        TreeNode? nodeToSelect = null;
        if (selected is not null)
        {
            nodeToSelect = FindTreeNode(selected.Section.Key, selected.ProfileKey);
        }

        if (nodeToSelect is null && selectFirstNode && ruleTreeView.Nodes.Count > 0)
        {
            nodeToSelect = ruleTreeView.Nodes[0];
        }

        suppressTreeSelection = false;
        if (nodeToSelect is not null)
        {
            ruleTreeView.SelectedNode = nodeToSelect;
        }
    }

    private TreeNode? FindTreeNode(string sectionKey, string? profileKey)
    {
        foreach (TreeNode sectionNode in ruleTreeView.Nodes)
        {
            if (sectionNode.Tag is RuleTreeSelection sectionSelection
                && string.Equals(sectionSelection.Section.Key, sectionKey, StringComparison.OrdinalIgnoreCase)
                && profileKey is null)
            {
                return sectionNode;
            }

            foreach (TreeNode childNode in sectionNode.Nodes)
            {
                if (childNode.Tag is RuleTreeSelection childSelection
                    && string.Equals(childSelection.Section.Key, sectionKey, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(childSelection.ProfileKey, profileKey, StringComparison.OrdinalIgnoreCase))
                {
                    return childNode;
                }
            }
        }

        return null;
    }

    private void RefreshGrid(bool preserveSelection)
    {
        var previousSelection = preserveSelection ? GetSelectedEntryKey() : null;
        visibleEntries.Clear();
        visibleEntries.AddRange(allEntries.Where(MatchesSelectionAndSearch));

        ruleGridView.Rows.Clear();
        foreach (var entry in visibleEntries)
        {
            var rowIndex = ruleGridView.Rows.Add();
            var row = ruleGridView.Rows[rowIndex];
            row.Tag = entry;
            ApplyRowValues(row, entry);
        }

        if (previousSelection is not null)
        {
            foreach (DataGridViewRow row in ruleGridView.Rows)
            {
                if (row.Tag is RuleRangeEntry entry
                    && string.Equals(entry.Section.Key, previousSelection.Value.SectionKey, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(entry.ProfileKey, previousSelection.Value.ProfileKey, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(entry.FieldKey, previousSelection.Value.FieldKey, StringComparison.OrdinalIgnoreCase))
                {
                    row.Selected = true;
                    ruleGridView.CurrentCell = row.Cells[minColumn.Index];
                    break;
                }
            }
        }

        if (ruleGridView.SelectedRows.Count == 0 && ruleGridView.Rows.Count > 0)
        {
            ruleGridView.Rows[0].Selected = true;
            ruleGridView.CurrentCell = ruleGridView.Rows[0].Cells[minColumn.Index];
        }

        UpdateGroupTitles();
        RefreshStatus();
        UpdateExplanation();
    }

    private void ApplyRowValues(DataGridViewRow row, RuleRangeEntry entry)
    {
        var fieldHelp = RuleEditorCatalog.GetFieldHelp(entry.FieldKey);
        row.Cells[fieldColumn.Index].Value = fieldHelp.DisplayName.Get(currentLanguage);
        row.Cells[minColumn.Index].Value = RuleEditorCatalog.FormatNumber(entry.MinValue, entry.PreferInt);
        row.Cells[maxColumn.Index].Value = RuleEditorCatalog.FormatNumber(entry.MaxValue, entry.PreferInt);
        row.Cells[preferIntColumn.Index].Value = entry.PreferInt;
        row.Cells[sourceColumn.Index].Value = entry.Section.SourceLabel.Get(currentLanguage);
        row.DefaultCellStyle.BackColor = entry.IsDirty ? Color.FromArgb(255, 248, 220) : SystemColors.Window;
        row.DefaultCellStyle.ForeColor = entry.IsDirty ? Color.DarkOrange : SystemColors.ControlText;
    }

    private bool MatchesSelectionAndSearch(RuleRangeEntry entry)
    {
        if (selectedSection is not null && !string.Equals(entry.Section.Key, selectedSection.Key, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(selectedProfileKey) && !string.Equals(entry.ProfileKey, selectedProfileKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var query = searchTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var fieldHelp = RuleEditorCatalog.GetFieldHelp(entry.FieldKey);
        return entry.ProfileKey.Contains(query, StringComparison.OrdinalIgnoreCase)
            || entry.FieldKey.Contains(query, StringComparison.OrdinalIgnoreCase)
            || fieldHelp.DisplayName.Chinese.Contains(query, StringComparison.OrdinalIgnoreCase)
            || fieldHelp.DisplayName.English.Contains(query, StringComparison.OrdinalIgnoreCase)
            || entry.Section.DisplayName.Chinese.Contains(query, StringComparison.OrdinalIgnoreCase)
            || entry.Section.DisplayName.English.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateExplanation()
    {
        var selectedEntry = GetSelectedEntry();
        if (selectedEntry is null)
        {
            if (selectedSection is null)
            {
                explanationTextBox.Text = T("Message.ExplanationHint");
                return;
            }

            explanationTextBox.Text = string.Join(Environment.NewLine, new[]
            {
                $"{T("Explanation.Section")}: {selectedSection.DisplayName.Get(currentLanguage)}",
                string.Empty,
                $"{T("Explanation.Description")}: {selectedSection.Description.Get(currentLanguage)}",
                string.Empty,
                T("Message.NoFieldSelected"),
            });
            return;
        }

        var help = RuleEditorCatalog.GetFieldHelp(selectedEntry.FieldKey);
        explanationTextBox.Text = string.Join(Environment.NewLine, new[]
        {
            $"{T("Explanation.Section")}: {selectedEntry.Section.DisplayName.Get(currentLanguage)}",
            $"{T("Explanation.Profile")}: {DisplayProfileKey(selectedEntry.ProfileKey)}",
            $"{T("Explanation.Field")}: {help.DisplayName.Get(currentLanguage)} ({selectedEntry.FieldKey})",
            $"{T("Explanation.CurrentRange")}: {RuleEditorCatalog.FormatNumber(selectedEntry.MinValue, selectedEntry.PreferInt)} - {RuleEditorCatalog.FormatNumber(selectedEntry.MaxValue, selectedEntry.PreferInt)}",
            $"{T("Explanation.IntegerMode")}: {(selectedEntry.PreferInt ? T("Label.Yes") : T("Label.No"))}",
            $"{T("Explanation.Modified")}: {(selectedEntry.IsDirty ? T("Explanation.ModifiedYes") : T("Explanation.ModifiedNo"))}",
            string.Empty,
            $"{T("Explanation.Description")}: {selectedEntry.Section.Description.Get(currentLanguage)}",
            string.Empty,
            $"{T("Explanation.FieldEffect")}: {help.Effect.Get(currentLanguage)}",
            string.Empty,
            $"{T("Explanation.DirectionHint")}: {help.DirectionHint.Get(currentLanguage)}",
        });
    }

    private RuleRangeEntry? GetSelectedEntry()
    {
        return ruleGridView.SelectedRows.Count > 0 ? ruleGridView.SelectedRows[0].Tag as RuleRangeEntry : null;
    }

    private (string SectionKey, string ProfileKey, string FieldKey)? GetSelectedEntryKey()
    {
        var entry = GetSelectedEntry();
        return entry is null ? null : (entry.Section.Key, entry.ProfileKey, entry.FieldKey);
    }

    private string DisplayProfileKey(string profileKey)
    {
        return string.Equals(profileKey, GlobalProfileKey, StringComparison.OrdinalIgnoreCase)
            ? T("Label.ProfileGlobal")
            : RuleEditorCatalog.GetProfileDisplayName(profileKey, currentLanguage);
    }

    private string? ResolveDataRoot()
    {
        return currentRepositoryRoot;
    }

    private string ResolveOutputPath()
    {
        var outputPath = outputPathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return currentRepositoryRoot is not null
                ? Path.Combine(currentRepositoryRoot, "output")
                : string.Empty;
        }

        return Path.GetFullPath(outputPath);
    }

    private void EnsureRulesInitialized(string repositoryRoot, bool appendToLog)
    {
        RuleWorkspace.EnsureInitialized(repositoryRoot, message =>
        {
            if (appendToLog)
            {
                BeginInvoke(() => AppendLog(message));
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

    private void ToggleBusy(bool busy, string? stateKey = null)
    {
        isBusy = busy;
        browseButton.Enabled = !busy;
        saveAllButton.Enabled = !busy;
        reloadButton.Enabled = !busy;
        generateButton.Enabled = !busy && ResolveDataRoot() is not null;
        auditButton.Enabled = !busy && ResolveDataRoot() is not null;
        searchTextBox.Enabled = !busy;
        outputPathTextBox.Enabled = !busy;
        ruleTreeView.Enabled = !busy;
        ruleGridView.Enabled = !busy;
        languageComboBox.Enabled = !busy;
        if (stateKey is not null)
        {
            SetState(stateKey);
        }
        else
        {
            RefreshStatus();
        }
    }

    private void RefreshStatus()
    {
        totalItemsStatusLabel.Text = Tf("Status.TotalItems", allEntries.Count);
        visibleItemsStatusLabel.Text = Tf("Status.VisibleItems", visibleEntries.Count);
        dirtyItemsStatusLabel.Text = Tf("Status.DirtyItems", ruleDocuments.Values.Count(document => document.IsDirty));
        stateStatusLabel.Text = ResolveStateText();
        if (!isBusy && ResolveDataRoot() is null)
        {
            stateStatusLabel.Text = T("State.InvalidRoot");
        }

        UpdateGroupTitles();
    }

    private void SetState(string stateKey)
    {
        currentStateKey = stateKey;
        stateStatusLabel.Text = ResolveStateText();
    }

    private string ResolveStateText()
    {
        if (!isBusy && ResolveDataRoot() is null)
        {
            return T("State.InvalidRoot");
        }

        return T(currentStateKey);
    }

    private void ClearLoadedRules()
    {
        ruleDocuments.Clear();
        allEntries.Clear();
        visibleEntries.Clear();
        ruleTreeView.Nodes.Clear();
        ruleGridView.Rows.Clear();
        selectedSection = null;
        selectedProfileKey = null;
        explanationTextBox.Text = T("Message.ExplanationHint");
        UpdateGroupTitles();
    }

    private void UpdateGroupTitles()
    {
        navigationGroupBox.Text = Tf("Group.NavigationWithCount", RuleEditorCatalog.Sections.Count);

        if (selectedSection is null)
        {
            gridGroupBox.Text = Tf("Group.GridAll", visibleEntries.Count);
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedProfileKey))
        {
            gridGroupBox.Text = Tf("Group.GridSection", selectedSection.DisplayName.Get(currentLanguage), visibleEntries.Count);
            return;
        }

        gridGroupBox.Text = Tf(
            "Group.GridProfile",
            selectedSection.DisplayName.Get(currentLanguage),
            DisplayProfileKey(selectedProfileKey),
            visibleEntries.Count);
    }

    private void ShowInvalidDataRootMessage()
    {
        MessageBox.Show(this, T("Message.InvalidDataRoot"), T("Message.InvalidDataRootTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ShowInfoMessage(string message, string title)
    {
        MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private static bool TryParseDoubleFlexible(string? text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)
            || double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value);
    }

    private sealed record RuleTreeSelection(RuleEditorSectionDefinition Section, string? ProfileKey);
}
