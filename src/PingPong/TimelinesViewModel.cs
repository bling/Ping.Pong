using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Autofac.Features.OwnedInstances;
using Caliburn.Micro;
using PingPong.Timelines;

namespace PingPong
{
    public class TimelinesViewModel : Screen
    {
        private readonly TwitterClient _client;
        private readonly TimelineFactory _timelineFactory;
        private readonly IWindowManager _windowManager;
        private readonly IDisposable _refreshSubscription;
        private IDisposable _streamingSubscription;
        private string _searchText;
        private bool _showUpdateStatus;

        public ObservableCollection<object> Timelines { get; private set; }

        public bool ShowUpdateStatus
        {
            get { return _showUpdateStatus; }
            set { this.SetValue("ShowUpdateStatus", value, ref _showUpdateStatus); }
        }

        public string SearchText
        {
            get { return _searchText; }
            set { this.SetValue("SearchText", value, ref _searchText); }
        }

        public TimelinesViewModel(TwitterClient client, TimelineFactory timelineFactory, IWindowManager windowManager)
        {
            _client = client;
            _timelineFactory = timelineFactory;
            _windowManager = windowManager;

            Timelines = new ObservableCollection<object>();
            Timelines.Add(timelineFactory.StatusFactory(StatusType.Home));
            Timelines.Add(timelineFactory.StatusFactory(StatusType.Mentions));

            _refreshSubscription = Observable.Interval(TimeSpan.FromSeconds(30))
                .DispatcherSubscribe(_ =>
                {
                    foreach (var tweet in Timelines.Cast<dynamic>().Select(x => (Timeline)x.Value).SelectMany(x => x))
                        tweet.NotifyOfPropertyChange("CreatedAt");
                });
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            Stop();
            _refreshSubscription.Dispose();
        }

        public void OnStatusTextBoxTextInput(TextCompositionEventArgs e)
        {
            if (e.Text[0] == 27) // esc
                ShowUpdateStatus = false;
        }

        public void OnStatusTextBoxChanged(TextBox sender, TextChangedEventArgs e)
        {
            string text = sender.Text;
            if (text.Contains("\r"))
            {
                ShowUpdateStatus = false;
                _client.UpdateStatus(text);
            }
        }

        public void Search()
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                _windowManager.ShowDialog(new ErrorViewModel("Search terms are required."));
            }
            else
            {
                var old = Timelines.OfType<Owned<StreamingTimeline>>().ToArray();
                old.ForEach(t => t.Dispose());
                old.ForEach(t => Timelines.Remove(t));

                var allTerms = SearchText.Split(' ', ',', ';', '|');
                var allParts = SearchText.Split(' ', ',', ';');
                var ob = _client.GetStreamingFilter(allTerms).Publish();

                for (int i = 0; i < allParts.Length; i++)
                {
                    string[] terms = allParts[i].Split('|');
                    var streamline = _timelineFactory.StreamingFactory(ob);
                    streamline.Value.FilterTerms = terms;
                    Timelines.Add(streamline);
                }

                _streamingSubscription.DisposeIfNotNull();
                _streamingSubscription = ob.Connect();
            }
        }

        public void Stop()
        {
            _streamingSubscription.DisposeIfNotNull();
        }
    }
}