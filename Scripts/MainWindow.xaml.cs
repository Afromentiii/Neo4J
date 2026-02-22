using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Neo4J
{
    public partial class MainWindow : Window
    {
        private User currentUser;
        private List<object> movies;
        public MainWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            initUserXAML();
            initMovies();
        }
        private async void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button likeButton) return;

            var button = sender as Button;
            dynamic movie = likeButton.DataContext;
            string movieTitle = movie.Title;
            bool liked = await Neo4jDriver.Instance.CreateLikeMovieRelation(currentUser.Username, movieTitle);

            likeButton.Background = (Brush)FindResource("LikeGreen");

        }

        private async void DisLikeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button disLikeButton) return;

            var parentGrid = VisualTreeHelper.GetParent(disLikeButton) as Grid;

            var likeButton = parentGrid?.FindName("LikeButton") as Button;

            dynamic movie = disLikeButton.DataContext;
            string movieTitle = movie.Title;

            bool removed = await Neo4jDriver.Instance.RemoveLikeMovieRelation(currentUser.Username, movieTitle);

            if (removed)
            {
                if (likeButton != null)
                {
                    likeButton.Background = Brushes.Gray;
                }
            }
        }
        public async void initUserXAML()
        {
            UserText.Text = "User: " + currentUser.Username;
        }

        public async void initMovies()
        {
            var likedTitles = await Neo4jDriver.Instance.GetLikedMovieTitles(currentUser.Username);
            movies = await Neo4jDriver.Instance.GetMovies();
            MoviesList.ItemsSource = movies;

            MoviesList.UpdateLayout();

            await Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var item in MoviesList.Items)
                {
                    var container = MoviesList.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                    if (container == null) continue;

                    var likeButton = FindChild<Button>(container, "LikeButton");

                    if (likeButton != null)
                    {
                        var titleProp = item.GetType().GetProperty("Title");
                        string title = titleProp?.GetValue(item)?.ToString();

                        if (title != null && likedTitles.Contains(title))
                        {
                            likeButton.Background = (Brush)FindResource("LikeGreen");
                        }
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild && (child as FrameworkElement).Name == childName) return tChild;

                var result = FindChild<T>(child, childName);
                if (result != null) return result;
            }
            return null;
        }
    }


}