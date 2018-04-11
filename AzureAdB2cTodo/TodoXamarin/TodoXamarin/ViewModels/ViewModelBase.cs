using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation;

namespace TodoXamarin.ViewModels
{
    public class ViewModelBase : BindableBase, INavigationAware, IDestructible
    {
        private string _title;

        public ViewModelBase(INavigationService navigationService, IEventAggregator eventAggregator)
        {
            if (NavigationService == null)
            {
                NavigationService = navigationService;
            }

            if (EventAggregator == null)
            {
                EventAggregator = eventAggregator;
            }
        }

        protected INavigationService NavigationService { get; }

        public IEventAggregator EventAggregator { get; }
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public virtual void Destroy()
        {
        }

        public virtual void OnNavigatedFrom(NavigationParameters parameters)
        {
        }

        public virtual void OnNavigatedTo(NavigationParameters parameters)
        {
        }

        public virtual void OnNavigatingTo(NavigationParameters parameters)
        {
        }
    }
}