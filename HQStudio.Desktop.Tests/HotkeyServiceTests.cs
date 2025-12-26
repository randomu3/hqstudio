using FluentAssertions;
using Xunit;
using System.Windows.Input;

namespace HQStudio.Desktop.Tests;

/// <summary>
/// Тесты для логики HotkeyService (без зависимости от WPF Window)
/// </summary>
public class HotkeyServiceTests
{
    #region HotkeyDefinition Tests

    [Fact]
    public void HotkeyDefinition_Matches_CorrectKeyAndModifiers_ReturnsTrue()
    {
        var hotkey = new TestHotkeyDefinition(Key.S, ModifierKeys.Control, "Save");
        
        var result = hotkey.Matches(Key.S, ModifierKeys.Control);
        
        result.Should().BeTrue();
    }

    [Fact]
    public void HotkeyDefinition_Matches_WrongKey_ReturnsFalse()
    {
        var hotkey = new TestHotkeyDefinition(Key.S, ModifierKeys.Control, "Save");
        
        var result = hotkey.Matches(Key.N, ModifierKeys.Control);
        
        result.Should().BeFalse();
    }

    [Fact]
    public void HotkeyDefinition_Matches_WrongModifiers_ReturnsFalse()
    {
        var hotkey = new TestHotkeyDefinition(Key.S, ModifierKeys.Control, "Save");
        
        var result = hotkey.Matches(Key.S, ModifierKeys.Alt);
        
        result.Should().BeFalse();
    }

    [Fact]
    public void HotkeyDefinition_Matches_NoModifiers_ReturnsTrue()
    {
        var hotkey = new TestHotkeyDefinition(Key.Delete, ModifierKeys.None, "Delete");
        
        var result = hotkey.Matches(Key.Delete, ModifierKeys.None);
        
        result.Should().BeTrue();
    }

    [Fact]
    public void HotkeyDefinition_ToDisplayString_CtrlS_ReturnsCorrectFormat()
    {
        var hotkey = new TestHotkeyDefinition(Key.S, ModifierKeys.Control, "Save");
        
        var result = hotkey.ToDisplayString();
        
        result.Should().Be("Ctrl+S");
    }

    [Fact]
    public void HotkeyDefinition_ToDisplayString_CtrlShiftN_ReturnsCorrectFormat()
    {
        var hotkey = new TestHotkeyDefinition(Key.N, ModifierKeys.Control | ModifierKeys.Shift, "NewSpecial");
        
        var result = hotkey.ToDisplayString();
        
        result.Should().Be("Ctrl+Shift+N");
    }

    [Fact]
    public void HotkeyDefinition_ToDisplayString_Delete_ReturnsKeyOnly()
    {
        var hotkey = new TestHotkeyDefinition(Key.Delete, ModifierKeys.None, "Delete");
        
        var result = hotkey.ToDisplayString();
        
        result.Should().Be("Delete");
    }

    [Fact]
    public void HotkeyDefinition_ToDisplayString_Escape_ReturnsKeyOnly()
    {
        var hotkey = new TestHotkeyDefinition(Key.Escape, ModifierKeys.None, "CloseDialog");
        
        var result = hotkey.ToDisplayString();
        
        result.Should().Be("Escape");
    }

    #endregion

    #region TestHotkeyService Tests

    [Fact]
    public void HotkeyService_DefaultHotkeys_ContainsSave()
    {
        var service = new TestHotkeyService();
        
        var saveHotkey = service.FindHotkeyByAction("Save");
        
        saveHotkey.Should().NotBeNull();
        saveHotkey!.Key.Should().Be(Key.S);
        saveHotkey.Modifiers.Should().Be(ModifierKeys.Control);
    }

    [Fact]
    public void HotkeyService_DefaultHotkeys_ContainsNew()
    {
        var service = new TestHotkeyService();
        
        var newHotkey = service.FindHotkeyByAction("New");
        
        newHotkey.Should().NotBeNull();
        newHotkey!.Key.Should().Be(Key.N);
        newHotkey.Modifiers.Should().Be(ModifierKeys.Control);
    }

    [Fact]
    public void HotkeyService_DefaultHotkeys_ContainsDelete()
    {
        var service = new TestHotkeyService();
        
        var deleteHotkey = service.FindHotkeyByAction("Delete");
        
        deleteHotkey.Should().NotBeNull();
        deleteHotkey!.Key.Should().Be(Key.Delete);
        deleteHotkey.Modifiers.Should().Be(ModifierKeys.None);
    }

