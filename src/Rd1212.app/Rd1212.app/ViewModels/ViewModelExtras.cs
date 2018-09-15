namespace Rd1212.app.ViewModels
{
    //  This special class will allow us to have IntelliSense while we are editing our XAML view files in Visual
    //  Studio with ReSharper.  I.e. it is for design-time only, and does nothing at compile-time or run-time.
    //  For more info, check these pages -
    //  https://github.com/PrismLibrary/Prism/issues/986
    //  https://gist.github.com/nuitsjp/7478bfc7eba0f2a25b866fa2e7e9221d
    //  https://blog.nuits.jp/enable-intellisense-for-viewmodel-members-with-prism-for-xamarin-forms-2f274e7c6fb6

    public static class DesignTimeViewModelLocator
    {
        public static MainPageViewModel MainPage => null;
    }

    //Handy place to keep the "magic strings" used as keys for NavigationParameters
    public static class NavParamKeys
    {

    }
}
