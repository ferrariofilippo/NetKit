﻿using CommunityToolkit.Mvvm.ComponentModel;
using NetKit.Model;
using NetKit.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NetKit.ViewModels
{
    public class WildcardViewModel : ObservableObject
    {
        private string _methodName = "Range";
        public string MethodName
        {
            get => _methodName;
            set => SetProperty(ref _methodName, value);
        }

        private WildcardMethod _wildcardMethod = WildcardMethod.Range;
        public WildcardMethod WildcardMethod
        {
            get => _wildcardMethod;
            set
            {
                if (SetProperty(ref _wildcardMethod, value))
                {
                    OnPropertyChanged(nameof(IsRange));
					OnPropertyChanged(nameof(IsGreaterOrSmaller));
					OnPropertyChanged(nameof(IsClass));
                    OnPropertyChanged(nameof(IsSubmitEnabled));
                }
            }
        }

        private string _className;
        public string ClassName
        {
            get => _className;
            set => SetProperty(ref _className, value);
        }

        private NetworkClass _networkClass;
        public NetworkClass NetworkClass
        {
            get => _networkClass;
            set => SetProperty(ref _networkClass, value);
        }

        private string _networkAddress;
        public string NetworkAddress
        {
            get => _networkAddress;
            set
            {
                if (SetProperty(ref _networkAddress, value))
                    OnPropertyChanged(nameof(IsSubmitEnabled));
            }
        }

        private string _lowerBound;
        public string LowerBound
        {
            get => _lowerBound;
            set
            {
                if (SetProperty(ref _lowerBound, value))
                    OnPropertyChanged(nameof(IsSubmitEnabled));
            }
        }

        private string _upperBound;
        public string UpperBound
        {
            get => _upperBound;
            set
            {
                if (SetProperty(ref _upperBound, value))
                    OnPropertyChanged(nameof(IsSubmitEnabled));
            }
        }

        private string _valueLimit;
        public string ValueLimit
        {
            get => _valueLimit;
            set
            {
                if (SetProperty(ref _valueLimit, value))
                    OnPropertyChanged(nameof(IsSubmitEnabled));
            }
        }

        public bool IsSubmitEnabled => _wildcardMethod is WildcardMethod.Class ||
            (!string.IsNullOrWhiteSpace(_networkAddress) &&
            (_wildcardMethod is WildcardMethod.Even ||
            _wildcardMethod is WildcardMethod.Odd ||
            _wildcardMethod is WildcardMethod.Network ||
            ((_wildcardMethod is WildcardMethod.Greater || _wildcardMethod is WildcardMethod.Smaller) && !string.IsNullOrWhiteSpace(_valueLimit)) ||
            (_wildcardMethod is WildcardMethod.Range && !string.IsNullOrWhiteSpace(_lowerBound) && !string.IsNullOrWhiteSpace(_upperBound))));

        public bool IsRange => _wildcardMethod is WildcardMethod.Range;

        public bool IsGreaterOrSmaller => _wildcardMethod is WildcardMethod.Greater || _wildcardMethod is WildcardMethod.Smaller;

        public bool IsClass => _wildcardMethod is WildcardMethod.Class;

        public double ListViewHeight => AccessControlEntries.Count * 80;

        public ObservableCollection<ACE> AccessControlEntries = new ObservableCollection<ACE>();

        public readonly string[] WildcardMethods = Enum.GetNames(typeof(WildcardMethod));

        public readonly string[] ClassNames = Enum.GetNames(typeof(NetworkClass));

        public Task CalculateACEs(byte[] network, int networkBits)
        {
            return Task.Run(() =>
            { 
                AccessControlEntries.Clear();
                List<ACE> entries;
                switch (_wildcardMethod)
                {
                    case WildcardMethod.Range:
                        var lowerBound = uint.Parse(_lowerBound);
                        var upperBound = uint.Parse(_upperBound);
                        entries = WildcardHelpers.CalculateRangeWildcardMask(network, lowerBound, upperBound, networkBits);

                        if (entries.Count > 0)
                            entries.ForEach(ace => AccessControlEntries.Add(ace));
                        break;

                    case WildcardMethod.Greater:
                        var lowerLimit = uint.Parse(_valueLimit);
                        entries = WildcardHelpers.CalculateGreaterThanWildcardMask(network, lowerLimit, networkBits);

                        if (entries.Count > 0)
                            entries.ForEach(ace => AccessControlEntries.Add(ace));
                        break;

                    case WildcardMethod.Smaller:
                        var upperLimit = uint.Parse(_valueLimit);
                        entries = WildcardHelpers.CalculateSmallerThanWildcardMask(network, upperLimit, networkBits);

                        if (entries.Count > 0)
                            entries.ForEach(ace => AccessControlEntries.Add(ace));
                        break;

                    case WildcardMethod.Even:
                        AccessControlEntries.Add(WildcardHelpers.CalculateEvenOrOddWildcard(true, network, networkBits));
                        break;

                    case WildcardMethod.Odd:
                        AccessControlEntries.Add(WildcardHelpers.CalculateEvenOrOddWildcard(false, network, networkBits));
                        break;

                    case WildcardMethod.Network:
                        AccessControlEntries.Add(WildcardHelpers.CalculateNetworkWildcard(network, networkBits));
                        break;

                    case WildcardMethod.Class:
                        AccessControlEntries.Add(WildcardHelpers.CalculateClassWildcard(_networkClass));
                        break;
                }
                OnPropertyChanged(nameof(ListViewHeight));
            });
        }
    }

    public enum WildcardMethod
    {
        Range,
        Greater,
        Smaller,
        Even,
        Odd,
        Network,
        Class
    }

    public enum NetworkClass
    {
        A,
        B,
        C,
        D,
        E
    }
}
