using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Bloxstrap.Models
{
    public class ServerEntry : INotifyPropertyChanged
    {
        private int _number;
        private string _serverId = string.Empty;
        private string _players = string.Empty;
        private string _region = string.Empty;
        private int? _dataCenterId;
        private string _uptime = "Loading...";
        private ICommand? _joinCommand;

        public int Number
        {
            get => _number;
            set { _number = value; OnPropertyChanged(); }
        }

        public string ServerId
        {
            get => _serverId;
            set { _serverId = value; OnPropertyChanged(); }
        }

        public string Players
        {
            get => _players;
            set { _players = value; OnPropertyChanged(); }
        }

        public string Region
        {
            get => _region;
            set { _region = value; OnPropertyChanged(); }
        }

        public int? DataCenterId
        {
            get => _dataCenterId;
            set { _dataCenterId = value; OnPropertyChanged(); }
        }

        public string Uptime
        {
            get => _uptime;
            set { _uptime = value; OnPropertyChanged(); }
        }

        public ICommand? JoinCommand
        {
            get => _joinCommand;
            set { _joinCommand = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}