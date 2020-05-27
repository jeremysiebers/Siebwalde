using SiebwaldeApp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SiebwaldeApp
{
    /// <summary
    /// 
    /// <summary
    public class SiebwaldeInitPageViewModel : BaseViewModel
    {
        #region Private property



        #endregion

        #region Public property

        public ObservableCollection<string> Log2 { get; set; }

        //public ObservableCollection<StringObject> LogObj { get; private set; }

        //public List<string> Log { get; set; }

        #endregion

        #region Public Commands

        /// <summary>
        /// The command to init all controllers at once
        /// </summary>
        public ICommand InitAllControllers { get; set; }
        
        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// <summary>
        public SiebwaldeInitPageViewModel()
        {
            //Log = new ObservableCollection<LogList>();
            //Log.Add(IoC.siebwaldeApplicationModel.Logging);
            //Log = new List<string>();

            Log2 = new ObservableCollection<string>();

            //LogObj = new ObservableCollection<StringObject> { };

            //IoC.siebwaldeApplicationModel.//SiebwaldeApplicationMainLogging.PropertyChanged += //SiebwaldeApplicationMainLogging_PropertyChanged;

            InitAllControllers = new RelayCommand(() => IoC.siebwaldeApplicationModel.StartFYController());           
        }

        private void SiebwaldeApplicationMainLogging_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //Log.Add(sender.GetType().GetProperty(e.PropertyName).GetValue(sender).ToString());

            Log2.Add(sender.GetType().GetProperty(e.PropertyName).GetValue(sender).ToString());

            //LogObj.Add(new StringObject { Value = sender.GetType().GetProperty(e.PropertyName).GetValue(sender).ToString() });
        }

        #endregion
    }

    public class StringObject : BaseViewModel
    {
        public string Value { get; set; }
    }

}
