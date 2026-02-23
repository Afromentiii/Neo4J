using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4J
{
    public class Movie
    {
        public string Title { get; set; }
        public string Genre { get; set; }
        public int Released { get; set; }
        public string PosterUrl { get; set; }
        public string DisplayInfo { get; set; }  
        public string RecommendationInfo  { get; set; }
}
}
