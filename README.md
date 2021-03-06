# DependenciesTracking - automatic calculation of dependent properties on .NET INotifyPropertyChanged objects.
DependenciesTracking is a .NET library that implements the tracking of dependent property values (i.e. updating them at the moment of changing the properties they depend on) and provides declarative way of setting up dependencies. It's a lightweight library, so none of the creating any kind of property wrappers (ObservableProperty<TProperty>, Independent<TProperty> and so on), deriving view model from any special base class, marking properties with any special attribute should be done by a developer to get the tracking worked. Also the implementation isn't based on any kind of post build assembly rewriting.
### Supported dependencies
* [Simple property dependencies](https://github.com/ademchenko/DependenciesTracker/wiki#simple-property-dependencies) (like _Cost = Price * Quantity_)
* [Property chain dependencies](https://github.com/ademchenko/DependenciesTracker/wiki#property-chain-dependencies) (like _CostWithDiscount = Cost * (100 - Discount.Percent)/100_)
* [Collection item dependencies](https://github.com/ademchenko/DependenciesTracker/wiki#collection-item-dependencies) (like _TotalCost = Orders.Sum(o => o.Price * o.Quantity)_)

### Downloads
The stable covered with tests release 1.0.1 version can be installed through the NuGet.
https://www.nuget.org/packages/DependenciesTracking/
```
PM> Install-Package DependenciesTracking
```

### Documentation
* A quick start article about Dependencies Tracker can be found [here](https://github.com/ademchenko/DependenciesTracker/wiki).
* ["Samples" folder](https://github.com/ademchenko/DependenciesTracker/tree/master/Samples) in DependenciesTracking repository contains WPF solution which demonstrates Tracker features.
