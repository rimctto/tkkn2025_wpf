using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Firebase.Database;
using Newtonsoft.Json;
using System.Reflection;

namespace tkkn2025.DataAccess;

/// <summary>
/// RelayCommand implementation for MVVM command binding
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = _ => execute();
        _canExecute = canExecute != null ? _ => canExecute() : null;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);
}

/// <summary>
/// Base ViewModel class implementing INotifyPropertyChanged
/// </summary>
public class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

/// <summary>
/// MVVM ViewModel for Firebase Database Editor
/// </summary>
public class FireBaseEditorVM : BaseViewModel
{
    private FireBaseConnector? _firebaseConnector;
    private ObservableCollection<DatabaseNodeVM> _rootNodes = new();
    private DatabaseNodeVM? _selectedNode;
    private ObservableCollection<DynamicSubNodeItemVM> _subNodes = new();
    private List<string> _availableSortKeys = new();
    private int _selectedSortIndex;
    private string _statusMessage = "Ready";
    private string _connectionStatus = "Not Connected";

    public FireBaseEditorVM()
    {
        InitializeCommands();
        InitializeFirebase();
    }

    #region Properties

    public ObservableCollection<DatabaseNodeVM> RootNodes
    {
        get => _rootNodes;
        set => SetProperty(ref _rootNodes, value);
    }

