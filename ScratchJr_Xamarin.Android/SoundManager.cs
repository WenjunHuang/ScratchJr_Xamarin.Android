using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;
using Android.Content.Res;
using Android.Media;
using Android.Util;
using Java.IO;
using Stream = Android.Media.Stream;

namespace ScratchJr.Android
{
    /// <summary>
    /// Manages sound playing for ScratchJr.
    /// </summary>
    /// <author>Wenjun Huang</author>
    public class SoundManager
    {
        private const string LogTag = nameof(SoundManager);
        private readonly ScratchJrActivity _application;
        private const string Html5BasePath = "HTML5";

        /// <summary>
        /// Pool of pre-loaded sound effects
        /// </summary>
        private SoundPool _soundEffectPool;

        /// <summary>
        /// Maps filename to sound id in the sound effect pool
        /// </summary>
        private readonly Dictionary<string, int> _soundEffectMap = new Dictionary<string, int>();

        /// <summary>
        /// Active sounds playing currently
        /// </summary>
        private readonly ConcurrentDictionary<int, MediaPlayer> _activeSoundMap = new ConcurrentDictionary<int, MediaPlayer>();

        /// <summary>
        /// Running count of active sounds, so each one has a unique id
        /// </summary>
        private int _activeSoundCount = 0;

        /// <summary>
        /// Set of assets in the HTML5 directory
        /// </summary>
        private readonly HashSet<string> _html5AssetList;

        public SoundManager(ScratchJrActivity application)
        {
            _application = application;
            _html5AssetList = new HashSet<string>(ListHTML5Assets(application));
            LoadSoundEffectsAsync();
        }

        public void PlaySoundEffect(string name)
        {
            PlaySoundEffectWithVolume(name, 1.0f);
        }

        public void PlaySoundEffectWithVolume(string name, float volume)
        {
            if (_soundEffectPool == null)
            {
                Log.Error(LogTag, $"Sound effect pool is closed. Cannot play '{name}' right now");
            }
            else
            {
                int soundId;
                if (_soundEffectMap.TryGetValue(name, out soundId))
                {
                    _soundEffectPool.Play(soundId, volume, volume, 0, 0, 1.0f);
                }
                else
                {
                    Log.Error(LogTag, $"Can not find sound effect '{name}'");
                }
            }
        }

        /// <summary>
        /// Play the given sound and return an id that can be used to stop the sound later.
        /// </summary>
        /// <param name="file">Path to sound to play. If relative, sound will come from assets HTML5/ directory else
        /// comes from an absolute path.
        /// </param>
        /// <returns>An id which can be used to stop the sound later.</returns>
        public async Task<int> PlaySoundAsync(string file)
        {
            if (file.Equals("pop.mp3"))
            {
                // We special-case pop.mp3 because it is easier to get it to play as a sound effect than a sound resource
                PlaySoundEffect(file);
                return -1;
            }

            int result = Interlocked.Increment(ref _activeSoundCount);
            var player = new MediaPlayer();
            _activeSoundMap.TryAdd(result, player);

            FileDescriptor fd = null;
            long startOffset = 0;
            long length;
            Action closeTask = null;

            if (file.StartsWith("/"))
            {
                // absolute path
                var fis = new FileInputStream(file);
                fd = fis.FD;
                length = new FileInfo(file).Length;
                closeTask = () => { fis.Close(); };
            }
            else if (_html5AssetList.Contains(file))
            {
                var path = Path.Combine(Html5BasePath, file);
                var afd = _application.Assets.OpenFd(path);
                fd = afd.FileDescriptor;
                startOffset = afd.StartOffset;
                length = afd.Length;
                closeTask = () =>
                {
                    afd.Close();
                };
            }
            else
            {
                var soundFile = Path.Combine(_application.FilesDir.Path, file);
                var input = new FileInputStream(soundFile);
                fd = input.FD;
                startOffset = 0;
                length = new FileInfo(soundFile).Length;
                closeTask = () =>
                {
                    input.Close();
                };
            }

            player.SetDataSource(fd, startOffset, length);
            player.Completion += (sender, args) =>
            {
                var mp = sender as MediaPlayer;
                mp?.Release();
                _activeSoundMap.TryRemove(result, out mp);
                closeTask();
            };

            var tc = new TaskCompletionSource<int>();
            player.Prepared += (sender, args) =>
            {
                var mp = sender as MediaPlayer;
                mp?.Start();
                tc.SetResult(result);
            };
            player.PrepareAsync();

            return await tc.Task;
        }

        /// <summary>
        /// Returns true if the sound for the given id is playing
        /// </summary>
        /// <param name="soundId"></param>
        /// <returns></returns>
        public bool IsPlaying(int soundId)
        {
            bool result = false;
            MediaPlayer player;
            if (_activeSoundMap.TryGetValue(soundId, out player))
            {
                result = player.IsPlaying;
            }
            return result;
        }


        public int SoundDuration(int soundId)
        {
            int result = 0;
            MediaPlayer player;
            if (_activeSoundMap.TryGetValue(soundId, out player))
            {
                result = player.Duration;
            }
            return result;
        }

        public void StopSound(int soundId)
        {
            MediaPlayer player;
            if (_activeSoundMap.TryGetValue(soundId, out player))
            {
                player.Stop();
            }
        }

        /// <summary>
        /// Load all sound effects
        /// </summary>
        public void Open()
        {
            LoadSoundEffectsAsync();
        }

        /// <summary>
        /// Release all resources
        /// </summary>
        public void Close()
        {
            ReleaseSoundEffects();
        }

        private void ReleaseSoundEffects()
        {
            if (_soundEffectPool != null)
            {
                _soundEffectPool.Release();
                _soundEffectPool = null;
                _soundEffectMap.Clear();
            }
            _activeSoundMap.Clear();
        }

        private void LoadSoundEffectsAsync()
        {
            if (_soundEffectPool == null)
            {
                _soundEffectPool = new SoundPool(11, Stream.Music, 0);

                // Load all sound effects into memory
                var assetManager = _application.Assets;
                var soundEffects = assetManager.List(Path.Combine(Html5BasePath,"sounds"));
                LoadSoundEffects(assetManager, Path.Combine(Html5BasePath, "sounds"), soundEffects);
                LoadSoundEffects(assetManager, Html5BasePath, "pop.mp3");
            }
        }

        private void LoadSoundEffects(AssetManager assetManager, string basePath, params string[] soundEffects)
        {
            foreach (var fileName in soundEffects)
            {
                var fd = assetManager.OpenFd(Path.Combine(basePath, fileName));
                var soundId = _soundEffectPool.Load(fd, 1);
                _soundEffectMap[fileName] = soundId;
            }
        }

        private List<string> ListHTML5Assets(ScratchJrActivity application)
        {
            var result = new List<string>();
            result.AddRange(application.Assets.List(Html5BasePath));
            foreach (var path in application.Assets.List(Path.Combine(Html5BasePath, "samples")))
            {
                result.Add(Path.Combine("samples", path));
            }
            return result;
        }
    }
}