    [Fact]
    public void HotkeyService_DefaultHotkeys_ContainsFocusSearch()
    {
        var service = new TestHotkeyService();
        
        var searchHotkey = service.FindHotkeyByAction("FocusSearch");
        
        searchHotkey.Should().NotBeNull();
        searchHotkey!.Key.Should().Be(Key.F);
        searchHotkey.Modifiers.Should().Be(ModifierKeys.Control);
    }

    [Fact]
    public void HotkeyService_DefaultHotkeys_ContainsCloseDialog()
    {
        var service = new TestHotkeyService();
        
        var escapeHotkey = service.FindHotkeyByAction("CloseDialog");
        
        escapeHotkey.Should().NotBeNull();
        escapeHotkey!.Key.Should().Be(Key.Escape);
        escapeHotkey.Modifiers.Should().Be(ModifierKeys.None);
    }

    [Fact]
    public void HotkeyService_FindHotkey_CtrlS_ReturnsSaveHotkey()
    {
        var service = new TestHotkeyService();
        
        var hotkey = service.FindHotkey(Key.S, ModifierKeys.Control);
        
        hotkey.Should().NotBeNull();
        hotkey!.ActionName.Should().Be("Save");
    }

    [Fact]
    public void HotkeyService_FindHotkey_UnknownKey_ReturnsNull()
    {
        var service = new TestHotkeyService();
        
        var hotkey = service.FindHotkey(Key.Z, ModifierKeys.Control);
        
        hotkey.Should().BeNull();
    }

    [Fact]
    public void HotkeyService_IsHotkey_CtrlS_ReturnsTrue()
    {
        var service = new TestHotkeyService();
        
        var result = service.IsHotkey(Key.S, ModifierKeys.Control);
        
        result.Should().BeTrue();
    }

    [Fact]
    public void HotkeyService_IsHotkey_UnknownKey_ReturnsFalse()
    {
        var service = new TestHotkeyService();
        
        var result = service.IsHotkey(Key.Z, ModifierKeys.Control);
        
        result.Should().BeFalse();
    }

    [Fact]
    public void HotkeyService_GetHotkeyDisplayString_Save_ReturnsCtrlS()
    {
        var service = new TestHotkeyService();
        
        var result = service.GetHotkeyDisplayString("Save");
        
        result.Should().Be("Ctrl+S");
    }

    [Fact]
    public void HotkeyService_GetHotkeyDisplayString_UnknownAction_ReturnsEmpty()
    {
        var service = new TestHotkeyService();
        
        var result = service.GetHotkeyDisplayString("Unknown");
        
        result.Should().BeEmpty();
    }


    #endregion

    #region Action Handler Tests

    [Fact]
    public void HotkeyService_SetActionHandler_SetsAction()
    {
        var service = new TestHotkeyService();
        var actionExecuted = false;
        
        service.SetActionHandler("Save", () => actionExecuted = true);
        
        var hotkey = service.FindHotkeyByAction("Save");
        hotkey!.Action!.Invoke();
        
        actionExecuted.Should().BeTrue();
    }

    [Fact]
    public void HotkeyService_SetActionHandler_SetsCanExecute()
    {
        var service = new TestHotkeyService();
        
        service.SetActionHandler("Save", () => { }, () => false);
        
        var hotkey = service.FindHotkeyByAction("Save");
        hotkey!.CanExecute!.Invoke().Should().BeFalse();
    }

    [Fact]
    public void HotkeyService_ClearActionHandler_ClearsAction()
    {
        var service = new TestHotkeyService();
        service.SetActionHandler("Save", () => { });
        
        service.ClearActionHandler("Save");
        
        var hotkey = service.FindHotkeyByAction("Save");
        hotkey!.Action.Should().BeNull();
    }

    [Fact]
    public void HotkeyService_ClearAllHandlers_ClearsAllActions()
    {
        var service = new TestHotkeyService();
        service.SetActionHandler("Save", () => { });
        service.SetActionHandler("New", () => { });
        
        service.ClearAllHandlers();
        
        service.FindHotkeyByAction("Save")!.Action.Should().BeNull();
        service.FindHotkeyByAction("New")!.Action.Should().BeNull();
    }

