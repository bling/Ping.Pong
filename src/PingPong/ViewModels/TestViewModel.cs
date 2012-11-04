﻿using System;
using System.Diagnostics;
using Caliburn.Micro;
using PingPong.Core;

namespace PingPong.ViewModels
{
    public class TestViewModel : Screen
    {
        private readonly TwitterClient _client;

        public TestViewModel()
        {
            _client = new TwitterClient();
        }

        public void Homeline()
        {
            _client.GetHomeTimeline()
                   .Subscribe(t => Debug.WriteLine(t));
        }
    }
}