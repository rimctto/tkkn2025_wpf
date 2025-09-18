using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Firebase.Database;
using tkkn2025.DataAccess;
using System.Dynamic;
using Newtonsoft.Json;
using System.Reflection;

namespace tkkn2025.UI.Windows
{
    /// <summary>
    /// Firebase Database Editor window for viewing and editing Firebase data
    /// </summary>
    public partial class FireBaseEditor : Window
    {
        private FireBaseConnector? _firebaseConnector;
        private ObservableCollection<DatabaseNode> _rootNodes;
        private DatabaseNode? _selectedNode;
        private ObservableCollection<DynamicSubNodeItem> _subNodes;
        private List<string> _availableSortKeys;

        public FireBaseEditor()
        {
            InitializeComponent();
            InitializeCollections();
            InitializeFirebase();                                   
        }

        private void InitializeCollections()
        {
            _rootNodes = new ObservableCollection<DatabaseNode>();
            _subNodes = new ObservableCollection<DynamicSubNodeItem>();
            _availableSortKeys = new List<string>();
            
            DatabaseTreeView.ItemsSource = _rootNodes;
            SubNodesListBox.ItemsSource = _subNodes;
            
            // Set default sort option without triggering the event during initialization
            if (SortComboBox != null)
            {
                SortComboBox.SelectedIndex = 0; // Sort by Key by default
            }
        }

        private void InitializeFirebase()
        {
            try
            {
                _firebaseConnector = new FireBaseConnector();
                UpdateConnectionStatus("Connected");
                _ = LoadRootNodes(); // Fire and forget to avoid blocking constructor
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to connect to Firebase: {ex.Message}");
                UpdateConnectionStatus("Connection Failed");
            }
        }

        private async Task LoadRootNodes()
        {
            if (_firebaseConnector == null) return;

            try
            {
               UpdateStatus("Discovering root nodes...");
                
                _rootNodes.Clear();
                
                // First approach: Try to read from root and discover available paths
                try
                {
                    // Read all data from the root level
                    var rootData = await _firebaseConnector.ReadAllDataAsync<object>("");
                    
                    if (rootData?.Count > 0)
                    {
                        UpdateStatus($"Found {rootData.Count} root nodes, loading details...");
                        
                        foreach (var rootItem in rootData)
                        {
                            try
                            {
                                // Each root item key is a potential node path
                                var nodePath = rootItem.Key;
                                
                                // Read the data for this specific path to get accurate count and structure
                                var nodeData = await _firebaseConnector.ReadAllDataAsync<object>(nodePath);
                                
                                if (nodeData?.Count > 0)
                                {
                                    var node = new DatabaseNode
                                    {
                                        Name = nodePath,
                                        Path = nodePath,
                                        ChildCount = nodeData.Count,
                                        Data = nodeData
                                    };
                                    _rootNodes.Add(node);
                                    UpdateStatus($"Loaded node '{nodePath}' with {nodeData.Count} items");
                                }
                                else
                                {
                                    // Handle nodes that might be single values or empty
                                    var node = new DatabaseNode
                                    {
                                        Name = nodePath,
                                        Path = nodePath,
                                        ChildCount = rootItem.Object != null ? 1 : 0,
                                        Data = rootItem.Object != null ? new[] { rootItem } : Array.Empty<FirebaseObject<object>>()
                                    };
                                    _rootNodes.Add(node);
                                    UpdateStatus($"Loaded node '{nodePath}' as single value");
                                }
                            }
                            catch (Exception ex)
                            {
                                UpdateStatus($"Error loading node '{rootItem.Key}': {ex.Message}");
                                // Continue with other nodes
                            }
                        }
                    }
                    else
                    {
                        UpdateStatus("No root nodes found in database");
                    }
                }
                catch (Exception rootEx)
                {
                    UpdateStatus($"Could not read from root level, falling back to known paths: {rootEx.Message}");
                    
                    // Fallback to the original approach with known paths
                    await LoadKnownRootPaths();
                }

                UpdateStatus($"Successfully loaded {_rootNodes.Count} root nodes");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading root nodes: {ex.Message}");
            }
        }