    /// <summary>
    /// Property 2: Save hotkey triggers save when changes exist
    /// For any form with HasUnsavedChanges=true, pressing Ctrl+S should invoke the Save command.
    /// **Validates: Requirements 2.1**
    /// </summary>
    [Fact]
    public void Property2_SaveHotkey_TriggersAction_WhenCanExecuteIsTrue()
    {
        var service = new TestHotkeyService();
        var saveExecuted = false;
        var hasUnsavedChanges = true;
        
        service.SetActionHandler("Save", 
            () => saveExecuted = true, 
            () => hasUnsavedChanges);
        
        // Симулируем нажатие Ctrl+S
        var result = service.TryExecuteHotkey(Key.S, ModifierKeys.Control);
        
        result.Should().BeTrue();
        saveExecuted.Should().BeTrue();
    }

    [Fact]
    public void Property2_SaveHotkey_DoesNotTrigger_WhenCanExecuteIsFalse()
    {
        var service = new TestHotkeyService();
        var saveExecuted = false;
        var hasUnsavedChanges = false;
        
        service.SetActionHandler("Save", 
            () => saveExecuted = true, 
            () => hasUnsavedChanges);
        
        // Симулируем нажатие Ctrl+S
        var result = service.TryExecuteHotkey(Key.S, ModifierKeys.Control);
        
        result.Should().BeFalse();
        saveExecuted.Should().BeFalse();
    }

    [Fact]
    public void HotkeyService_TryExecuteHotkey_ExecutesAction_WhenNoCanExecute()
    {
        var service = new TestHotkeyService();
        var actionExecuted = false;
        
        service.SetActionHandler("New", () => actionExecuted = true);
        
        var result = service.TryExecuteHotkey(Key.N, ModifierKeys.Control);
        
        result.Should().BeTrue();
        actionExecuted.Should().BeTrue();
    }

    [Fact]
    public void HotkeyService_TryExecuteHotkey_ReturnsFalse_WhenNoAction()
    {
        var service = new TestHotkeyService();
        
        var result = service.TryExecuteHotkey(Key.S, ModifierKeys.Control);
        
        result.Should().BeFalse();
    }

    [Fact]
    public void HotkeyService_TryExecuteHotkey_ReturnsFalse_WhenDisabled()
    {
        var service = new TestHotkeyService();
        var actionExecuted = false;
        service.SetActionHandler("Save", () => actionExecuted = true);
        service.IsEnabled = false;
        
        var result = service.TryExecuteHotkey(Key.S, ModifierKeys.Control);
        
        result.Should().BeFalse();
        actionExecuted.Should().BeFalse();
    }

    [Fact]
    public void HotkeyService_SuspendHotkeys_DisablesTemporarily()
    {
        var service = new TestHotkeyService();
        var actionExecuted = false;
        service.SetActionHandler("Save", () => actionExecuted = true);
        
        using (service.SuspendHotkeys())
        {
            var result = service.TryExecuteHotkey(Key.S, ModifierKeys.Control);
            result.Should().BeFalse();
            actionExecuted.Should().BeFalse();
        }
        
        // После dispose должно снова работать
        var resultAfter = service.TryExecuteHotkey(Key.S, ModifierKeys.Control);
        resultAfter.Should().BeTrue();
        actionExecuted.Should().BeTrue();
    }

    #endregion

    #region Event Tests

    [Fact]
    public void HotkeyService_HotkeyExecuted_RaisedOnExecution()
    {
        var service = new TestHotkeyService();
        service.SetActionHandler("Save", () => { });
        TestHotkeyDefinition? executedHotkey = null;
        service.HotkeyExecuted += (s, e) => executedHotkey = e.Hotkey;
        
        service.TryExecuteHotkey(Key.S, ModifierKeys.Control);
        
        executedHotkey.Should().NotBeNull();
        executedHotkey!.ActionName.Should().Be("Save");
    }

    #endregion
}

#region Test Classes

