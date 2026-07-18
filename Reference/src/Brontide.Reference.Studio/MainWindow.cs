using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Brontide.Reference.Experimental.Composition;
using Brontide.Reference.Extensions.Events;
using Brontide.Reference.Extensions.Flow;
using Brontide.Reference.Studio.Scenes;
using Brontide.Reference.Vocabularies.Cooling;

namespace Brontide.Reference.Studio;

public sealed class MainWindow : Window
{
    private readonly VirtualDeviceBoardScene _board = new();
    private readonly StudioInspector _inspector = new();
    private readonly TextBlock _sceneOutput = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };

    public MainWindow()
    {
        Title = "Brontide.Reference Studio 0.2 — Brontide 0.5 evidence";
        Width = 1280;
        Height = 780;
        MinWidth = 900;
        MinHeight = 600;
        Content = BuildContent();
        _inspector.Refresh(_board.Domain);
    }

    private Control BuildContent()
    {
        var actions = new WrapPanel { Margin = new Thickness(12) };
        actions.Children.Add(Button("Attach mouse", (_, _) =>
        {
            _board.AttachMouse();
            return ValueTask.FromResult("Virtual mouse attached through a recorded Genesis occurrence.");
        }));
        actions.Children.Add(Button("Device move", async (_, _) =>
        {
            var result = await _board.MoveMouseAsync();
            return $"Device move: {result.Outcome.Status}; origin {result.Events.Single().Interaction.Origin}";
        }));
        actions.Children.Add(Button("Malware attempt", async (_, _) =>
        {
            var result = await _board.AttemptMalwareInjectionAsync();
            return result.Outcome.Message;
        }));
        actions.Children.Add(Button("Remote desktop", async (_, _) =>
        {
            var result = await _board.MoveRemoteDesktopAsync();
            return $"Remote input: {result.Outcome.Status}; visibly {result.Events.Single().Interaction.Origin}";
        }));
        actions.Children.Add(Button("Attack: secure", async (_, _) =>
        {
            var result = await WorkedAttackStudioScene.RunAsync(false);
            return result.Execution.Outcome.Message;
        }));
        actions.Children.Add(Button("Attack: weakened", async (_, _) =>
        {
            var result = await WorkedAttackStudioScene.RunAsync(true);
            return $"What-if result: {result.Execution.Outcome.Status}; effects={result.Effects}";
        }));
        actions.Children.Add(Button("Cooling", async (_, _) =>
        {
            var result = await CoolingScenario.RunAsync();
            return string.Join(Environment.NewLine, result.Transcript);
        }));
        actions.Children.Add(Button("Event distribution", async (_, _) =>
        {
            var result = await EventDistributionScenario.RunAsync();
            return $"Published to {result.Subscriptions.Length} independent observers; replay origin {result.Replay.Interaction.Origin}.";
        }));
        actions.Children.Add(Button("Pointer Flow", async (_, _) =>
        {
            var result = await PointerFlowScenario.RunAsync();
            return
                $"Denied spoofed origin, dropped position {result.InitialRead.Gaps.Single().FromPosition}, and replayed {result.Replay.Items.Single().SourcePosition} as {result.Replay.Items.Single().Interaction.Origin}.";
        }));
        actions.Children.Add(Button("Audit macro", async (_, _) =>
        {
            var result = await MacroOperationScene.RunAsync();
            return string.Join(Environment.NewLine, result.Transcript);
        }));
        actions.Children.Add(Button("Brontide 0.5 image workspace", async (_, _) =>
        {
            var result = await ImageWorkspaceShowcase.RunAsync();
            var stageFour = result.StageFour.Explanation.Provider;
            return string.Join(
                Environment.NewLine,
                $"Stage 1: {result.StageOne.Explanation.Provider.SelectedComponent.Name} ({result.StageOne.Explanation.Provider.Placement})",
                $"Stage 2: {string.Join(", ", result.StageTwoAdoptions.Select(adoption => adoption.Resolution.Facility!.Component))}",
                $"Stage 3: {result.StageThreeSubstitution.Previous.Provider} → {result.StageThreeSubstitution.Replacement.Provider}",
                $"Stage 4: {stageFour.SelectedComponent.Name} ({stageFour.Placement}); batch={stageFour.BatchSize}; copies={stageFour.Copies}",
                $"Boundary: {string.Join(", ", stageFour.CrossedBoundaries)}",
                $"Outstanding cross-stack evidence: {string.Join("; ", result.OutstandingCrossStackEvidence)}",
                $"Experimental sideline projects: {string.Join("; ", result.ExperimentalSidelineProjects)}");
        }));

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("1*,1.35*,2.4*"),
            RowDefinitions = new RowDefinitions("Auto,*")
        };
        Grid.SetColumnSpan(actions, 3);
        grid.Children.Add(actions);
        grid.Children.Add(Panel("Actors", new ListBox { ItemsSource = _inspector.ActorGraph }, 0));
        grid.Children.Add(Panel("Capability derivation trees", new ListBox { ItemsSource = _inspector.CapabilityTrees }, 1));
        var rightTabs = new TabControl
        {
            Margin = new Thickness(6),
            ItemsSource = new[]
            {
                new TabItem { Header = "Live Executions", Content = new ListBox { ItemsSource = _inspector.ExecutionLog } },
                new TabItem { Header = "Scene evidence", Content = new ScrollViewer { Content = _sceneOutput, Padding = new Thickness(12) } }
            }
        };
        Grid.SetRow(rightTabs, 1);
        Grid.SetColumn(rightTabs, 2);
        grid.Children.Add(rightTabs);
        return grid;
    }

    private Control Panel(string title, Control content, int column)
    {
        var panel = new DockPanel { Margin = new Thickness(6) };
        var heading = new TextBlock { Text = title, FontSize = 17, Margin = new Thickness(6) };
        DockPanel.SetDock(heading, Dock.Top);
        panel.Children.Add(heading);
        panel.Children.Add(content);
        Grid.SetRow(panel, 1);
        Grid.SetColumn(panel, column);
        return panel;
    }

    private Button Button(string text, Func<object?, Avalonia.Interactivity.RoutedEventArgs, ValueTask<string>> action)
    {
        var button = new Button { Content = text, Margin = new Thickness(4) };
        button.Click += async (sender, args) =>
        {
            try
            {
                _sceneOutput.Text = await action(sender, args);
            }
            catch (Exception exception)
            {
                _sceneOutput.Text = exception.Message;
            }

            _inspector.Refresh(_board.Domain);
        };
        return button;
    }

}
