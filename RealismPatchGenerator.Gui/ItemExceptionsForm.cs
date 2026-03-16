using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using RealismPatchGenerator.Core;

namespace RealismPatchGenerator.Gui;

internal sealed class ItemExceptionsForm : Form
{
    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string repositoryRoot;
    private readonly string outputPath;
    private readonly UiLanguage language;
    private readonly BindingList<SearchResultRow> searchResults = [];
    private readonly BindingList<SavedItemRow> savedItems = [];
    private readonly BindingList<EditableFieldRow> fieldRows = [];

    private ItemExceptionDocument document = new();
    private string currentItemId = string.Empty;
    private string currentSourceFile = string.Empty;
    private string currentOrigin = string.Empty;
    private ItemExceptionFieldCategory currentCategory = ItemExceptionFieldCategory.Unknown;
    private bool currentDirty;
    private bool suppressFieldSelectionChanged;
    private bool suppressEditorChanges;

    private readonly Label introLabel = new();
    private readonly SplitContainer mainSplitContainer = new();
    private readonly SplitContainer leftSplitContainer = new();
    private readonly GroupBox searchGroupBox = new();
    private readonly Panel searchPanel = new();
    private readonly Label searchNoteLabel = new();
    private readonly ComboBox sourceComboBox = new();
    private readonly TextBox searchTextBox = new();
    private readonly Button searchButton = new();
    private readonly Button loadSearchResultButton = new();
    private readonly DataGridView searchResultsGridView = new();
    private readonly GroupBox savedItemsGroupBox = new();
    private readonly Panel savedItemsPanel = new();
    private readonly Button loadSavedItemButton = new();
    private readonly DataGridView savedItemsGridView = new();
    private readonly GroupBox editorGroupBox = new();
    private readonly TableLayoutPanel editorLayout = new();
    private readonly Panel itemInfoPanel = new();
    private readonly Label itemIdLabel = new();
    private readonly TextBox itemIdTextBox = new();
    private readonly Label itemNameLabel = new();
    private readonly TextBox itemNameTextBox = new();
    private readonly Label sourceFileLabel = new();
    private readonly TextBox sourceFileTextBox = new();
    private readonly Label originLabel = new();
    private readonly TextBox originTextBox = new();
    private readonly CheckBox enabledCheckBox = new();
    private readonly Label notesLabel = new();
    private readonly TextBox notesTextBox = new();
    private readonly DataGridView fieldsGridView = new();
    private readonly Panel fieldEditorPanel = new();
    private readonly Label fieldNameLabel = new();
    private readonly ComboBox fieldNameComboBox = new();
    private readonly Label fieldValueLabel = new();
    private readonly TextBox fieldValueTextBox = new();
    private readonly Label guidanceLabel = new();
    private readonly Label editorHelpLabel = new();
    private readonly Button addNewFieldButton = new();
    private readonly Button removeFieldButton = new();
    private readonly Button saveItemButton = new();
    private readonly Button deleteItemButton = new();
    private readonly Button closeButton = new();

    public ItemExceptionsForm(string repositoryRoot, string outputPath, UiLanguage language)
    {
        this.repositoryRoot = repositoryRoot;
        this.outputPath = outputPath;
        this.language = language;

        InitializeComponent();
        LoadDocument();
        PopulateKnownFieldNames();
    }

    public bool SavedToDisk { get; private set; }

    private void InitializeComponent()
    {
        SuspendLayout();

        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1440, 860);
        MinimumSize = new Size(1260, 760);
        StartPosition = FormStartPosition.CenterParent;
        Text = T("Title");

        introLabel.Dock = DockStyle.Top;
        introLabel.Height = 44;
        introLabel.Padding = new Padding(12, 10, 12, 0);
        introLabel.Text = T("Intro");

        mainSplitContainer.Dock = DockStyle.Fill;
        mainSplitContainer.SplitterDistance = 500;

        leftSplitContainer.Dock = DockStyle.Fill;
        leftSplitContainer.Orientation = Orientation.Horizontal;
        leftSplitContainer.SplitterDistance = 410;

        searchGroupBox.Dock = DockStyle.Fill;
        searchGroupBox.Padding = new Padding(8);
        searchGroupBox.Text = T("SearchGroup");

        searchPanel.Dock = DockStyle.Top;
        searchPanel.Height = 72;

        sourceComboBox.Visible = false;

        searchTextBox.Location = new Point(8, 10);
        searchTextBox.Size = new Size(300, 23);

        searchButton.Location = new Point(316, 9);
        searchButton.Size = new Size(110, 26);
        searchButton.Text = T("Search");
        searchButton.UseVisualStyleBackColor = true;
        searchButton.Click += searchButton_Click;

        searchNoteLabel.AutoSize = true;
        searchNoteLabel.Location = new Point(8, 44);
        searchNoteLabel.Size = new Size(420, 18);
        searchNoteLabel.Text = T("SearchNote");

        loadSearchResultButton.Location = new Point(320, 40);
        loadSearchResultButton.Size = new Size(152, 26);
        loadSearchResultButton.Text = T("LoadSelectedSearch");
        loadSearchResultButton.UseVisualStyleBackColor = true;
        loadSearchResultButton.Click += loadSearchResultButton_Click;

        searchPanel.Controls.Add(searchTextBox);
        searchPanel.Controls.Add(searchButton);
        searchPanel.Controls.Add(searchNoteLabel);
        searchPanel.Controls.Add(loadSearchResultButton);

