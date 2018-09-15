using Acr.UserDialogs;
using CodeBrix.Prism.Abstract;
using Prism.Navigation;

namespace Rd1212.app.ViewModels
{
    public abstract class BaseViewModel : ViewModelBase
    {
        #region Bindable properties

        private string _pageTitle;
        public virtual string PageTitle
        {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }

        #endregion

        #region Commands and their implementations


        #endregion

        protected BaseViewModel(
            INavigationService navigationService,
            IUserDialogs dialogService)
            : base(navigationService, null, dialogService)
        { }
    }
}
