﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using NHibernate.Cfg;

namespace NHibernate.Cache.DynamicCacheBuster
{
    using Action = System.Action;
    using Tuple = System.Tuple;
    using RootClass = NHibernate.Mapping.RootClass;
    using Collection = NHibernate.Mapping.Collection;
    using Component = NHibernate.Mapping.Component;
    using IType = NHibernate.Type.IType;

    public class CacheBuster
    {
        private List<ChangeEventHandler> onChange = new List<ChangeEventHandler>();
        private GetRootClassHashInput getRootClassHashInput = DefaultRootClassSerializer;
        private GetCollectionHashInput getCollectionHashInput = DefaultCollectionSerializer;

        public CacheBuster()
        {

        }

        /// <summary>
        /// Add a callback used when the region name is changed.
        /// </summary>
        /// <param name="onChange"></param>
        /// <returns></returns>
        public CacheBuster OnChange(ChangeEventHandler onChange)
        {
            if (onChange == null) throw new ArgumentNullException("onChange");
            this.onChange.Add(onChange);
            return this;
        }

        /// <summary>
        /// Set the delegate used to get an object that will be used as the
        /// input to the hash function for each <see cref="NHibernate.Mapping.RootClass"/>.
        /// By default, a collection of all properties (including collection 
        /// properties) by name and type is used.
        /// </summary>
        /// <param name="getHashInput"></param>
        /// <returns></returns>
        public CacheBuster WithRootClassHashInput(GetRootClassHashInput getHashInput)
        {
            if (getHashInput == null) throw new ArgumentNullException("getHashInput");
            this.getRootClassHashInput = getHashInput;
            return this;
        }

        /// <summary>
        /// Set the delegate used to get an object that will be used as the
        /// input to the hash function for each <see cref="NHibernate.Mapping.Collection"/>.
        /// By default, the role, collection type and element type are used.
        /// </summary>
        /// <param name="getHashInput"></param>
        /// <returns></returns>
        public CacheBuster WithCollectionHashInput(GetCollectionHashInput getHashInput)
        {
            if (getHashInput == null) throw new ArgumentNullException("serializer");
            this.getCollectionHashInput = getHashInput;
            return this;
        }

        /// <summary>
        /// Generate and apply the hashed version to each cache region for
        /// classes and collections.
        /// </summary>
        /// <param name="configuration"></param>
        public void AppendVersionToCacheRegionNames(Configuration configuration)
        {
            // When configuration.BuildSessionFactory() is called, it'll run 
            // the second phase of mapping compliation. Because we append the 
            // hash *before* BuildSessionFactory(), it will override the 
            // collection mapping region names. Therefore, always pre-build the
            // mappings so that they're fully compiled and can have their cache
            // region name set without being overridden.
            configuration.BuildMappings();

            var setCacheRegionNameQueue = new Queue<Action>();

            using (var hashAlgorithm = new MD5CryptoServiceProvider())
            {
                // NOTE: Only RootClasses and Collections are supported with caching.

                foreach (var rootClassMapping in configuration.ClassMappings.OfType<RootClass>())
                {
                    AppendComputedVersion(rootClassMapping, hashAlgorithm, setCacheRegionNameQueue);
                }

                foreach (var collectionMapping in configuration.CollectionMappings)
                {
                    AppendComputedVersion(collectionMapping, hashAlgorithm, setCacheRegionNameQueue);
                }
            }

            // Queue setting the cache region name so that setting the region 
            // name doesn't interfere with changing the hash of the object.
            foreach (var item in setCacheRegionNameQueue)
            {
                item();
            }
        }

        private void AppendComputedVersion(RootClass rootClass, HashAlgorithm hashAlgorithm, Queue<Action> actionQueue)
        {
            var isCacheDisabled = String.IsNullOrEmpty(rootClass.CacheConcurrencyStrategy);
            if (isCacheDisabled) return;

            var hashInput = getRootClassHashInput(rootClass);
            var hash = Hash(hashAlgorithm, hashInput);

            actionQueue.Enqueue(() =>
            {
                var oldCacheRegionName = rootClass.CacheRegionName;
                var newCacheRegionName = oldCacheRegionName + "(" + hash + ")";
                onChange.ForEach(x => x(oldCacheRegionName, newCacheRegionName, hash));

                rootClass.CacheRegionName = newCacheRegionName;
            });
        }

        private void AppendComputedVersion(Collection collection, HashAlgorithm hashAlgorithm, Queue<Action> actionQueue)
        {
            var isCacheDisabled = String.IsNullOrEmpty(collection.CacheConcurrencyStrategy);
            if (isCacheDisabled) return;

            var hashInput = getCollectionHashInput(collection);
            var hash = Hash(hashAlgorithm, hashInput);

            actionQueue.Enqueue(() =>
            {
                var oldCacheRegionName = collection.CacheRegionName;
                var newCacheRegionName = oldCacheRegionName + "(" + hash + ")";
                onChange.ForEach(x => x(oldCacheRegionName, newCacheRegionName, hash));

                collection.CacheRegionName = newCacheRegionName;
            });
        }

        public static object DefaultRootClassSerializer(RootClass rootClass)
        {
            // This is the standard enumeration done for the EntityMetamodel
            // constructor to build *all* properties (including properties that
            // refer to collections).
            var serialized = new List<Tuple<string, string>>(rootClass.PropertyClosureSpan);
            foreach (var property in rootClass.PropertyClosureIterator)
            {
                serialized.Add(Tuple.Create(property.Name, property.Type.ToString()));
            }

            return serialized;
        }

        public static object DefaultCollectionSerializer(Collection collection)
        {
            var serialized = Tuple.Create(collection.Role, collection.CollectionType.ToString(), collection.Element.Type.ToString());
            return serialized;
        }

        private static string Hash(HashAlgorithm hashAlgorithm, object input)
        {
            var formatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, input);

                var mappingBuffer = memoryStream.ToArray();
                var hash = hashAlgorithm.ComputeHash(mappingBuffer);

                var readableHash = BitConverter.ToString(hash).Replace("-", "");
                return readableHash;
            }
        }
    }
}
