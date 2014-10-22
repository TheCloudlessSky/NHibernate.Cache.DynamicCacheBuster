using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate.Cache.DynamicCacheBuster.Sample.Models
{
    public class Post
    {
        public virtual int Id { get; protected set; }
        public virtual Blog Blog { get; protected set; }
        public virtual string Name { get; set; }
        public virtual string Body { get; set; }

        // For NHibernate.
        protected Post() { }

        internal Post(Blog blog, string name, string body)
        {
            this.Blog = blog;
            this.Name = name;
            this.Body = body;
        }
    }
}
