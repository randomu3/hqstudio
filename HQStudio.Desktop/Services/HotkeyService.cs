using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.Services
{
    /// <summary>
    /// Определение горячей клавиши
    /// </summary>
    public class HotkeyDefinition
    {
        public Key Key { get; set; }
        public ModifierKeys Modifiers { get; set; }
        public string ActionName { get; set; } = string.Empty;
        public Action? Action { get; set; }
        public Func<bool>? CanExecute { get; set; }

        public HotkeyDefinition(Key key, ModifierKeys modifiers, string actionName)
        {
            Key = key;
            Modifiers = modifiers;
            ActionName = actionName;
        }

        /// <summary>
        /// Проверяет, соответствует ли нажатие клавиши этому хоткею
        /// </summary>
        public bool Matches(Key key, ModifierKeys modifiers)
        {
            return Key == key && Modifiers == modifiers;
        }

        /// <summary>
        /// Строковое представление хоткея (например, "Ctrl+S")
        /// </summary>
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
    /// Интерфейс для обработки горячих клавиш в представлениях
    /// </summary>
    public interface IHotkeyHandler
    {
        /// <summary>
        /// Выполнить сохранение (Ctrl+S)
        /// </summary>
        void ExecuteSave();
        
        /// <summary>
        /// Можно ли выполнить сохранение
        /// </summary>
        bool CanExecuteSave();
        
        /// <summary>
        /// Создать новый элемент (Ctrl+N)
        /// </summary>
        void ExecuteNew();
        
        /// <summary>
        /// Можно ли создать новый элемент
        /// </summary>
        bool CanExecuteNew();
        
        /// <summary>
        /// Удалить выбранный элемент (Delete)
        /// </summary>
        void ExecuteDelete();
        
        /// <summary>
        /// Можно ли удалить элемент
        /// </summary>
        bool CanExecuteDelete();
        
        /// <summary>
        /// Фокус на поле поиска (Ctrl+F)
        /// </summary>
        void ExecuteFocusSearch();
        
        /// <summary>
        /// Есть ли поле поиска
        /// </summary>
        bool HasSearchField();
    }

    /// <summary>
    /// Сервис для управления горячими клавишами
    /// </summary>
    public class HotkeyService
    {
        private static HotkeyService? _instance;
        public static HotkeyService Instance => _instance ??= new HotkeyService();

        private readonly List<HotkeyDefinition> _hotkeys = new();
        private Window? _registeredWindow;
        private bool _isEnabled = true;

        /// <summary>
        /// Включены ли горячие клавиши
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Список зарегистрированных горячих клавиш
        /// </summary>
        public IReadOnlyList<HotkeyDefinition> Hotkeys => _hotkeys.AsReadOnly();

        /// <summary>
        /// Событие при выполнении горячей клавиши
        /// </summary>
        public event EventHandler<HotkeyExecutedEventArgs>? HotkeyExecuted;

        private HotkeyService()
        {
            InitializeDefaultHotkeys();
        }

        /// <summary>
        /// Инициализация стандартных горячих клавиш
        /// </summary>
        private void InitializeDefaultHotkeys()
        {
            // Ctrl+S - Сохранить
            _hotkeys.Add(new HotkeyDefinition(Key.S, ModifierKeys.Control, "Save"));
            
            // Ctrl+N - Создать новый
            _hotkeys.Add(new HotkeyDefinition(Key.N, ModifierKeys.Control, "New"));
            
            // Delete - Удалить выбранный
            _hotkeys.Add(new HotkeyDefinition(Key.Delete, ModifierKeys.None, "Delete"));
            
            // Ctrl+F - Фокус на поиск
            _hotkeys.Add(new HotkeyDefinition(Key.F, ModifierKeys.Control, "FocusSearch"));
            
            // Escape - Закрыть диалог
            _hotkeys.Add(new HotkeyDefinition(Key.Escape, ModifierKeys.None, "CloseDialog"));
        }


        /// <summary>
        /// Регистрирует обработчик горячих клавиш для окна
        /// </summary>
        public void RegisterGlobalHotkeys(Window window)
        {
            if (_registeredWindow != null)
            {
                UnregisterHotkeys();
            }

            _registeredWindow = window;
            _registeredWindow.PreviewKeyDown += OnWindowPreviewKeyDown;
        }

        /// <summary>
        /// Отменяет регистрацию горячих клавиш
        /// </summary>
        public void UnregisterHotkeys()
        {
            if (_registeredWindow != null)
            {
                _registeredWindow.PreviewKeyDown -= OnWindowPreviewKeyDown;
                _registeredWindow = null;
            }
        }

        /// <summary>
        /// Обработчик нажатия клавиш
        /// </summary>
        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isEnabled) return;

            var modifiers = Keyboard.Modifiers;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Ищем соответствующий хоткей
            var hotkey = FindHotkey(key, modifiers);
            if (hotkey != null)
            {
                // Проверяем, можно ли выполнить действие
                if (hotkey.CanExecute?.Invoke() != false)
                {
                    // Выполняем действие
                    hotkey.Action?.Invoke();
                    
                    // Уведомляем о выполнении
                    HotkeyExecuted?.Invoke(this, new HotkeyExecutedEventArgs(hotkey));
                    
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Находит хоткей по нажатой клавише
        /// </summary>
        public HotkeyDefinition? FindHotkey(Key key, ModifierKeys modifiers)
        {
            return _hotkeys.Find(h => h.Matches(key, modifiers));
        }

        /// <summary>
        /// Находит хоткей по имени действия
        /// </summary>
        public HotkeyDefinition? FindHotkeyByAction(string actionName)
        {
            return _hotkeys.Find(h => h.ActionName == actionName);
        }

        /// <summary>
        /// Устанавливает обработчик для действия
        /// </summary>
        public void SetActionHandler(string actionName, Action action, Func<bool>? canExecute = null)
        {
            var hotkey = FindHotkeyByAction(actionName);
            if (hotkey != null)
            {
                hotkey.Action = action;
                hotkey.CanExecute = canExecute;
            }
        }

        /// <summary>
        /// Очищает обработчик для действия
        /// </summary>
        public void ClearActionHandler(string actionName)
        {
            var hotkey = FindHotkeyByAction(actionName);
            if (hotkey != null)
            {
                hotkey.Action = null;
                hotkey.CanExecute = null;
            }
        }

        /// <summary>
        /// Очищает все обработчики
        /// </summary>
        public void ClearAllHandlers()
        {
            foreach (var hotkey in _hotkeys)
            {
                hotkey.Action = null;
                hotkey.CanExecute = null;
            }
        }

        /// <summary>
        /// Проверяет, является ли нажатие клавиши хоткеем
        /// </summary>
        public bool IsHotkey(Key key, ModifierKeys modifiers)
        {
            return FindHotkey(key, modifiers) != null;
        }

        /// <summary>
        /// Получает строковое представление хоткея для действия
        /// </summary>
        public string GetHotkeyDisplayString(string actionName)
        {
            var hotkey = FindHotkeyByAction(actionName);
            return hotkey?.ToDisplayString() ?? string.Empty;
        }

        /// <summary>
        /// Временно отключает горячие клавиши (например, при редактировании текста)
        /// </summary>
        public IDisposable SuspendHotkeys()
        {
            return new HotkeySuspender(this);
        }

        private class HotkeySuspender : IDisposable
        {
            private readonly HotkeyService _service;
            private readonly bool _wasEnabled;

            public HotkeySuspender(HotkeyService service)
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

    /// <summary>
    /// Аргументы события выполнения горячей клавиши
    /// </summary>
    public class HotkeyExecutedEventArgs : EventArgs
    {
        public HotkeyDefinition Hotkey { get; }
        public string ActionName => Hotkey.ActionName;

        public HotkeyExecutedEventArgs(HotkeyDefinition hotkey)
        {
            Hotkey = hotkey;
        }
    }
}