    public DatabaseNodeVM? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
            {
                _ = OnSelectedNodeChanged();
            }
        }
    }

    public ObservableCollection<DynamicSubNodeItemVM> SubNodes
    {
        get => _subNodes;
        set => SetProperty(ref _subNodes, value);
    }

    public int SelectedSortIndex
    {
        get => _selectedSortIndex;
        set
        {
            if (SetProperty(ref _selectedSortIndex, value))
            {
                SortSubNodes();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public List<string> SortOptions { get; private set; } = new List<string> { "Sort by Key" };

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand CopyNodeCommand { get; private set; } = null!;
    public ICommand AddNodeCommand { get; private set; } = null!;
    public ICommand DeleteNodeCommand { get; private set; } = null!;
    public ICommand SavePropertiesCommand { get; private set; } = null!;
    public ICommand DeleteSubNodeCommand { get; private set; } = null!;
    public ICommand SaveSubNodeCommand { get; private set; } = null!;
    public ICommand AddSubNodeCommand { get; private set; } = null!;

    #endregion

    private void InitializeCommands()
    {
        RefreshCommand = new RelayCommand(async () => await LoadRootNodes());
        CopyNodeCommand = new RelayCommand(async () => await CopySelectedNode(), () => SelectedNode != null);
        AddNodeCommand = new RelayCommand(() => StatusMessage = "Add node functionality requires UI interaction");
        DeleteNodeCommand = new RelayCommand(async () => await DeleteSelectedNode(), () => SelectedNode != null);
        SavePropertiesCommand = new RelayCommand(() => StatusMessage = "Save properties functionality requires UI implementation");
        DeleteSubNodeCommand = new RelayCommand(async (param) => await DeleteSubNode(param as DynamicSubNodeItemVM));
        SaveSubNodeCommand = new RelayCommand(async (param) => await SaveSubNode(param as DynamicSubNodeItemVM));
        AddSubNodeCommand = new RelayCommand(() => StatusMessage = "Add sub-node functionality requires UI interaction");
    }

    private void InitializeFirebase()
    {
        try
        {
            _firebaseConnector = new FireBaseConnector();
            ConnectionStatus = "Connected";
            _ = LoadRootNodes(); // Fire and forget to avoid blocking constructor
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to connect to Firebase: {ex.Message}";
            ConnectionStatus = "Connection Failed";
        }
    }

    private async Task LoadRootNodes()
    {
        if (_firebaseConnector == null) return;

        try
        {
            StatusMessage = "Discovering root nodes...";
            RootNodes.Clear();

            // Try to read from root level and discover available paths
            try
            {
                var rootData = await _firebaseConnector.ReadAllDataAsync<object>("");

                if (rootData?.Count > 0)
                {
                    StatusMessage = $"Found {rootData.Count} root nodes, loading details...";

                    foreach (var rootItem in rootData)
                    {
                        try
                        {
                            var nodePath = rootItem.Key;
                            var nodeData = await _firebaseConnector.ReadAllDataAsync<object>(nodePath);

                            if (nodeData?.Count > 0)
                            {
                                var node = new DatabaseNodeVM
                                {
                                    Name = nodePath,
                                    Path = nodePath,
                                    ChildCount = nodeData.Count,
                                    Data = nodeData
                                };
                                RootNodes.Add(node);
                            }
                            else
                            {
                                // Handle nodes that might be single values or empty
                                var node = new DatabaseNodeVM
                                {
                                    Name = nodePath,
                                    Path = nodePath,
                                    ChildCount = rootItem.Object != null ? 1 : 0,
                                    Data = rootItem.Object != null ? new[] { rootItem } : Array.Empty<FirebaseObject<object>>()
                                };
                                RootNodes.Add(node);
                            }
                        }
                        catch (Exception ex)
                        {
                            StatusMessage = $"Error loading node '{rootItem.Key}': {ex.Message}";
                        }
                    }
                }
                else
                {
                    StatusMessage = "No root nodes found in database";
                }
            }
            catch (Exception rootEx)
            {
                StatusMessage = $"Could not read from root level, falling back to known paths: {rootEx.Message}";
                await LoadKnownRootPaths();
            }

            StatusMessage = $"Successfully loaded {RootNodes.Count} root nodes";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading root nodes: {ex.Message}";
        }
    }

    private async Task LoadKnownRootPaths()
    {
        var knownRootPaths = new[] { "users", "scores", "settings", "games", "sessions" };

        foreach (var path in knownRootPaths)
        {
            try
            {
                var data = await _firebaseConnector!.ReadAllDataAsync<object>(path);
                if (data?.Count > 0)
                {
                    var node = new DatabaseNodeVM
                    {
                        Name = path,
                        Path = path,
                        ChildCount = data.Count,
                        Data = data
                    };
                    RootNodes.Add(node);
                }
            }
            catch
            {
                // Path might not exist, skip it
            }
        }
    }

    private async Task OnSelectedNodeChanged()
    {
        if (SelectedNode == null) return;
        await LoadSubNodesAsDynamic(SelectedNode);
        PopulateSortOptions();
    }

    private async Task LoadSubNodesAsDynamic(DatabaseNodeVM node)
    {
        try
        {
            SubNodes.Clear();
            _availableSortKeys.Clear();
            var allKeys = new HashSet<string>();

            if (node.Data != null)
            {
                foreach (var item in node.Data)
                {
                    var dynamicObject = ConvertToDynamicObject(item.Object);
                    var subNode = new DynamicSubNodeItemVM
                    {
                        Key = item.Key,
                        DynamicData = dynamicObject,
                        FirebaseObject = item
                    };

                    if (dynamicObject is IDictionary<string, object> dict)
                    {
                        foreach (var key in dict.Keys)
                        {
                            allKeys.Add(key);
                        }
                    }

                    SubNodes.Add(subNode);
                }

                _availableSortKeys = allKeys.OrderBy(k => k).ToList();
            }

            StatusMessage = $"Loaded {SubNodes.Count} items as dynamic objects with {_availableSortKeys.Count} sortable properties";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error converting to dynamic objects: {ex.Message}";
        }
    }

    private dynamic ConvertToDynamicObject(object? obj)
    {
        if (obj == null) return new ExpandoObject();

        try
        {
            if (obj is IDictionary<string, object>) return obj;

            var json = JsonConvert.SerializeObject(obj);
            var dynamicObj = JsonConvert.DeserializeObject<ExpandoObject>(json);
            return dynamicObj ?? new ExpandoObject();
        }
        catch
        {
            var expando = new ExpandoObject();
            var expandoDict = expando as IDictionary<string, object>;

            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(obj);
                    expandoDict[prop.Name] = value ?? "";
                }
                catch
                {
                    expandoDict[prop.Name] = "[Error reading value]";
                }
            }

            return expando;
        }
    }

    private void PopulateSortOptions()
    {
        SortOptions.Clear();
        SortOptions.Add("Sort by Key");

        foreach (var key in _availableSortKeys)
        {
            SortOptions.Add($"Sort by {key}");
        }

        OnPropertyChanged(nameof(SortOptions));
        SelectedSortIndex = 0;
    }

    private void SortSubNodes()
    {
        if (SelectedSortIndex < 0 || SubNodes.Count == 0) return;

        try
        {
            List<DynamicSubNodeItemVM> sortedItems;

            if (SelectedSortIndex == 0)
            {
                sortedItems = SubNodes.OrderBy(x => x.Key).ToList();
            }
            else if (SelectedSortIndex - 1 < _availableSortKeys.Count)
            {
                var selectedProperty = _availableSortKeys[SelectedSortIndex - 1];
                sortedItems = SubNodes.OrderBy(x => GetDynamicPropertyValue(x.DynamicData, selectedProperty)).ToList();
            }
            else
            {
                return;
            }

            SubNodes.Clear();
            foreach (var item in sortedItems)
            {
                SubNodes.Add(item);
            }

            var sortKey = SelectedSortIndex == 0 ? "Key" : _availableSortKeys[SelectedSortIndex - 1];
            StatusMessage = $"Sorted {SubNodes.Count} items by '{sortKey}'";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error sorting items: {ex.Message}";
        }
    }

    private object GetDynamicPropertyValue(dynamic obj, string propertyName)
    {
        try
        {
            if (obj is IDictionary<string, object> dict && dict.TryGetValue(propertyName, out var value))
            {
                return value ?? "";
            }

            var property = obj.GetType().GetProperty(propertyName);
            return property?.GetValue(obj) ?? "";
        }
        catch
        {
            return "";
        }
    }

    private async Task CopySelectedNode()
    {
        if (SelectedNode == null || _firebaseConnector == null)
        {
            StatusMessage = "Please select a node to copy";
            return;
        }

        var copyName = $"{SelectedNode.Name}_copy";
        try
        {
            StatusMessage = $"Copying node '{SelectedNode.Name}' to '{copyName}'...";

            var sourceData = await _firebaseConnector.ReadAllDataAsync<object>(SelectedNode.Path);
            if (sourceData == null || sourceData.Count == 0)
            {
                StatusMessage = "No data found in source node to copy";
                return;
            }

            var copiedItems = 0;
            foreach (var item in sourceData)
            {
                try
                {
                    await _firebaseConnector.UpdateDataAsync(copyName, item.Key, item.Object);
                    copiedItems++;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error copying item '{item.Key}': {ex.Message}";
                }
            }

            StatusMessage = $"Successfully copied node '{SelectedNode.Name}' to '{copyName}' with {copiedItems} items";
            await LoadRootNodes();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error copying node: {ex.Message}";
        }
    }

    private async Task DeleteSelectedNode()
    {
        if (SelectedNode == null || _firebaseConnector == null) return;

        try
        {
            StatusMessage = "Deleting node...";

            foreach (var item in SelectedNode.Data ?? Array.Empty<FirebaseObject<object>>())
            {
                await _firebaseConnector.DeleteDataAsync(SelectedNode.Path, item.Key);
            }

            StatusMessage = "Node deleted successfully";
            await LoadRootNodes();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting node: {ex.Message}";
        }
    }

    private async Task DeleteSubNode(DynamicSubNodeItemVM? subNode)
    {
        if (subNode == null || _firebaseConnector == null || SelectedNode == null) return;

        try
        {
            StatusMessage = $"Deleting sub-node '{subNode.Key}'...";
            await _firebaseConnector.DeleteDataAsync(SelectedNode.Path, subNode.Key);
            StatusMessage = $"Sub-node '{subNode.Key}' deleted successfully";

            await LoadSubNodesAsDynamic(SelectedNode);
            PopulateSortOptions();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting sub-node '{subNode.Key}': {ex.Message}";
        }
    }

    private async Task SaveSubNode(DynamicSubNodeItemVM? subNode)
    {
        if (subNode == null || _firebaseConnector == null || SelectedNode == null) return;

        try
        {
            StatusMessage = $"Saving sub-node '{subNode.Key}'...";

            var updates = new Dictionary<string, object>();
            foreach (var propertyItem in subNode.PropertyItems)
            {
                updates[propertyItem.Key] = propertyItem.Value;
            }

            await _firebaseConnector.UpdateDataAsync(SelectedNode.Path, subNode.Key, updates);
            StatusMessage = $"Sub-node '{subNode.Key}' saved successfully with {updates.Count} properties";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving sub-node '{subNode.Key}': {ex.Message}";
        }
    }
}

