using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AvaloniaGRBL.Models;

namespace AvaloniaGRBL.Services;

public class MaterialDatabaseService
{
    private static readonly string DataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AvaloniaGRBL"
    );
    
    private static readonly string UserMaterialsFile = Path.Combine(DataPath, "UserMaterials.xml");
    private static readonly string StandardMaterialsFile = Path.Combine(DataPath, "StandardMaterials.xml");
    
    public MaterialDatabaseService()
    {
        // Ensure data directory exists
        if (!Directory.Exists(DataPath))
            Directory.CreateDirectory(DataPath);
    }
    
    public List<Material> LoadMaterials()
    {
        var materials = new List<Material>();
        
        try
        {
            // Try to load user materials first
            if (File.Exists(UserMaterialsFile))
            {
                materials.AddRange(LoadFromFile(UserMaterialsFile));
            }
            
            // If no user materials, load standard materials
            if (materials.Count == 0 && File.Exists(StandardMaterialsFile))
            {
                materials.AddRange(LoadFromFile(StandardMaterialsFile));
            }
            
            // If still no materials, add some default ones
            if (materials.Count == 0)
            {
                materials.AddRange(GetDefaultMaterials());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading materials: {ex.Message}");
            materials.AddRange(GetDefaultMaterials());
        }
        
        return materials;
    }
    
    public void SaveMaterials(IEnumerable<Material> materials)
    {
        try
        {
            var doc = new XDocument(
                new XElement("MaterialDB",
                    new XElement("Materials",
                        materials.Select(m => new XElement("Material",
                            new XElement("Id", m.Id),
                            new XElement("Visible", m.Visible),
                            new XElement("Model", m.Model),
                            new XElement("MaterialName", m.MaterialName),
                            new XElement("Thickness", m.Thickness),
                            new XElement("Action", m.Action),
                            new XElement("Power", m.Power),
                            new XElement("Speed", m.Speed),
                            new XElement("Cycles", m.Cycles),
                            new XElement("Remarks", m.Remarks)
                        ))
                    )
                )
            );
            
            doc.Save(UserMaterialsFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving materials: {ex.Message}");
        }
    }
    
    private List<Material> LoadFromFile(string filePath)
    {
        var materials = new List<Material>();
        
        try
        {
            var doc = XDocument.Load(filePath);
            var materialElements = doc.Descendants("Material");
            
            foreach (var element in materialElements)
            {
                var material = new Material(
                    id: Guid.Parse(element.Element("Id")?.Value ?? Guid.NewGuid().ToString()),
                    visible: bool.Parse(element.Element("Visible")?.Value ?? "true"),
                    model: element.Element("Model")?.Value ?? "Generic Laser",
                    materialName: element.Element("MaterialName")?.Value ?? "Generic Material",
                    thickness: element.Element("Thickness")?.Value ?? "",
                    action: element.Element("Action")?.Value ?? "",
                    power: int.Parse(element.Element("Power")?.Value ?? "100"),
                    speed: int.Parse(element.Element("Speed")?.Value ?? "1000"),
                    cycles: int.Parse(element.Element("Cycles")?.Value ?? "1"),
                    remarks: element.Element("Remarks")?.Value ?? ""
                );
                
                materials.Add(material);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading from file {filePath}: {ex.Message}");
        }
        
        return materials;
    }
    
    private List<Material> GetDefaultMaterials()
    {
        return new List<Material>
        {
            new Material(Guid.NewGuid(), true, "Generic Laser", "Wood - Plywood", "3mm", "Cut", 100, 800, 1, ""),
            new Material(Guid.NewGuid(), true, "Generic Laser", "Wood - Plywood", "3mm", "Engrave", 80, 1500, 1, ""),
            new Material(Guid.NewGuid(), true, "Generic Laser", "Acrylic", "3mm", "Cut", 100, 600, 1, ""),
            new Material(Guid.NewGuid(), true, "Generic Laser", "Acrylic", "3mm", "Engrave", 70, 2000, 1, ""),
            new Material(Guid.NewGuid(), true, "Generic Laser", "Cardboard", "2mm", "Cut", 80, 1000, 1, ""),
            new Material(Guid.NewGuid(), true, "Generic Laser", "Leather", "2mm", "Cut", 90, 800, 1, ""),
            new Material(Guid.NewGuid(), true, "Generic Laser", "Leather", "2mm", "Engrave", 60, 1800, 1, ""),
            new Material(Guid.NewGuid(), true, "Generic Laser", "Cork", "3mm", "Cut", 85, 900, 1, ""),
            new Material(Guid.NewGuid(), true, "Generic Laser", "Paper", "0.2mm", "Cut", 50, 1500, 1, ""),
            new Material(Guid.NewGuid(), true, "Generic Laser", "Felt", "3mm", "Cut", 70, 1200, 1, ""),
        };
    }
    
    public IEnumerable<string> GetModels(IEnumerable<Material> materials)
    {
        return materials.Where(m => m.Visible)
                       .Select(m => m.Model)
                       .Distinct()
                       .OrderBy(s => s);
    }
    
    public IEnumerable<string> GetMaterialsForModel(IEnumerable<Material> materials, string model)
    {
        return materials.Where(m => m.Visible && m.Model == model)
                       .Select(m => m.MaterialName)
                       .Distinct()
                       .OrderBy(s => s);
    }
    
    public IEnumerable<string> GetActionsForMaterial(IEnumerable<Material> materials, string model, string materialName)
    {
        return materials.Where(m => m.Visible && m.Model == model && m.MaterialName == materialName)
                       .Select(m => m.Action)
                       .Distinct()
                       .OrderBy(s => s);
    }
    
    public IEnumerable<string> GetThicknessForAction(IEnumerable<Material> materials, string model, string materialName, string action)
    {
        return materials.Where(m => m.Visible && m.Model == model && m.MaterialName == materialName && m.Action == action)
                       .Select(m => m.Thickness)
                       .Distinct()
                       .OrderBy(s => s);
    }
    
    public Material? GetMaterial(IEnumerable<Material> materials, string model, string materialName, string action, string thickness)
    {
        return materials.FirstOrDefault(m => 
            m.Visible && 
            m.Model == model && 
            m.MaterialName == materialName && 
            m.Action == action && 
            m.Thickness == thickness
        );
    }
}