        searchResultsGridView.AllowUserToAddRows = false;
        searchResultsGridView.AllowUserToDeleteRows = false;
        searchResultsGridView.AutoGenerateColumns = false;
        searchResultsGridView.BackgroundColor = SystemColors.Window;
        searchResultsGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        searchResultsGridView.Dock = DockStyle.Fill;
        searchResultsGridView.MultiSelect = false;
        searchResultsGridView.RowHeadersVisible = false;
        searchResultsGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        searchResultsGridView.DataSource = searchResults;
        searchResultsGridView.CellDoubleClick += searchResultsGridView_CellDoubleClick;

        searchResultsGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SearchResultRow.Name),
            HeaderText = T("Column.Name"),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 170,
            ReadOnly = true,
        });
        searchResultsGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SearchResultRow.ItemId),
            HeaderText = T("Column.ItemId"),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 140,
            ReadOnly = true,
        });
        searchResultsGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SearchResultRow.SourceFile),
            HeaderText = T("Column.SourceFile"),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 140,
            ReadOnly = true,
        });

        searchGroupBox.Controls.Add(searchResultsGridView);
        searchGroupBox.Controls.Add(searchPanel);

        savedItemsGroupBox.Dock = DockStyle.Fill;
        savedItemsGroupBox.Padding = new Padding(8);
        savedItemsGroupBox.Text = T("SavedGroup");

        savedItemsPanel.Dock = DockStyle.Top;
        savedItemsPanel.Height = 40;

        loadSavedItemButton.Location = new Point(8, 6);
        loadSavedItemButton.Size = new Size(152, 26);
        loadSavedItemButton.Text = T("LoadSelectedSaved");
        loadSavedItemButton.UseVisualStyleBackColor = true;
        loadSavedItemButton.Click += loadSavedItemButton_Click;

        savedItemsPanel.Controls.Add(loadSavedItemButton);

        savedItemsGridView.AllowUserToAddRows = false;
        savedItemsGridView.AllowUserToDeleteRows = false;
        savedItemsGridView.AutoGenerateColumns = false;
        savedItemsGridView.BackgroundColor = SystemColors.Window;
        savedItemsGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        savedItemsGridView.Dock = DockStyle.Fill;
        savedItemsGridView.MultiSelect = false;
        savedItemsGridView.RowHeadersVisible = false;
        savedItemsGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        savedItemsGridView.DataSource = savedItems;
        savedItemsGridView.CellDoubleClick += savedItemsGridView_CellDoubleClick;

        savedItemsGridView.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(SavedItemRow.Enabled),
            HeaderText = T("Column.Enabled"),
            Width = 60,
            ReadOnly = true,
        });
        savedItemsGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SavedItemRow.Name),
            HeaderText = T("Column.Name"),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 170,
            ReadOnly = true,
        });
        savedItemsGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SavedItemRow.ItemId),
            HeaderText = T("Column.ItemId"),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 140,
            ReadOnly = true,
        });
        savedItemsGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(SavedItemRow.FieldCountText),
            HeaderText = T("Column.OverrideCount"),
            Width = 70,
            ReadOnly = true,
        });

        savedItemsGroupBox.Controls.Add(savedItemsGridView);
        savedItemsGroupBox.Controls.Add(savedItemsPanel);

        leftSplitContainer.Panel1.Controls.Add(searchGroupBox);
        leftSplitContainer.Panel2.Controls.Add(savedItemsGroupBox);

        editorGroupBox.Dock = DockStyle.Fill;
        editorGroupBox.Padding = new Padding(8);
        editorGroupBox.Text = T("EditorGroup");

        editorLayout.ColumnCount = 1;
        editorLayout.RowCount = 3;
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108F));
        editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 236F));
        editorLayout.Dock = DockStyle.Fill;

        itemInfoPanel.Dock = DockStyle.Fill;

        itemIdLabel.AutoSize = true;
        itemIdLabel.Location = new Point(8, 12);
        itemIdLabel.Text = T("Label.ItemId");

        itemIdTextBox.Location = new Point(90, 9);
        itemIdTextBox.ReadOnly = true;
        itemIdTextBox.Size = new Size(250, 23);

        itemNameLabel.AutoSize = true;
        itemNameLabel.Location = new Point(356, 12);
        itemNameLabel.Text = T("Label.Name");

        itemNameTextBox.Location = new Point(438, 9);
        itemNameTextBox.ReadOnly = true;
        itemNameTextBox.Size = new Size(300, 23);

        sourceFileLabel.AutoSize = true;
        sourceFileLabel.Location = new Point(8, 45);
        sourceFileLabel.Text = T("Label.SourceFile");

        sourceFileTextBox.Location = new Point(90, 42);
        sourceFileTextBox.ReadOnly = true;
        sourceFileTextBox.Size = new Size(410, 23);

        originLabel.AutoSize = true;
        originLabel.Location = new Point(516, 45);
        originLabel.Text = T("Label.Origin");

        originTextBox.Location = new Point(568, 42);
        originTextBox.ReadOnly = true;
        originTextBox.Size = new Size(170, 23);

        enabledCheckBox.AutoSize = true;
        enabledCheckBox.Location = new Point(8, 76);
        enabledCheckBox.Text = T("Label.Enabled");
        enabledCheckBox.Checked = true;
        enabledCheckBox.CheckedChanged += currentItemMetadata_Changed;

        notesLabel.AutoSize = true;
        notesLabel.Location = new Point(8, 77);
        notesLabel.Text = T("Label.Notes");

        notesTextBox.Location = new Point(438, 74);
        notesTextBox.Multiline = false;
        notesTextBox.Size = new Size(300, 23);
        notesTextBox.TextChanged += currentItemMetadata_Changed;

        itemInfoPanel.Controls.Add(itemIdLabel);
        itemInfoPanel.Controls.Add(itemIdTextBox);
        itemInfoPanel.Controls.Add(itemNameLabel);
        itemInfoPanel.Controls.Add(itemNameTextBox);
        itemInfoPanel.Controls.Add(sourceFileLabel);
        itemInfoPanel.Controls.Add(sourceFileTextBox);
        itemInfoPanel.Controls.Add(originLabel);
        itemInfoPanel.Controls.Add(originTextBox);
        itemInfoPanel.Controls.Add(enabledCheckBox);
        itemInfoPanel.Controls.Add(notesLabel);
        itemInfoPanel.Controls.Add(notesTextBox);

        fieldsGridView.AllowUserToAddRows = false;
        fieldsGridView.AllowUserToDeleteRows = false;
        fieldsGridView.AutoGenerateColumns = false;
        fieldsGridView.BackgroundColor = SystemColors.Window;
        fieldsGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        fieldsGridView.Dock = DockStyle.Fill;
        fieldsGridView.MultiSelect = false;
        fieldsGridView.RowHeadersVisible = false;
        fieldsGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        fieldsGridView.DataSource = fieldRows;
        fieldsGridView.SelectionChanged += fieldsGridView_SelectionChanged;

        fieldsGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(EditableFieldRow.FieldName),
            HeaderText = T("Column.Field"),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 150,
            ReadOnly = true,
        });
        fieldsGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(EditableFieldRow.ValueType),
            HeaderText = T("Column.ValueType"),
            Width = 95,
            ReadOnly = true,
        });
        fieldsGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(EditableFieldRow.Preview),
            HeaderText = T("Column.Preview"),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 240,
            ReadOnly = true,
        });

        fieldEditorPanel.Dock = DockStyle.Fill;

        fieldNameLabel.AutoSize = true;
        fieldNameLabel.Location = new Point(8, 12);
        fieldNameLabel.Text = T("Label.Field");

        fieldNameComboBox.Location = new Point(90, 9);
        fieldNameComboBox.Size = new Size(320, 25);
        fieldNameComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        fieldNameComboBox.SelectedIndexChanged += fieldNameComboBox_SelectedIndexChanged;
        fieldNameComboBox.TextChanged += fieldEditorControl_Changed;

        fieldValueLabel.AutoSize = true;
        fieldValueLabel.Location = new Point(8, 45);
        fieldValueLabel.Text = T("Label.FieldValue");

        fieldValueTextBox.Location = new Point(90, 42);
        fieldValueTextBox.Multiline = true;
        fieldValueTextBox.ScrollBars = ScrollBars.Both;
        fieldValueTextBox.WordWrap = false;
        fieldValueTextBox.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        fieldValueTextBox.Size = new Size(648, 116);
        fieldValueTextBox.TextChanged += fieldEditorControl_Changed;

        guidanceLabel.AutoSize = true;
        guidanceLabel.MaximumSize = new Size(730, 0);
        guidanceLabel.Location = new Point(8, 168);
        guidanceLabel.Text = T("Guidance.Empty");

        editorHelpLabel.AutoSize = true;
        editorHelpLabel.MaximumSize = new Size(452, 0);
        editorHelpLabel.Location = new Point(8, 194);
        editorHelpLabel.Text = T("EditorHelp");

        addNewFieldButton.Location = new Point(470, 8);
        addNewFieldButton.Size = new Size(104, 28);
        addNewFieldButton.Text = T("AddField");
        addNewFieldButton.UseVisualStyleBackColor = true;
        addNewFieldButton.Click += addNewFieldButton_Click;

        removeFieldButton.Location = new Point(470, 194);
        removeFieldButton.Size = new Size(104, 28);
        removeFieldButton.Text = T("RemoveField");
        removeFieldButton.UseVisualStyleBackColor = true;
        removeFieldButton.Click += removeFieldButton_Click;

        saveItemButton.Location = new Point(582, 194);
        saveItemButton.Size = new Size(118, 28);
        saveItemButton.Text = T("SaveItem");
        saveItemButton.UseVisualStyleBackColor = true;
        saveItemButton.Click += saveItemButton_Click;

        deleteItemButton.Location = new Point(708, 194);
        deleteItemButton.Size = new Size(104, 28);
        deleteItemButton.Text = T("DeleteItem");
        deleteItemButton.UseVisualStyleBackColor = true;
        deleteItemButton.Click += deleteItemButton_Click;

        closeButton.Location = new Point(820, 194);
        closeButton.Size = new Size(96, 28);
        closeButton.Text = T("Close");
        closeButton.UseVisualStyleBackColor = true;
        closeButton.Click += closeButton_Click;

        fieldEditorPanel.Controls.Add(fieldNameLabel);
        fieldEditorPanel.Controls.Add(fieldNameComboBox);
        fieldEditorPanel.Controls.Add(addNewFieldButton);
        fieldEditorPanel.Controls.Add(fieldValueLabel);
        fieldEditorPanel.Controls.Add(fieldValueTextBox);
        fieldEditorPanel.Controls.Add(guidanceLabel);
        fieldEditorPanel.Controls.Add(editorHelpLabel);
        fieldEditorPanel.Controls.Add(removeFieldButton);
        fieldEditorPanel.Controls.Add(saveItemButton);
        fieldEditorPanel.Controls.Add(deleteItemButton);
        fieldEditorPanel.Controls.Add(closeButton);

        editorLayout.Controls.Add(itemInfoPanel, 0, 0);
        editorLayout.Controls.Add(fieldsGridView, 0, 1);
        editorLayout.Controls.Add(fieldEditorPanel, 0, 2);
        editorGroupBox.Controls.Add(editorLayout);

        mainSplitContainer.Panel1.Controls.Add(leftSplitContainer);
        mainSplitContainer.Panel2.Controls.Add(editorGroupBox);

        Controls.Add(mainSplitContainer);
        Controls.Add(introLabel);

        FormClosing += ItemExceptionsForm_FormClosing;
        ResumeLayout(false);
    }

    private void LoadDocument()
    {
        document = ItemExceptionStore.Load(repositoryRoot);
        RefreshSavedItems();
        ResetEditor();
    }

    private void PopulateKnownFieldNames()
    {
        RefreshFieldNameSuggestions();
    }

    private void RefreshSavedItems()
    {
        savedItems.Clear();
        foreach (var pair in document.Items.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            savedItems.Add(new SavedItemRow
            {
                Enabled = pair.Value.Enabled,
                ItemId = pair.Value.ItemId,
                Name = pair.Value.Name,
                SourceFile = pair.Value.SourceFile,
                FieldCount = pair.Value.Overrides.Count,
            });
        }
    }

    private void searchButton_Click(object? sender, EventArgs e)
    {
        if (!ConfirmDiscardCurrentChanges())
        {
            return;
        }

        var query = searchTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            MessageBox.Show(this, T("Error.EmptySearch"), T("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        IReadOnlyList<ItemExceptionImportCandidate> results = ItemExceptionImportService.SearchFromOutputByName(outputPath, query);

        searchResults.Clear();
        foreach (var candidate in results)
        {
            searchResults.Add(new SearchResultRow(candidate));
        }

        if (searchResultsGridView.Rows.Count > 0)
        {
            searchResultsGridView.Rows[0].Selected = true;
            searchResultsGridView.CurrentCell = searchResultsGridView.Rows[0].Cells[0];
        }
    }

    private void loadSearchResultButton_Click(object? sender, EventArgs e)
    {
        LoadSelectedSearchResult();
    }

    private void searchResultsGridView_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            LoadSelectedSearchResult();
        }
    }

    private void LoadSelectedSearchResult()
    {
        if (!ConfirmDiscardCurrentChanges())
        {
            return;
        }

        if (searchResultsGridView.CurrentRow?.DataBoundItem is not SearchResultRow row)
        {
            return;
        }

        LoadCandidate(row.Candidate);
    }

    private void loadSavedItemButton_Click(object? sender, EventArgs e)
    {
        LoadSelectedSavedItem();
    }

    private void savedItemsGridView_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            LoadSelectedSavedItem();
        }
    }

    private void LoadSelectedSavedItem()
    {
        if (!ConfirmDiscardCurrentChanges())
        {
            return;
        }

        if (savedItemsGridView.CurrentRow?.DataBoundItem is not SavedItemRow row)
        {
            return;
        }

        if (!document.Items.TryGetValue(row.ItemId, out var entry))
        {
            return;
        }

        var hydrated = TryHydrateSavedEntry(entry) ?? new ItemExceptionImportCandidate
        {
            ItemId = entry.ItemId,
            Name = entry.Name,
            SourceFile = entry.SourceFile,
            Origin = "saved",
            LocatedFile = string.Empty,
            Fields = (JsonObject)entry.Overrides.DeepClone(),
        };

        LoadCandidate(hydrated);
    }

    private ItemExceptionImportCandidate? TryHydrateSavedEntry(ItemExceptionEntry entry)
    {
        var outputCandidate = ItemExceptionImportService.ImportFromOutput(outputPath, entry.ItemId, entry.SourceFile);
        if (outputCandidate is not null)
        {
            return outputCandidate;
        }

        return ItemExceptionImportService.ImportFromInput(repositoryRoot, entry.ItemId, entry.SourceFile);
    }

    private void LoadCandidate(ItemExceptionImportCandidate candidate)
    {
        currentItemId = candidate.ItemId;
        currentSourceFile = candidate.SourceFile;
        currentOrigin = candidate.Origin;

        itemIdTextBox.Text = currentItemId;
        itemNameTextBox.Text = candidate.Name;
        sourceFileTextBox.Text = currentSourceFile;
        originTextBox.Text = currentOrigin;

        var mergedFields = new JsonObject();
        foreach (var pair in candidate.Fields)
        {
            mergedFields[pair.Key] = pair.Value?.DeepClone();
        }

        enabledCheckBox.Checked = true;
        notesTextBox.Text = string.Empty;
        if (document.Items.TryGetValue(candidate.ItemId, out var savedEntry))
        {
            enabledCheckBox.Checked = savedEntry.Enabled;
            notesTextBox.Text = savedEntry.Notes;
            if (!string.IsNullOrWhiteSpace(savedEntry.Name))
            {
                itemNameTextBox.Text = savedEntry.Name;
            }

            foreach (var pair in savedEntry.Overrides)
            {
                mergedFields[pair.Key] = pair.Value?.DeepClone();
            }
        }

        currentCategory = ItemExceptionFieldGuardService.DetectCategory(currentSourceFile, mergedFields);

        fieldRows.Clear();
        foreach (var pair in mergedFields.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            fieldRows.Add(new EditableFieldRow
            {
                FieldName = pair.Key,
                JsonValueText = SerializeJsonNode(pair.Value),
            });
        }

        RefreshFieldNameSuggestions();
        if (fieldsGridView.Rows.Count > 0)
        {
            fieldsGridView.Rows[0].Selected = true;
            fieldsGridView.CurrentCell = fieldsGridView.Rows[0].Cells[0];
            LoadSelectedFieldIntoEditor();
        }
        else
        {
            ClearFieldEditor();
        }

        currentDirty = false;
        SavedToDisk = false;
    }

    private void RefreshFieldNameSuggestions()
    {
        var currentText = fieldNameComboBox.Text;
        var allNames = fieldRows.Select(row => row.FieldName)
            .Concat(ItemExceptionFieldGuardService.GetKnownFieldNames(repositoryRoot, currentCategory))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        fieldNameComboBox.BeginUpdate();
        fieldNameComboBox.Items.Clear();
        foreach (var name in allNames)
        {
            fieldNameComboBox.Items.Add(name);
        }

        fieldNameComboBox.EndUpdate();

        if (allNames.Length == 0)
        {
            fieldNameComboBox.SelectedIndex = -1;
            return;
        }

        var restoredIndex = Array.FindIndex(allNames, name => string.Equals(name, currentText, StringComparison.OrdinalIgnoreCase));
        fieldNameComboBox.SelectedIndex = restoredIndex >= 0 ? restoredIndex : 0;
    }

    private void fieldsGridView_SelectionChanged(object? sender, EventArgs e)
    {
        if (suppressFieldSelectionChanged)
        {
            return;
        }

        LoadSelectedFieldIntoEditor();
    }

    private void LoadSelectedFieldIntoEditor()
    {
        if (fieldsGridView.CurrentRow?.DataBoundItem is not EditableFieldRow row)
        {
            ClearFieldEditor();
            return;
        }

        suppressEditorChanges = true;
        fieldNameComboBox.Text = row.FieldName;
        fieldValueTextBox.Text = row.JsonValueText;
        suppressEditorChanges = false;
        UpdateGuidanceLabel(row.FieldName);
    }

    private void ClearFieldEditor()
    {
        suppressEditorChanges = true;
        fieldNameComboBox.Text = string.Empty;
        fieldValueTextBox.Text = string.Empty;
        suppressEditorChanges = false;
        guidanceLabel.Text = T("Guidance.Empty");
    }

    private void fieldEditorControl_Changed(object? sender, EventArgs e)
    {
        if (suppressEditorChanges)
        {
            return;
        }

        UpdateGuidanceLabel(fieldNameComboBox.Text);
    }

    private void fieldNameComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (suppressEditorChanges)
        {
            return;
        }

        SyncSelectedFieldValue();
    }

    private void addNewFieldButton_Click(object? sender, EventArgs e)
    {
        SaveFieldFromEditor();
    }

    private void BeginAddField()
    {
        if (string.IsNullOrWhiteSpace(currentItemId))
        {
            MessageBox.Show(this, T("Error.NoItemLoaded"), T("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var allowedFieldNames = ItemExceptionFieldGuardService.GetKnownFieldNames(repositoryRoot, currentCategory)
            .Where(name => fieldRows.All(row => !string.Equals(row.FieldName, name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (allowedFieldNames.Length == 0)
        {
            MessageBox.Show(this, T("Error.NoAvailableFieldToAdd"), T("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        suppressFieldSelectionChanged = true;
        try
        {
            fieldsGridView.ClearSelection();
            fieldsGridView.CurrentCell = null;
        }
        finally
        {
            suppressFieldSelectionChanged = false;
        }

        suppressEditorChanges = true;
        fieldNameComboBox.SelectedItem = allowedFieldNames[0];
        suppressEditorChanges = false;
        SyncSelectedFieldValue();
        UpdateGuidanceLabel(fieldNameComboBox.Text);
        fieldValueTextBox.Focus();
        fieldValueTextBox.SelectAll();
    }

    private void SaveFieldFromEditor()
    {
        if (string.IsNullOrWhiteSpace(currentItemId))
        {
            MessageBox.Show(this, T("Error.NoItemLoaded"), T("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var fieldName = fieldNameComboBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            BeginAddField();
            fieldName = fieldNameComboBox.Text.Trim();
        }

        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return;
        }

        var existingRow = fieldRows.FirstOrDefault(row => string.Equals(row.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));
        if (existingRow is null && !IsFieldAllowedForCurrentItem(fieldName))
        {
            MessageBox.Show(this, string.Format(T("Error.FieldNotAllowedForCategory"), fieldName, T(CategoryTextKey(currentCategory))), T("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!TryParseEditorValue(fieldValueTextBox.Text, out var parsedValue, out var errorMessage))
        {
            MessageBox.Show(this, errorMessage, T("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var normalized = ItemExceptionFieldGuardService.NormalizeValue(repositoryRoot, fieldName, parsedValue);
        var normalizedText = SerializeJsonNode(normalized.Value);
        if (existingRow is null)
        {
            fieldRows.Add(new EditableFieldRow
            {
                FieldName = fieldName,
                JsonValueText = normalizedText,
            });
        }
        else
        {
            existingRow.FieldName = fieldName;
            existingRow.JsonValueText = normalizedText;
        }

        SortFieldRows();
        SelectField(fieldName);
        RefreshFieldNameSuggestions();
        currentDirty = true;
        SavedToDisk = false;
        fieldValueTextBox.Focus();
        fieldValueTextBox.SelectAll();

        if (normalized.WasAdjusted)
        {
            MessageBox.Show(this, string.Format(T("AdjustedValue"), fieldName, normalized.Message), T("AdjustedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void SyncSelectedFieldValue()
    {
        var fieldName = fieldNameComboBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return;
        }

        var existingRow = fieldRows.FirstOrDefault(row => string.Equals(row.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));
        var valueText = existingRow is null
            ? SerializeJsonNode(ItemExceptionFieldGuardService.GetSuggestedValue(repositoryRoot, fieldName))
            : existingRow.JsonValueText;

        suppressEditorChanges = true;
        fieldValueTextBox.Text = valueText;
        suppressEditorChanges = false;
        UpdateGuidanceLabel(fieldName);
    }

    private void UpdateGuidanceLabel(string fieldName)
    {
        var trimmed = fieldName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            guidanceLabel.Text = T("Guidance.Empty");
            return;
        }

        var guidance = ItemExceptionFieldGuardService.GetGuidance(repositoryRoot, trimmed);
        if (guidance.Min is null || guidance.Max is null)
        {
            guidanceLabel.Text = string.Format(T("Guidance.None"), trimmed);
            return;
        }

        guidanceLabel.Text = string.Format(T("Guidance.Range"), trimmed, guidance.FormatRange());
    }

    private void removeFieldButton_Click(object? sender, EventArgs e)
    {
        var fieldName = fieldNameComboBox.Text.Trim();
        var row = fieldRows.FirstOrDefault(item => string.Equals(item.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            return;
        }

        fieldRows.Remove(row);
        RefreshFieldNameSuggestions();
        currentDirty = true;
        SavedToDisk = false;
        if (fieldsGridView.Rows.Count > 0)
        {
            fieldsGridView.Rows[0].Selected = true;
            fieldsGridView.CurrentCell = fieldsGridView.Rows[0].Cells[0];
            LoadSelectedFieldIntoEditor();
        }
        else
        {
            ClearFieldEditor();
        }
    }

    private void saveItemButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(currentItemId))
        {
            MessageBox.Show(this, T("Error.NoItemLoaded"), T("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var overrides = new JsonObject();
        foreach (var row in fieldRows)
        {
            if (!TryParseEditorValue(row.JsonValueText, out var parsedValue, out var errorMessage))
            {
                MessageBox.Show(this, string.Format(T("Error.InvalidFieldJson"), row.FieldName, errorMessage), T("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            overrides[row.FieldName] = ItemExceptionFieldGuardService.NormalizeValue(repositoryRoot, row.FieldName, parsedValue).Value;
        }

        document.Items[currentItemId] = new ItemExceptionEntry
        {
            ItemId = currentItemId,
            Enabled = enabledCheckBox.Checked,
            Name = itemNameTextBox.Text.Trim(),
            SourceFile = currentSourceFile,
            Notes = notesTextBox.Text.Trim(),
            Overrides = overrides,
        };

        ItemExceptionStore.Save(repositoryRoot, document);
        RefreshSavedItems();
        currentDirty = false;
        SavedToDisk = true;
        MessageBox.Show(this, T("Saved"), T("SavedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void deleteItemButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(currentItemId) || !document.Items.ContainsKey(currentItemId))
        {
            return;
        }

        var result = MessageBox.Show(this, string.Format(T("DeleteConfirm"), currentItemId), T("DeleteConfirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        document.Items.Remove(currentItemId);
        ItemExceptionStore.Save(repositoryRoot, document);
        RefreshSavedItems();
        ResetEditor();
        SavedToDisk = true;
    }

    private void closeButton_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void currentItemMetadata_Changed(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(currentItemId))
        {
            currentDirty = true;
            SavedToDisk = false;
        }
    }

    private bool ConfirmDiscardCurrentChanges()
    {
        if (!currentDirty)
        {
            return true;
        }

        var result = MessageBox.Show(this, T("DiscardPrompt"), T("DiscardPromptTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        return result == DialogResult.Yes;
    }

    private void ResetEditor()
    {
        currentItemId = string.Empty;
        currentSourceFile = string.Empty;
        currentOrigin = string.Empty;
        currentCategory = ItemExceptionFieldCategory.Unknown;
        currentDirty = false;
        itemIdTextBox.Text = string.Empty;
        itemNameTextBox.Text = string.Empty;
        sourceFileTextBox.Text = string.Empty;
        originTextBox.Text = string.Empty;
        enabledCheckBox.Checked = true;
        notesTextBox.Text = string.Empty;
        fieldRows.Clear();
        ClearFieldEditor();
        SavedToDisk = false;
    }

    private bool IsFieldAllowedForCurrentItem(string fieldName)
    {
        return ItemExceptionFieldGuardService.GetKnownFieldNames(repositoryRoot, currentCategory)
            .Any(name => string.Equals(name, fieldName, StringComparison.OrdinalIgnoreCase));
    }

    private static string CategoryTextKey(ItemExceptionFieldCategory category)
    {
        return category switch
        {
            ItemExceptionFieldCategory.Weapon => "Category.Weapon",
            ItemExceptionFieldCategory.Attachment => "Category.Attachment",
            ItemExceptionFieldCategory.Gear => "Category.Gear",
            ItemExceptionFieldCategory.Ammo => "Category.Ammo",
            _ => "Category.Unknown",
        };
    }

    private void SortFieldRows()
    {
        var ordered = fieldRows.OrderBy(row => row.FieldName, StringComparer.OrdinalIgnoreCase).ToArray();
        fieldRows.Clear();
        foreach (var row in ordered)
        {
            fieldRows.Add(row);
        }
    }

    private void SelectField(string fieldName)
    {
        suppressFieldSelectionChanged = true;
        try
        {
            foreach (DataGridViewRow row in fieldsGridView.Rows)
            {
                if (row.DataBoundItem is EditableFieldRow data && string.Equals(data.FieldName, fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    row.Selected = true;
                    fieldsGridView.CurrentCell = row.Cells[0];
                    break;
                }
            }
        }
        finally
        {
            suppressFieldSelectionChanged = false;
        }

        LoadSelectedFieldIntoEditor();
    }

    private void ItemExceptionsForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!currentDirty)
        {
            return;
        }

        var result = MessageBox.Show(this, T("CloseConfirm"), T("CloseConfirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            e.Cancel = true;
        }
    }

    private static bool TryParseEditorValue(string rawText, out JsonNode parsedValue, out string errorMessage)
    {
        errorMessage = string.Empty;
        var text = rawText.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            parsedValue = JsonValue.Create((string?)null)!;
            return true;
        }

        try
        {
            parsedValue = JsonNode.Parse(text) ?? JsonValue.Create((string?)null)!;
            return true;
        }
        catch
        {
            if (!LooksLikeStructuredJson(text))
            {
                parsedValue = JsonValue.Create(text)!;
                return true;
            }

            parsedValue = JsonValue.Create((string?)null)!;
            errorMessage = "JSON value is invalid.";
            return false;
        }
    }

    private static bool LooksLikeStructuredJson(string text)
    {
        return text.StartsWith("{") || text.StartsWith("[") || text.StartsWith("\"")
            || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "false", StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, "null", StringComparison.OrdinalIgnoreCase)
            || double.TryParse(text, out _);
    }

    private static string SerializeJsonNode(JsonNode? node)
    {
        return node is null ? "null" : node.ToJsonString(JsonWriteOptions);
    }

    private string T(string key)
    {
        return Texts[key].Get(language);
    }

    private static readonly IReadOnlyDictionary<string, LocalizedText> Texts = new Dictionary<string, LocalizedText>(StringComparer.Ordinal)
    {
        ["Title"] = new("例外物品管理", "Item Exceptions"),
        ["Intro"] = new("流程调整为：第一步只按 Name 搜索 output 里的现有输出物品；载入后，右侧会显示当前属性，你可以在同一区域新增、修改、删除字段，最后单独保存这一件例外物品。", "Workflow: step 1 searches only existing items in output by Name; after loading, the right side shows current properties so you can add, edit, and remove fields before saving that single exception item."),
        ["SearchGroup"] = new("第一步：按名称搜索物品", "Step 1: Search Item By Name"),
        ["SavedGroup"] = new("已保存的例外物品", "Saved Exception Items"),
        ["EditorGroup"] = new("第二步到第四步：加载、编辑并保存物品", "Steps 2-4: Load, Edit, and Save Item"),
        ["SearchNote"] = new("这里只搜索 output 结果，不再搜索 input。", "This search only scans output results and no longer searches input."),
        ["Source.Input"] = new("输入源 input", "Input"),
        ["Source.Output"] = new("输出结果 output", "Output"),
        ["Search"] = new("搜索 Name", "Search Name"),
        ["LoadSelectedSearch"] = new("载入搜索结果", "Load Search Result"),
        ["LoadSelectedSaved"] = new("载入已保存项", "Load Saved Item"),
        ["Label.ItemId"] = new("ItemID", "ItemID"),
        ["Label.Name"] = new("名称", "Name"),
        ["Label.SourceFile"] = new("来源文件", "Source File"),
        ["Label.Origin"] = new("来源类型", "Origin"),
        ["Label.Enabled"] = new("启用这条例外配置", "Enable This Exception"),
        ["Label.Notes"] = new("备注", "Notes"),
        ["Label.Field"] = new("字段", "Field"),
        ["Label.FieldValue"] = new("字段值", "Field Value"),
        ["AddField"] = new("新增/修改字段", "Add or Update Field"),
        ["ApplyField"] = new("应用字段", "Apply Field"),
        ["RemoveField"] = new("删除字段", "Remove Field"),
        ["SaveItem"] = new("保存物品", "Save Item"),
        ["DeleteItem"] = new("删除物品", "Delete Item"),
        ["Close"] = new("关闭", "Close"),
        ["Category.Weapon"] = new("武器", "weapon"),
        ["Category.Attachment"] = new("附件", "attachment"),
        ["Category.Gear"] = new("装备", "gear"),
        ["Category.Ammo"] = new("子弹", "ammo"),
        ["Category.Unknown"] = new("未知类别", "unknown category"),
        ["Column.Enabled"] = new("启用", "Enabled"),
        ["Column.ItemId"] = new("ItemID", "ItemID"),
        ["Column.Name"] = new("名称", "Name"),
        ["Column.SourceFile"] = new("来源文件", "Source File"),
        ["Column.OverrideCount"] = new("字段数", "Fields"),
        ["Column.Field"] = new("字段名", "Field"),
        ["Column.ValueType"] = new("值类型", "Value Type"),
        ["Column.Preview"] = new("当前值预览", "Value Preview"),
        ["Guidance.Empty"] = new("选择一个字段后，这里会显示建议范围和兜底说明。", "Select a field to view suggested ranges and safeguards."),
        ["Guidance.None"] = new("字段 {0} 当前没有现成规则范围，将只做基础 JSON 校验。", "Field {0} has no known rule range; only basic JSON validation will apply."),
        ["Guidance.Range"] = new("字段 {0} 的建议安全范围: {1}。如果输入超出范围，保存时会自动夹紧。", "Suggested safe range for {0}: {1}. Out-of-range numeric values will be clamped on save."),
        ["EditorHelp"] = new("新增字段时只能从当前物品所属大类的字段列表里选。数字可以直接填 12、-7、1.25；布尔值填 true/false；字符串可直接填文本；对象和数组仍然支持 JSON。", "New fields can only be picked from the current item's category field list. Numbers can be entered directly as 12, -7, 1.25; booleans as true/false; plain text becomes a string; objects and arrays still support JSON."),
        ["Saved"] = new("当前物品的例外配置已保存。", "Current item exception saved."),
        ["SavedTitle"] = new("保存完成", "Saved"),
        ["AdjustedTitle"] = new("已自动收敛数值", "Value Adjusted"),
        ["AdjustedValue"] = new("字段 {0} 超出建议范围，已自动调整到 {1}", "Field {0} was outside the suggested range and has been adjusted to {1}"),
        ["DeleteConfirm"] = new("确定删除 ItemID={0} 的例外配置吗？", "Delete exception for ItemID={0}?"),
        ["DeleteConfirmTitle"] = new("确认删除", "Confirm Delete"),
        ["DiscardPrompt"] = new("当前物品有未保存修改，继续会丢失这些更改。是否继续？", "The current item has unsaved changes. Continue and discard them?"),
        ["DiscardPromptTitle"] = new("放弃未保存修改", "Discard Unsaved Changes"),
        ["CloseConfirm"] = new("当前物品有未保存修改，关闭会丢失这些更改。是否继续？", "The current item has unsaved changes. Closing will discard them. Continue?"),
        ["CloseConfirmTitle"] = new("确认关闭", "Confirm Close"),
        ["ErrorTitle"] = new("无法继续", "Cannot Continue"),
        ["Error.EmptySearch"] = new("请先输入要搜索的 Name 关键字。", "Enter a Name query first."),
        ["Error.NoItemLoaded"] = new("请先从左侧搜索结果或已保存列表中载入一个物品。", "Load an item from search results or saved items first."),
        ["Error.NoAvailableFieldToAdd"] = new("当前物品所属大类没有可新增的剩余字段，或者还无法识别该物品的大类。", "No additional fields are available for this item's category, or the category could not be determined yet."),
        ["Error.EmptyFieldNameSingle"] = new("请先选择或输入字段名。", "Select or enter a field name first."),
        ["Error.FieldNotAllowedForCategory"] = new("字段 {0} 不属于当前物品的 {1} 字段列表，不能新增。", "Field {0} is not in the current item's {1} field list and cannot be added."),
        ["Error.InvalidFieldJson"] = new("字段 {0} 无法解析: {1}", "Field {0} could not be parsed: {1}"),
    };

    private sealed class SearchResultRow
    {
        public SearchResultRow(ItemExceptionImportCandidate candidate)
        {
            Candidate = candidate;
            ItemId = candidate.ItemId;
            Name = candidate.Name;
            SourceFile = candidate.SourceFile;
        }

        public ItemExceptionImportCandidate Candidate { get; }
        public string ItemId { get; }
        public string Name { get; }
        public string SourceFile { get; }
    }

    private sealed class SavedItemRow
    {
        public bool Enabled { get; init; }
        public string ItemId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string SourceFile { get; init; } = string.Empty;
        public int FieldCount { get; init; }
        public string FieldCountText => FieldCount.ToString();
    }

    private sealed class EditableFieldRow : INotifyPropertyChanged
    {
        private string fieldName = string.Empty;
        private string jsonValueText = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string FieldName
        {
            get => fieldName;
            set
            {
                if (string.Equals(fieldName, value, StringComparison.Ordinal))
                {
                    return;
                }

                fieldName = value;
                OnPropertyChanged(nameof(FieldName));
            }
        }

        public string JsonValueText
        {
            get => jsonValueText;
            set
            {
                if (string.Equals(jsonValueText, value, StringComparison.Ordinal))
                {
                    return;
                }

                jsonValueText = value;
                OnPropertyChanged(nameof(JsonValueText));
                OnPropertyChanged(nameof(ValueType));
                OnPropertyChanged(nameof(Preview));
            }
        }

        public string ValueType
        {
            get
            {
                try
                {
                    var parsed = JsonNode.Parse(string.IsNullOrWhiteSpace(JsonValueText) ? "null" : JsonValueText);
                    return parsed switch
                    {
                        null => "null",
                        JsonObject => "object",
                        JsonArray => "array",
                        JsonValue jsonValue when jsonValue.TryGetValue<bool>(out _) => "bool",
                        JsonValue jsonValue when jsonValue.TryGetValue<int>(out _) => "int",
                        JsonValue jsonValue when jsonValue.TryGetValue<long>(out _) => "int",
                        JsonValue jsonValue when jsonValue.TryGetValue<double>(out _) => "number",
                        _ => "string",
                    };
                }
                catch
                {
                    return "string";
                }
            }
        }

        public string Preview
        {
            get
            {
                var preview = JsonValueText.Replace(Environment.NewLine, " ").Trim();
                return preview.Length > 120 ? preview[..120] + "..." : preview;
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