#region Data Model ViewModels

public class DatabaseNodeVM : BaseViewModel
{
    private string _name = "";
    private string _path = "";
    private int _childCount;
    private IReadOnlyCollection<FirebaseObject<object>>? _data;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Path
    {
        get => _path;
        set => SetProperty(ref _path, value);
    }

    public int ChildCount
    {
        get => _childCount;
        set => SetProperty(ref _childCount, value);
    }

    public IReadOnlyCollection<FirebaseObject<object>>? Data
    {
        get => _data;
        set => SetProperty(ref _data, value);
    }

    public ObservableCollection<DatabaseNodeVM> Children { get; } = new();
}

public class DynamicSubNodeItemVM : BaseViewModel
{
    private string _key = "";
    private dynamic? _dynamicData;
    private FirebaseObject<object>? _firebaseObject;
    private ObservableCollection<PropertyItemVM> _propertyItems = new();

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    public dynamic? DynamicData
    {
        get => _dynamicData;
        set
        {
            if (SetProperty(ref _dynamicData, value))
            {
                UpdatePropertyItems();
            }
        }
    }

    public FirebaseObject<object>? FirebaseObject
    {
        get => _firebaseObject;
        set => SetProperty(ref _firebaseObject, value);
    }

    public ObservableCollection<PropertyItemVM> PropertyItems
    {
        get => _propertyItems;
        set => SetProperty(ref _propertyItems, value);
    }

