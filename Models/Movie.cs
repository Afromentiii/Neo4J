using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Neo4J.Models
{
    public class Movie : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public string Genre { get; set; }
        public int Released { get; set; }
        public string PosterUrl { get; set; }
        public string DisplayInfo { get; set; }  
        public string RecommendationInfo  { get; set; }
        
        private bool _isLiked;
        public bool IsLiked 
        { 
            get => _isLiked; 
            set { _isLiked = value; OnPropertyChanged(); } 
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
