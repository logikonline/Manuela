﻿using Microsoft.Maui.Handlers;

namespace Manuela.Forms;

public class DatePickerInput : BaseInput<DatePicker, DateTime, IDatePickerHandler>
{
    public DatePickerInput()
    {
        BaseControl.BackgroundColor = Colors.Transparent;
        ValueChanged += (_, _) =>
        {
            var newValue = BaseControl.Date;
            SetValue(ValueProperty, newValue);
            ((IInputControl)this).ValueChangedCommand?.Execute(newValue);
        };
        var a = 1;
    }

    public event EventHandler<DateChangedEventArgs> ValueChanged
    {
        add => BaseControl.DateSelected += value;
        remove => BaseControl.DateSelected -= value;
    }

    protected override bool CanRestoreLabelOnUnFocus => false;
    protected override void SetInputValue(object? value) =>
        BaseControl.Date = (DateTime?)value ?? DateTime.MinValue;
    protected override BindableProperty GetTextColorProperty() => DatePicker.TextColorProperty;
    protected override BindableProperty GetFontSizeProperty() => DatePicker.FontSizeProperty;
    protected override BindableProperty GetFontAttributesProperty() => DatePicker.FontAttributesProperty;

    protected override void OnInputHandlerChanged(IDatePickerHandler handler)
    {
#if ANDROID
        handler.PlatformView.BackgroundTintList =
            Android.Content.Res.ColorStateList.ValueOf(
                Microsoft.Maui.Controls.Compatibility.Platform.Android.ColorExtensions.ToAndroid(Colors.Transparent));
#elif IOS && !MACCATALYST
        handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif MACCATALYST
        // how?
#elif WINDOWS
        handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        handler.PlatformView.Style = null;
#endif
        SetInputFocus(transformViewBox: false);
    }
}
