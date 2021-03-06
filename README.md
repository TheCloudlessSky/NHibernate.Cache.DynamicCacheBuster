# NHibernate.Cache.DynamicCacheBuster

Caching in NHibernate is awesome. But, what happens if your schema changes
and your cache isn't cleared? For example, if you practice hot compatibility
with your application (running two versions concurrently), you don't want
your app to blow up if a column was added in the new version and the old
cache isn't aware of it.

The typical solution is to bust the cache (use a new set of cache keys). You can
do this by adding a version to the cache's region name. For example, with 
FluentNHibernate:

```csharp
public class BlogMapping : ClassMap<Blog>
{
    public BlogMapping()
    {
        Cache.ReadWrite().Region("Blog.v2");
    }
}
```

This works well. We can have two versions of the cache running and
there won't be any exceptions with different schemas.

What happens if you change your schema and you *forget* to update the cache
version for busting? Uh-oh... :boom:

This project fixes that problem by computing a hash of the schema so that you
don't have to manually bust the cache.

## Usage

1. Install the package:

    PM> Install-Package NHibernate.Cache.DynamicCacheBuster

2. Setup your NHibernate `Configuration`:

    ```csharp
    var config = new Configuration();
    // Load all mappings...
    // Set your caching provider...
    // Enable second-level and query caches...
    ```

3. Append the computed versions (this is the magic!):

    ```csharp
    new CacheBuster()
        .AppendVersionToCacheRegionNames(config);
    ```

4. Build the `ISessionFactory` and start using NHibernate:

    ```csharp
    var sessionFactory = config.BuildSessionFactory();

    using (var session = sessionFactory.OpenSession())
    {
        // Use NHibernate as normal...
    }
    ```

## Extras

- This project also computes hashes for collection caches.

- If you want to bust *all* of your model's caches (in addition to individual
model changes) manually, you can use the `cache.region_prefix` setting. However,
it has the same problem as cache region names for each model.

- This project is similar to how 
[Shopify's IdentityCache](https://github.com/Shopify/identity_cache) does cache
key generation for Ruby on Rails.

## Changelog

**2.1.0**
- Add `WithFormatRegionName` to customize the formatting of the new region name.

**2.0.0**
- Remove `WithLogging` in favor of `OnChange` to include the old/new region
  names.

**1.1.1**
- Change default hash input functions to use type names instead of serializing
  the whole type. 

**1.1.0**
- Add logging: `buster.WithLogging(Logger)`
- Add customization for `RootClass` hash input: `buster.WithRootClassHashInput(GetRootClassHashInput)`
- Add customization for `Collection` hash input: `buster.WithCollectionHashInput(GetCollectionHashInput)`
- Better hashing of `RootClass` that only looks at all properties (including
  collections and reference properties).
- .NET 4.0 and 4.5 packages.

**1.0.0**
- Initial release :sparkles:.