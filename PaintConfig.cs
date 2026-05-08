using System.Text.Json.Serialization;
using System.Windows.Media;

namespace ClearPaint.Models
{
    public class PaintConfig
    {
        public int BrushSize { get; set; } = 4;
        public string DrawColorHex { get; set; } = "#FF000000";
        public string CanvasColorHex { get; set; } = "#FFFFFFFF";
        public string Language { get; set; } = "en-US";

        [JsonIgnore]
        public Color DrawColor
        {
            get => (Color)ColorConverter.ConvertFromString(DrawColorHex);
            set => DrawColorHex = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
        }
        [JsonIgnore]
        public Color CanvasColor
        {
            get => (Color)ColorConverter.ConvertFromString(CanvasColorHex);
            set => CanvasColorHex = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
        }
    }
}