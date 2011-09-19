using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Autofac.Features.OwnedInstances;
using Caliburn.Micro;
using PingPong.Messages;
using PingPong.Models;
using PingPong.Timelines;

namespace PingPong
{
    public class TimelinesViewModel : Screen, IHandle<ReplyMessage>, IHandle<RetweetMessage>, IHandle<QuoteMessage>
    {
        private static readonly TimeSpan StreamThrottleRate = TimeSpan.FromSeconds(20);

        private readonly TwitterClient _client;
        private readonly TimelineFactory _timelineFactory;
        private readonly IWindowManager _windowManager;
        private readonly IDisposable _refreshSubscription;
        private IDisposable _streamingSubscription;
        private string _searchText;
        private string _statusText;
        private bool _showUpdateStatus;
        private DateTime _streamStartTime = DateTime.MinValue;

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

        public string StatusText
        {
            get { return _statusText; }
            set { this.SetValue("StatusText", value, ref _statusText); }
        }

        public TimelinesViewModel(TwitterClient client, TimelineFactory timelineFactory, IWindowManager windowManager)
        {
            _client = client;
            _timelineFactory = timelineFactory;
            _windowManager = windowManager;

            Timelines = new ObservableCollection<object>();
            Add(timelineFactory.StatusFactory(StatusType.Home));
            Add(timelineFactory.StatusFactory(StatusType.Mentions));

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
            if (text.Contains("\r") && text.Length <= Tweet.MaxLength)
            {
                ShowUpdateStatus = false;
                _client.UpdateStatus(text);
            }
        }

        public void Search()
        {
            if (DateTime.UtcNow - _streamStartTime < StreamThrottleRate)
            {
                _windowManager.ShowDialog(new ErrorViewModel("You are initiating too many connections in a short period of time.  Twitter doesn't like that :("));
            }
            else if (string.IsNullOrEmpty(SearchText))
            {
                _windowManager.ShowDialog(new ErrorViewModel("Search terms are required."));
            }
            else
            {
                _streamStartTime = DateTime.UtcNow;

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
                    Add(streamline);
                }

                _streamingSubscription.DisposeIfNotNull();
                _streamingSubscription = ob.Connect();
            }
        }

        public void Stop()
        {
            _streamingSubscription.DisposeIfNotNull();
        }

        private void Add<T>(Owned<T> owned)
        {
            var line = (Timeline)((dynamic)owned).Value;
            line.OnError += ex => _windowManager.ShowDialog(new ErrorViewModel(ex.ToString()));
            Timelines.Add(owned);
        }

        void IHandle<ReplyMessage>.Handle(ReplyMessage message)
        {
            StatusText = '@' + message.Tweet.ScreenName;
            ShowUpdateStatus = true;
        }

        void IHandle<RetweetMessage>.Handle(RetweetMessage message)
        {
            StatusText = string.Format("RT @{0} {1}", message.Tweet.ScreenName, message.Tweet.Text);
            ShowUpdateStatus = true;
        }

        void IHandle<QuoteMessage>.Handle(QuoteMessage message)
        {
            StatusText = string.Format("RT @{0} {1}", message.Tweet.ScreenName, message.Tweet.Text);
            ShowUpdateStatus = true;
        }
    }
}