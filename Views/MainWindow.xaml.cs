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
using Neo4J.Models;
using Neo4J.Services;
using Neo4J;

namespace Neo4J.Views
{
    public partial class MainWindow : Window
    {
        private User currentUser;
        private List<Movie> movies;
        private List<object> users;
        public MainWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            InitUserXAML();
            InitMoviesRefresh();
        }
        private async void ShowRecommendedMoviesButton_Click(object sender, RoutedEventArgs e) 
        {
            if(sender is not Button) return;

            var recommendedMovies = new List<Movie>();
            recommendedMovies = await Neo4jDriver.Instance.RecommendMoviesForUser(currentUser.Username, 10);

            RecommendedMoviesList.ItemsSource = recommendedMovies;
        }

        private async void ShowRecommendedByGenreButton_Click(object sender, RoutedEventArgs e) 
        {
            if (sender is not Button) return;

            var recommendedMovies = new List<Movie>();
            recommendedMovies = await Neo4jDriver.Instance.RecommendMoviesByGenre(currentUser.Username);

            RecommendedByGenreList.ItemsSource = recommendedMovies;
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e) 
        {
            ((App)Application.Current).ShowWindow(new LoginRegisterWindow());
            ((App)Application.Current).CloseWindow(this);
        }
        private async void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button likeButton) return;

            dynamic movie = likeButton.DataContext;
            string movieTitle = movie.Title;
            bool liked = await Neo4jDriver.Instance.CreateLikeMovieRelation(currentUser.Username, movieTitle);

            if (liked)
            {
                likeButton.Background = (Brush)FindResource("LikeGreen");
            }

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

        private async void FollowButton_Click(object sender, RoutedEventArgs e) 
        {
            if (sender is not Button followButton) return;

            dynamic userField = followButton.DataContext;
            string username = userField.Username;
            try
            {
                bool isTrue = await Neo4jDriver.Instance.CreateObservesRelation(currentUser.Username, username);
                if (isTrue)
                {
                    followButton.Background = (Brush)FindResource("LikeGreen");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas obserwowania użytkownika: {ex.Message}");
            }
        }

        private async void UnFollowButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button unfollowButton) return;

            var parentGrid = VisualTreeHelper.GetParent(unfollowButton) as Grid;
            var followButton = parentGrid?.FindName("FollowButton") as Button;

            dynamic user = unfollowButton.DataContext;
            string usernameToUnfollow = user.Username;

            bool removed = await Neo4jDriver.Instance.RemoveObservesRelation(currentUser.Username, usernameToUnfollow);

            if (removed)
            {
                if (followButton != null)
                    followButton.Background = Brushes.Gray;
            }
        }
        public async void InitUserXAML()
        {
            UserText.Text = "User: " + currentUser.Username;
        }

        public async Task InitMoviesRefresh()
        {
            var likedTitles = await Neo4jDriver.Instance.GetLikedMovieTitles(currentUser.Username);
            movies = await Neo4jDriver.Instance.GetMovies();
            users = await Neo4jDriver.Instance.GetUsers(currentUser.Username);

            MoviesList.ItemsSource = movies;
            UsersList.ItemsSource = users;

            MoviesList.UpdateLayout();
            UsersList.UpdateLayout();

            await HighlightLikedMoviesAsync(likedTitles);
            await InitUsersHighlight();
        }

        private async Task HighlightLikedMoviesAsync(HashSet<string> likedTitles)
        {
            if (MoviesList.Items.Count == 0) return;

            await Dispatcher.InvokeAsync(() =>
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
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        public async Task InitUsersHighlight()
        {
            var followedUsers = await Neo4jDriver.Instance.GetFollowedUsers(currentUser.Username);

            foreach (var item in UsersList.Items)
            {
                var container = UsersList.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                if (container == null) continue;

                var followButton = FindChild<Button>(container, "FollowButton");
                if (followButton != null)
                {
                    var usernameProp = item.GetType().GetProperty("Username");
                    string username = usernameProp?.GetValue(item)?.ToString();

                    if (username != null && followedUsers.Contains(username))
                    {
                        followButton.Background = (Brush)FindResource("LikeGreen");
                    }
                }
            }
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