    private void UpdatePropertyItems()
    {
        PropertyItems.Clear();

        if (DynamicData == null) return;

        try
        {
            if (DynamicData is IDictionary<string, object> dict)
            {
                foreach (var kvp in dict.Where(kvp => !IsNestedObject(kvp.Value)))
                {
                    PropertyItems.Add(new PropertyItemVM
                    {
                        Key = kvp.Key,
                        Value = FormatPropertyValue(kvp.Value),
                        OriginalValue = kvp.Value,
                        ParentSubNode = this
                    });
                }
            }
        }
        catch (Exception ex)
        {
            PropertyItems.Add(new PropertyItemVM
            {
                Key = "Error",
                Value = ex.Message,
                OriginalValue = null,
                ParentSubNode = this
            });
        }
    }

    private bool IsNestedObject(object? value)
    {
        if (value == null) return false;

        var type = value.GetType();
        return !type.IsPrimitive &&
               type != typeof(string) &&
               type != typeof(DateTime) &&
               type != typeof(decimal) &&
               !type.IsEnum &&
               !type.IsValueType;
    }

    private string FormatPropertyValue(object? value)
    {
        if (value == null) return "";

        if (value is DateTime dateTime)
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");

        return value.ToString() ?? "";
    }
}

public class PropertyItemVM : BaseViewModel
{
    private string _key = "";
    private string _value = "";
    private object? _originalValue;
    private DynamicSubNodeItemVM? _parentSubNode;

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public object? OriginalValue
    {
        get => _originalValue;
        set => SetProperty(ref _originalValue, value);
    }

    public DynamicSubNodeItemVM? ParentSubNode
    {
        get => _parentSubNode;
        set => SetProperty(ref _parentSubNode, value);
    }
}

#endregion
