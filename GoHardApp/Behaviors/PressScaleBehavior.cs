using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Threading.Tasks;

namespace GoHardApp.Behaviors
{
    /// <summary>
    /// Provides smooth touch feedback animation for visual elements.
    /// Scales down to 0.97 on press and springs back to 1.0 on release.
    /// </summary>
    public class PressScaleBehavior : Behavior<View>
    {
        private View? _associatedObject;

        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);
            _associatedObject = bindable;

            // Add touch gesture recognizers
            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += OnTapped;
            bindable.GestureRecognizers.Add(tapGestureRecognizer);

            // Add pointer gesture recognizers for press/release
            var pointerGestureRecognizer = new PointerGestureRecognizer();
            pointerGestureRecognizer.PointerPressed += OnPointerPressed;
            pointerGestureRecognizer.PointerReleased += OnPointerReleased;
            pointerGestureRecognizer.PointerExited += OnPointerExited;
            bindable.GestureRecognizers.Add(pointerGestureRecognizer);
        }

        protected override void OnDetachingFrom(View bindable)
        {
            base.OnDetachingFrom(bindable);

            // Clean up gesture recognizers
            if (bindable.GestureRecognizers.Count > 0)
            {
                foreach (var recognizer in bindable.GestureRecognizers.ToList())
                {
                    if (recognizer is TapGestureRecognizer tapRecognizer)
                    {
                        tapRecognizer.Tapped -= OnTapped;
                    }
                    else if (recognizer is PointerGestureRecognizer pointerRecognizer)
                    {
                        pointerRecognizer.PointerPressed -= OnPointerPressed;
                        pointerRecognizer.PointerReleased -= OnPointerReleased;
                        pointerRecognizer.PointerExited -= OnPointerExited;
                    }
                }
            }

            _associatedObject = null;
        }

        private void OnPointerPressed(object? sender, PointerEventArgs e)
        {
            if (_associatedObject != null)
            {
                _ = AnimatePress(_associatedObject);
            }
        }

        private void OnPointerReleased(object? sender, PointerEventArgs e)
        {
            if (_associatedObject != null)
            {
                _ = AnimateRelease(_associatedObject);
            }
        }

        private void OnPointerExited(object? sender, PointerEventArgs e)
        {
            if (_associatedObject != null)
            {
                _ = AnimateRelease(_associatedObject);
            }
        }

        private void OnTapped(object? sender, TappedEventArgs e)
        {
            // Tapped event is handled by the press/release animations
            // This is here to ensure tap gestures work properly
        }

        private async Task AnimatePress(VisualElement element)
        {
            // Scale down to 0.97 with cubic easing for smooth press effect
            await element.ScaleTo(0.97, 150, Easing.CubicOut);
        }

        private async Task AnimateRelease(VisualElement element)
        {
            // Spring back to 1.0 with spring easing for natural bounce
            await element.ScaleTo(1.0, 200, Easing.SpringOut);
        }
    }
}