/// <summary>
/// Тестовое определение горячей клавиши (без зависимости от WPF)
/// </summary>
public class TestHotkeyDefinition
{
    public Key Key { get; set; }
    public ModifierKeys Modifiers { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public Action? Action { get; set; }
    public Func<bool>? CanExecute { get; set; }

    public TestHotkeyDefinition(Key key, ModifierKeys modifiers, string actionName)
    {
        Key = key;
        Modifiers = modifiers;
        ActionName = actionName;
    }

    public bool Matches(Key key, ModifierKeys modifiers)
    {
        return Key == key && Modifiers == modifiers;
    }

    public string ToDisplayString()
    {
        var parts = new List<string>();
        
        if (Modifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (Modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        if (Modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        
        parts.Add(Key.ToString());
        
        return string.Join("+", parts);
    }
}

/// <summary>
/// Аргументы события выполнения горячей клавиши для тестов
/// </summary>
public class TestHotkeyExecutedEventArgs : EventArgs
{
    public TestHotkeyDefinition Hotkey { get; }
    public string ActionName => Hotkey.ActionName;

    public TestHotkeyExecutedEventArgs(TestHotkeyDefinition hotkey)
    {
        Hotkey = hotkey;
    }
}

/// <summary>
/// Тестовая реализация HotkeyService без зависимости от WPF Window
/// </summary>
public class TestHotkeyService
{
    private readonly List<TestHotkeyDefinition> _hotkeys = new();
    private bool _isEnabled = true;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public IReadOnlyList<TestHotkeyDefinition> Hotkeys => _hotkeys.AsReadOnly();

    public event EventHandler<TestHotkeyExecutedEventArgs>? HotkeyExecuted;

    public TestHotkeyService()
    {
        InitializeDefaultHotkeys();
    }

    private void InitializeDefaultHotkeys()
    {
        _hotkeys.Add(new TestHotkeyDefinition(Key.S, ModifierKeys.Control, "Save"));
        _hotkeys.Add(new TestHotkeyDefinition(Key.N, ModifierKeys.Control, "New"));
        _hotkeys.Add(new TestHotkeyDefinition(Key.Delete, ModifierKeys.None, "Delete"));
        _hotkeys.Add(new TestHotkeyDefinition(Key.F, ModifierKeys.Control, "FocusSearch"));
        _hotkeys.Add(new TestHotkeyDefinition(Key.Escape, ModifierKeys.None, "CloseDialog"));
    }

    public TestHotkeyDefinition? FindHotkey(Key key, ModifierKeys modifiers)
    {
        return _hotkeys.Find(h => h.Matches(key, modifiers));
    }

    public TestHotkeyDefinition? FindHotkeyByAction(string actionName)
    {
        return _hotkeys.Find(h => h.ActionName == actionName);
    }

    public void SetActionHandler(string actionName, Action action, Func<bool>? canExecute = null)
    {
        var hotkey = FindHotkeyByAction(actionName);
        if (hotkey != null)
        {
            hotkey.Action = action;
            hotkey.CanExecute = canExecute;
        }
    }

    public void ClearActionHandler(string actionName)
    {
        var hotkey = FindHotkeyByAction(actionName);
        if (hotkey != null)
        {
            hotkey.Action = null;
            hotkey.CanExecute = null;
        }
    }

    public void ClearAllHandlers()
    {
        foreach (var hotkey in _hotkeys)
        {
            hotkey.Action = null;
            hotkey.CanExecute = null;
        }
    }

    public bool IsHotkey(Key key, ModifierKeys modifiers)
    {
        return FindHotkey(key, modifiers) != null;
    }

    public string GetHotkeyDisplayString(string actionName)
    {
        var hotkey = FindHotkeyByAction(actionName);
        return hotkey?.ToDisplayString() ?? string.Empty;
    }

    /// <summary>
    /// Пытается выполнить хоткей (для тестирования)
    /// </summary>
    public bool TryExecuteHotkey(Key key, ModifierKeys modifiers)
    {
        if (!_isEnabled) return false;

        var hotkey = FindHotkey(key, modifiers);
        if (hotkey == null) return false;

        if (hotkey.Action == null) return false;

        if (hotkey.CanExecute?.Invoke() == false) return false;

        hotkey.Action.Invoke();
        HotkeyExecuted?.Invoke(this, new TestHotkeyExecutedEventArgs(hotkey));
        return true;
    }

    public IDisposable SuspendHotkeys()
    {
        return new HotkeySuspender(this);
    }

    private class HotkeySuspender : IDisposable
    {
        private readonly TestHotkeyService _service;
        private readonly bool _wasEnabled;

        public HotkeySuspender(TestHotkeyService service)
        {
            _service = service;
            _wasEnabled = _service._isEnabled;
            _service._isEnabled = false;
        }

        public void Dispose()
        {
            _service._isEnabled = _wasEnabled;
        }
    }
}

#endregion
