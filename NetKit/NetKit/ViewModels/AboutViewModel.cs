using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace NetKit.ViewModels
{
	public class AboutViewModel : ObservableObject
	{
		private string description;
		public String Description
		{
			get => description;
			set => SetProperty(ref description, value);
		}
	}
}
