using System;
using System.Collections.Generic;

namespace DependenciesTracking
{
    public interface IDependenciesTracker : IDisposable
    {
        void UpdateAll();
    }

    internal partial class DependenciesTracker<T> : IDependenciesTracker
    {
        private readonly DependenciesMap<T> _map;

        private readonly T _trackedObject;

        private readonly IList<ISubscriberBase> _rootSubscribers = new List<ISubscriberBase>();

        public DependenciesTracker(DependenciesMap<T> map, T trackedObject, bool provokeDependentPropertiesUpdate)
        {
            // ReSharper disable once CompareNonConstrainedGenericWithNull
            if (trackedObject == null)
                throw new ArgumentNullException(nameof(trackedObject));

            _map = map ?? throw new ArgumentNullException(nameof(map));
            _trackedObject = trackedObject;

            StartTrackingChanges(provokeDependentPropertiesUpdate);
        }

        public DependenciesTracker(DependenciesMap<T> map, T trackedObject)
            : this(map, trackedObject, true)
        {
        }

        private void StartTrackingChanges(bool provokeDependentPropertiesUpdate)
        {
            foreach (var map in _map.MapItems)
            {
                _rootSubscribers.Add(SubscriberBase.CreateSubscriber(_trackedObject, map, OnPropertyChanged));
                if (provokeDependentPropertiesUpdate)
                    ProvokeDependentPropertiesUpdate(map);
            }
        }

        private void OnPropertyChanged(PathItemBase<T> subscriber)
        {
            ProvokeDependentPropertiesUpdate(subscriber);
        }

        private void ProvokeDependentPropertiesUpdate(PathItemBase<T> pathItem)
        {
            while (pathItem != null)
            {
                pathItem.UpdateDependentPropertyOrFieldAction?.Invoke(_trackedObject);
                pathItem = pathItem.Ancestor;
            }
        }

        public void UpdateAll()
        {
            foreach (var map in _map.MapItems)
            {
                ProvokeDependentPropertiesUpdate(map);
            }
        }

        public void Dispose()
        {
            foreach (var rootSubscriber in _rootSubscribers)
                rootSubscriber.Dispose();
        }
    }
}
