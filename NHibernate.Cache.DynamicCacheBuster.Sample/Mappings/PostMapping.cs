using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using NHibernate.Cache.DynamicCacheBuster.Sample.Models;

namespace NHibernate.Cache.DynamicCacheBuster.Sample.Mappings
{
    public class PostMapping : ClassMap<Post>
    {
        public PostMapping()
        {
            Table("Posts");
            Id(x => x.Id)
                .Column("Id");
            Map(x => x.Name)
                .Column("Name");
            Map(x => x.Body)
                .Column("Body");
            References(x => x.Blog)
                .Column("BlogId");

            Cache.ReadWrite();
        }
    }
}
