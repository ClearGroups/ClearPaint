using System.Collections.Generic;
namespace ClearPaint.Services
{
    public class LanguageService
    {
        private static LanguageService? _i;
        public static LanguageService I => _i ??= new LanguageService();
        private string _l = "en-US";
        public string L { get => _l; set => _l = value; }

        private Dictionary<string, Dictionary<string, string>> _d = new()
        {
            ["en-US"] = new()
            {
                ["File"]="_File",["New"]="_New Canvas",["Open"]="_Open...",["Save"]="_Save",["SaveAs"]="Save _As...",["Exit"]="E_xit",
                ["Edit"]="_Edit",["Undo"]="_Undo",["Redo"]="_Redo",["ClearCanvas"]="_Clear Canvas",
                ["Settings"]="_Settings",["GitHub"]="Our GitHub",
                ["Language"]="_Language",["English"]="English",["Russian"]="Русский",
                ["Brush"]="Brush",["BrushSize"]="Size:",
                ["DrawColor"]="Draw Color",["CanvasColor"]="Canvas Color",
                ["SavePrompt"]="Save changes?",["OpenTitle"]="Open Image",["SaveTitle"]="Save As",
                ["Untitled"]="Untitled",["LangStatus"]="English"
            },
            ["ru-RU"] = new()
            {
                ["File"]="_Файл",["New"]="_Новый холст",["Open"]="_Открыть...",["Save"]="_Сохранить",["SaveAs"]="Сохранить _как...",["Exit"]="В_ыход",
                ["Edit"]="_Правка",["Undo"]="_Отменить",["Redo"]="_Повторить",["ClearCanvas"]="_Очистить холст",
                ["Settings"]="_Настройки",["GitHub"]="Наш GitHub",
                ["Language"]="_Язык",["English"]="English",["Russian"]="Русский",
                ["Brush"]="Кисть",["BrushSize"]="Размер:",
                ["DrawColor"]="Цвет рисования",["CanvasColor"]="Цвет холста",
                ["SavePrompt"]="Сохранить изменения?",["OpenTitle"]="Открыть изображение",["SaveTitle"]="Сохранить как",
                ["Untitled"]="Безымянный",["LangStatus"]="Русский"
            }
        };

        public string S(string k) => _d[_l].TryGetValue(k, out var v) ? v : k;
    }
}