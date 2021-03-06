using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WebApi.Hal.Interfaces;

namespace WebApi.Hal
{
    public class Link<T> : Link where T : class, IResource
    {
        // simply for typing purposes (avoid magic strings) ...

        public Link()
        {
        }

        public Link(string rel, string href, CuriesLink curie) : base(rel, href, curie)
        {
        }

        public Link(string rel, string href, string title = null) : base(rel, href, title)
        {
        }
    }

    public class Link
    {
        public const string RelForSelf = "self";
        public const string RelForCuries = "curies";

        static readonly IEqualityComparer<Link> ComparerInstance = new LinkEqualityComparer();

        string linkRelation;
        readonly CuriesLink curie;

        public Link()
        { }
        
        public Link(string rel, string href, CuriesLink curie)
        {
            if (string.IsNullOrEmpty(rel))
                throw new ArgumentNullException("rel");

            if (string.IsNullOrEmpty(href))
                throw new ArgumentNullException("href");

            if (curie == null)
                throw new ArgumentNullException("curie");

            Rel = rel;
            Href = href;
            this.curie = curie;
        }

        public Link(string rel, string href, string title = null)
        {
            Rel = rel;
            Href = href;
            Title = title;
        }

        public CuriesLink Curie
        {
            get { return curie; }
        }

        public string Rel
        {
            get { return linkRelation; }
            set
            {
                // should be case insensitive when comparing, so default to lower-case (http://tools.ietf.org/html/rfc5988#section-4.1)
                linkRelation = string.IsNullOrEmpty(value) ? value : value.ToLowerInvariant();
            }
        }

        public string Href { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string Deprecation { get; set; }
        public string Name { get; set; }
        public string Profile { get; set; }
        public string HrefLang { get; set; }
        
        public bool IsTemplated
        {
            get { return !string.IsNullOrEmpty(Href) && IsTemplatedRegex.IsMatch(Href); }
        }

        private static readonly Regex IsTemplatedRegex = new Regex(@"{.+}", RegexOptions.Compiled);

        /// <summary>
        /// If this link is templated, you can use this method to make a non templated copy
        /// </summary>
        /// <param name="newRel">A different rel</param>
        /// <param name="parameters">The parameters, i.e 'new {id = "1"}'</param>
        /// <returns>A non templated link</returns>
        public Link CreateLink(string newRel, params object[] parameters)
        {
            var clone = Clone();

            clone.Rel = newRel;
            clone.Href = CreateUri(parameters).ToString();

            return clone;
        }
        
        /// <summary>
        /// If this link is templated, you can use this method to make a non templated copy
        /// </summary>
        /// <param name="parameters">The parameters, i.e 'new {id = "1"}'</param>
        /// <returns>A non templated link</returns>
        public Link CreateLink(params object[] parameters)
        {
            return CreateLink(Rel, parameters);
        }

        public Uri CreateUri(params object[] parameters)
        {
            var href = Href;
            href = SubstituteParams(href, parameters);

            return new Uri(href, UriKind.RelativeOrAbsolute);
        }

        public static string SubstituteParams(string href, params object[] parameters)
        {
            var uriTemplate = new UriTemplate(href);
            foreach (var parameter in parameters)
            {
                foreach (var substitution in parameter.GetType().GetProperties())
                {
                    var name = substitution.Name;
                    var value = substitution.GetValue(parameter, null);
                    uriTemplate.SetParameter(name, value);
                }
            }

            return uriTemplate.Resolve();
        }

        /// <summary>
        /// Performs a shallow clone of the instance
        /// </summary>
        /// <returns>Cloned instance</returns>
        public Link Clone()
        {
            return (Link)MemberwiseClone();
        }

        sealed class LinkEqualityComparer : IEqualityComparer<Link>
        {
            public bool Equals(Link l1, Link l2)
            {
                return string.Compare(l1.Href, l2.Href, StringComparison.OrdinalIgnoreCase) == 0 &&
                       string.Compare(l1.Rel, l2.Rel, StringComparison.OrdinalIgnoreCase) == 0;
            }


            public int GetHashCode(Link lnk)
            {
                var str = (string.IsNullOrEmpty(lnk.Rel) ? "norel" : lnk.Rel) + "~" + (string.IsNullOrEmpty(lnk.Href) ? "nohref" : lnk.Href);
                var h = str.GetHashCode();
                return h;
            }
        }

        public static IEqualityComparer<Link> EqualityComparer
        {
            get { return ComparerInstance; }
        }
    }

    
}