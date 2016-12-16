using Android.App;
using Android.OS;
using Android.Webkit;
using Android.Widget;

namespace ScratchJr.Android
{
    /// <summary>
    /// Main activity for Scratch Jr., consisting of a full-screen landscape WebView.
    /// This activity creates an embeded WebView, which runs the HTML5 app containing the majority of the source code.
    /// </summary>
    [Activity(Label = "ScratchJr_Xamarin.Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity : global::Android.App.Activity
    {
        /// <summary>
        /// Milliseconds to pan when showing the soft keyboard
        /// </summary>
        private const int SoftKeyBoardPanMs = 250;

        /// <summary>
        /// Log tag for Scratch Jr. app
        /// </summary>
        private const string LogTag = nameof(Activity);

        /// <summary>
        /// Bundle key in which the current url is stored
        /// </summary>
        private const string BundleKeyUrl = "url";

        /// <summary>
        /// The url of the index page
        /// </summary>
        private const string IndexPageUrl = "file:///android_asset/HTML5/index.html";

        /// <summary>
        /// COntainer containing the web view
        /// </summary>
        private RelativeLayout _container;

        /// <summary>
        /// Web browser containing the Scratch Jr. HTML5 webapp
        /// </summary>
        private WebView _webView;

        /// <summary>
        /// Maintains connection to database
        /// </summary>
        private DatabaseManager _databaseManager;

        /// <summary>
        /// Performs file IO
        /// </summary>
        private IOManager _ioManager;

        /// <summary>
        /// Manages sounds
        /// </summary>
        private SoundManager _soundManager;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activity_scratch_jr);

            _soundManager = new SoundManager(this);

        }
    }
}

