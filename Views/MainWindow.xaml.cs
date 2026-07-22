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
using System.Threading;

namespace Neo4J.Views
{
    public partial class MainWindow : Window
    {
        private User currentUser;

        public MainWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            InitUserXAML();
            InitMoviesRefresh();
        }
        private async void ShowRecommendedMoviesButton_Click(object sender, RoutedEventArgs e) 
        {
            var recommendedMovies = new List<Movie>();
            recommendedMovies = await Neo4jDriver.Instance.RecommendMoviesForUser(currentUser.Username, 10);

            RecommendedMoviesList.ItemsSource = recommendedMovies;
        }

        private async void ShowRecommendedByGenreButton_Click(object sender, RoutedEventArgs e) 
        {
            var recommendedMovies = new List<Movie>();
            recommendedMovies = await Neo4jDriver.Instance.RecommendMoviesByGenre(currentUser.Username);

            RecommendedByGenreList.ItemsSource = recommendedMovies;
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await InitMoviesRefresh();
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e) 
        {
            ((App)Application.Current).ShowWindow(new LoginRegisterWindow());
            ((App)Application.Current).CloseWindow(this);
        }
        private async void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button likeButton) return;
            if (likeButton.DataContext is not Movie movie) return;

            bool liked = await Neo4jDriver.Instance.CreateLikeMovieRelation(currentUser.Username, movie.Title);
            if (liked) movie.IsLiked = true;
        }

        private async void DisLikeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button disLikeButton) return;
            if (disLikeButton.DataContext is not Movie movie) return;

            bool removed = await Neo4jDriver.Instance.RemoveLikeMovieRelation(currentUser.Username, movie.Title);
            if (removed) movie.IsLiked = false;
        }

        private async void FollowButton_Click(object sender, RoutedEventArgs e) 
        {
            if (sender is not Button followButton) return;
            if (followButton.DataContext is not User targetUser) return;

            try
            {
                bool isTrue = await Neo4jDriver.Instance.CreateObservesRelation(currentUser.Username, targetUser.Username);
                if (isTrue) targetUser.IsFollowed = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas obserwowania użytkownika: {ex.Message}");
            }
        }

        private async void UnFollowButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button unfollowButton) return;
            if (unfollowButton.DataContext is not User targetUser) return;

            bool removed = await Neo4jDriver.Instance.RemoveObservesRelation(currentUser.Username, targetUser.Username);
            if (removed) targetUser.IsFollowed = false;
        }
        public async void InitUserXAML()
        {
            UserText.Text = "User: " + currentUser.Username;
        }

        private List<Movie> allMovies = new List<Movie>();

        private void FilterMovies_Changed()
        {
            if (allMovies == null || MoviesList == null) return;
            string searchText = SearchMovieBox.Text?.ToLower() ?? "";
            string selectedGenre = GenreFilterBox.SelectedItem as string ?? "All Genres";

            var filtered = allMovies.Where(m => 
                (string.IsNullOrWhiteSpace(searchText) || m.Title.ToLower().Contains(searchText)) &&
                (selectedGenre == "All Genres" || m.Genre == selectedGenre)
            ).ToList();

            MoviesList.ItemsSource = filtered;
        }

        private void FilterMovies_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterMovies_Changed();
        }

        private void FilterMovies_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterMovies_Changed();
        }

        private LoadingWindow? _loadingWindow;

        public async Task InitMoviesRefresh()
        {
            MainContentScroll.Visibility = Visibility.Collapsed;

            ManualResetEvent windowCreatedEvent = new ManualResetEvent(false);
            Thread loadingThread = new Thread(() =>
            {
                _loadingWindow = new LoadingWindow();
                _loadingWindow.Show();
                windowCreatedEvent.Set();
                System.Windows.Threading.Dispatcher.Run();
            });
            loadingThread.SetApartmentState(ApartmentState.STA);
            loadingThread.IsBackground = true;
            loadingThread.Start();

            windowCreatedEvent.WaitOne();

            var tLiked = Neo4jDriver.Instance.GetLikedMovieTitles(currentUser.Username);
            var tMovies = Neo4jDriver.Instance.GetMovies();
            var tUsers = Neo4jDriver.Instance.GetUsers(currentUser.Username);


            var likedTitles = await tLiked;
            var movies = await tMovies;
            var users = await tUsers;
            
            allMovies = movies;

            foreach (var movie in movies)
            {
                if (likedTitles.Contains(movie.Title)) movie.IsLiked = true;
            }

            var followedUsers = await Neo4jDriver.Instance.GetFollowedUsers(currentUser.Username);
            foreach (var u in users)
            {
                if (followedUsers.Contains(u.Username)) u.IsFollowed = true;
            }

            var genres = allMovies.Select(m => m.Genre).Distinct().OrderBy(g => g).ToList();
            genres.Insert(0, "All Genres");
            GenreFilterBox.SelectionChanged -= FilterMovies_SelectionChanged;
            SearchMovieBox.TextChanged -= FilterMovies_TextChanged;
            GenreFilterBox.ItemsSource = genres;
            if (GenreFilterBox.SelectedIndex == -1) GenreFilterBox.SelectedIndex = 0;
            GenreFilterBox.SelectionChanged += FilterMovies_SelectionChanged;
            SearchMovieBox.TextChanged += FilterMovies_TextChanged;

            FilterMovies_Changed();
            UsersList.ItemsSource = users;

            ShowRecommendedMoviesButton_Click(this, new RoutedEventArgs());
            ShowRecommendedByGenreButton_Click(this, new RoutedEventArgs());

            MainContentScroll.Visibility = Visibility.Visible;

            await Application.Current.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            await Task.Delay(100);

            if (_loadingWindow != null)
            {
                _loadingWindow.Dispatcher.Invoke(() =>
                {
                    _loadingWindow.Close();
                });
                _loadingWindow.Dispatcher.InvokeShutdown();
                _loadingWindow = null;
            }
        }

        private void InnerScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                return;
            }

            MainContentScroll.ScrollToVerticalOffset(MainContentScroll.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}