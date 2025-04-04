using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Rendering;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia;
using ReactiveUI;
using DynamicData;
using Xunit;
using Splat;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class AvaloniaActivationForViewFetcherTest
    {
        public class TestUserControl : UserControl, IActivatableView { }

        public class TestUserControlWithWhenActivated : UserControl, IActivatableView
        {
            public bool Active { get; private set; }

            public TestUserControlWithWhenActivated()
            {
                this.WhenActivated(disposables => 
                {
                    Active = true;
                    Disposable
                        .Create(() => Active = false)
                        .DisposeWith(disposables);
                });
            }
        }

        public class TestWindowWithWhenActivated : Window, IActivatableView
        {
            public bool Active { get; private set; }

            public TestWindowWithWhenActivated()
            {
                this.WhenActivated(disposables => 
                {
                    Active = true;
                    Disposable
                        .Create(() => Active = false)
                        .DisposeWith(disposables);
                });
            }
        }

        public class ActivatableViewModel : IActivatableViewModel
        {
            public ViewModelActivator Activator { get; }

            public bool IsActivated { get; private set; }

            public ActivatableViewModel() 
            {
                Activator = new ViewModelActivator();
                this.WhenActivated(disposables => 
                {
                    IsActivated = true;
                    Disposable
                        .Create(() => IsActivated = false)
                        .DisposeWith(disposables);
                });
            }
        }

        public class ActivatableWindow : ReactiveWindow<ActivatableViewModel>
        {
            public ActivatableWindow()
            {
                Content = new Border();
                this.WhenActivated(disposables => { });
            }
        }

        public class ActivatableUserControl : ReactiveUserControl<ActivatableViewModel>
        {
            public ActivatableUserControl()
            {
                Content = new Border();
                this.WhenActivated(disposables => { });
            }
        }

        public AvaloniaActivationForViewFetcherTest() =>
            Locator
                .CurrentMutable
                .RegisterConstant(
                    new AvaloniaActivationForViewFetcher(),
                    typeof(IActivationForViewFetcher));

        [Fact]
        public void Visual_Element_Is_Activated_And_Deactivated()
        {
            var userControl = new TestUserControl();
            var activationForViewFetcher = new AvaloniaActivationForViewFetcher();

            activationForViewFetcher
                .GetActivationForView(userControl)
                .ToObservableChangeSet(scheduler: ImmediateScheduler.Instance)
                .Bind(out var activated)
                .Subscribe();

            var fakeRenderedDecorator = new TestRoot();
            fakeRenderedDecorator.Child = userControl;
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
            Assert.True(activated[0]);
            Assert.Equal(1, activated.Count);

            fakeRenderedDecorator.Child = null;
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
            Assert.True(activated[0]);
            Assert.False(activated[1]);
            Assert.Equal(2, activated.Count);
        }

        [Fact]
        public void Get_Affinity_For_View_Should_Return_Non_Zero_For_Visual_Elements() 
        {
            var userControl = new TestUserControl();
            var activationForViewFetcher = new AvaloniaActivationForViewFetcher();

            var forUserControl = activationForViewFetcher.GetAffinityForView(userControl.GetType());
            var forNonUserControl = activationForViewFetcher.GetAffinityForView(typeof(object));

            Assert.NotEqual(0, forUserControl);
            Assert.Equal(0, forNonUserControl);
        }

        [Fact]
        public void Activation_For_View_Fetcher_Should_Support_When_Activated()
        {
            var userControl = new TestUserControlWithWhenActivated();
            Assert.False(userControl.Active);

            var fakeRenderedDecorator = new TestRoot();
            fakeRenderedDecorator.Child = userControl;
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
            Assert.True(userControl.Active);

            fakeRenderedDecorator.Child = null;
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
            Assert.False(userControl.Active);
        }

        [Fact]
        public void Activation_For_View_Fetcher_Should_Support_Windows() 
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow)) 
            {
                var window = new TestWindowWithWhenActivated();
                Assert.False(window.Active);

                window.Show();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
                Assert.True(window.Active);

                window.Close();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
                Assert.False(window.Active);
            }
        }

        [Fact]
        public void Activatable_Window_View_Model_Is_Activated_And_Deactivated() 
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow)) 
            {
                var viewModel = new ActivatableViewModel();
                var window = new ActivatableWindow { ViewModel = viewModel };
                Assert.False(viewModel.IsActivated);

                window.Show();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
                Assert.True(viewModel.IsActivated);

                window.Close();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
                Assert.False(viewModel.IsActivated);
            }
        }

        [Fact]
        public void Activatable_User_Control_View_Model_Is_Activated_And_Deactivated() 
        {
            var root = new TestRoot();
            var viewModel = new ActivatableViewModel();
            var control = new ActivatableUserControl { ViewModel = viewModel };
            Assert.False(viewModel.IsActivated);

            root.Child = control;
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
            Assert.True(viewModel.IsActivated);

            root.Child = null;
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
            Assert.False(viewModel.IsActivated);
        }
    }
}
