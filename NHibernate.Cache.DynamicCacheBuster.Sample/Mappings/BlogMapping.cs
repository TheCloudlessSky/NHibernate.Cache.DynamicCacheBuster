using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using NHibernate.Cache.DynamicCacheBuster.Sample.Models;

namespace NHibernate.Cache.DynamicCacheBuster.Sample.Mappings
{
    public class BlogMapping : ClassMap<Blog>
    {
        public BlogMapping()
        {
            Table("Blogs");
            Id(x => x.Id)
                .Column("Id");
            Map(x => x.Name)
                .Column("Name");
            HasMany(x => x.Posts)
                .Access.CamelCaseField().KeyColumn("BlogId")
                .Cascade.All()
                .Cache.ReadWrite();

            Cache.ReadWrite();
        }
    }
}
