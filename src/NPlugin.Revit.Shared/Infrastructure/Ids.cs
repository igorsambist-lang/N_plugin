using Autodesk.Revit.DB;

namespace NPlugin.Infrastructure;

/// <summary>
/// Единая работа с ElementId для всех версий Revit.
/// С Revit 2024 идентификаторы 64-битные (ElementId.Value / конструктор long),
/// в более ранних версиях — IntegerValue / конструктор int.
/// </summary>
public static class Ids
{
    public static long GetValue(ElementId id)
    {
#if REVIT2024_OR_GREATER
        return id.Value;
#else
        return id.IntegerValue;
#endif
    }

    public static ElementId Of(long id)
    {
#if REVIT2024_OR_GREATER
        return new ElementId(id);
#else
        return new ElementId((int)id);
#endif
    }
}
