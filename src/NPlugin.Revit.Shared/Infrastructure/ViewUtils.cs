using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace NPlugin.Infrastructure;

public static class ViewUtils
{
    public static ViewFamilyType Get3dViewFamilyType(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(v => v.ViewFamily == ViewFamily.ThreeDimensional);
    }

    /// <summary>Все имена видов документа (для обеспечения уникальности имён).</summary>
    public static HashSet<string> GetViewNames(Document doc)
    {
        return new HashSet<string>(
            new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().Select(v => v.Name),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Подбирает уникальное имя вида: имя, имя_1, имя_2 ...</summary>
    public static string UniqueViewName(HashSet<string> existing, string baseName)
    {
        var name = baseName;
        var i = 1;
        while (existing.Contains(name) && i < 1000)
        {
            name = baseName + "_" + i;
            i++;
        }

        existing.Add(name);
        return name;
    }

    /// <summary>Объединённая огибающая элементов, раздутый на margin футов.</summary>
    public static BoundingBoxXYZ UnionBoundingBox(IEnumerable<Element> elements, double margin)
    {
        BoundingBoxXYZ result = null;
        foreach (var element in elements)
        {
            var box = element.get_BoundingBox(null);
            if (box == null)
                continue;

            if (result == null)
            {
                result = new BoundingBoxXYZ
                {
                    Min = box.Min,
                    Max = box.Max,
                    Transform = Transform.Identity
                };
            }
            else
            {
                result.Min = new XYZ(
                    Math.Min(result.Min.X, box.Min.X),
                    Math.Min(result.Min.Y, box.Min.Y),
                    Math.Min(result.Min.Z, box.Min.Z));
                result.Max = new XYZ(
                    Math.Max(result.Max.X, box.Max.X),
                    Math.Max(result.Max.Y, box.Max.Y),
                    Math.Max(result.Max.Z, box.Max.Z));
            }
        }

        if (result != null)
        {
            result.Min = new XYZ(result.Min.X - margin, result.Min.Y - margin, result.Min.Z - margin);
            result.Max = new XYZ(result.Max.X + margin, result.Max.Y + margin, result.Max.Z + margin);
        }

        return result;
    }
}
