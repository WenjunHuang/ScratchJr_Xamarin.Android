using System.Linq;
using Android.App;
using Android.Widget;
using Android.OS;
using ScratchJr.Android;
using Activity = Android.App.Activity;

namespace ScratchJr.Test
{
    [Activity(Label = "ScratchJr.Test", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private SoundManager _soundManager;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            _soundManager = new SoundManager(this);

            SetContentView(Resource.Layout.Main);

            var adapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleSpinnerItem);
            adapter.SetDropDownViewResource(global::Android.Resource.Layout.SimpleSpinnerDropDownItem);
            adapter.AddAll(_soundManager.GetSoundEffectNames().ToList());

            var sounds = FindViewById<Spinner>(Resource.Id.sounds);
            sounds.Adapter = adapter;

            var btn = FindViewById<Button>(Resource.Id.playsound);
            btn.Click += (sender, args) =>
            {
                var selectSound = adapter.GetItem(sounds.SelectedItemPosition);
                _soundManager.PlaySoundEffect(selectSound);
            };
        }
    }
}