using System.Windows;
using System.Windows.Controls;
using SnapClip.Models;
using SnapClip.ViewModels;

namespace SnapClip.Converters;

/// <summary>
/// Selects the appropriate DataTemplate based on the clip type.
/// </summary>
public sealed class ClipTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }
    public DataTemplate? ImageTemplate { get; set; }
    public DataTemplate? FileTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not ClipItemViewModel clipVm)
            return TextTemplate;

        return clipVm.Type switch
        {
            ClipType.Image => ImageTemplate ?? TextTemplate,
            ClipType.File => FileTemplate ?? TextTemplate,
            _ => TextTemplate
        };
    }
}
