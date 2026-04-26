using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomUninstaller.Core;

namespace CustomUninstaller.UI;

public class MainForm : Form
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _searchBox = new();
    private readonly CheckBox _showSystemCheckBox = new();
    private readonly Button _btnRefresh = new();
    private readonly Button _btnUninstall = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Label _statusLabel = new();
    private List<InstalledProgram> _allPrograms = new();

    public MainForm()
    {
        Text = "Custom Uninstaller";
        Size = new Size(950, 650);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(750, 450);

        SetupControls();
        Load += (_, _) => RefreshList();
    }

    private void SetupControls()
    {
        _searchBox.Location = new Point(12, 12);
        _searchBox.Size = new Size(420, 26);
        _searchBox.PlaceholderText = "🔍 Поиск программ...";
        _searchBox.TextChanged += (_, _) => FilterGrid();

        _showSystemCheckBox.Text = "Показывать системные компоненты";
        _showSystemCheckBox.Location = new Point(440, 14);
        _showSystemCheckBox.CheckedChanged += (_, _) => RefreshList();

        _btnRefresh.Text = "🔄 Обновить";
        _btnRefresh.Location = new Point(700, 10);
        _btnRefresh.Size = new Size(110, 28);
        _btnRefresh.Click += (_, _) => RefreshList();

        _btnUninstall.Text = "🗑 Удалить выбранные";
        _btnUninstall.Location = new Point(815, 10);
        _btnUninstall.Size = new Size(150, 28);
        _btnUninstall.Click += async (_, _) => await UninstallSelectedAsync();

        _grid.Dock = DockStyle.Fill;
        _grid.Location = new Point(0, 45);
        _grid.AllowUserToAddRows = false;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = true;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.Columns.Add("DisplayName", "Название");
        _grid.Columns.Add("SizeMB", "Размер (МБ)");
        _grid.Columns.Add("Path", "Ключ реестра");
        _grid.Columns["SizeMB"].ValueType = typeof(double);
        _grid.Columns["SizeMB"].DefaultCellStyle.Format = "0.##";

        _progressBar.Location = new Point(12, Height - 45);
        _progressBar.Size = new Size(Width - 24, 22);
        _progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        _statusLabel.Location = new Point(12, Height - 18);
        _statusLabel.Size = new Size(Width - 24, 15);
        _statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _statusLabel.Text = "Готово";

        Controls.AddRange(new Control[] { _searchBox, _showSystemCheckBox, _btnRefresh, _btnUninstall, _grid, _progressBar, _statusLabel });
    }

    private void RefreshList()
    {
        _btnUninstall.Enabled = false;
        _btnRefresh.Enabled = false;
        _statusLabel.Text = "Загрузка списка...";
        Cursor = Cursors.WaitCursor;

        bool hideSystem = !_showSystemCheckBox.Checked;
        _allPrograms = UninstallManager.GetInstalledPrograms(hideSystem);
        FilterGrid();

        Cursor = Cursors.Default;
        _statusLabel.Text = $"Загружено: {_allPrograms.Count} программ";
        _btnUninstall.Enabled = true;
        _btnRefresh.Enabled = true;
    }

    private void FilterGrid()
    {
        var query = _allPrograms.AsEnumerable();
        var term = _searchBox.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(p => p.DisplayName.ToLower().Contains(term));
        }

        _grid.Rows.Clear();
        foreach (var p in query)
        {
            _grid.Rows.Add(p.DisplayName, Math.Round(p.SizeKB / 1024.0, 2), p.RegistryPath);
            _grid.Rows[^1].Tag = p;
        }
        _statusLabel.Text = $"Найдено: {_grid.RowCount}";
    }

    private async Task UninstallSelectedAsync()
    {
        var selected = _grid.SelectedRows.Cast<DataGridViewRow>()
            .Select(r => (InstalledProgram)r.Tag!)
            .ToList();

        if (selected.Count == 0)
        {
            MessageBox.Show("Выберите хотя бы одну программу.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnUninstall.Enabled = false;
        _btnRefresh.Enabled = false;
        _progressBar.Value = 0;
        _progressBar.Maximum = selected.Count;
        _statusLabel.Text = "Начало удаления...";

        int successCount = 0;
        for (int i = 0; i < selected.Count; i++)
        {
            var app = selected[i];
            _statusLabel.Text = $"Удаление {i + 1}/{selected.Count}: {app.DisplayName}";
            
            bool ok = await UninstallManager.UninstallAsync(app, silent: true);
            if (ok) successCount++;

            _progressBar.Value = i + 1;
            await Task.Delay(400); // Даём UI обновиться
        }

        _statusLabel.Text = $"Готово. Удалено: {successCount}/{selected.Count}";
        _progressBar.Value = 0;
        _btnRefresh.Enabled = true;
        _btnUninstall.Enabled = true;

        RefreshList();
    }
}