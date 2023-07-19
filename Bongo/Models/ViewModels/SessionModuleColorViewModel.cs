namespace Bongo.Models.ViewModels
{
    public class SessionModuleColorViewModel
    {
        public Session Session { get; set; }
        public ModuleColor ModuleColor { get; set; }
        public IEnumerable<Color> Colors { get; set;}
    }
    public class SessionModuleColorsUpdate
    {
        public int[] ModuleColorId { get; set; }
        public int[] ColorId { get; set; }
    }
}
