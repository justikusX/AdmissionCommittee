using System.Windows;
using System.Windows.Controls;

namespace AdmissionCommittee.Infrastructure;

/// <summary>
/// У PasswordBox свойство Password нельзя привязать (Binding) напрямую.
/// Это вложенное свойство позволяет связать пароль со свойством ViewModel,
/// не нарушая MVVM (ViewModel ничего не знает о PasswordBox).
///
/// Использование в XAML:
///   &lt;PasswordBox inf:PasswordHelper.Attach="True"
///                inf:PasswordHelper.Password="{Binding Password}" /&gt;
/// </summary>
public static class PasswordHelper
{
    public static readonly DependencyProperty AttachProperty =
        DependencyProperty.RegisterAttached("Attach", typeof(bool), typeof(PasswordHelper),
            new PropertyMetadata(false, OnAttachChanged));

    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.RegisterAttached("Password", typeof(string), typeof(PasswordHelper),
            new FrameworkPropertyMetadata(string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordPropertyChanged));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached("IsUpdating", typeof(bool), typeof(PasswordHelper),
            new PropertyMetadata(false));

    public static bool GetAttach(DependencyObject d) => (bool)d.GetValue(AttachProperty);
    public static void SetAttach(DependencyObject d, bool value) => d.SetValue(AttachProperty, value);

    public static string GetPassword(DependencyObject d) => (string)d.GetValue(PasswordProperty);
    public static void SetPassword(DependencyObject d, string value) => d.SetValue(PasswordProperty, value);

    private static bool GetIsUpdating(DependencyObject d) => (bool)d.GetValue(IsUpdatingProperty);
    private static void SetIsUpdating(DependencyObject d, bool value) => d.SetValue(IsUpdatingProperty, value);

    private static void OnAttachChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box)
            return;

        box.PasswordChanged -= OnPasswordBoxChanged;
        if ((bool)e.NewValue)
            box.PasswordChanged += OnPasswordBoxChanged;
    }

    // ViewModel изменила пароль (например, очистила поле) -> обновляем PasswordBox
    private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PasswordBox box && !GetIsUpdating(box))
            box.Password = e.NewValue as string ?? string.Empty;
    }

    // Пользователь ввёл символ -> передаём пароль во ViewModel
    private static void OnPasswordBoxChanged(object sender, RoutedEventArgs e)
    {
        var box = (PasswordBox)sender;
        SetIsUpdating(box, true);
        SetPassword(box, box.Password);
        SetIsUpdating(box, false);
    }
}
