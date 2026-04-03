// tests/FlexibleMonitoringDashboard.Tests/Services/DashboardStateServiceTests.cs
using FlexibleMonitoringDashboard.Client.Models;
using FlexibleMonitoringDashboard.Client.Services;

namespace FlexibleMonitoringDashboard.Tests.Services;

public class DashboardStateServiceTests
{
    private readonly DashboardStateService _service = new();

    [Fact]
    public void AddSection_CreatesSection_MarksDirty()
    {
        var id = _service.AddSection("Test Section");

        Assert.NotEmpty(id);
        Assert.True(_service.IsDirty);
        Assert.Single(_service.Config.Sections);
        Assert.Equal("Test Section", _service.Config.Sections[0].Title);
    }

    [Fact]
    public void AddWidget_AddsToSection_MarksDirty()
    {
        var sectionId = _service.AddSection("Section");
        var widget = new WidgetConfig { Title = "Widget 1" };

        var widgetId = _service.AddWidget(sectionId, widget);

        Assert.NotEmpty(widgetId);
        Assert.Single(_service.Config.Sections[0].Widgets);
    }

    [Fact]
    public void RemoveWidget_RemovesFromSection()
    {
        var sectionId = _service.AddSection("Section");
        var widget = new WidgetConfig { Title = "To Delete" };
        var widgetId = _service.AddWidget(sectionId, widget);

        _service.RemoveWidget(sectionId, widgetId);

        Assert.Empty(_service.Config.Sections[0].Widgets);
    }

    [Fact]
    public void RemoveSection_RemovesWithAllWidgets()
    {
        var sectionId = _service.AddSection("Section");
        _service.AddWidget(sectionId, new WidgetConfig { Title = "W1" });
        _service.AddWidget(sectionId, new WidgetConfig { Title = "W2" });

        _service.RemoveSection(sectionId);

        Assert.Empty(_service.Config.Sections);
    }

    [Fact]
    public void LoadConfig_ReplacesEntireConfig_MarksClean()
    {
        _service.AddSection("Old");

        var newConfig = new DashboardConfig
        {
            Name = "Imported",
            Sections = new() { new SectionConfig { Title = "New" } }
        };
        _service.LoadConfig(newConfig);

        Assert.False(_service.IsDirty);
        Assert.Equal("Imported", _service.Config.Name);
        Assert.Single(_service.Config.Sections);
    }

    [Fact]
    public void MarkClean_ResetsIsDirty()
    {
        _service.AddSection("Section");
        Assert.True(_service.IsDirty);

        _service.MarkClean();
        Assert.False(_service.IsDirty);
    }

    [Fact]
    public void OnStateChanged_FiresOnMutations()
    {
        int callCount = 0;
        _service.OnStateChanged += () => callCount++;

        _service.AddSection("Section");
        _service.SetDashboardName("New Name");

        Assert.Equal(2, callCount);
    }

    [Fact]
    public void MoveSection_SwapsOrder()
    {
        _service.AddSection("First");
        _service.AddSection("Second");

        var secondId = _service.Config.Sections[1].Id;
        _service.MoveSection(secondId, -1);

        Assert.Equal("Second", _service.Config.Sections[0].Title);
        Assert.Equal("First", _service.Config.Sections[1].Title);
    }

    [Fact]
    public void GetAllWidgets_ReturnsWidgetsAcrossAllSections()
    {
        var s1 = _service.AddSection("Section 1");
        var s2 = _service.AddSection("Section 2");
        _service.AddWidget(s1, new WidgetConfig { Title = "W1" });
        _service.AddWidget(s2, new WidgetConfig { Title = "W2" });

        var all = _service.GetAllWidgets().ToList();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void Clear_RemovesAllSections_MarksClean()
    {
        _service.AddSection("Section");
        _service.SetDashboardName("Named Dashboard");

        _service.Clear();

        Assert.Empty(_service.Config.Sections);
        Assert.Equal("My Dashboard", _service.Config.Name);
        Assert.False(_service.IsDirty);
    }

    [Fact]
    public void UpdateSection_ChangesTitle_MarksDirty()
    {
        var sectionId = _service.AddSection("Original");
        _service.MarkClean();

        _service.UpdateSection(sectionId, "Updated", "A description");

        Assert.Equal("Updated", _service.Config.Sections[0].Title);
        Assert.Equal("A description", _service.Config.Sections[0].Description);
        Assert.True(_service.IsDirty);
    }

    [Fact]
    public void HasWidgets_ReturnsTrueWhenWidgetsExist()
    {
        Assert.False(_service.HasWidgets);

        var sectionId = _service.AddSection("Section");
        _service.AddWidget(sectionId, new WidgetConfig { Title = "W1" });

        Assert.True(_service.HasWidgets);
    }

    [Fact]
    public void WidgetCount_ReturnsCorrectTotal()
    {
        var s1 = _service.AddSection("S1");
        var s2 = _service.AddSection("S2");
        _service.AddWidget(s1, new WidgetConfig { Title = "W1" });
        _service.AddWidget(s1, new WidgetConfig { Title = "W2" });
        _service.AddWidget(s2, new WidgetConfig { Title = "W3" });

        Assert.Equal(3, _service.WidgetCount);
    }
}
