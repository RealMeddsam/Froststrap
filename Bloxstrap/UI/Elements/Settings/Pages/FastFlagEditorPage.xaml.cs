﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;

using Wpf.Ui.Mvvm.Contracts;
using Bloxstrap.UI.Elements.Dialogs;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using System.Windows.Input;
using Bloxstrap.UI.ViewModels.Dialogs;
using System.Windows.Media;
using System.Text.Json;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for FastFlagEditorPage.xaml
    /// </summary>
    public partial class FastFlagEditorPage
    {
        private readonly ObservableCollection<FastFlag> _fastFlagList = new();

        private bool _showPresets = true;
        private string _searchFilter = string.Empty;
        private string _lastSearch = string.Empty;
        private DateTime _lastSearchTime = DateTime.MinValue;
        private const int _debounceDelay = 70;

        private bool LoadShowPresetColumnSetting()
        {
            return App.Settings.Prop.ShowPresetColumn;
        }

        private void FastFlagEditorPage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.Z)
                {
                    App.FastFlags.Undo();
                    ReloadList();
                    e.Handled = true;
                }
                else if (e.Key == Key.Y)
                {
                    App.FastFlags.Redo();
                    ReloadList();
                    e.Handled = true;
                }
            }
        }

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (App.Settings.Prop.CtrlCJsonFormat && e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                // Let editing TextBox handle Ctrl+C normally
                if (DataGrid.CurrentCell.IsValid &&
                    DataGrid.CurrentCell.Column is DataGridBoundColumn &&
                    DataGrid.IsKeyboardFocusWithin &&
                    DataGrid.CurrentColumn.GetCellContent(DataGrid.CurrentItem) is TextBox)
                {
                    return;
                }

                var selectedItems = DataGrid.SelectedItems.Cast<FastFlag>().ToList();
                if (selectedItems.Count == 0)
                    return;

                var dict = selectedItems.ToDictionary(item => item.Name, item => item.Value);
                var format = App.Settings.Prop.SelectedCopyFormat;

                string output;

                if (format == CopyFormatMode.Format1)
                {
                    output = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                }
                else if (format == CopyFormatMode.Format2)
                {
                    var groupedFlags = dict
                        .GroupBy(kvp =>
                        {
                            var match = Regex.Match(kvp.Key, @"^[A-Z]+[a-z]*");
                            return match.Success ? match.Value : "Other";
                        })
                        .OrderBy(g => g.Key);

                    var formattedJson = new StringBuilder();
                    formattedJson.AppendLine("{");

                    int totalItems = dict.Count;
                    int writtenItems = 0;

                    foreach (var group in groupedFlags)
                    {
                        foreach (var kvp in group.OrderByDescending(kvp => kvp.Key.Length + (kvp.Value?.ToString()?.Length ?? 0)))
                        {
                            writtenItems++;
                            string line = $"    \"{kvp.Key}\": \"{kvp.Value}\"";
                            if (writtenItems < totalItems) line += ",";
                            formattedJson.AppendLine(line);
                        }
                    }

                    formattedJson.AppendLine("}");
                    output = formattedJson.ToString();
                }
                else if (format == CopyFormatMode.Format3)
                {
                    var sortedFlags = dict.OrderBy(kvp => kvp.Key);

                    var formattedJson = new StringBuilder();
                    formattedJson.AppendLine("{");

                    int totalItems = dict.Count;
                    int writtenItems = 0;

                    foreach (var kvp in sortedFlags)
                    {
                        writtenItems++;
                        string line = $"    \"{kvp.Key}\": \"{kvp.Value}\"";
                        if (writtenItems < totalItems) line += ",";
                        formattedJson.AppendLine(line);
                    }

                    formattedJson.AppendLine("}");
                    output = formattedJson.ToString();
                }
                else if (format == CopyFormatMode.Format4)
                {
                    var sortedFlags = dict.OrderByDescending(kvp =>
                        $"    \"{kvp.Key}\": \"{kvp.Value}\"".Length
                    );

                    var formattedJson = new StringBuilder();
                    formattedJson.AppendLine("{");

                    int totalItems = dict.Count;
                    int writtenItems = 0;

                    foreach (var kvp in sortedFlags)
                    {
                        writtenItems++;
                        string line = $"    \"{kvp.Key}\": \"{kvp.Value}\"";
                        if (writtenItems < totalItems) line += ",";
                        formattedJson.AppendLine(line);
                    }

                    formattedJson.AppendLine("}");
                    output = formattedJson.ToString();
                }
                else
                {
                    // fallback if no match
                    output = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                }

                Clipboard.SetText(output);
                e.Handled = true;
            }
        }

        public FastFlagEditorPage()
        {
            InitializeComponent();

            AdvancedSettingViewModel.ShowPresetColumnChanged += (_, _) =>
            {
                Dispatcher.Invoke(() =>
                {
                    PresetColumn.Visibility = LoadShowPresetColumnSetting()
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                });
            };

            AdvancedSettingViewModel.ShowFlagCountChanged += (_, _) =>
            {
                Dispatcher.Invoke(UpdateTotalFlagsCount);
            };

            SetDefaultStates();
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: FastFlag Editor");
        }

        private void SetDefaultStates()
        {
            TogglePresetsButton.IsChecked = true;
        }

        public void ReloadList()
        {
            PresetColumn.Visibility = LoadShowPresetColumnSetting() ? Visibility.Visible : Visibility.Collapsed;

            _fastFlagList.Clear();

            var presetFlags = FastFlagManager.PresetFlags.Values;

            foreach (var pair in App.FastFlags.Prop.OrderBy(x => x.Key))
            {
                if (!_showPresets && presetFlags.Contains(pair.Key))
                    continue;

                if (!pair.Key.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                var entry = new FastFlag
                {
                    Name = pair.Key,
                    Value = pair.Value?.ToString() ?? string.Empty,
                    Preset = presetFlags.Contains(pair.Key)
                        ? "pack://application:,,,/Resources/Checkmark.ico"
                        : "pack://application:,,,/Resources/CrossMark.ico"
                };

                _fastFlagList.Add(entry);
            }

            if (DataGrid.ItemsSource is null)
                DataGrid.ItemsSource = _fastFlagList;

            UpdateTotalFlagsCount();
        }

        public string FlagCountText => $"Total flags: {_fastFlagList.Count}";

        public void UpdateTotalFlagsCount()
        {
            // Get current flags count from the DataGrid's ItemsSource, safely
            int count = 0;
            if (DataGrid.ItemsSource is IEnumerable<FastFlag> flags)
                count = flags.Count();

            // Update the TextBlock text with the count
            TotalFlagsTextBlock.Text = $"Total Flags: {count}";

            // Toggle visibility based on the user setting
            TotalFlagsTextBlock.Visibility = App.Settings.Prop.ShowFlagCount
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ClearSearch(bool refresh = true)
        {
            SearchTextBox.Text = "";
            _searchFilter = "";

            if (refresh)
                ReloadList();
        }

        private void ShowAddDialog()
        {
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Dialog: Add FastFlag");

            var dialog = new AddFastFlagDialog();
            dialog.ShowDialog();

            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: FastFlag Editor");

            if (dialog.Result != MessageBoxResult.OK)
                return;

            if (dialog.Tabs.SelectedIndex == 0)
                AddSingle(dialog.FlagNameTextBox.Text.Trim(), dialog.FlagValueComboBox.Text);
            else if (dialog.Tabs.SelectedIndex == 1)
                ImportJSON(dialog.JsonTextBox.Text);
            else if (dialog.Tabs.SelectedIndex == 2)
                AddWithGameId(
                    dialog.GameFlagNameTextBox.Text.Trim(),
                    dialog.GameFlagValueComboBox.Text,
                    dialog.GameFlagIdTextBox.Text,
                    dialog.AddIdFilterType
                );
            else if (dialog.Tabs.SelectedIndex == 3)
                ImportGameIdJson(
                    dialog.ImportGameIdJson,
                    dialog.ImportGameId,
                    dialog.ImportIdFilterType
                );
        }

        private void AdvancedSettings_Click(object sender, RoutedEventArgs e)
        {
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Dialog: Advanced Settings");

            var dialog = new AdvancedSettingsDialog();
            dialog.Owner = Window.GetWindow(this);

            dialog.SettingsSaved += (_, _) =>
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    mainWindow.SettingsSavedSnackbar.Show();
                }
            };

            dialog.ShowDialog();

            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: FastFlag Editor");
        }

        private MainWindow GetMainWindow() => (MainWindow)Application.Current.MainWindow;

        private static HashSet<string> DecodeBase64Flags(string? base64)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(base64))
                return result;

            try
            {
                // Remove whitespace and newlines
                var cleanBase64 = base64.Trim().Replace("\n", "").Replace("\r", "");

                var bytes = Convert.FromBase64String(cleanBase64);
                var jsonText = Encoding.UTF8.GetString(bytes);

                using var doc = JsonDocument.Parse(jsonText);
                if (doc.RootElement.TryGetProperty("Allowed", out var allowed))
                {
                    foreach (var flagElem in allowed.EnumerateArray())
                    {
                        var flag = flagElem.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(flag))
                            result.Add(flag);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("DecodeBase64Flags", $"Failed to decode Base64: {ex.Message}");
            }

            return result;
        }

        public async void CleanListButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = GetMainWindow();
            mainWindow?.ShowLoading("Cleaning List...");
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Dialog: Cleaning List");

            App.FastFlags.suspendUndoSnapshot = true;
            App.FastFlags.SaveUndoSnapshot();

            try
            {
                var remoteManager = new RemoteDataManager();
                await remoteManager.LoadData();

                var base64Flags = DecodeBase64Flags(remoteManager.Prop.AllowedFastFlags);
                App.Logger.WriteLine("CleanList", $"Loaded {base64Flags.Count} allowed flags.");

                var allFlags = App.FastFlags.GetAllFlags();
                var invalidRemoved = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                // Remove any flag not in the remote Allowed list
                foreach (var flag in allFlags)
                {
                    var name = flag.Name.Trim();
                    if (!base64Flags.Contains(name))
                    {
                        invalidRemoved[name] = flag.Value;
                        App.FastFlags.SetValue(name, null);
                    }
                }

                int totalChanges = invalidRemoved.Count;
                if (totalChanges == 0)
                {
                    Frontend.ShowMessageBox("No invalid FastFlags detected.", MessageBoxImage.Information);
                    return;
                }

                var message =
                            $"{totalChanges} invalid FastFlag{(totalChanges == 1 ? "" : "s")} have been removed.\n\n" +
                            "Do you want to see a list of all the removed flags?";

                if (Frontend.ShowMessageBox(message, MessageBoxImage.Question, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // Pass empty collections to avoid null warnings
                    var flagDialog = new FlagDialog(this, invalidRemoved, new Dictionary<string, string>(), new List<(string OldName, string NewName)>())
                    {
                        Owner = Application.Current.MainWindow
                    };
                    flagDialog.ShowDialog();
                }

                ReloadList();
                UpdateTotalFlagsCount();
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"An error occurred during FastFlag cleanup: {ex.Message}", MessageBoxImage.Error);
            }
            finally
            {
                App.FastFlags.suspendUndoSnapshot = false;
                mainWindow?.HideLoading();
            }
        }

        private void ShowProfilesDialog()
        {
            (App.Current as App)?._froststrapRPC?.UpdatePresence("Dialog: Profiles");

            var dialog = new FlagProfilesDialog();
            dialog.ShowDialog();

            (App.Current as App)?._froststrapRPC?.UpdatePresence("Page: FastFlag Editor");

            if (dialog.Result != MessageBoxResult.OK)
                return;

            if (dialog.Tabs.SelectedIndex == 0)
                App.FastFlags.SaveProfile(dialog.SaveProfile.Text);
            else if (dialog.Tabs.SelectedIndex == 1)
            {
                if (dialog.LoadProfile.SelectedValue == null)
                    return;
                App.FastFlags.LoadProfile(dialog.LoadProfile.SelectedValue.ToString(), dialog.ClearFlags.IsChecked);
            }

            Thread.Sleep(1000);
            ReloadList();
        }

        private void AddWithGameId(string name, string value, string gameId, FastFlagFilterType filterType)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(gameId))
            {
                Frontend.ShowMessageBox("Please fill in all fields.", MessageBoxImage.Warning);
                return;
            }

            string suffix = filterType == FastFlagFilterType.DataCenterFilter ? "_DataCenterFilter" : "_PlaceFilter";
            string formattedName = $"{name}{suffix}";

            // Try to parse value numeric part for bannable check
            double? numericValue = null;
            var valueParts = value.Split(';');
            if (valueParts.Length > 0 && double.TryParse(valueParts[0], out double parsed))
                numericValue = parsed;

            App.FastFlags.suspendUndoSnapshot = true;
            App.FastFlags.SaveUndoSnapshot();

            string formattedValue = $"{value};{gameId}";
            FastFlag? entry;

            if (App.FastFlags.GetValue(formattedName) is null)
            {
                entry = new FastFlag
                {
                    Name = formattedName,
                    Value = formattedValue
                };

                if (!formattedName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                    ClearSearch();

                App.FastFlags.SetValue(entry.Name, entry.Value);
                _fastFlagList.Add(entry);
            }
            else
            {
                Frontend.ShowMessageBox(Strings.Menu_FastFlagEditor_AlreadyExists, MessageBoxImage.Information);

                bool refresh = false;

                if (!_showPresets && FastFlagManager.PresetFlags.Values.Contains(formattedName))
                {
                    TogglePresetsButton.IsChecked = true;
                    _showPresets = true;
                    refresh = true;
                }

                if (!formattedName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                {
                    ClearSearch(false);
                    refresh = true;
                }

                if (refresh)
                    ReloadList();

                entry = _fastFlagList.FirstOrDefault(x => x.Name == formattedName);
            }

            DataGrid.SelectedItem = entry;
            DataGrid.ScrollIntoView(entry);

            App.FastFlags.suspendUndoSnapshot = false;

            UpdateTotalFlagsCount();
        }

        private void ImportGameIdJson(string? json, string? gameId, FastFlagFilterType filterType)
        {
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(gameId))
                return;

            Dictionary<string, JsonElement>? list = null;

            json = json.Trim();

            if (!json.StartsWith('{'))
                json = '{' + json;

            if (!json.EndsWith('}'))
            {
                int lastIndex = json.LastIndexOf('}');
                if (lastIndex == -1)
                    json += '}';
                else
                    json = json.Substring(0, lastIndex + 1);
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                list = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);

                if (list is null)
                    throw new Exception("JSON deserialization returned null");
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Invalid JSON: {ex.Message}", MessageBoxImage.Error);
                return;
            }

            string suffix = filterType == FastFlagFilterType.DataCenterFilter ? "_DataCenterFilter" : "_PlaceFilter";

            App.FastFlags.suspendUndoSnapshot = true;
            App.FastFlags.SaveUndoSnapshot();

            // Prepare bannable flag pairs with values parsed as double?
            var bannableFlagPairs = list
                .Select(kvp =>
                {
                    double? val = null;
                    if (kvp.Value.ValueKind == JsonValueKind.Number)
                    {
                        if (kvp.Value.TryGetDouble(out double d))
                            val = d;
                    }
                    else if (kvp.Value.ValueKind == JsonValueKind.String)
                    {
                        if (double.TryParse(kvp.Value.GetString(), out double parsed))
                            val = parsed;
                    }

                    string flagNameWithSuffix = $"{kvp.Key}{suffix}";
                    return (Name: flagNameWithSuffix, Value: val, BaseKey: kvp.Key);
                });

            foreach (var pair in list)
            {
                string newName = $"{pair.Key}{suffix}";

                string newValue = pair.Value.ValueKind == JsonValueKind.String
                    ? $"{pair.Value.GetString()};{gameId}"
                    : $"{pair.Value.ToString()};{gameId}";

                AddSingle(newName, newValue);
            }

            App.FastFlags.suspendUndoSnapshot = false;

            ReloadList();
            ClearSearch();
        }

        private void AddSingle(string name, string value)
        {
            double? val = null;

            if (!string.IsNullOrEmpty(value))
            {
                if (double.TryParse(value, out double parsed))
                    val = parsed;
            }

            FastFlag? entry;

            if (App.FastFlags.GetValue(name) is null)
            {
                entry = new FastFlag
                {
                    Name = name,
                    Value = value
                };

                if (!name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                    ClearSearch();

                App.FastFlags.SetValue(entry.Name, entry.Value);
                _fastFlagList.Add(entry);
            }
            else
            {
                Frontend.ShowMessageBox(Strings.Menu_FastFlagEditor_AlreadyExists, MessageBoxImage.Information);

                bool refresh = false;

                if (!_showPresets && FastFlagManager.PresetFlags.Values.Contains(name))
                {
                    TogglePresetsButton.IsChecked = true;
                    _showPresets = true;
                    refresh = true;
                }

                if (!name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                {
                    ClearSearch(false);
                    refresh = true;
                }

                if (refresh)
                    ReloadList();

                entry = _fastFlagList.FirstOrDefault(x => x.Name == name);
            }

            DataGrid.SelectedItem = entry;
            DataGrid.ScrollIntoView(entry);
            UpdateTotalFlagsCount();
        }

        private void ImportJSON(string json)
        {
            Dictionary<string, object>? list = null;

            json = json.Trim();

            if (!json.StartsWith('{'))
                json = '{' + json;

            if (!json.EndsWith('}'))
            {
                int lastIndex = json.LastIndexOf('}');

                if (lastIndex == -1)
                    json += '}';
                else
                    json = json.Substring(0, lastIndex + 1);
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                list = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);

                if (list is null)
                    throw new Exception("JSON deserialization returned null");
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    string.Format(Strings.Menu_FastFlagEditor_InvalidJSON, ex.Message),
                    MessageBoxImage.Error
                );

                ShowAddDialog();

                return;
            }

            App.FastFlags.suspendUndoSnapshot = true;
            App.FastFlags.SaveUndoSnapshot();

            var bannableFlagPairs = list
                .Select(kvp =>
                {
                    double? val = null;
                    if (kvp.Value != null)
                    {
                        if (kvp.Value is double d)
                            val = d;
                        else if (double.TryParse(kvp.Value.ToString(), out double parsed))
                            val = parsed;
                    }
                    return (Name: kvp.Key, Value: val);
                });

            var conflictingFlags = App.FastFlags.Prop.Where(x => list.ContainsKey(x.Key)).Select(x => x.Key);
            bool overwriteConflicting = false;

            if (conflictingFlags.Any())
            {
                int count = conflictingFlags.Count();

                string message = string.Format(
                    Strings.Menu_FastFlagEditor_ConflictingImport,
                    count,
                    string.Join(", ", conflictingFlags.Take(25))
                );

                if (count > 25)
                    message += "...";

                var result = Frontend.ShowMessageBox(message, MessageBoxImage.Question, MessageBoxButton.YesNo);

                overwriteConflicting = result == MessageBoxResult.Yes;
            }

            foreach (var pair in list)
            {
                if (App.FastFlags.Prop.ContainsKey(pair.Key) && !overwriteConflicting)
                    continue;

                if (pair.Value is null)
                    continue;

                var val = pair.Value.ToString();

                if (val is null)
                    continue;

                App.FastFlags.SetValue(pair.Key, val);
            }

            App.FastFlags.suspendUndoSnapshot = false;

            ClearSearch();
        }

        private void Editor_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.All(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                                   f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
                {
                    DragOverlay.Visibility = Visibility.Visible;
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }

            DragOverlay.Visibility = Visibility.Collapsed;
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Editor_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.All(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                                   f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
                {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Editor_DragLeave(object sender, DragEventArgs e)
        {
            DragOverlay.Visibility = Visibility.Collapsed;
            e.Handled = true;
        }

        private void Editor_Drop(object sender, DragEventArgs e)
        {
            DragOverlay.Visibility = Visibility.Collapsed;

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var file in files)
            {
                if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
                    !file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    Frontend.ShowMessageBox(
                        $"Invalid file type: {Path.GetFileName(file)}\nOnly .json and .txt are supported.",
                        MessageBoxImage.Error
                    );
                    continue;
                }

                try
                {
                    string content = File.ReadAllText(file);
                    ImportJSON(content);
                }
                catch (Exception ex)
                {
                    Frontend.ShowMessageBox(
                        $"Failed to import \"{Path.GetFileName(file)}\": {ex.Message}",
                        MessageBoxImage.Error
                    );
                }
            }

            e.Handled = true;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) => ReloadList();

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            App.FastFlags.suspendUndoSnapshot = true;
            App.FastFlags.SaveUndoSnapshot();

            if (e.Row.DataContext is not FastFlag entry)
                return;

            if (e.EditingElement is not TextBox textbox)
                return;

            switch (e.Column.Header)
            {
                case "Name":
                    string oldName = entry.Name;
                    string newName = textbox.Text;

                    if (newName == oldName)
                        break;

                    if (App.FastFlags.GetValue(newName) is not null)
                    {
                        Frontend.ShowMessageBox(Strings.Menu_FastFlagEditor_AlreadyExists, MessageBoxImage.Information);
                        e.Cancel = true;
                        textbox.Text = oldName;
                        break;
                    }

                    App.FastFlags.SetValue(oldName, null);
                    App.FastFlags.SetValue(newName, entry.Value);

                    if (!newName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                        ClearSearch();

                    entry.Name = newName;
                    break;

                case "Value":
                    string newValue = textbox.Text;
                    App.FastFlags.SetValue(entry.Name, newValue);
                    break;
            }

            App.FastFlags.suspendUndoSnapshot = false;


            UpdateTotalFlagsCount();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e) => ShowAddDialog();

        private void FlagProfiles_Click(object sender, RoutedEventArgs e) => ShowProfilesDialog();

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            App.FastFlags.SaveUndoSnapshot();
            App.FastFlags.suspendUndoSnapshot = true;

            var tempList = new List<FastFlag>();

            foreach (FastFlag entry in DataGrid.SelectedItems)
                tempList.Add(entry);

            foreach (FastFlag entry in tempList)
            {
                _fastFlagList.Remove(entry);
                App.FastFlags.SetValue(entry.Name, null);
            }

            App.FastFlags.suspendUndoSnapshot = false;

            UpdateTotalFlagsCount();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton button)
                return;

            DataGrid.Columns[0].Visibility = button.IsChecked ?? false ? Visibility.Visible : Visibility.Collapsed;

            _showPresets = button.IsChecked ?? true;
            ReloadList();
        }

        private void ExportJSONButton_Click(object sender, RoutedEventArgs e)
        {
            var flags = App.FastFlags.Prop;

            var groupedFlags = flags
                .GroupBy(kvp =>
                {
                    var match = Regex.Match(kvp.Key, @"^[A-Z]+[a-z]*");
                    return match.Success ? match.Value : "Other";
                })
                .OrderBy(g => g.Key);

            var formattedJson = new StringBuilder();
            formattedJson.AppendLine("{");

            int totalItems = flags.Count;
            int writtenItems = 0;
            int groupIndex = 0;

            foreach (var group in groupedFlags)
            {
                if (groupIndex > 0)
                    formattedJson.AppendLine();

                var sortedGroup = group
                    .OrderByDescending(kvp => kvp.Key.Length + (kvp.Value?.ToString()?.Length ?? 0));

                foreach (var kvp in sortedGroup)
                {
                    writtenItems++;
                    bool isLast = (writtenItems == totalItems);
                    string line = $"    \"{kvp.Key}\": \"{kvp.Value}\"";

                    if (!isLast)
                        line += ",";

                    formattedJson.AppendLine(line);
                }

                groupIndex++;
            }

            formattedJson.AppendLine("}");

            SaveJSONToFile(formattedJson.ToString());
        }

        private void CopyJSONButton_Click(object sender, RoutedEventArgs e)
        {
            CopyFormatMode format = App.Settings.Prop.SelectedCopyFormat;

            if (format == CopyFormatMode.Format1)
            {
                string json = JsonSerializer.Serialize(App.FastFlags.Prop, new JsonSerializerOptions { WriteIndented = true });
                Clipboard.SetDataObject(json);
            }
            else if (format == CopyFormatMode.Format2)
            {
                var flags = App.FastFlags.Prop;

                var groupedFlags = flags
                    .GroupBy(kvp =>
                    {
                        var match = Regex.Match(kvp.Key, @"^[A-Z]+[a-z]*");
                        return match.Success ? match.Value : "Other";
                    })
                    .OrderBy(g => g.Key);

                var formattedJson = new StringBuilder();
                formattedJson.AppendLine("{");

                int totalItems = flags.Count;
                int writtenItems = 0;
                int groupIndex = 0;

                foreach (var group in groupedFlags)
                {
                    if (groupIndex > 0)
                        formattedJson.AppendLine();

                    var sortedGroup = group
                        .OrderByDescending(kvp => kvp.Key.Length + (kvp.Value?.ToString()?.Length ?? 0));

                    foreach (var kvp in sortedGroup)
                    {
                        writtenItems++;
                        bool isLast = (writtenItems == totalItems);
                        string line = $"    \"{kvp.Key}\": \"{kvp.Value}\"";

                        if (!isLast)
                            line += ",";

                        formattedJson.AppendLine(line);
                    }

                    groupIndex++;
                }

                formattedJson.AppendLine("}");
                Clipboard.SetText(formattedJson.ToString());
            }
            else if (format == CopyFormatMode.Format3)
            {
                var flags = App.FastFlags.Prop;

                // Sort all flags alphabetically by key
                var sortedFlags = flags.OrderBy(kvp => kvp.Key);

                var formattedJson = new StringBuilder();
                formattedJson.AppendLine("{");

                int totalItems = flags.Count;
                int writtenItems = 0;

                foreach (var kvp in sortedFlags)
                {
                    writtenItems++;
                    bool isLast = (writtenItems == totalItems);
                    string line = $"    \"{kvp.Key}\": \"{kvp.Value}\"";

                    if (!isLast)
                        line += ",";

                    formattedJson.AppendLine(line);
                }

                formattedJson.AppendLine("}");
                Clipboard.SetText(formattedJson.ToString());
            }
            else if (format == CopyFormatMode.Format4) 
            {
                var flags = App.FastFlags.Prop;

                var sortedFlags = flags.OrderByDescending(kvp =>
                    $"    \"{kvp.Key}\": \"{kvp.Value}\"".Length
                );

                var formattedJson = new StringBuilder();
                formattedJson.AppendLine("{");

                int totalItems = flags.Count;
                int writtenItems = 0;

                foreach (var kvp in sortedFlags)
                {
                    writtenItems++;
                    bool isLast = (writtenItems == totalItems);
                    string line = $"    \"{kvp.Key}\": \"{kvp.Value}\"";

                    if (!isLast)
                        line += ",";

                    formattedJson.AppendLine(line);
                }

                formattedJson.AppendLine("}");
                Clipboard.SetText(formattedJson.ToString());

            }
        }


        private void SaveJSONToFile(string json)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|Text files (*.txt)|*.txt",
                Title = "Save JSON or TXT File",
                FileName = "FroststrapExport.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {

                    var filePath = saveFileDialog.FileName;
                    if (string.IsNullOrEmpty(Path.GetExtension(filePath)))
                    {
                        filePath += ".json";
                    }

                    File.WriteAllText(filePath, json);
                    Frontend.ShowMessageBox("JSON file saved successfully!", MessageBoxImage.Information);
                }
                catch (IOException ioEx)
                {
                    Frontend.ShowMessageBox($"Error saving file: {ioEx.Message}", MessageBoxImage.Error);
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    Frontend.ShowMessageBox($"Permission error: {uaEx.Message}", MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    Frontend.ShowMessageBox($"Unexpected error: {ex.Message}", MessageBoxImage.Error);
                }
            }
        }

        private void ShowDeleteAllFlagsConfirmation()
        {
            // Show a confirmation message box to the user
            if (Frontend.ShowMessageBox(
                "Are you sure you want to delete all flags?",
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return; // Exit if the user cancels the action
            }

            // Exit if there are no flags to delete
            if (!HasFlagsToDelete())
            {
                Frontend.ShowMessageBox(
                "There are no flags to delete.",
                MessageBoxImage.Information,
                MessageBoxButton.YesNo);
                return;
            }

            try
            {
                App.FastFlags.SaveUndoSnapshot();
                App.FastFlags.suspendUndoSnapshot = true;
                _fastFlagList.Clear();

                foreach (var key in App.FastFlags.Prop.Keys.ToList())
                {
                    App.FastFlags.SetValue(key, null);
                }
                App.FastFlags.suspendUndoSnapshot = false;

                ReloadList();
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }
        private bool HasFlagsToDelete()
        {
            return _fastFlagList.Any() || App.FastFlags.Prop.Any();
        }

        private void HandleError(Exception ex)
        {
            // Display and log the error message
            Frontend.ShowMessageBox($"An error occurred while deleting flags:\n{ex.Message}", MessageBoxImage.Error, MessageBoxButton.OK);
            LogError(ex); // Logging error in a centralized method
        }

        private void LogError(Exception ex)
        {
            // Detailed logging for developers
            Console.WriteLine(ex.ToString());
        }

        private void DeleteAllButton_Click(object sender, RoutedEventArgs e) => ShowDeleteAllFlagsConfirmation();

        private CancellationTokenSource? _searchCancellationTokenSource;

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textbox) return;

            string newSearch = textbox.Text.Trim();

            if (newSearch == _lastSearch && (DateTime.Now - _lastSearchTime).TotalMilliseconds < _debounceDelay)
                return;

            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();

            _searchFilter = newSearch;
            _lastSearch = newSearch;
            _lastSearchTime = DateTime.Now;

            try
            {
                await Task.Delay(_debounceDelay, _searchCancellationTokenSource.Token);

                if (_searchCancellationTokenSource.Token.IsCancellationRequested)
                    return;

                Dispatcher.Invoke(() =>
                {
                    ReloadList();
                    ShowSearchSuggestion(newSearch);
                });
            }
            catch (TaskCanceledException)
            {
            }
        }


        private void ShowSearchSuggestion(string searchFilter)
        {
            if (string.IsNullOrWhiteSpace(searchFilter))
            {
                AnimateSuggestionVisibility(0);
                return;
            }

            var bestMatch = App.FastFlags.Prop.Keys
                .Where(flag => flag.Contains(searchFilter, StringComparison.OrdinalIgnoreCase))
                .OrderBy(flag => !flag.StartsWith(searchFilter, StringComparison.OrdinalIgnoreCase))
                .ThenBy(flag => flag.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase))
                .ThenBy(flag => flag.Length)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(bestMatch))
            {
                SuggestionKeywordRun.Text = bestMatch;
                AnimateSuggestionVisibility(1);
            }
            else
            {
                AnimateSuggestionVisibility(0);
            }
        }

        private void SuggestionTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var suggestion = SuggestionKeywordRun.Text;
            if (!string.IsNullOrEmpty(suggestion))
            {
                SearchTextBox.Text = suggestion;
                SearchTextBox.CaretIndex = suggestion.Length;
            }
        }

        private void AnimateSuggestionVisibility(double targetOpacity)
        {
            var opacityAnimation = new DoubleAnimation
            {
                To = targetOpacity,
                Duration = TimeSpan.FromMilliseconds(120),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            var translateAnimation = new DoubleAnimation
            {
                To = targetOpacity > 0 ? 0 : 10,
                Duration = TimeSpan.FromMilliseconds(120),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            opacityAnimation.Completed += (s, e) =>
            {
                if (targetOpacity == 0)
                {
                    SuggestionTextBlock.Visibility = Visibility.Collapsed;
                }
            };

            if (targetOpacity > 0)
                SuggestionTextBlock.Visibility = Visibility.Visible;

            SuggestionTextBlock.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
            SuggestionTranslateTransform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
        }
    }
}