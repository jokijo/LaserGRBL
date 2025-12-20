using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using System.Windows.Input;

namespace AvaloniaGRBL.ViewModels;

public partial class LaserUsageStatsViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<LaserLifeCounterViewModel> _lasers = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyPropertyChangedFor(nameof(CanRemove))]
    [NotifyPropertyChangedFor(nameof(CanMarkDeath))]
    [NotifyPropertyChangedFor(nameof(CanUnmarkDeath))]
    [NotifyPropertyChangedFor(nameof(ShowMarkDeath))]
    [NotifyPropertyChangedFor(nameof(SelectedLaserInfo))]
    private LaserLifeCounterViewModel? _selectedLaser;

    public bool CanEdit => SelectedLaser != null;
    public bool CanRemove => SelectedLaser != null;
    public bool CanMarkDeath => SelectedLaser != null && SelectedLaser.DeathDate == null;
    public bool CanUnmarkDeath => SelectedLaser != null && SelectedLaser.DeathDate != null;
    public bool ShowMarkDeath => SelectedLaser == null || SelectedLaser.DeathDate == null;

    public string SelectedLaserInfo
    {
        get
        {
            if (SelectedLaser == null)
                return "No laser selected";

            return $"Name: {SelectedLaser.Name}\n" +
                   $"Brand: {SelectedLaser.Brand}\n" +
                   $"Model: {SelectedLaser.Model}\n" +
                   $"\n" +
                   $"Run Time: {SelectedLaser.RunTimeFormatted}\n" +
                   $"Power Time: {SelectedLaser.PowerTimeFormatted}\n" +
                   $"Stress Time: {SelectedLaser.StressTimeFormatted}\n" +
                   $"Avg Power: {SelectedLaser.AveragePowerFormatted}\n" +
                   $"\n" +
                   $"Purchased: {SelectedLaser.PurchaseDateFormatted}\n" +
                   $"Monitoring: {SelectedLaser.MonitoringDateFormatted}\n" +
                   (SelectedLaser.DeathDate.HasValue ? $"Ended: {SelectedLaser.DeathDateFormatted}" : "Status: Active");
        }
    }

    public ICommand? CloseCommand { get; set; }

    public LaserUsageStatsViewModel()
    {
        // Sample data for design-time preview
        if (Design.IsDesignMode)
        {
            Lasers.Add(new LaserLifeCounterViewModel
            {
                Name = "Default",
                Brand = "Unknown",
                Model = "Unknown",
                RunTime = TimeSpan.FromHours(100),
                PowerTime = TimeSpan.FromHours(50),
                StressTime = TimeSpan.FromHours(5),
                AveragePower = 0.5,
                PurchaseDate = DateTime.Now.AddMonths(-6),
                MonitoringDate = DateTime.Now.AddMonths(-6),
                DeathDate = null
            });

            Lasers.Add(new LaserLifeCounterViewModel
            {
                Name = "Laser Module 2",
                Brand = "Ortur",
                Model = "LU2-4",
                RunTime = TimeSpan.FromHours(250),
                PowerTime = TimeSpan.FromHours(180),
                StressTime = TimeSpan.FromHours(20),
                AveragePower = 0.72,
                PurchaseDate = DateTime.Now.AddYears(-1),
                MonitoringDate = DateTime.Now.AddYears(-1),
                DeathDate = DateTime.Now.AddDays(-30)
            });
        }
        else
        {
            LoadLasers();
        }
    }

    private void LoadLasers()
    {
        // TODO: Load from actual data source (LaserLifeHandler equivalent)
        // For now, create a default entry if empty
        if (!Lasers.Any())
        {
            Lasers.Add(new LaserLifeCounterViewModel
            {
                Name = "Default",
                Brand = "Unknown",
                Model = "Unknown",
                RunTime = TimeSpan.Zero,
                PowerTime = TimeSpan.Zero,
                StressTime = TimeSpan.Zero,
                AveragePower = 0,
                PurchaseDate = DateTime.Today,
                MonitoringDate = DateTime.Today,
                DeathDate = null
            });
        }
    }

    [RelayCommand]
    private void AddNew()
    {
        // TODO: Open add/edit dialog
        var newLaser = new LaserLifeCounterViewModel
        {
            Name = "New Laser",
            Brand = "Unknown",
            Model = "Unknown",
            RunTime = TimeSpan.Zero,
            PowerTime = TimeSpan.Zero,
            StressTime = TimeSpan.Zero,
            AveragePower = 0,
            PurchaseDate = DateTime.Today,
            MonitoringDate = DateTime.Today,
            DeathDate = null
        };
        Lasers.Add(newLaser);
        SelectedLaser = newLaser;
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedLaser == null)
            return;

        // TODO: Open edit dialog
    }

    [RelayCommand]
    private void Remove()
    {
        if (SelectedLaser == null)
            return;

        // TODO: Add confirmation dialog
        Lasers.Remove(SelectedLaser);
    }

    [RelayCommand]
    private void MarkDeath()
    {
        if (SelectedLaser == null)
            return;

        // TODO: Add confirmation dialog
        SelectedLaser.DeathDate = DateTime.Today;
        OnPropertyChanged(nameof(SelectedLaser));
    }

    [RelayCommand]
    private void UnmarkDeath()
    {
        if (SelectedLaser == null)
            return;

        SelectedLaser.DeathDate = null;
        OnPropertyChanged(nameof(SelectedLaser));
    }
}

public partial class LaserLifeCounterViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "Default";

    [ObservableProperty]
    private string _brand = "Unknown";

    [ObservableProperty]
    private string _model = "Unknown";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RunTimeFormatted))]
    private TimeSpan _runTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerTimeFormatted))]
    private TimeSpan _powerTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StressTimeFormatted))]
    private TimeSpan _stressTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AveragePowerFormatted))]
    private double _averagePower;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PurchaseDateFormatted))]
    private DateTime? _purchaseDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonitoringDateFormatted))]
    private DateTime? _monitoringDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeathDateFormatted))]
    private DateTime? _deathDate;

    public string RunTimeFormatted => $"{RunTime.TotalHours:0.0} h";
    public string PowerTimeFormatted => $"{PowerTime.TotalHours:0.0} h";
    public string StressTimeFormatted => $"{StressTime.TotalHours:0.0} h";
    public string AveragePowerFormatted => $"{Math.Round(AveragePower * 100, 0)} %";
    public string PurchaseDateFormatted => PurchaseDate?.ToShortDateString() ?? "";
    public string MonitoringDateFormatted => MonitoringDate?.ToShortDateString() ?? "";
    public string DeathDateFormatted => DeathDate?.ToShortDateString() ?? "";
}
