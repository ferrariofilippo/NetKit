using CommunityToolkit.Mvvm.ComponentModel;
using NetKit.Model;
using System.Collections.ObjectModel;

namespace NetKit.ViewModels
{
	public class VLSMViewModel : ObservableObject
	{
		private int inputListViewHeight = 30;
		public int InputListViewHeight
		{
			get => inputListViewHeight; 
			set => SetProperty(ref inputListViewHeight, value);
		}

		private int inputLineHeight = 50;
		public int InputLineHeight
		{
			get => inputLineHeight;
			set => SetProperty(ref inputLineHeight, value);
		}

		public ObservableCollection<HostLine> AddedItems { get; private set; } = new ObservableCollection<HostLine>();

		public ObservableCollection<Network> Networks { get; private set; } = new ObservableCollection<Network>();

		public void ExpandInputListView()
		{
			AddedItems.Add(new HostLine());
			InputListViewHeight += InputLineHeight;
		}

		public void ContractInputListView()
		{
			int lastIndex = AddedItems.Count - 1;
			if (lastIndex <= 0)
				return;
			AddedItems.RemoveAt(lastIndex);
			InputListViewHeight -= InputLineHeight;
		}
	}
}
