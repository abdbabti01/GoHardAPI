using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace GoHardApp.Behaviors
{
    /// <summary>
    /// Provides a smooth fade-in animation when a page appears.
    /// Creates a polished entrance effect for better user experience.
    /// </summary>
    public class FadeInBehavior : Behavior<Page>
    {
        private Page? _page;
        private bool _hasAnimated = false;

        protected override void OnAttachedTo(Page bindable)
        {
            base.OnAttachedTo(bindable);
            _page = bindable;

            // Subscribe to page events
            bindable.Appearing += OnPageAppearing;
            bindable.Disappearing += OnPageDisappearing;
        }

        protected override void OnDetachingFrom(Page bindable)
        {
            base.OnDetachingFrom(bindable);

            if (_page != null)
            {
                _page.Appearing -= OnPageAppearing;
                _page.Disappearing -= OnPageDisappearing;
            }

            _page = null;
        }

        private void OnPageAppearing(object? sender, EventArgs e)
        {
            if (_page != null && !_hasAnimated)
            {
                try
                {
                    // Start the fade-in animation
                    _page.Dispatcher.Dispatch(async () =>
                    {
                        try
                        {
                            await AnimateFadeIn(_page);
                            _hasAnimated = true;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"FadeInBehavior animation error: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"FadeInBehavior dispatch error: {ex.Message}");
                }
            }
        }

        private void OnPageDisappearing(object? sender, EventArgs e)
        {
            // Reset animation state when page disappears
            // This allows the animation to play again if the page reappears
            _hasAnimated = false;
        }

        private async Task AnimateFadeIn(Page page)
        {
            // Set initial state (transparent)
            page.Opacity = 0;

            // Small delay to ensure page is loaded
            await Task.Delay(50);

            // Fade in smoothly
            await page.FadeTo(1.0, 350, Easing.CubicOut);
        }
    }
}
