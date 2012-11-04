using System;
using PingPong.ViewModels;

namespace PingPong
{
    public class ViewModelFactory
    {
        public Func<AuthorizationViewModel> AuthorizationFactory { get; set; }
        public Func<InstallViewModel> InstallFactory { get; set; }
        public Func<TimelinesViewModel> TimelinesFactory { get; set; }
        public Func<TestViewModel> TestFactory { get; set; }
    }
}