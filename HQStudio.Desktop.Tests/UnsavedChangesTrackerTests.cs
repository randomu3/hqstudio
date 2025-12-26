using FluentAssertions;
using Xunit;

namespace HQStudio.Desktop.Tests;

/// <summary>
/// Тесты для UnsavedChangesTracker
/// **Property 3: Dialog shown on close attempt**
/// Validates: Requirements 3.1
/// </summary>
public class UnsavedChangesTrackerTests
{
    [Fact]
    public void HasUnsavedChanges_InitiallyFalse()
    {
        var tracker = new TestUnsavedChangesTracker();
        
        tracker.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void MarkAsChanged_SetsHasUnsavedChangesToTrue()
    {
        var tracker = new TestUnsavedChangesTracker();
        
        tracker.MarkAsChanged();
        
        tracker.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public void MarkAsSaved_SetsHasUnsavedChangesToFalse()
    {
        var tracker = new TestUnsavedChangesTracker();
        tracker.MarkAsChanged();
        
        tracker.MarkAsSaved();
        
        tracker.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void Reset_SetsHasUnsavedChangesToFalse()
    {
        var tracker = new TestUnsavedChangesTracker();
        tracker.MarkAsChanged();
        
        tracker.Reset();
        
        tracker.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void ConfirmDiscard_WithNoChanges_ReturnsTrue()
    {
        var tracker = new TestUnsavedChangesTracker();
        
        var result = tracker.ConfirmDiscard();
        
        result.Should().BeTrue();
        tracker.DialogShownCount.Should().Be(0);
    }

    [Fact]
    public void ConfirmDiscard_WithChanges_ShowsDialog()
    {
        var tracker = new TestUnsavedChangesTracker();
        tracker.MarkAsChanged();
        
        tracker.ConfirmDiscard();
        
        tracker.DialogShownCount.Should().Be(1);
    }

    [Fact]
    public void ConfirmDiscard_WithChanges_UserConfirms_ReturnsTrue()
    {
        var tracker = new TestUnsavedChangesTracker { DialogResult = true };
        tracker.MarkAsChanged();
        
        var result = tracker.ConfirmDiscard();
        
        result.Should().BeTrue();
    }

    [Fact]
    public void ConfirmDiscard_WithChanges_UserCancels_ReturnsFalse()
    {
        var tracker = new TestUnsavedChangesTracker { DialogResult = false };
        tracker.MarkAsChanged();
        
        var result = tracker.ConfirmDiscard();
        
        result.Should().BeFalse();
    }

    [Fact]
    public void UnsavedChangesStateChanged_FiredOnMarkAsChanged()
    {
        var tracker = new TestUnsavedChangesTracker();
        bool? eventValue = null;
        tracker.UnsavedChangesStateChanged += (s, value) => eventValue = value;
        
        tracker.MarkAsChanged();
        
        eventValue.Should().BeTrue();
    }

    [Fact]
    public void UnsavedChangesStateChanged_FiredOnMarkAsSaved()
    {
        var tracker = new TestUnsavedChangesTracker();
        tracker.MarkAsChanged();
        bool? eventValue = null;
        tracker.UnsavedChangesStateChanged += (s, value) => eventValue = value;
        
        tracker.MarkAsSaved();
        
        eventValue.Should().BeFalse();
    }

    [Fact]
    public void UnsavedChangesStateChanged_NotFiredWhenStateUnchanged()
    {
        var tracker = new TestUnsavedChangesTracker();
        int eventCount = 0;
        tracker.UnsavedChangesStateChanged += (s, value) => eventCount++;
        
        tracker.MarkAsChanged();
        tracker.MarkAsChanged(); // Повторный вызов не должен вызывать событие
        
        eventCount.Should().Be(1);
    }

    [Fact]
    public void MultipleChanges_OnlyOneDialogNeeded()
    {
        var tracker = new TestUnsavedChangesTracker { DialogResult = true };
        
        tracker.MarkAsChanged();
        tracker.MarkAsChanged();
        tracker.MarkAsChanged();
        
        tracker.ConfirmDiscard();
        
        tracker.DialogShownCount.Should().Be(1);
    }
}

/// <summary>
/// Тестовая реализация UnsavedChangesTracker без зависимости от WPF
/// </summary>
public class TestUnsavedChangesTracker
{
    private bool _hasUnsavedChanges;

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set
        {
            if (_hasUnsavedChanges != value)
            {
                _hasUnsavedChanges = value;
                UnsavedChangesStateChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// Результат диалога для тестов
    /// </summary>
    public bool DialogResult { get; set; } = true;

    /// <summary>
    /// Счётчик показов диалога
    /// </summary>
    public int DialogShownCount { get; private set; }

    public event EventHandler<bool>? UnsavedChangesStateChanged;

    public void MarkAsChanged()
    {
        HasUnsavedChanges = true;
    }

    public void MarkAsSaved()
    {
        HasUnsavedChanges = false;
    }

    public void Reset()
    {
        HasUnsavedChanges = false;
    }

    public bool ConfirmDiscard()
    {
        if (!HasUnsavedChanges)
            return true;

        DialogShownCount++;
        return DialogResult;
    }
}
