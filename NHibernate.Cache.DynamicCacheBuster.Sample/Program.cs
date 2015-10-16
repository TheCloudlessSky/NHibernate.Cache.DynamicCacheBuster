using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate.Cache.DynamicCacheBuster.Sample.Mappings;
using NHibernate.Cache.DynamicCacheBuster.Sample.Models;
using NHibernate.Caches.Redis;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using StackExchange.Redis;

namespace NHibernate.Cache.DynamicCacheBuster.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new Program().Run();
        }

        private const string dbName = "blogs.db";

        private void Run()
        {
            SetupRedisCacheProvider();
            var cfg = SetupNHibernateConfiguration();
            
            // The magic!
            new CacheBuster() 
                .OnChange((oldCacheRegionName, newCacheRegionName, hash) =>
                {
                    Console.WriteLine("Old Region = {0}\nNew Region = {1}\nHash = {2}\n---", oldCacheRegionName, newCacheRegionName, hash);
                })
                .AppendVersionToCacheRegionNames(cfg);

            var sessionFactory = SetupDatabaseAndCreateSessionFactory(cfg);

            UseSession(sessionFactory, session =>
            {
                var blog = new Blog("My Awesome Blog");
                blog.AddPost("First Post", "Hello World");
                blog.AddPost("Second Post", "Goodbye World");
                session.Save(blog);
            });

            UseSession(sessionFactory, session =>
            {
                var blog = session.QueryOver<Blog>().Where(b => b.Name == "My Awesome Blog").SingleOrDefault();

                Console.WriteLine("Blog\n----");
                Console.WriteLine("Id={0}, Name={1}, Posts={2}", blog.Id, blog.Name, blog.Posts.Count);

                foreach (var post in blog.Posts)
                {
                    Console.WriteLine("  - Id={0}, Name={1}, Body={2}", post.Id, post.Name, post.Body);
                }
            });

            Console.WriteLine("Done!");
            Console.Read();
        }

        private void SetupRedisCacheProvider()
        {
            var redisConfig = new ConfigurationOptions();
            redisConfig.EndPoints.Add("localhost:6379");
            var mux = ConnectionMultiplexer.Connect(redisConfig);
            RedisCacheProvider.SetConnectionMultiplexer(mux);
        }

        private Configuration SetupNHibernateConfiguration()
        {
            var configuration = Fluently.Configure()
                .Database(
                    SQLiteConfiguration.Standard.InMemory().UsingFile(dbName)
                )
                .Mappings(m =>
                {
                    m.FluentMappings
                        .Add<BlogMapping>()
                        .Add<PostMapping>();
                })
                .Cache(cache =>
                {
                    cache
                        .UseQueryCache()
                        .UseSecondLevelCache()
                        .ProviderClass<RedisCacheProvider>();
                })
                .BuildConfiguration();
            return configuration;
        }

        private ISessionFactory SetupDatabaseAndCreateSessionFactory(Configuration cfg)
        {
            if (File.Exists(dbName))
            {
                File.Delete(dbName);
            }

            new SchemaExport(cfg).Create(script: false, export: true);

            var sessionFactory = cfg.BuildSessionFactory();
            return sessionFactory;
        }

        private void UseSession(ISessionFactory sessionFactory, Action<ISession> action)
        {
            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                action(session);
                transaction.Commit();
            }
        }
    }
}