        /// <summary>
        /// Fallback method to load known root paths when dynamic discovery fails
        /// </summary>
        private async Task LoadKnownRootPaths()
        {
            var knownRootPaths = new[] { "users", "scores", "settings", "games", "sessions" };
            
            foreach (var path in knownRootPaths)
            {
                try
                {
                    var data = await _firebaseConnector.ReadAllDataAsync<object>(path);
                    if (data?.Count > 0)
                    {
                        var node = new DatabaseNode
                        {
                            Name = path,
                            Path = path,
                            ChildCount = data.Count,
                            Data = data
                        };
                        _rootNodes.Add(node);
                        UpdateStatus($"Loaded known path '{path}' with {data.Count} items");
                    }
                }
                catch
                {
                    // Path might not exist, skip it
                }
            }
        }

        private async void DatabaseTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is DatabaseNode selectedNode)
            {
                _selectedNode = selectedNode;
                await LoadNodeDetails(selectedNode);
                await LoadSubNodesAsDynamic(selectedNode);
                PopulateSortOptions();
            }
        }

        private async Task LoadNodeDetails(DatabaseNode node)
        {
            try
            {
                PropertiesPanel.Children.Clear();
                
                if (node.Data != null && node.Data.Any())
                {
                    // Show general node information
                    AddPropertyEditor("Node Name", node.Name, false);
                    AddPropertyEditor("Path", node.Path, false);
                    AddPropertyEditor("Child Count", node.ChildCount.ToString(), false);
                    
                    // If it's a single object node, show its properties
                    if (node.Data.Count() == 1)
                    {
                        var firstItem = node.Data.First();
                        var objectData = firstItem.Object;
                        
                        if (objectData != null)
                        {
                            var properties = objectData.GetType().GetProperties();
                            foreach (var prop in properties)
                            {
                                try
                                {
                                    var value = prop.GetValue(objectData)?.ToString() ?? "";
                                    AddPropertyEditor(prop.Name, value, true, prop.Name);
                                }
                                catch
                                {
                                    AddPropertyEditor(prop.Name, "[Error reading value]", false);
                                }
                            }
                        }
                    }
                }

                PropertiesStatusText.Text = $"Showing properties for: {node.Name}";
            }
            catch (Exception ex)
            {
                PropertiesStatusText.Text = $"Error loading properties: {ex.Message}";
            }
        }

        private void AddPropertyEditor(string propertyName, string value, bool isEditable, string? propertyKey = null)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.Margin = new Thickness(0, 2, 0, 2);

            var label = new TextBlock
            {
                Text = propertyName + ":",
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(label, 0);

            var textBox = new TextBox
            {
                Text = value,
                IsReadOnly = !isEditable,
                Tag = propertyKey,
                Style = (Style)FindResource("PropertyTextBoxStyle")
            };
            
            if (!isEditable)
            {
                textBox.Background = System.Windows.Media.Brushes.LightGray;
            }
            
            Grid.SetColumn(textBox, 1);

            grid.Children.Add(label);
            grid.Children.Add(textBox);
            PropertiesPanel.Children.Add(grid);
        }

        private async Task LoadSubNodesAsDynamic(DatabaseNode node)
        {
            try
            {
                // Clear sub-nodes collection safely
                if (_subNodes.Count > 0)
                {
                    _subNodes.Clear();
                }
                
                _availableSortKeys.Clear();
                var allKeys = new HashSet<string>();
                
                if (node.Data != null)
                {
                    foreach (var item in node.Data)
                    {
                        var dynamicObject = ConvertToDynamicObject(item.Object);
                        var subNode = new DynamicSubNodeItem
                        {
                            Key = item.Key,
                            DynamicData = dynamicObject,
                            FirebaseObject = item
                        };
                        
                        // Collect all available keys for sorting
                        if (dynamicObject is IDictionary<string, object> dict)
                        {
                            foreach (var key in dict.Keys)
                            {
                                allKeys.Add(key);
                            }
                        }
                        
                        _subNodes.Add(subNode);
                    }
                    
                    // Update available sort keys
                    _availableSortKeys.AddRange(allKeys.OrderBy(k => k));
                }

                UpdateStatus($"Loaded {_subNodes.Count} items as dynamic objects with {_availableSortKeys.Count} sortable properties");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error converting to dynamic objects: {ex.Message}");
            }
        }

        private dynamic ConvertToDynamicObject(object? obj)
        {
            if (obj == null) return new ExpandoObject();

            try
            {
                // If it's already a dynamic object or dictionary, return as is
                if (obj is IDictionary<string, object>) return obj;
                
                // Try to serialize and deserialize as JSON to get dynamic object
                var json = JsonConvert.SerializeObject(obj);
                var dynamicObj = JsonConvert.DeserializeObject<ExpandoObject>(json);
                return dynamicObj ?? new ExpandoObject();
            }
            catch
            {
                // Fallback: use reflection to create dynamic object
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
            if (SortComboBox == null) return;

            // Store current selection
            var currentSelection = SortComboBox.SelectedIndex;
            
            // Clear and repopulate sort options
            SortComboBox.Items.Clear();
            SortComboBox.Items.Add("Sort by Key");
            
            // Add dynamic property-based sorting options
            foreach (var key in _availableSortKeys)
            {
                SortComboBox.Items.Add($"Sort by {key}");
            }
            
            // Restore selection or default to first option
            SortComboBox.SelectedIndex = currentSelection >= 0 && currentSelection < SortComboBox.Items.Count ? currentSelection : 0;
        }

        private void SortSubNodes()
        {
            if (SortComboBox.SelectedIndex < 0 || _subNodes.Count == 0) return;

            try
            {
                List<DynamicSubNodeItem> sortedItems;

                if (SortComboBox.SelectedIndex == 0)
                {
                    // Sort by Key
                    sortedItems = _subNodes.OrderBy(x => x.Key).ToList();
                }
                else
                {
                    // Sort by dynamic property
                    var selectedProperty = _availableSortKeys[SortComboBox.SelectedIndex - 1];
                    sortedItems = _subNodes.OrderBy(x => GetDynamicPropertyValue(x.DynamicData, selectedProperty)).ToList();
                }

                // Move items to their correct positions
                for (int i = 0; i < sortedItems.Count; i++)
                {
                    var currentIndex = _subNodes.IndexOf(sortedItems[i]);
                    if (currentIndex != i && currentIndex >= 0)
                    {
                        _subNodes.Move(currentIndex, i);
                    }
                }

                var sortKey = SortComboBox.SelectedIndex == 0 ? "Key" : _availableSortKeys[SortComboBox.SelectedIndex - 1];
                UpdateStatus($"Sorted {_subNodes.Count} items by '{sortKey}'");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error sorting items: {ex.Message}");
            }
        }

        private object GetDynamicPropertyValue(dynamic obj, string propertyName)
        {
            try
            {
                if (obj is IDictionary<string, object> dict)
                {
                    if (dict.TryGetValue(propertyName, out var value))
                    {
                        return value ?? "";
                    }
                }
                
                // Try to access property directly
                var property = obj.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    return property.GetValue(obj) ?? "";
                }
                
                return "";
            }
            catch
            {
                return "";
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Add a check to ensure we have sub-nodes to sort
            if (_subNodes?.Count > 0)
            {
                SortSubNodes();
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadRootNodes();
        }

        private async void SavePropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode == null || _firebaseConnector == null) return;

            try
            {
                UpdateStatus("Saving properties...");
                
                // Collect edited properties
                var updates = new Dictionary<string, string>();
                foreach (var child in PropertiesPanel.Children.OfType<Grid>())
                {
                    var textBox = child.Children.OfType<TextBox>().FirstOrDefault();
                    if (textBox != null && !textBox.IsReadOnly && textBox.Tag is string propertyKey)
                    {
                        updates[propertyKey] = textBox.Text;
                    }
                }

                if (updates.Any())
                {
                    // Note: This is a simplified update mechanism
                    // In a real application, you'd need more sophisticated handling based on data types
                    await _firebaseConnector.UpdateDataAsync(_selectedNode.Path, _selectedNode.Data.First().Key, updates);
                    UpdateStatus("Properties saved successfully");
                    await LoadNodeDetails(_selectedNode);
                }
                else
                {
                    UpdateStatus("No editable properties to save");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error saving properties: {ex.Message}");
            }
        }

        private async void DeleteNodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode == null || _firebaseConnector == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete the node '{_selectedNode.Name}' and all its data?",
                "Delete Node",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    UpdateStatus("Deleting node...");
                    
                    // Delete all items in this node
                    foreach (var item in _selectedNode.Data)
                    {
                        await _firebaseConnector.DeleteDataAsync(_selectedNode.Path, item.Key);
                    }
                    
                    UpdateStatus("Node deleted successfully");
                    await LoadRootNodes();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error deleting node: {ex.Message}");
                }
            }
        }

        private void AddNodeButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddNodeDialog();
            if (dialog.ShowDialog() == true)
            {
                // Handle adding new root node
                // This would require additional implementation
                UpdateStatus($"Add node functionality not yet implemented for: {dialog.NodeName}");
            }
        }

        private async void CopyNodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode == null || _firebaseConnector == null)
            {
                UpdateStatus("Please select a node to copy");
                return;
            }

            var dialog = new CopyNodeDialog(_selectedNode.Name);
            if (dialog.ShowDialog() == true)
            {
                await CopyNodeWithAllData(_selectedNode, dialog.CopyName);
            }
        }

        private async Task CopyNodeWithAllData(DatabaseNode sourceNode, string copyName)
        {
            if (_firebaseConnector == null) return;

            try
            {
                UpdateStatus($"Copying node '{sourceNode.Name}' to '{copyName}'...");

                // Read all data from the source node
                var sourceData = await _firebaseConnector.ReadAllDataAsync<object>(sourceNode.Path);
                if (sourceData == null || sourceData.Count == 0)
                {
                    UpdateStatus("No data found in source node to copy");
                    return;
                }

                var totalItems = sourceData.Count;
                var copiedItems = 0;

                // Copy each item to the new node
                foreach (var item in sourceData)
                {
                    try
                    {
                        // Write the item to the new path using the same key
                        await _firebaseConnector.UpdateDataAsync(copyName, item.Key, item.Object);
                        copiedItems++;
                        
                        // Update progress
                        UpdateStatus($"Copying... {copiedItems}/{totalItems} items copied");
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus($"Error copying item '{item.Key}': {ex.Message}");
                        // Continue with other items even if one fails
                    }
                }

                UpdateStatus($"Successfully copied node '{sourceNode.Name}' to '{copyName}' with {copiedItems} items");
                
                // Refresh the root nodes to show the new copied node
                await LoadRootNodes();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error copying node: {ex.Message}");
            }
        }

        private void SubNodesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SubNodesListBox.SelectedItem is DynamicSubNodeItem selectedSubNode)
            {
                SubNodeDetailsHeader.Text = "Sub-Node Information";
                SubNodeSelectionInfo.Text = $"Selected: {selectedSubNode.Key} with {selectedSubNode.PropertyItems.Count} properties";
            }
            else
            {
                SubNodeDetailsHeader.Text = "Sub-Node Information";
                SubNodeSelectionInfo.Text = "Select a sub-node above to see details";
            }
        }

        private async void SaveSubNodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DynamicSubNodeItem subNode && _firebaseConnector != null && _selectedNode != null)
            {
                try
                {
                    UpdateStatus($"Saving sub-node '{subNode.Key}'...");

                    // Collect all property updates
                    var updates = new Dictionary<string, object>();
                    foreach (var propertyItem in subNode.PropertyItems)
                    {
                        try
                        {
                            // Convert the string value back to the appropriate type
                            var convertedValue = ConvertStringToPropertyType(propertyItem.Value, propertyItem.OriginalValue);
                            updates[propertyItem.Key] = convertedValue;
                            
                            // Update the original value and dynamic data
                            propertyItem.OriginalValue = convertedValue;
                            if (subNode.DynamicData is IDictionary<string, object> dict)
                            {
                                dict[propertyItem.Key] = convertedValue;
                            }
                        }
                        catch (Exception ex)
                        {
                            UpdateStatus($"Error converting property '{propertyItem.Key}': {ex.Message}");
                            return;
                        }
                    }

                    // Save to Firebase
                    await _firebaseConnector.UpdateDataAsync(_selectedNode.Path, subNode.Key, updates);
                    UpdateStatus($"Sub-node '{subNode.Key}' saved successfully with {updates.Count} properties");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error saving sub-node '{subNode.Key}': {ex.Message}");
                }
            }
        }

        private void AddSubNodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode == null) return;

            var dialog = new AddNodeDialog();
            if (dialog.ShowDialog() == true)
            {
                // Handle adding new sub-node
                // This would require additional implementation
                UpdateStatus($"Add sub-node functionality not yet implemented for: {dialog.NodeName}");
            }
        }

        private async void DeleteSubNodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DynamicSubNodeItem subNode && _firebaseConnector != null && _selectedNode != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete the sub-node '{subNode.Key}'?\n\nThis action cannot be undone and will permanently remove all data for this item.",
                    "Confirm Delete Sub-Node",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        UpdateStatus($"Deleting sub-node '{subNode.Key}'...");
                        await _firebaseConnector.DeleteDataAsync(_selectedNode.Path, subNode.Key);
                        UpdateStatus($"Sub-node '{subNode.Key}' deleted successfully");
                        
                        // Refresh the sub-nodes list
                        await LoadSubNodesAsDynamic(_selectedNode);
                        PopulateSortOptions();
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus($"Error deleting sub-node '{subNode.Key}': {ex.Message}");
                    }
                }
            }
        }

        private object ConvertStringToPropertyType(string stringValue, object? originalValue)
        {
            if (originalValue == null) return stringValue;

            try
            {
                var originalType = originalValue.GetType();
                
                if (originalType == typeof(int) && int.TryParse(stringValue, out int intValue))
                    return intValue;
                if (originalType == typeof(long) && long.TryParse(stringValue, out long longValue))
                    return longValue;
                if (originalType == typeof(double) && double.TryParse(stringValue, out double doubleValue))
                    return doubleValue;
                if (originalType == typeof(float) && float.TryParse(stringValue, out float floatValue))
                    return floatValue;
                if (originalType == typeof(bool) && bool.TryParse(stringValue, out bool boolValue))
                    return boolValue;
                if (originalType == typeof(DateTime) && DateTime.TryParse(stringValue, out DateTime dateValue))
                    return dateValue;
                
                // Default to string
                return stringValue;
            }
            catch
            {
                return stringValue;
            }
        }

        private string FormatPropertyValue(object? value)
        {
            if (value == null) return "";
            
            if (value is DateTime dateTime)
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            
            return value.ToString() ?? "";
        }

        private bool IsNestedObject(object? value)
        {
            if (value == null) return false;
            
            var type = value.GetType();
            
            // Consider primitives, strings, and common value types as non-nested
            return !type.IsPrimitive && 
                   type != typeof(string) && 
                   type != typeof(DateTime) && 
                   type != typeof(decimal) && 
                   !type.IsEnum &&
                   !type.IsValueType;
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        private void UpdateConnectionStatus(string status)
        {
            ConnectionStatusText.Text = status;
        }

        protected override void OnClosed(EventArgs e)
        {
            _firebaseConnector?.Dispose();
            base.OnClosed(e);
        }

        private void PropertyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is PropertyItem propertyItem)
            {
                try
                {
                    // Update the property value
                    propertyItem.Value = textBox.Text;
                    UpdateStatus($"Property '{propertyItem.Key}' modified (not yet saved to Firebase)");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error updating property: {ex.Message}");
                }
            }
        }
    }

    // Data models for the Firebase Editor
    public class DatabaseNode : INotifyPropertyChanged
    {
        private string _name = "";
        private string _path = "";
        private int _childCount;
        private IReadOnlyCollection<FirebaseObject<object>>? _data;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Path
        {
            get => _path;
            set { _path = value; OnPropertyChanged(); }
        }

        public int ChildCount
        {
            get => _childCount;
            set { _childCount = value; OnPropertyChanged(); }
        }

        public IReadOnlyCollection<FirebaseObject<object>>? Data
        {
            get => _data;
            set { _data = value; OnPropertyChanged(); }
        }

        public ObservableCollection<DatabaseNode> Children { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }
    }

    public class DynamicSubNodeItem : INotifyPropertyChanged
    {
        private string _key = "";
        private dynamic? _dynamicData;
        private FirebaseObject<object>? _firebaseObject;
        private ObservableCollection<PropertyItem> _propertyItems;

        public DynamicSubNodeItem()
        {
            _propertyItems = new ObservableCollection<PropertyItem>();
        }

        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(); }
        }

        public dynamic? DynamicData
        {
            get => _dynamicData;
            set 
            { 
                _dynamicData = value; 
                OnPropertyChanged(); 
                UpdatePropertyItems();
            }
        }

        public FirebaseObject<object>? FirebaseObject
        {
            get => _firebaseObject;
            set { _firebaseObject = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PropertyItem> PropertyItems
        {
            get => _propertyItems;
            set { _propertyItems = value; OnPropertyChanged(); }
        }

        private void UpdatePropertyItems()
        {
            _propertyItems.Clear();
            
            if (DynamicData == null) return;

            try
            {
                if (DynamicData is IDictionary<string, object> dict)
                {
                    foreach (var kvp in dict.Where(kvp => !IsNestedObject(kvp.Value)))
                    {
                        _propertyItems.Add(new PropertyItem
                        {
                            Key = kvp.Key,
                            Value = FormatPropertyValue(kvp.Value),
                            OriginalValue = kvp.Value,
                            ParentSubNode = this
                        });
                    }
                }
                else
                {
                    // Fallback: use reflection for other types
                    var objProperties = DynamicData.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in objProperties)
                    {
                        try
                        {
                            var value = prop.GetValue(DynamicData);
                            if (!IsNestedObject(value))
                            {
                                _propertyItems.Add(new PropertyItem
                                {
                                    Key = prop.Name,
                                    Value = FormatPropertyValue(value),
                                    OriginalValue = value,
                                    ParentSubNode = this
                                });
                            }
                        }
                        catch
                        {
                            _propertyItems.Add(new PropertyItem
                            {
                                Key = prop.Name,
                                Value = "[Error reading value]",
                                OriginalValue = null,
                                ParentSubNode = this
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Add error property item
                _propertyItems.Add(new PropertyItem
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
            
            // Consider primitives, strings, and common value types as non-nested
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }
    }

    // Property item for individual key-value pairs
    public class PropertyItem : INotifyPropertyChanged
    {
        private string _key = "";
        private string _value = "";
        private object? _originalValue;
        private DynamicSubNodeItem? _parentSubNode;

        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public object? OriginalValue
        {
            get => _originalValue;
            set { _originalValue = value; OnPropertyChanged(); }
        }

        public DynamicSubNodeItem? ParentSubNode
        {
            get => _parentSubNode;
            set { _parentSubNode = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Simple dialog for adding new nodes
    public class AddNodeDialog : Window
    {
        public string NodeName { get; private set; } = "";

        public AddNodeDialog()
        {
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            Title = "Add New Node";
            Width = 300;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.Margin = new Thickness(10);

            var label = new TextBlock { Text = "Node Name:", Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(label, 0);

            var textBox = new TextBox { Name = "NodeNameTextBox", Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(textBox, 1);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 0, 5, 0) };
            var cancelButton = new Button { Content = "Cancel", Width = 75 };

            okButton.Click += (s, e) =>
            {
                NodeName = textBox.Text;
                DialogResult = true;
                Close();
            };

            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(label);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }

    // Simple dialog for copying nodes
    public class CopyNodeDialog : Window
    {
        public string CopyName { get; private set; } = "";
        private TextBox _copyNameTextBox;

        public CopyNodeDialog(string originalNodeName)
        {
            InitializeDialog(originalNodeName);
        }

        private void InitializeDialog(string originalNodeName)
        {
            Title = "Copy Node";
            Width = 350;
            Height = 180;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.Margin = new Thickness(10);

            var infoLabel = new TextBlock 
            { 
                Text = $"Copying node: '{originalNodeName}'", 
                Margin = new Thickness(0, 0, 0, 5),
                FontWeight = FontWeights.Bold
            };
            Grid.SetRow(infoLabel, 0);

            var label = new TextBlock { Text = "New node name:", Margin = new Thickness(0, 5, 0, 5) };
            Grid.SetRow(label, 1);

            _copyNameTextBox = new TextBox 
            { 
                Text = $"{originalNodeName}_copy",
                Margin = new Thickness(0, 0, 0, 10) 
            };
            Grid.SetRow(_copyNameTextBox, 2);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 0, 5, 0) };
            var cancelButton = new Button { Content = "Cancel", Width = 75 };

            okButton.Click += (s, e) =>
            {
                var copyName = _copyNameTextBox.Text.Trim();
                if (string.IsNullOrEmpty(copyName))
                {
                    MessageBox.Show("Please enter a valid node name.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (copyName == originalNodeName)
                {
                    MessageBox.Show("The copy name cannot be the same as the original node name.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CopyName = copyName;
                DialogResult = true;
                Close();
            };

            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 3);

            grid.Children.Add(infoLabel);
            grid.Children.Add(label);
            grid.Children.Add(_copyNameTextBox);
            grid.Children.Add(buttonPanel);

            Content = grid;
            
            // Focus on the text box and select the default text for easy editing
            Loaded += (s, e) =>
            {
                _copyNameTextBox.Focus();
                _copyNameTextBox.SelectAll();
            };
        }
    }
}