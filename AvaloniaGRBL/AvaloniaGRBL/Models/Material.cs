using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaGRBL.Models;

public partial class Material : ObservableObject
{
    [ObservableProperty]
    private Guid _id = Guid.NewGuid();
    
    [ObservableProperty]
    private bool _visible = true;
    
    [ObservableProperty]
    private string _model = "Generic Laser";
    
    [ObservableProperty]
    private string _materialName = "Generic Material";
    
    [ObservableProperty]
    private string _thickness = "";
    
    [ObservableProperty]
    private string _action = "";
    
    [ObservableProperty]
    private int _power = 100;
    
    [ObservableProperty]
    private int _speed = 1000;
    
    [ObservableProperty]
    private int _cycles = 1;
    
    [ObservableProperty]
    private string _remarks = "";
    
    public Material()
    {
    }
    
    public Material(Guid id, bool visible, string model, string materialName, string thickness, 
                   string action, int power, int speed, int cycles, string remarks)
    {
        Id = id;
        Visible = visible;
        Model = model;
        MaterialName = materialName;
        Thickness = thickness;
        Action = action;
        Power = power;
        Speed = speed;
        Cycles = cycles;
        Remarks = remarks;
    }
}
