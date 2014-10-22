using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate.Cache.DynamicCacheBuster.Sample.Models
{
    public class Blog
    {
        public virtual int Id { get; protected set; }
        public virtual string Name { get; set; }

        private readonly ICollection<Post> posts = new List<Post>();
        public virtual ICollection<Post> Posts { get { return posts; } }

        // For NHibernate.
        protected Blog() { }

        public Blog(string name)
        {
            this.Name = name;
        }

        public virtual Post AddPost(string name, string body)
        {
            var post = new Post(this, name, body);
            posts.Add(post);
            return post;
        }
    }
}
