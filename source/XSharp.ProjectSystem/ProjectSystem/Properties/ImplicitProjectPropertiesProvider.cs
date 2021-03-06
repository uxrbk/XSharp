﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// The file has been modified

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace XSharp.ProjectSystem.Properties
{
    /// <summary>
    /// Provides project properties that normally should not live in the project file but may be 
    /// written from an external source (e.g. the solution file). This provider avoids writing 
    /// these properties to the project file, but if they are already present there, it updates 
    /// the value to keep in sync with the external source.
    /// Values that are not written are held in memory so they can be read for the lifetime of this
    /// provider. If the property is changed after loading the project file, the file may be out of 
    /// sync until a full project reload occurs. Specifically, if provider is managing property in 
    /// memory, and a property is added the project file, all operations ignore the value in the 
    /// project file until this property is deleted or a full reload of the project system occurs.
    /// </summary>
    [Export("ImplicitProjectFile", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [Export("ImplicitProjectFile", typeof(IProjectInstancePropertiesProvider))]
    [Export(typeof(IProjectInstancePropertiesProvider))]
    [ExportMetadata("Name", "ImplicitProjectFile")]
    [AppliesTo(ProjectCapability.XSharp)]
    internal class ImplicitProjectPropertiesProvider : DelegatedProjectPropertiesProviderBase
    {
        private readonly ImplicitProjectPropertiesStore<string, string> _propertyStore;

        [ImportingConstructor]
        public ImplicitProjectPropertiesProvider(
            [Import("Microsoft.VisualStudio.ProjectSystem.ProjectFile")] IProjectPropertiesProvider provider,
            [Import("Microsoft.VisualStudio.ProjectSystem.ProjectFile")] IProjectInstancePropertiesProvider instanceProvider,
            ImplicitProjectPropertiesStore<string, string> propertyStore,
            UnconfiguredProject unconfiguredProject)
            : base(provider, instanceProvider, unconfiguredProject)
        {
            _propertyStore = propertyStore;
        }

        public override IProjectProperties GetProperties(string file, string itemType, string item)
            => new ImplicitProjectProperties(DelegatedProvider.GetProperties(file, itemType, item), _propertyStore);

        public override IProjectProperties GetCommonProperties()
            => new ImplicitProjectProperties(DelegatedProvider.GetCommonProperties(), _propertyStore);

        public override IProjectProperties GetItemProperties(string itemType, string item)
            => new ImplicitProjectProperties(DelegatedProvider.GetItemProperties(itemType, item), _propertyStore);

        public override IProjectProperties GetItemTypeProperties(string itemType)
            => new ImplicitProjectProperties(DelegatedProvider.GetItemTypeProperties(itemType), _propertyStore);

        public override IProjectProperties GetCommonProperties(ProjectInstance projectInstance)
            => new ImplicitProjectProperties(DelegatedInstanceProvider.GetCommonProperties(projectInstance), _propertyStore);

        public override IProjectProperties GetItemTypeProperties(ProjectInstance projectInstance, string itemType)
            => new ImplicitProjectProperties(DelegatedInstanceProvider.GetItemTypeProperties(projectInstance, itemType), _propertyStore);

        public override IProjectProperties GetItemProperties(ProjectInstance projectInstance, string itemType, string itemName)
            => new ImplicitProjectProperties(DelegatedInstanceProvider.GetItemProperties(projectInstance, itemType, itemName), _propertyStore);

        public override IProjectProperties GetItemProperties(ITaskItem taskItem)
            => new ImplicitProjectProperties(DelegatedInstanceProvider.GetItemProperties(taskItem), _propertyStore);

        /// <summary>
        /// Implementation of IProjectProperties that avoids writing properties unless they
        /// already exist (i.e. are being updated) and delegates the rest of its operations
        /// to another IProjectProperties object
        /// </summary>
        private class ImplicitProjectProperties : DelegatedProjectPropertiesBase
        {
            private readonly ConcurrentDictionary<string, string> _propertyValues;

            public ImplicitProjectProperties(
                IProjectProperties properties,
                ConcurrentDictionary<string, string> propertyValues)
                : base(properties)
            {
                _propertyValues = propertyValues;
            }

            /// <summary>
            /// If a property exists in the delegated properties object, then pass the set
            /// through (overwrite). Otherwise manage the value in memory in this properties 
            /// object.
            /// </summary>
            public override async Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string> dimensionalConditions = null)
            {
                var propertyNames = await DelegatedProperties.GetPropertyNamesAsync().ConfigureAwait(false);
                if (propertyNames.Contains(propertyName))
                {
                    // overwrite the property if it exists
                    await DelegatedProperties.SetPropertyValueAsync(propertyName, unevaluatedPropertyValue, dimensionalConditions).ConfigureAwait(false);
                }
                else
                {
                    // store the property in this property object, not in the project file
                    _propertyValues[propertyName] = unevaluatedPropertyValue;
                }
            }

            /// <summary>
            /// If the property name is one that is implicitly managed here, remove it from
            /// the value map. Otherwise delegate this request to the backing property.
            /// </summary>
            public override Task DeletePropertyAsync(string propertyName, IReadOnlyDictionary<string, string> dimensionalConditions = null)
            {
                if (_propertyValues.TryRemove(propertyName, out string unevaluatedPropertyValue))
                {
                    return Task.CompletedTask;
                }
                return DelegatedProperties.DeletePropertyAsync(propertyName, dimensionalConditions);
            }

            /// <summary>
            /// If the property name is one that is implicitly managed here, return the unevaluated value.
            /// Otherwise delegate this request to the backing property.
            /// </summary>
            public override Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
            {
                if (_propertyValues.TryGetValue(propertyName, out string unevaluatedPropertyValue))
                {
                    return Task.FromResult(unevaluatedPropertyValue);
                }
                return DelegatedProperties.GetEvaluatedPropertyValueAsync(propertyName);
            }

            /// <summary>
            /// If the property name is one that is implicitly managed here, return that.
            /// Otherwise delegate this request to the backing property.
            /// </summary>
            public override Task<string> GetUnevaluatedPropertyValueAsync(string propertyName)
            {
                if (_propertyValues.TryGetValue(propertyName, out string unevaluatedPropertyValue))
                {
                    return Task.FromResult(unevaluatedPropertyValue);
                }
                return DelegatedProperties.GetUnevaluatedPropertyValueAsync(propertyName);
            }

            public override async Task<IEnumerable<string>> GetPropertyNamesAsync()
            {
                var builder = new List<string>(await base.GetPropertyNamesAsync().ConfigureAwait(false));
                builder.AddRange(_propertyValues.Keys);
                return builder.AsEnumerable();
            }
        }
    }
}
