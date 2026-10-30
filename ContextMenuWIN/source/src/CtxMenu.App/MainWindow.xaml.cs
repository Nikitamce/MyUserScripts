using System.Windows;
using System.Windows.Input;

namespace CtxMenu.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Ctrl+F jumps to the search box on the entries page
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, (_, _) => { SearchBox.Focus(); SearchBox.SelectAll(); }));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Find, Key.F, ModifierKeys.Control));
    }
}
