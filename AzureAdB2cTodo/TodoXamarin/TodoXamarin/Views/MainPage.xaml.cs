using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoXamarin.Events;
using TodoXamarin.ViewModels;
using Xamarin.Forms;

namespace TodoXamarin.Views
{
	public partial class MainPage : ContentPage
	{
		public MainPage ()
		{
			InitializeComponent ();
		    
        }

	    protected override void OnAppearing()
	    {
	        base.OnAppearing();

	        ViewModel?.EventAggregator.GetEvent<AlertEvent>().Subscribe(OnAlertEvent);
        }

	    public MainPageViewModel ViewModel => this.BindingContext as MainPageViewModel;

	    private async void OnAlertEvent(AlertEventArgs args)
	    {
	        if (string.IsNullOrEmpty(args.Accept))
	        {
	            await DisplayAlert(args.Title, args.Message, args.Cancel);
	        }
	        else
	        {
	            await DisplayAlert(args.Title, args.Message, args.Accept, args.Cancel);
	        }
	    }
    }
}