using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace NPlugin.Application;

public static class RibbonBuilder
{
    private const string TabName = "N Plugin";

    public static void Build(UIControlledApplication app)
    {
        app.CreateRibbonTab(TabName);

        var printPanel = app.CreateRibbonPanel(TabName, "Печать и экспорт");
        AddButton(printPanel, "npl_batchprint", "Пакетная\nпечать",
            "NPlugin.Commands.BatchPrintCommand", "print",
            "Пакетная печать листов в PDF/DWG с именами по шаблону (аналог ProSheets).");
        AddButton(printPanel, "npl_navis", "Вид\nNavisworks",
            "NPlugin.Commands.NavisworksViewCommand", "navis",
            "Создание 3D-вида Navisworks по шаблону, опциональный экспорт NWC.");
        AddButton(printPanel, "npl_export", "Экспорт\nмоделей",
            "NPlugin.Commands.ModelExportCommand", "export",
            "Пакетный экспорт моделей со всеми связями (аналог eTransmit).");

        var modelPanel = app.CreateRibbonPanel(TabName, "Модель");
        AddButton(modelPanel, "npl_clash", "Пересечения",
            "NPlugin.Commands.ClashCommand", "clash",
            "Поиск коллизий между категориями: отчёт Excel, выбор элементов, вид зоны пересечения.");
        AddButton(modelPanel, "npl_systems3d", "3D виды\nпо системам",
            "NPlugin.Commands.Systems3dCommand", "systems",
            "Пакетное создание 3D-видов по инженерным системам с рамкой вида.");

        var wsPanel = app.CreateRibbonPanel(TabName, "Рабочие наборы");
        AddButton(wsPanel, "npl_worksets", "Создать\nнаборы",
            "NPlugin.Commands.WorksetsCommand", "worksets",
            "Пакетное создание рабочих наборов из списка.");
        AddButton(wsPanel, "npl_release", "Освободить\nнаборы",
            "NPlugin.Commands.RelinquishCommand", "release",
            "Освобождение забранных рабочих наборов без входа в модель.");

        var sysPanel = app.CreateRibbonPanel(TabName, "Система");
        AddButton(sysPanel, "npl_update", "Проверить\nобновления",
            "NPlugin.Commands.CheckUpdatesCommand", "update",
            "Проверка и установка обновлений плагина.");
    }

    private static void AddButton(RibbonPanel panel, string name, string text, string className, string icon, string tooltip)
    {
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var data = new PushButtonData(name, text, assemblyPath, className)
        {
            ToolTip = tooltip,
            Image = LoadIcon(icon + "16.png"),
            LargeImage = LoadIcon(icon + "32.png")
        };
        panel.AddItem(data);
    }

    private static BitmapImage LoadIcon(string resourceName)
    {
        try
        {
            var asm = typeof(NPlugin.UI.Views.BatchPrintWindow).Assembly;
            using var stream = asm.GetManifestResourceStream("NPlugin.UI.Icons." + resourceName);
            if (stream == null)
                return null;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
