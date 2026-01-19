using System.ComponentModel;

namespace PlayCutWin.Models
{
    public enum TagGroup
    {
        Offense,
        Defense
    }

    public class TagItem : INotifyPropertyChanged
    {
        public string Name { get; }
        public TagGroup Group { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public TagItem(string name, TagGroup group)
        {
            Name = name;
            Group = group;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
