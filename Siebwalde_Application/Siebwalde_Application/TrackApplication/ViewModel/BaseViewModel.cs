using System.ComponentModel;

namespace Siebwalde_Application
{
    /// <summary>
    /// A base view model that fires proprty changed events as needed
    /// </summary>
    public class BaseViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The event that is fired when any child property changes it value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (Sender, e) => { };
    }
}
