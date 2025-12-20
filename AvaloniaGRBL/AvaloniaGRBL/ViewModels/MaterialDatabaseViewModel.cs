using System;
using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaGRBL.Models;
using AvaloniaGRBL.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaGRBL.ViewModels;

public partial class MaterialDatabaseViewModel : ViewModelBase
{
    private readonly MaterialDatabaseService _materialService;
    
    [ObservableProperty]
    private ObservableCollection<Material> _materials = new();
    
    [ObservableProperty]
    private Material? _selectedMaterial;
    
    [ObservableProperty]
    private string _statusMessage = "Material database loaded";
    
    [ObservableProperty]
    private string _filterModel = "";
    
    [ObservableProperty]
    private string _filterMaterial = "";
    
    [ObservableProperty]
    private string _filterAction = "";
    
    public MaterialDatabaseViewModel()
    {
        _materialService = new MaterialDatabaseService();
        LoadMaterials();
    }
    
    private void LoadMaterials()
    {
        try
        {
            var materials = _materialService.LoadMaterials();
            Materials.Clear();
            foreach (var material in materials)
            {
                Materials.Add(material);
            }
            StatusMessage = $"Loaded {Materials.Count} materials";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading materials: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private void AddMaterial()
    {
        var newMaterial = new Material
        {
            Id = Guid.NewGuid(),
            Visible = true,
            Model = "Generic Laser",
            MaterialName = "New Material",
            Thickness = "",
            Action = "Cut",
            Power = 100,
            Speed = 1000,
            Cycles = 1,
            Remarks = ""
        };
        
        Materials.Add(newMaterial);
        SelectedMaterial = newMaterial;
        StatusMessage = "New material added";
    }
    
    [RelayCommand]
    private void DeleteMaterial()
    {
        if (SelectedMaterial != null)
        {
            Materials.Remove(SelectedMaterial);
            StatusMessage = "Material deleted";
        }
    }
    
    [RelayCommand]
    private void DuplicateMaterial()
    {
        if (SelectedMaterial != null)
        {
            var duplicate = new Material(
                Guid.NewGuid(),
                SelectedMaterial.Visible,
                SelectedMaterial.Model,
                SelectedMaterial.MaterialName,
                SelectedMaterial.Thickness,
                SelectedMaterial.Action,
                SelectedMaterial.Power,
                SelectedMaterial.Speed,
                SelectedMaterial.Cycles,
                SelectedMaterial.Remarks
            );
            
            Materials.Add(duplicate);
            SelectedMaterial = duplicate;
            StatusMessage = "Material duplicated";
        }
    }
    
    [RelayCommand]
    private void SaveChanges()
    {
        try
        {
            _materialService.SaveMaterials(Materials);
            StatusMessage = $"Saved {Materials.Count} materials successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving materials: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private void ReloadMaterials()
    {
        LoadMaterials();
    }
    
    [RelayCommand]
    private void ClearFilter()
    {
        FilterModel = "";
        FilterMaterial = "";
        FilterAction = "";
        StatusMessage = "Filter cleared";
    }
    
    public ObservableCollection<Material> FilteredMaterials
    {
        get
        {
            var filtered = Materials.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(FilterModel))
            {
                filtered = filtered.Where(m => m.Model.Contains(FilterModel, StringComparison.OrdinalIgnoreCase));
            }
            
            if (!string.IsNullOrWhiteSpace(FilterMaterial))
            {
                filtered = filtered.Where(m => m.MaterialName.Contains(FilterMaterial, StringComparison.OrdinalIgnoreCase));
            }
            
            if (!string.IsNullOrWhiteSpace(FilterAction))
            {
                filtered = filtered.Where(m => m.Action.Contains(FilterAction, StringComparison.OrdinalIgnoreCase));
            }
            
            return new ObservableCollection<Material>(filtered);
        }
    }
    
    public event EventHandler? CloseRequested;
    
    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
