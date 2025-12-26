using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HQStudio.Desktop.Behaviors;

/// <summary>
/// Attached behavior для отображения индикатора Caps Lock на PasswordBox.
/// Отслеживает состояние Caps Lock в реальном времени при фокусе на поле пароля.
/// </summary>
public static class CapsLockBehavior
{
    /// <summary>
    /// Включает отслеживание Caps Lock для PasswordBox.
    /// </summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(CapsLockBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    /// <summary>
    /// Показывает, включён ли Caps Lock (только для чтения, обновляется автоматически).
    /// </summary>
    public static readonly DependencyProperty IsCapsLockOnProperty =
        DependencyProperty.RegisterAttached(
            "IsCapsLockOn",
            typeof(bool),
            typeof(CapsLockBehavior),
            new PropertyMetadata(false));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static bool GetIsCapsLockOn(DependencyObject obj) => (bool)obj.GetValue(IsCapsLockOnProperty);
    public static void SetIsCapsLockOn(DependencyObject obj, bool value) => obj.SetValue(IsCapsLockOnProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PasswordBox passwordBox)
        {
            if ((bool)e.NewValue)
            {
                passwordBox.GotFocus += OnGotFocus;
                passwordBox.LostFocus += OnLostFocus;
                passwordBox.PreviewKeyDown += OnPreviewKeyDown;
                passwordBox.PreviewKeyUp += OnPreviewKeyUp;
                
                // Проверяем начальное состояние если уже в фокусе
                if (passwordBox.IsFocused)
                {
                    UpdateCapsLockState(passwordBox);
                }
            }
            else
            {
                passwordBox.GotFocus -= OnGotFocus;
                passwordBox.LostFocus -= OnLostFocus;
                passwordBox.PreviewKeyDown -= OnPreviewKeyDown;
                passwordBox.PreviewKeyUp -= OnPreviewKeyUp;
                SetIsCapsLockOn(passwordBox, false);
            }
        }
    }

    private static void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            UpdateCapsLockState(passwordBox);
        }
    }

    private static void OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            // Скрываем индикатор при потере фокуса
            SetIsCapsLockOn(passwordBox, false);
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            // Обновляем состояние при нажатии любой клавиши (включая Caps Lock)
            UpdateCapsLockState(passwordBox);
        }
    }

    private static void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            // Обновляем состояние после отпускания клавиши (особенно важно для Caps Lock)
            UpdateCapsLockState(passwordBox);
        }
    }

    private static void UpdateCapsLockState(PasswordBox passwordBox)
    {
        var isCapsLockOn = Keyboard.IsKeyToggled(Key.CapsLock);
        SetIsCapsLockOn(passwordBox, isCapsLockOn && passwordBox.IsFocused);
    }
}
