using Microsoft.Maui.Controls;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace GoHardApp.Behaviors
{
    /// <summary>
    /// Provides staggered entrance animations for CollectionView items.
    /// Each item fades in and scales up with a subtle delay for a smooth, cascading effect.
    /// </summary>
    public class StaggeredAnimationBehavior : Behavior<CollectionView>
    {
        private CollectionView? _collectionView;
        private const int MaxAnimatedItems = 10; // Limit animations for performance
        private const uint StaggerDelay = 50; // Delay between each item animation
        private const uint AnimationDuration = 400; // Duration of each animation
        private bool _hasAnimated = false;

        protected override void OnAttachedTo(CollectionView bindable)
        {
            base.OnAttachedTo(bindable);
            _collectionView = bindable;

            // Subscribe to the ChildAdded event to detect when items are added
            bindable.ChildAdded += OnChildAdded;

            // Subscribe to the Loaded event for initial animation
            bindable.Loaded += OnLoaded;
        }

        protected override void OnDetachingFrom(CollectionView bindable)
        {
            base.OnDetachingFrom(bindable);

            if (_collectionView != null)
            {
                _collectionView.ChildAdded -= OnChildAdded;
                _collectionView.Loaded -= OnLoaded;
            }

            _collectionView = null;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            if (_collectionView != null && !_hasAnimated)
            {
                // Trigger initial animation when the view is loaded
                _collectionView.Dispatcher.Dispatch(async () =>
                {
                    await AnimateItems();
                    _hasAnimated = true;
                });
            }
        }

        private void OnChildAdded(object? sender, ElementEventArgs e)
        {
            // Animate newly added items
            if (e.Element is VisualElement element && !_hasAnimated && _collectionView != null)
            {
                _collectionView.Dispatcher.Dispatch(async () =>
                {
                    await AnimateElement(element, 0);
                });
            }
        }

        private async Task AnimateItems()
        {
            if (_collectionView == null)
                return;

            // Get visible children (items in the CollectionView)
            var children = _collectionView.GetVisualTreeDescendants()
                .OfType<VisualElement>()
                .Where(v => v.Parent == _collectionView || IsCollectionViewItem(v))
                .Take(MaxAnimatedItems)
                .ToList();

            // Animate each item with stagger
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                _ = AnimateElement(child, (uint)(i * StaggerDelay));
            }

            await Task.CompletedTask;
        }

        private static bool IsCollectionViewItem(VisualElement element)
        {
            // Check if this element is a direct child of a CollectionView
            return element.Parent is CollectionView;
        }

        private async Task AnimateElement(VisualElement element, uint delay)
        {
            // Set initial state (invisible and slightly scaled down)
            element.Opacity = 0;
            element.Scale = 0.95;

            // Wait for stagger delay
            if (delay > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(delay));
            }

            // Animate to visible state
            await Task.WhenAll(
                element.FadeTo(1.0, AnimationDuration, Easing.CubicOut),
                element.ScaleTo(1.0, AnimationDuration, Easing.CubicOut)
            );
        }

        /// <summary>
        /// Resets the animation state, allowing items to be animated again.
        /// Call this when refreshing the collection.
        /// </summary>
        public void Reset()
        {
            _hasAnimated = false;
        }
    }
}
