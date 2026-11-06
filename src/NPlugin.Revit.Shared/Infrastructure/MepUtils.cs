using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace NPlugin.Infrastructure;

public static class MepUtils
{
    /// <summary>Дисциплина системы: ВК (трубы), ОВ (воздуховоды), ЭО (электрика).</summary>
    public static string Discipline(MEPSystem system)
    {
        if (system is ElectricalSystem)
            return "ЭО";
        if (system is PipingSystem)
            return "ВК";
        if (system is MechanicalSystem)
            return "ОВ";
        return "Прочее";
    }

    public static List<Element> GetSystemElements(MEPSystem system)
    {
        var result = new List<Element>();
        if (system?.Elements == null)
            return result;

        foreach (Element element in system.Elements)
            result.Add(element);

        return result;
    }

    /// <summary>Имя MEP-системы элемента (для отчётов).</summary>
    public static string SystemName(Element element)
    {
        try
        {
            if (element is MEPCurve curve && curve.MEPSystem != null)
                return curve.MEPSystem.Name ?? "";

            if (element is FamilyInstance instance && instance.MEPModel?.ConnectorManager != null)
            {
                foreach (Connector connector in instance.MEPModel.ConnectorManager.Connectors)
                {
                    if (connector.MEPSystem != null && !string.IsNullOrEmpty(connector.MEPSystem.Name))
                        return connector.MEPSystem.Name;
                }
            }
        }
        catch
        {
            // элемент может не иметь системы
        }

        return "";
    }

    public static string LevelName(Document doc, Element element)
    {
        try
        {
            var level = doc.GetElement(element.LevelId) as Level;
            return level?.Name ?? "";
        }
        catch
        {
            return "";
        }
    }

    public static string ElementName(Element element)
    {
        if (!string.IsNullOrEmpty(element.Name))
            return element.Name;
        return (element.Category?.Name ?? "Элемент") + " " + element.Id;
    }

    /// <summary>Пересекаются ли огибающие двух элементов (быстрая предпроверка).</summary>
    public static bool BoundingBoxesIntersect(Element a, Element b)
    {
        var boxA = a.get_BoundingBox(null);
        var boxB = b.get_BoundingBox(null);
        if (boxA == null || boxB == null)
            return false;

        return boxA.Min.X <= boxB.Max.X && boxB.Min.X <= boxA.Max.X
            && boxA.Min.Y <= boxB.Max.Y && boxB.Min.Y <= boxA.Max.Y
            && boxA.Min.Z <= boxB.Max.Z && boxB.Min.Z <= boxA.Max.Z;
    }
}
