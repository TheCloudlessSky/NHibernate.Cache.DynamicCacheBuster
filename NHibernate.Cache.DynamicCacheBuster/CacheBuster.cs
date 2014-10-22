using System;
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
    using RootClass = NHibernate.Mapping.RootClass;
    using Collection = NHibernate.Mapping.Collection;

    public class CacheBuster
    {
        public void AppendVersionToCacheRegionNames(Configuration configuration)
        {
            // When configuration.BuildSessionFactory() is called, it'll run 
            // the second phase of mapping compliation. Because we append the 
            // hash *before* BuildSessionFactory(), it will override the 
            // collection mapping region names. Therefore, always pre-build the
            // mappings so that they're fully compiled and can have their cache
            // region name set without bein overridden.
            configuration.BuildMappings();

            var setCacheRegionNameQueue = new Queue<Action>();

            using (var hashAlgorithm = new MD5CryptoServiceProvider())
            {
                foreach (var classMapping in configuration.ClassMappings)
                {
                    var rootClassMapping = classMapping as RootClass;
                    if (rootClassMapping != null)
                    {
                        AppendComputedVersion(rootClassMapping, hashAlgorithm, setCacheRegionNameQueue);
                    }
                }

                foreach (var collectionMapping in configuration.CollectionMappings)
                {
                    AppendComputedVersion(collectionMapping, hashAlgorithm, setCacheRegionNameQueue);
                }
            }

            // Queue setting the cache region name so that setting the region 
            // name doesn't change the hash of the object.
            foreach (var item in setCacheRegionNameQueue)
            {
                item();
            }
        }

        private void AppendComputedVersion(RootClass classMapping, HashAlgorithm hashAlgorithm, Queue<Action> actionQueue)
        {
            var isCacheDisabled = String.IsNullOrEmpty(classMapping.CacheConcurrencyStrategy);
            if (isCacheDisabled) return;

            var hash = Hash(hashAlgorithm, classMapping);

            actionQueue.Enqueue(() =>
                classMapping.CacheRegionName = classMapping.CacheRegionName + "(" + hash + ")"
            );
        }

        private void AppendComputedVersion(Collection collectionMapping, HashAlgorithm hashAlgorithm, Queue<Action> actionQueue)
        {
            var isCacheDisabled = String.IsNullOrEmpty(collectionMapping.CacheConcurrencyStrategy);
            if (isCacheDisabled) return;

            var hash = Hash(hashAlgorithm, collectionMapping);

            actionQueue.Enqueue(() =>
                collectionMapping.CacheRegionName = collectionMapping.CacheRegionName + "(" + hash + ")"
            );
        }

        private string Hash(HashAlgorithm hashAlgorithm, object input)
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
